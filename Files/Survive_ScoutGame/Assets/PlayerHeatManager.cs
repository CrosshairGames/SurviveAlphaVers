using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHeatManager : MonoBehaviour
{
    public Text tempText;
    public Slider heatSlider;
    public Image heatSImage;
    public Text heatText;
    public Text warningText;
    public GameObject warningHolder;

    TemperatureHandler temperatureHandler;
    PlayerHealthManager playerHealthManager;

    public bool setCurrDayTemp = false;
    public bool setCurrNightTemp = false;

    public int currentTemp = 0;
    public int tooColdTemp = -10;
    public int tooHotTemp = 20;

    // Start is called before the first frame update
    void Start()
    {
        temperatureHandler = GameObject.FindObjectOfType<TemperatureHandler>();
        playerHealthManager = GameObject.FindObjectOfType<PlayerHealthManager>();
    }

    // Update is called once per frame
    void Update()
    {
        if (temperatureHandler.isDay)
        { 
            CheckHeatDay();
            if (setCurrDayTemp == false)
            {
                currentTemp = temperatureHandler.currentTemperature - 2;
                heatSlider.value = 0;
                setCurrDayTemp = true;
                setCurrNightTemp = false;
            }
        }
        else
        {
            CheckHeatNight();
            if (setCurrNightTemp == false)
            {
                currentTemp = temperatureHandler.currentTemperature + 2;
                heatSlider.value = 0;
                setCurrNightTemp = true;
                setCurrDayTemp = false;
            }
        }
        CheckTempStatus();
        tempText.text = currentTemp + "°C";
    }

    private void CheckTempStatus()
    {
        if(heatSlider.value == 50)
        {
            warningHolder.SetActive(true);
            warningText.text = "You are too hot/cold";
            playerHealthManager.health -= 3 * Time.deltaTime;
        }
        else
        {
            warningHolder.SetActive(false);
            warningText.text = "";
        }
    }

    private void CheckHeatNight()
    {
        if (currentTemp < tooColdTemp)
        {
            SetSliderNight();
        }
        else if (currentTemp > tooColdTemp)
        {
            if (heatSlider.value != 0)
            {
                heatSlider.value -= 3 * Time.deltaTime;
            }
            else if (heatSlider.value == 0)
            {
                heatSlider.gameObject.SetActive(false);
            }
        }
    }

    private void SetSliderNight()
    {
        heatSlider.gameObject.SetActive(true);
        heatSlider.value += 3 * Time.deltaTime;
        heatSImage.color = Color.blue;
        heatText.text = "Cold";
    }

    private void CheckHeatDay()
    {
        if (currentTemp > tooHotTemp)
        {
            SetSliderDay();
        }
        else if (currentTemp < tooHotTemp)
        {
            if (heatSlider.value != 0)
            {
                heatSlider.value -= 2 * Time.deltaTime;
            }
            else if (heatSlider.value == 0)
            {
                heatSlider.gameObject.SetActive(false);
            }
        }
    }

    private void SetSliderDay()
    {
        heatSlider.gameObject.SetActive(true);
        heatSlider.value += 3 * Time.deltaTime;
        heatSImage.color = Color.yellow;
        heatText.text = "Heat stroke";
    }
}
