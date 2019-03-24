using UnityEngine;
using UnityEngine.UI;

public class UIConstructionHotkeys : MonoBehaviour
{
    public GameObject panel;
    public Text rotationText;

    void Update()
    {
        // holding a structure?
        GameObject player = PlayerMeta.localPlayer;
        if (player != null)
        {
            Construction construction = player.GetComponent<Construction>();
            rotationText.text = construction.rotationKey + " - Rotate";
            panel.SetActive(construction.GetCurrentStructure() != null);
        }
        else panel.SetActive(false);
    }
}
