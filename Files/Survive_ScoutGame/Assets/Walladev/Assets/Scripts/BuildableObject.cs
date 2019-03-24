using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent (typeof (BoxCollider))]
public class BuildableObject : MonoBehaviour {
	#region General Object Information
	[Tooltip("Name will be used for UI display. If none provided, will use prefab name")]
	public string objectName = "";
	[Tooltip("Description used in UI display")]
	public string objectDescription = "...Enter Description...";
	[Tooltip("Image used in UI display")]
	public Texture objectImage;
	#endregion


	#region Object Placement Type Configuration
	[Tooltip("Change to alter the amount of overlap allowed between this and other objects")]
	public ObjectPlacementOverlapFlexibility objectOverlapPlacementFlexibility = ObjectPlacementOverlapFlexibility.NORMAL;
	[Tooltip("Change to change where this object can be placed")]
	public ObjectBuildLocations objectBuildLocationAllowance = ObjectBuildLocations.ANYWHERE;
	[Tooltip("Enables/Disables build buffer on top of object")]
	public bool noBuildingDirectlyOnTopOfObject = false;		//Enables/Disables buffer zone of building directly on top of the object
	[Tooltip("Will be set by the enum ObjectBuildLocations")]
	public bool onlyBuildOnOtherBuildables = false;				//Will be flipped by the enum "ObjectBuildLocations"

	private float overlapFactor = .8f;							// Will be set by the enum "ObjectPlacementOverlapFlexibility" and the objects trigger collider adjusted accordingly
	private float noBuildingOnTopBufferAmount = 1.5f;			// Factor of additional space given to the BoxCollider with Trigger
	#endregion

	#region Delete Variables
	[Tooltip("Amount of time to hold the delete key when focused on an object")]
	public float deleteDurationForPress = 1f;
	float deletePressTime = 0f;
	#endregion

	#region Placement Adjustment Configuration
	[Tooltip("Allows you to alter the height for weird meshes/objects. Also changed by ALT + scroll wheel")]
	public float placementHeightAdjustment = 0f;
	#endregion

	// ---------------------Non typically - user set variables ---------------

	#region Public Configration Variables (Not set by user)
	//These will be set throughout the script based on placement
	[Tooltip("Dictated by the object placement/script - Flag if the placement is valid")]
	public bool isValidPlacement = true;
	[Tooltip("Dictated by the object placement/script - Flag if the object is built")]
	public bool isBuilt = false;
	public BoxCollider boxColliderNoTrigger; 					//The box collider without the trigger, outline of the object
	BoxCollider boxColliderTrigger; 							//The box collider that will have the trigger
	#endregion

	#region Private Variables
	GameObject placementIndicatorObject;						//Will hold reference to the placement indicator
	BuildController buildController;							//Reference to the build controller (for variables)
	List<Collider> collidersWithinList;							//Required for the optimization of calls for "Only_placable_on_other_buildables" objects.
	bool isSelected = false;
	int buildableObjectLayer;  									//The layer it will set all buildables to
	#endregion




	#region Unity Functions
	void Start () {
		//initialize the variables
		buildController = GameObject.FindObjectOfType<BuildController> ();
		ConfigurePlacementVariables ();
		ConfigureBoxColliderVariables ();
		buildableObjectLayer = buildController.buildableObjectLayer;
		ConfigurePlaceableIndicator ();
		collidersWithinList = new List<Collider> ();
	}

	void Update() {
		if (placementIndicatorObject.activeSelf && (Input.GetKey(KeyCode.Delete) || Input.GetKeyDown(KeyCode.Backspace)|| Input.GetKey(KeyCode.Escape))) {
			deletePressTime += Time.deltaTime;

			if (deletePressTime > deleteDurationForPress) {
				buildController.StopPlacingObject (this.gameObject);
			}
		}
	}

	void OnTriggerEnter(Collider other) {
		if (other.gameObject.layer == buildableObjectLayer) {
			if (onlyBuildOnOtherBuildables)
				collidersWithinList.Add (other);
			isValidPlacement = false;
			AdjustMaterial (false);
		}
	}

	void OnTriggerExit(Collider other) {
		if (other.gameObject.layer == buildableObjectLayer) {
			if (onlyBuildOnOtherBuildables)
				collidersWithinList.Remove (other);
			AdjustMaterial (true);
			isValidPlacement = true;
		}
	}

