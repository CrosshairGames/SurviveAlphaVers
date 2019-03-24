using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BuildableUiPrefabController : MonoBehaviour {

	public RawImage image;
	public Text objectName, objectDescription, objectPrefabName;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

	/// <summary>
	/// Will be called by BuildController.cs to instasntiate correct data within this prefab.
	/// </summary>
	/// <param name="name">Name.</param>
	/// <param name="desc">Desc.</param>
	/// <param name="prefabName">Prefab name.</param>
	/// <param name="img">Image.</param>
	public void SetData(string name, string desc, string prefabName, Texture img) {
		objectName.text = name;
		objectDescription.text = desc;
		objectPrefabName.text = prefabName;
		if (img != null) {
			image.texture = img;
		} else {
			//No image, hide the image.
			image.enabled = false;
		}
	}

	public void ClickedObject() {
		//Get the build controller in the scene and startbuildilng that object.
		BuildController buildController = GameObject.FindObjectOfType<BuildController>();
		if (buildController != null) {
			buildController.FindAndBuildByPrefabName (objectPrefabName.text);
		} else {
			Debug.LogWarning ("No Build Controller found in the scene");
		}
	}
}
