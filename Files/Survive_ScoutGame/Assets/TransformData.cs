using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class TransformData : MonoBehaviour
{
    public Vector3 p_Transform;

    // Start is called before the first frame update
    void Start()
    {
        p_Transform = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
