using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using SaveIsEasy;

public class PauseMenuHandler : MonoBehaviour
{
    public GameObject buttonHolderObj;
    public Transform canvas;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (canvas.gameObject.activeInHierarchy == false)
            {
                Cursor.lockState = CursorLockMode.Confined;
                Cursor.visible = true;
                canvas.gameObject.SetActive(true);
                Time.timeScale = 0;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                canvas.gameObject.SetActive(false);
                Time.timeScale = 1;
            }
        }
    }

    public void Resume()
    {
        Cursor.lockState = CursorLockMode.Locked;
        buttonHolderObj.SetActive(false);
        Cursor.visible = false;
        Time.timeScale = 1;
    }

    public void ExitToMenu()
    {
        Time.timeScale = 1;
        SceneManager.LoadScene("MainMenu");
    }

    public void ExitToDesktop()
    {
        Application.Quit();
    }
    
}
