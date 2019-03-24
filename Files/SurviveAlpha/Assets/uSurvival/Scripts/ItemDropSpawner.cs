using UnityEngine;
using Mirror;

// requires proximity checker so that we can use .observers to check if any
// players are currently around before spawning
[RequireComponent(typeof(NetworkProximityGridChecker))]
public class ItemDropSpawner : NetworkBehaviour
{
    public ItemDrop itemDrop;

    public float spawnInterval = 15;

    ItemDrop recentSpawn; // to check if still around

    public override void OnStartServer()
    {
        InvokeRepeating(nameof(Spawn), 0, spawnInterval);
    }

    [Server]
    void Spawn()
    {
        // spawn only if previous one was taken by now
        // and if no players are around to see it happening (that's kinda cool)
        if (recentSpawn == null && itemDrop != null && netIdentity.observers.Count == 0)
        {
            GameObject go = Instantiate(itemDrop.gameObject, transform.position, transform.rotation);
            go.name = itemDrop.name; // avoid "(Clone)"
            NetworkServer.Spawn(go);
            recentSpawn = go.GetComponent<ItemDrop>();
            //Debug.Log("spawned " + itemDrop.name + " @ " + NetworkTime.time);
        }
    }
}
