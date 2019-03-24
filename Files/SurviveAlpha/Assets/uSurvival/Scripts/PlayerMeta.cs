// keep track of some meta info like class, account etc.
using UnityEngine;
using Mirror;
using System.Collections.Generic;

public class PlayerMeta : NetworkBehaviour
{
    [HideInInspector] public string account = "";
    [HideInInspector] public string className = "";
    public Sprite classIcon; // for character selection

    // online players cache on the server to save lots of computations
    // (otherwise we'd have to iterate NetworkServer.objects all the time)
    public static Dictionary<string, GameObject> onlinePlayers = new Dictionary<string, GameObject>();

    // localPlayer singleton for easier access from UI scripts etc.
    public static GameObject localPlayer;

    public override void OnStartServer()
    {
        onlinePlayers[name] = gameObject;
    }

    public override void OnStartLocalPlayer()
    {
        // set singleton
        localPlayer = gameObject;
    }

    void OnDestroy()
    {
        // Unity bug: isServer is false when called in host mode. only true when
        // called in dedicated mode. so we need a workaround:
        if (NetworkServer.active) // isServer
            onlinePlayers.Remove(name);

        if (isLocalPlayer)
            localPlayer = null;
    }
}