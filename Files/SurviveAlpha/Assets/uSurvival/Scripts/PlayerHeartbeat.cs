using UnityEngine;
using Mirror;

public class PlayerHeartbeat : NetworkBehaviour
{
    public AudioSource audioSource;
    public Health health;

    void Update()
    {
        audioSource.volume = isLocalPlayer ? (1 - health.Percent()) : 0;
    }
}
