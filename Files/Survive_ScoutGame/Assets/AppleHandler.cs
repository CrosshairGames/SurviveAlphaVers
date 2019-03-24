using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AppleHandler : MonoBehaviour
{

    HungerManager hungerManager;

    // Start is called before the first frame update
    void Start()
    {
        hungerManager = FindObjectOfType<HungerManager>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void addHunger(float amountToGive)
    {
        hungerManager.currentHunger += amountToGive;
    }

}
