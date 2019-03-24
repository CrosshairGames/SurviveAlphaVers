using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FoodHandler : MonoBehaviour
{
    StatHandler statHandler;
    PlayerHealthManager playerHealthManager;
    public int hungerToFill = 1;
    public int healthToHeal = 2;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        statHandler = GameObject.FindObjectOfType<StatHandler>();
        playerHealthManager = GameObject.FindObjectOfType<PlayerHealthManager>();
    }

    private void OnMouseOver()
    {
        if (Input.GetKey(KeyCode.E))
        {
            if (statHandler.food < 100)
            {
                statHandler.food += hungerToFill;
            }
            if (playerHealthManager.health != 100)
            {
                playerHealthManager.health += healthToHeal;
            }
            Destroy(gameObject);
        }
    }
}
