using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BuildCostHandler : MonoBehaviour
{
    public BuildableObject buildableObject;
    public StatHandler playerStats;
    public int buildCost;
    public bool hasDeducted = false;

    // Start is called before the first frame update
    void Start()
    {
        playerStats = FindObjectOfType<StatHandler>();
    }

    // Update is called once per frame
    void Update()
    {
        if (buildableObject.isBuilt == true)
        {
            if (playerStats.sticks >= buildCost && hasDeducted == false)
            {
                playerStats.sticks -= buildCost;
                hasDeducted = true;
            }
            else if (playerStats.sticks < buildCost && hasDeducted == false)
            {
                print("Not enough matirials");
                Destroy(gameObject);
            }
        }
    }

    private void OnDestroy()
    {

    }
}