	void OnTriggerStay(Collider col)
	{

		if (col.gameObject.layer == buildableObjectLayer) {
			isValidPlacement = false;
			AdjustMaterial (false);
		}
	}
	#endregion


	#region Initialization based on Variables
	/// <summary>
	/// Configures the placement variables. Will take the input enums and convert them to values.
	/// </summary>
	void ConfigurePlacementVariables() {
		//First configure the overlap flexibility
		switch (objectOverlapPlacementFlexibility) {
		case ObjectPlacementOverlapFlexibility.LENIENT:
			overlapFactor = .4f;
			break;
		case ObjectPlacementOverlapFlexibility.NORMAL:
			overlapFactor = .7f;
			break;
		case ObjectPlacementOverlapFlexibility.STRICT:
			overlapFactor = .95f;
			break;
		}

		switch (objectBuildLocationAllowance) {
		case ObjectBuildLocations.ANYWHERE:
			onlyBuildOnOtherBuildables = false;
			break;
		case ObjectBuildLocations.ONLY_ON_OTHER_BUILDABLES:
			onlyBuildOnOtherBuildables = true;
			break;
		}

	}

	/// <summary>
	/// Configures the box collider variables, finding the non trigger and trigger ones, and ensuring two are present on the object.
	/// Will also ensure a rigid body is attached the object and configured correctly
	/// </summary>
	void ConfigureBoxColliderVariables() {
		//first, ensure there is a kinematic rigidbody on this object. If not, add one.
		Rigidbody rb = this.gameObject.GetComponent<Rigidbody>();
		if (rb == null)
			rb = this.gameObject.AddComponent<Rigidbody> ();
		//Now set the conditions on the RB
		rb.isKinematic = true;
		rb.constraints = RigidbodyConstraints.None;

		BoxCollider[] allBoxColliders = GetComponents<BoxCollider> ();
			foreach (BoxCollider bc in allBoxColliders) {
				if (!bc.isTrigger)
					boxColliderNoTrigger = bc;
				else {
					boxColliderTrigger = bc;
				}
			}
			if (boxColliderNoTrigger == null) {
				Debug.LogError ("You need to AT LEAST have a box collider without a trigger (outline of object) to work. Please configure correctly. Disabling placement");
				this.enabled = false;
			buildController.StopPlacingObject (this.gameObject);
				return;
			}
		
		//Add in our trigger collider, if there isn't one. If it can stack, adjust its Y accordingly.
		if (boxColliderTrigger == null) {
			boxColliderTrigger = this.gameObject.AddComponent<BoxCollider> ();
			boxColliderTrigger.isTrigger = true; 
			boxColliderTrigger.center = boxColliderNoTrigger.center;
			//Make it 15% bigger

			Vector3 newBoxColliderSize = boxColliderNoTrigger.size * overlapFactor;
			if (noBuildingDirectlyOnTopOfObject) { //Will expand the trigger collider in the upwards direction to ensure nothign is placed directly on top. 
				Vector3 newTriggerColliderCenter = boxColliderTrigger.center;
				float centerMovement = 0f;
				if (this.transform.eulerAngles.x > 0) { //Frequently, meshes are rotated in the X. This will account for that.
					newBoxColliderSize.z = boxColliderTrigger.size.z * noBuildingOnTopBufferAmount;
					centerMovement = newBoxColliderSize.z - boxColliderTrigger.size.z;
					newTriggerColliderCenter.z = boxColliderTrigger.center.z  + centerMovement;
				} else {
					newBoxColliderSize.y = boxColliderTrigger.size.y * noBuildingOnTopBufferAmount;
					centerMovement = newBoxColliderSize.y - boxColliderTrigger.size.y;
					newTriggerColliderCenter.y = boxColliderTrigger.center.y + centerMovement;
				}
				boxColliderTrigger.center = newTriggerColliderCenter;
			}
			boxColliderTrigger.size = newBoxColliderSize;
		}
	}

	#endregion

