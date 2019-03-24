using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StatHandler : MonoBehaviour
{
    HungerManager hungerManager;

    public int food = 0;
    public int sticks = 0;

    public Text stickText;

    // Start is called before the first frame update
    void Start()
    {
        hungerManager = GameObject.FindObjectOfType<HungerManager>();
        stickText.text = "Sticks: 0";
    }

    // Update is called once per frame
    void Update()
    {
        stickText.text = "Sticks: " + sticks;
        if (food >= 1)
        {
            hungerManager.currentHunger += food;
            food -= food;
        }
    }
}
