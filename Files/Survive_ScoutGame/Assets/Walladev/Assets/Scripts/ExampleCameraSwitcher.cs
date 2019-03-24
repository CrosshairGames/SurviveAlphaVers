using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Example camera switcher. Will switch the Example scene between the three provided cameras.
/// </summary>
public class ExampleCameraSwitcher : MonoBehaviour {

	public GameObject staticCamera, fpsCamera, overheadCamera;

	// Use this for initialization
	void Start () {
		fpsCamera.SetActive (false);
		overheadCamera.SetActive (false);
		staticCamera.SetActive (true);
	}
	
	// Update is called once per frame
	void Update () {
		//F6 is normal angled
		if (Input.GetKeyDown (KeyCode.F6)) {
			fpsCamera.SetActive (false);
			overheadCamera.SetActive (false);
			staticCamera.SetActive (true);
		} 

		//F8 is overhead
		if (Input.GetKeyDown (KeyCode.F8)) {
			staticCamera.SetActive (false);
			fpsCamera.SetActive (false);
			overheadCamera.SetActive (true);
		}
	}
}
