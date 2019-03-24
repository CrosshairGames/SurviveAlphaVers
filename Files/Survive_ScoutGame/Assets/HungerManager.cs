using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HungerManager : MonoBehaviour
{
    public Slider hungerSlider;
    public float currentHunger = 100;
    public int hungerDeductAmount = 2;
    StatHandler statHandler;
    PlayerHealthManager playerHealthManager;

    // Start is called before the first frame update
    void Start()
    {
        playerHealthManager = GameObject.FindObjectOfType<PlayerHealthManager>();
    }

    // Update is called once per frame
    void Update()
    {
        if (currentHunger > 0)
        {
            SliderController();
        }
        else
        {
            playerHealthManager.health -= Time.deltaTime;
        }
    }

    private void SliderController()
    {
        currentHunger -= Time.deltaTime * hungerDeductAmount;
        hungerSlider.value = currentHunger;
    }
}
