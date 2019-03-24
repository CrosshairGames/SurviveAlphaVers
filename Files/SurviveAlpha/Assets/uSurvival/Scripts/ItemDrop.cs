using UnityEngine;
using Mirror;

// make sure that drops aren't sent to all players on the server, only to those
// that are close enough
[RequireComponent(typeof(NetworkProximityGridChecker))] // don't send drops to all players, just the close ones
[RequireComponent(typeof(Collider))] // needed for looting raycasting
public class ItemDrop : NetworkBehaviour, Interactable
{
    // default itemData, can be assigned in Inspector
    [SerializeField] ScriptableItem itemData; // not public, so that people use .item & .amount

    // drops need a real Item + amount so that we can set dynamic stats like ammo
    // note: we don't use 'ItemSlot' so that 'amount' can be assigned in Inspector for default spawns
    [SyncVar] public int amount = 1; // sometimes set on server, needs to sync
    [SyncVar, HideInInspector] public Item item;

    public override void OnStartServer()
    {
        // create slot from template, unless we assigned it manually already
        // (e.g. if an item spawner assigns it after instantiating it)
        if (item.hash == 0 && itemData != null)
            item = new Item(itemData);
    }

    // interactable ////////////////////////////////////////////////////////////
    public string GetInteractionText()
    {
        if (PlayerMeta.localPlayer != null && itemData != null && amount > 0)
            return amount > 1 ? item.name + " x " + amount : item.name;
        return "";
    }

    [Client]
    public void OnInteractClient(GameObject player) {}

    [Server]
    public void OnInteractServer(GameObject player)
    {
        // try to add it to the inventory, destroy drop if it worked
        if (amount > 0)
        {
            if (player.GetComponent<Inventory>().Add(item, amount))
            {
                // clear drop's item slot too so it can't be looted again
                // before truly destroyed
                amount = 0;
                NetworkServer.Destroy(gameObject);
            }
        }
    }
}
