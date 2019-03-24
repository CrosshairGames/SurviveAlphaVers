using UnityEngine;
using System.Collections;

public class BuildController : MonoBehaviour
{
	#region Public Configuration Variables
	public bool build = false;
//	public CameraController cc;
	public BuildableObject[] prefabs;
	public int buildableObjectLayer;
	public float colliderDetectionBufferFactor = 1.15f; //How big the buffer between objects is (how much bigger the trigger object is than the regular)
	#endregion

	#region Placement Variables
	[Tooltip("Specify the layers on which you can build")]
	public LayerMask objectsCanBeBuiltOnLayers;
	[Tooltip("Material to indicate Valid Placement")]
	public Material validPlacement;
	[Tooltip("Material to indicate Invalid Placement")]
	public Material invalidPlacement;
	[Tooltip("Material to indicate Selected Object")]
	public Material selectedPlacement;
	#endregion

	#region UI Variables
	[Tooltip("The UI Container (scroll rect content) that the prefabs will be displayed in")]
	public GameObject buildableObjectUiContainer;
	[Tooltip("The UI Prefab to instantiate")]
	public GameObject buildableObjectUiPrefab;

	private Animator buildableUiAnimator;
	private bool isUiOn = true;
	#endregion

	#region Variables for 'Snappy Alignment'
	[Tooltip("Enables a grid based snappy movement. Toggle with F5 at runtime. Switch off for more fine tuned movements")]
	public bool snappyMovement = false;
	[Tooltip("Offset for the snappy movement grid")]
	public float offset = 1.0f;
	[Tooltip("Grid size in unity units")]
	public float gridSize = 1.0f;
	#endregion

	#region Private Variables
	GameObject buildObject;
	RaycastHit hitInfo;
	float currentBuildYOffset = 0f;
	BuildableObject buildObjectComponent;
	BuildableObject lastSelectedBuildableObject;
	#endregion


	/// <summary>
	/// Begins the building of the passed in object, identified by its index in the prefabs array.
	/// </summary>
	/// <param name="index">Index.</param>
	public void BeginBuilding (int index) {
		if (prefabs.Length > index) {
			build = true;
			buildObject = GameObject.Instantiate (prefabs [index].gameObject) as GameObject;
			buildObjectComponent = buildObject.GetComponent<BuildableObject> ();
			ChangeLayer (buildObject, 2);
			if (lastSelectedBuildableObject)
				lastSelectedBuildableObject.ShowIndicatorForSelection (false);
		} else {
			Debug.LogError ("You're trying to use a build shortcut that doesn't have a prefab associated with it. Please assign more prefabs.");
		}
	}

	/// <summary>
	/// Uses the prefabName passed in to find the correct prefab and enter build mode.
	/// </summary>
	/// <returns>The and build by prefab name.</returns>
	/// <param name="name">Name.</param>
	public void FindAndBuildByPrefabName(string name) {
		for(int i = 0; i < prefabs.Length; i++) {
			GameObject go = prefabs[i].gameObject;
			if (go.name == name ) {
				BeginBuilding (i);
			}
		}
	}

	/// <summary>
	/// Stops the placing object. destroys the object and resets the appropriate variables
	/// </summary>
	/// <param name="obj">Object.</param>
	public void StopPlacingObject(GameObject obj) {
		//Cancel the build
		Destroy(obj);
		buildObjectComponent = null;
		build = false;
	}

	#region Utility Functions
	public void ChangeLayer(GameObject go, int layer)
	{
		go.layer = layer;
			int count = go.transform.childCount;
			for (int i = 0; i < count; i++) {
				go.transform.GetChild(i).gameObject.layer = layer;
			}
	}

	#endregion

	/// <summary>
	/// Checks for select collision, mouse hovering over a buildable object
	/// </summary>
	void CheckForSelectCollision() {
		int bitShiftedLayerMask = 1 << buildableObjectLayer;		//Need to bit shift the layermask to make it work (as its provided as a unity number)
		Ray ray = Camera.main.ScreenPointToRay (Input.mousePosition);
		RaycastHit hit;
		if (Physics.Raycast (ray, out hit, 100, bitShiftedLayerMask)) {
			if (hit.collider != null) {
				//Hit a BuildableObject. Select it.
				BuildableObject selectedObject = hit.collider.gameObject.GetComponent<BuildableObject> ();
				if (selectedObject != null) {
					selectedObject.ShowIndicatorForSelection (true);

					if (lastSelectedBuildableObject != selectedObject && lastSelectedBuildableObject != null)
						lastSelectedBuildableObject.ShowIndicatorForSelection (false);
					lastSelectedBuildableObject = selectedObject;
				}
			}
		} else {
			//Click anywhere else, clear the selectedObject if there is one
			if (lastSelectedBuildableObject != null)
				lastSelectedBuildableObject.ShowIndicatorForSelection (false);
		}
	}