	#region Placement Indicator Functions
	/// <summary>
	/// Configures the placeable indicator. Sets its size based on the appropriate box collider without the trigger (since the one with the trigger can change in size)
	/// </summary>
	void ConfigurePlaceableIndicator() {
		placementIndicatorObject = GameObject.CreatePrimitive (PrimitiveType.Cube);
		placementIndicatorObject.transform.parent = this.transform;
		placementIndicatorObject.GetComponent<Renderer> ().material = buildController.validPlacement;
		placementIndicatorObject.transform.localPosition = boxColliderNoTrigger.center;

		Vector3 placementIndicatorProperScale;
		if (this.gameObject.GetComponent<Renderer> () != null) {
			//---------- Begin scale and orientation of mesh (some meshes are rotated) equating
			placementIndicatorProperScale = this.gameObject.GetComponent<Renderer> ().bounds.size ;

			Vector3 gameObjectScale = transform.localScale;
			placementIndicatorProperScale.x /= gameObjectScale.x;
			placementIndicatorProperScale.y /= gameObjectScale.y;
			placementIndicatorProperScale.z /= gameObjectScale.z;
			placementIndicatorObject.transform.localScale = placementIndicatorProperScale;
			// ---------- End scaling--------------  
		} else {
			placementIndicatorProperScale= boxColliderNoTrigger.size;
		}
		placementIndicatorProperScale.Scale (new Vector3(1.01f, 1.01f, 1.01f)); //To scale it up SLIGHTLY to ensure you can see the renderer
		placementIndicatorObject.transform.localScale = placementIndicatorProperScale;
		placementIndicatorObject.layer = 2; //Ignore Raycast
		placementIndicatorObject.name = "Placable Indicator";
	}

	/// <summary>
	/// Shows the indicator for selection. Will show or hide the indicator, and change its color to the selection color. Will flip the bool for selection appropriately.
	/// Then within the update, if a DELETE key is pressed, then the object will poof.
	/// </summary>
	/// <param name="shouldShow">If set to <c>true</c> should show.</param>
	public void ShowIndicatorForSelection(bool shouldShow) {
		if (shouldShow) {
			placementIndicatorObject.GetComponent<Renderer> ().material = buildController.selectedPlacement;
			placementIndicatorObject.SetActive (true);
		} else {
			placementIndicatorObject.SetActive (false);
			deletePressTime = 0f; 			// Reset the timer for delete in case it was semi pressed
		}

		isSelected = shouldShow;
	}


	/// <summary>
	/// Adjusts the material of the placementIndicator to show if the placement is valid or not.
	/// </summary>
	/// <param name="canPlace">If set to <c>true</c> can place.</param>
	public void AdjustMaterial(bool canPlace) {
		Material matToSet = buildController.validPlacement;
		if(canPlace && collidersWithinList.Count == 0) {
			matToSet = buildController.validPlacement;
			isValidPlacement = true;
		} else {
			matToSet = buildController.invalidPlacement;
			isValidPlacement = false;
		}
		placementIndicatorObject.GetComponent<Renderer> ().material = matToSet;

	}

	#endregion



	/// <summary>
	/// Places the object. Called by the Build Controller when the user is in build mode and clicks the mouse.
	/// Handles the removing of the indicator object (since the object is now placed)
	/// </summary>
	public void PlaceObject() {
		if (isValidPlacement) {
			isBuilt = true;
			gameObject.layer = buildableObjectLayer;
			//Disable the indicator
			placementIndicatorObject.SetActive(false);
//			Destroy (placementIndicatorObject);

			//Check to see if there is a mesh collider attached to the object/its children (Mostly the case with models)
			//If so, remove the noTriggerCollider
			bool hasMeshCollider = false;
			foreach (MeshCollider collider in this.gameObject.GetComponentsInChildren<MeshCollider>()) {
				if (!collider.isTrigger && collider.sharedMesh != null) {
					hasMeshCollider = true;
					break;
				}
			}

			if (!hasMeshCollider) {
				//Try to add a meshcollider, and see if it picks up the mesh. If not, still keep the box collider
				MeshCollider mc = this.gameObject.AddComponent<MeshCollider>();
				mc.isTrigger = false;
				if (mc.sharedMesh != null)
					hasMeshCollider = true;
			}
			//Remove the noTriggerCollider if still no mesh collider working (Necessary for things like steps/ramps so people can still walk on them.
			if (hasMeshCollider) {
				Debug.LogWarning ("As the object has a working non-trigger MeshCollider, disabling the outline BoxCollider (to allow for player climbing)");
				boxColliderNoTrigger.enabled = false;
			}

		}
	}

	#region Enums
	/// <summary>
	/// Enum to specify the Object placement overlap flexibility.
	/// </summary>
	public enum ObjectPlacementOverlapFlexibility
	{
		NORMAL, STRICT, LENIENT
	}

	/// <summary>
	/// Enum to specify the valid Object build location
	/// </summary>
	public enum ObjectBuildLocations
	{
		ANYWHERE, ONLY_ON_OTHER_BUILDABLES
	}

	#endregion
}
