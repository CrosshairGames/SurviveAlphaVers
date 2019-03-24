using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TemperatureHandler : MonoBehaviour
{
    public int currentTemperature;
    public string stringTime;
    public bool nightBool = false;
    public bool dayBool = true;
    public bool isDay = true;

    PlayerHeatManager playerHeatManager;
    DayAndNightControl nightControl;

    //°C
    // Start is called before the first frame update
    void Start()
    {
        nightControl = GameObject.FindObjectOfType<DayAndNightControl>();
        playerHeatManager = GameObject.FindObjectOfType<PlayerHeatManager>();
    }

    // Update is called once per frame
    void Update()
    {
       stringTime = nightControl.TimeOfDay();
       if (stringTime == "Morning")
        {
            if(dayBool == true)
            {
                isDay = true;
                SetDayTemp();
                dayBool = false;
                nightBool = true;
            }
        }
       else if(stringTime == "Night")
        {
            if (nightBool == true)
            {
                isDay = false;
                SetNightTemp();
                nightBool = false;
                dayBool = true;
            }
        }
    }

    private void SetDayTemp()
    {
        playerHeatManager.currentTemp = 0;
        print("Morning temp");
        GetDayTemp();
        playerHeatManager.currentTemp += currentTemperature;
    }

    private void GetDayTemp()
    {
        int rand = Random.Range(15, 20);
        if (rand > 15 && rand < 20)
        {
            currentTemperature = rand;
            print(rand);
        }
        else
        {
            GetDayTemp();
        }
    }

    private void SetNightTemp()
    {
        playerHeatManager.currentTemp = 0;
        print("Night temp");
        GetNightTemp();
        playerHeatManager.currentTemp += currentTemperature;
    }

    private void GetNightTemp()
    {
        int rand = Random.Range(-15, -20);
        if (rand < -15 && rand > -20)
        {
            currentTemperature = rand;
            print(rand);
        }
        else
        {
            GetNightTemp();
        }
    }
}
