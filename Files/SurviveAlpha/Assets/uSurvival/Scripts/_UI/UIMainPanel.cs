using UnityEngine;
using UnityEngine.UI;

public class UIMainPanel : MonoBehaviour
{
    // singleton to access it from player scripts without FindObjectOfType
    public static UIMainPanel singleton;

    public KeyCode hotKey = KeyCode.Tab;
    public GameObject panel;
    public Button quitButton;

    public UIMainPanel()
    {
        // assign singleton only once (to work with DontDestroyOnLoad when
        // using Zones / switching scenes)
        if (singleton == null) singleton = this;
    }

    void Update()
    {
        GameObject player = PlayerMeta.localPlayer;
        if (player)
        {
            // hotkey (not while typing in chat, etc.)
            if (Input.GetKeyDown(hotKey) && !UIUtils.AnyInputActive())
                panel.SetActive(!panel.activeSelf);

            quitButton.onClick.SetListener(NetworkManagerSurvival.Quit);
        }
    }

    public void Show()
    {
        panel.SetActive(true);
    }
}