	/// <summary>
	/// Populates the user interface with the buildable objects in the prefabs array
	/// </summary>
	void PopulateUiWithBuildableObjects() {
		foreach (BuildableObject bo in prefabs) {
			GameObject boUi = Instantiate (buildableObjectUiPrefab, buildableObjectUiContainer.transform) as GameObject;
			BuildableUiPrefabController prefabController = boUi.GetComponent<BuildableUiPrefabController> ();
			if (prefabController != null) {
				// Add in the data for that buildable object
				string name = (bo.objectName.Length != 0) ? bo.objectName : bo.gameObject.name;
				string desc = (bo.objectDescription.Length > 0) ? bo.objectDescription : "....";
				prefabController.SetData(name, desc, bo.gameObject.name, bo.objectImage);
			}

		}
	}

	#region UI Hide/Show
	/// <summary>
	/// Shows the UI
	/// </summary>
	public void ShowUI() {
		if (buildableUiAnimator != null) {
			isUiOn = true;
			buildableUiAnimator.enabled = true;
			buildableUiAnimator.Play ("CraftingMenuOn");
		}

	}

	/// <summary>
	/// Hides the UI
	/// </summary>
	public void HideUI() {
		if (buildableUiAnimator != null) {
			isUiOn = false;
			buildableUiAnimator.enabled = true;
			buildableUiAnimator.Play ("CraftingMenuOff");
		}
	}
	#endregion


	#region Unity Functions

	void Start(){
		PopulateUiWithBuildableObjects ();
		buildableUiAnimator = buildableObjectUiContainer.transform.parent.transform.parent.GetComponent<Animator> (); //Will be a double layer parent
		ShowUI ();
	}

	void Update ()
	{
		//---------------- Anytime inputs ---------------------
		//Toggle the snappy movement on and off
		if (Input.GetKeyDown (KeyCode.F5)) {
			snappyMovement = !snappyMovement;
		}

		//Show building UI
		if (Input.GetKeyDown (KeyCode.B)) {
			if (isUiOn)
				HideUI ();
			else {
				ShowUI ();
			}
		}


		if (!build) {
			//Check for collision for delete
			CheckForSelectCollision();

			//Inputs for quick build access (first ten prefabs)
			if (Input.GetKeyDown (KeyCode.Alpha1)) {
				BeginBuilding (0);
			}

			if (Input.GetKeyDown (KeyCode.Alpha2)) {
				BeginBuilding (1);
			}

			if (Input.GetKeyDown (KeyCode.Alpha3)) {
				BeginBuilding (2);
			}

			if (Input.GetKeyDown (KeyCode.Alpha4)) {
				BeginBuilding (3);
			}

			if (Input.GetKeyDown (KeyCode.Alpha5)) {
				BeginBuilding (4);
			}

			if (Input.GetKeyDown (KeyCode.Alpha6)) {
				BeginBuilding (5);
			}

			if (Input.GetKeyDown (KeyCode.Alpha7)) {
				BeginBuilding (6);
			}

			if (Input.GetKeyDown (KeyCode.Alpha8)) {
				BeginBuilding (7);
			}

			if (Input.GetKeyDown (KeyCode.Alpha9)) {
				BeginBuilding (8);
			}

			if (Input.GetKeyDown (KeyCode.Alpha0)) {
				BeginBuilding (9);
			}



		} else {
			Ray ray = Camera.main.ScreenPointToRay (Input.mousePosition);
			if (Physics.Raycast (ray, out hitInfo, 100, objectsCanBeBuiltOnLayers)) {
				if (hitInfo.collider != null) {
					Vector3 position = hitInfo.point;
					position.y += buildObjectComponent.placementHeightAdjustment; //This will ensure objects that aren't on the surface are raycasted correctly.
					buildObject.transform.position = position;

					//Check if it can ONLY be on other buildables..
					if (buildObjectComponent.onlyBuildOnOtherBuildables) {
						bool canBuild = (hitInfo.collider.gameObject.layer == buildableObjectLayer) ? true : false;
						buildObjectComponent.AdjustMaterial (canBuild);
					}

					//For Snappy/Grid based movement;
					if (snappyMovement) {
						Vector3 currentPosition = buildObject.transform.position;
						currentPosition -= Vector3.one * offset;
						currentPosition.y /= gridSize;
						currentPosition /= gridSize;
						currentPosition = new Vector3 (Mathf.Round (currentPosition.x), Mathf.Round (currentPosition.y + buildObjectComponent.placementHeightAdjustment), Mathf.Round (currentPosition.z));
						currentPosition *= gridSize;
						currentPosition.y *= gridSize;
						currentPosition += Vector3.one * offset;
						buildObject.transform.position = currentPosition;
					}
				}
			}
			if (Input.GetKeyDown (KeyCode.Mouse0)) {
				BuildableObject bo = buildObject.GetComponent<BuildableObject>();
				if (bo.isValidPlacement) {
					bo.PlaceObject ();
					build = false;
				} 
			}

			if (Input.GetKeyDown (KeyCode.Mouse1)) {
				StopPlacingObject (buildObject);
			}

			//Rotation and height of objects
			float wheel = Input.GetAxis ("Mouse ScrollWheel");
			if (Input.GetKey (KeyCode.LeftAlt)) {
				//Height adjustment with alt
				if (buildObjectComponent) {
					buildObjectComponent.placementHeightAdjustment += (wheel);
				}
			} else {
				buildObject.transform.Rotate (new Vector3 (0, (wheel * 100), 0), Space.World);
			}


		}
	}
	#endregion
}
