using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using GameCreator.Characters;

public class RunControler : MonoBehaviour
{
    public PlayerCharacter playerCharactor;
    public Slider staminaSlider;
    public float currentStamina;
    public float staminaDeductTime = 5;
    public float staminaRegenTime = 3;

    // Start is called before the first frame update
    void Start()
    {
        playerCharactor.characterLocomotion.canRun = false;
    }

    // Update is called once per frame
    void Update()
    {

        if (Input.GetKey(KeyCode.LeftShift))
        {
           currentStamina -= Time.deltaTime * staminaDeductTime;
            if (currentStamina > staminaSlider.minValue)
            {
                playerCharactor.characterLocomotion.canRun = true;
            }          
        }
        else
        {
            if (currentStamina < 50f)
            {
                playerCharactor.characterLocomotion.canRun = false;
                currentStamina += Time.deltaTime * staminaRegenTime;
            }
        }

        staminaSlider.value = currentStamina;
    }
}
