using UnityEngine;
using System.Linq;

public class CursorLocking : MonoBehaviour
{
    public GameObject[] windowsThatUnlockCursor;

    void Update()
    {
        Cursor.lockState = windowsThatUnlockCursor.Any(go => go.activeSelf)
                           ? CursorLockMode.None
                           : CursorLockMode.Locked;

        // OSX auto hides cursor while locked, Windows doesn't so do it manually
        Cursor.visible = Cursor.lockState != CursorLockMode.Locked;
    }
}
