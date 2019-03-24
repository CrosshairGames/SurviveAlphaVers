using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CampfireHandler : MonoBehaviour
{
    public int stickDeduct = 1;
    public StatHandler statHandler;
    public BuildableObject campfireObj;
    public bool hadEnoughSticks = false;
    public bool hasEnteredFire = false;
    public int fireTemp = 10;
    PlayerHeatManager playerHeatManager;

    public Collider campfireCollider;

    // Start is called before the first frame update
    void Start()
    {
        playerHeatManager = FindObjectOfType<PlayerHeatManager>();
        statHandler = GameObject.FindObjectOfType<StatHandler>();
    }

    // Update is called once per frame
    void Update()
    {
        if (campfireObj.isBuilt != true)
        {
            campfireCollider.enabled = false;
        }
        else
        {
            campfireCollider.enabled = true;
        }

        if (campfireObj.isBuilt == true)
        {
            if (statHandler.sticks >= stickDeduct && hadEnoughSticks == false)
            {
                statHandler.sticks -= stickDeduct;
                hadEnoughSticks = true;
            }
            else if (statHandler.sticks < stickDeduct && hadEnoughSticks == false)
            {
                print("Not enough matirials");
                Destroy(campfireObj.gameObject);
            }
        }
    }

    private void OnDestroy()
    {

    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Player")
        {
            if (hasEnteredFire == false)
            {
                hasEnteredFire = true;
                playerHeatManager.currentTemp += fireTemp;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
      if (other.gameObject.tag == "Player")
      {
         if (hasEnteredFire == true)
         {
             playerHeatManager.currentTemp -= fireTemp;
              hasEnteredFire = false;
         }
      }
    }
}
