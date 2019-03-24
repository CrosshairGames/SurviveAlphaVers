using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StickHandler : MonoBehaviour
{
    StatHandler statHandler;

    // Start is called before the first frame update
    void Start()
    {
        statHandler = GameObject.FindObjectOfType<StatHandler>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnMouseOver()
    {
        if(Input.GetKey(KeyCode.E))
        {
            statHandler.sticks += 1;
            Destroy(gameObject);
        }
    }
}
