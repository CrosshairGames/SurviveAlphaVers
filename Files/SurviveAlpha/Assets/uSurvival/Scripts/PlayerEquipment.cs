using UnityEngine;
using Mirror;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Serialization;

[Serializable]
public struct EquipmentInfo
{
    public string requiredCategory;
    public Transform location;
    public ScriptableItem defaultItem;
}

[RequireComponent(typeof(Animator))]
public class PlayerEquipment : Equipment
{
    // Used components. Assign in Inspector. Easier than GetComponent caching.
    public Animator animator;

    public EquipmentInfo[] slotInfo =
    {
        new EquipmentInfo{requiredCategory="Head", location=null, defaultItem=null},
        new EquipmentInfo{requiredCategory="Chest", location=null, defaultItem=null},
        new EquipmentInfo{requiredCategory="Legs", location=null, defaultItem=null},
        new EquipmentInfo{requiredCategory="Feet", location=null, defaultItem=null}
    };

    // weapon location is a special case because it's no equipment slot, but
    // still needs a transform to be assigned the model etc.
    // IMPORTANT: use an empty weapon mount object that has no children,
    //            otherwise they will be destroyed by RefreshLocation!
    [FormerlySerializedAs("rightHandLocation")]
    public Transform weaponMount;

    void Awake()
    {
        // make sure that weaponmount is an empty transform without children.
        // if someone drags in the right hand, then all the fingers would be
        // destroyed by RefreshLocation.
        // => only check in awake once, because at runtime it will have children
        //    if a weapon is equipped (hence we don't check in OnValidate)
        if (weaponMount != null && weaponMount.childCount > 0)
            Debug.LogWarning(name + " PlayerEquipment.weaponMount should have no children, otherwise they will be destroyed.");
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        // setup synclist callbacks on client. no need to update and show and
        // animate equipment on server
        slots.Callback += OnEquipmentChanged;

        // refresh all locations once (on synclist changed won't be called for
        // initial lists)
        // -> needs to happen before ProximityChecker's initial SetVis call,
        //    otherwise we get a hidden character with visible equipment
        //    (hence OnStartClient and not Start)
        for (int i = 0; i < slots.Count; ++i)
            RefreshLocation(i);
    }

    void RebindAnimators()
    {
        foreach (var anim in GetComponentsInChildren<Animator>())
            anim.Rebind();
    }

    public void RefreshLocation(Transform location, ItemSlot slot)
    {
        // valid item, not cleared?
        if (slot.amount > 0)
        {
            ScriptableItem itemData = slot.item.data;
            // new model? (don't do anything if it's the same model, which
            // happens after only Item.ammo changed, etc.)
            // note: we compare .name because the current object and prefab
            // will never be equal
            if (location.childCount == 0 || itemData.modelPrefab == null ||
                location.GetChild(0).name != itemData.modelPrefab.name)
            {
                // delete old model (if any)
                if (location.childCount > 0)
                    Destroy(location.GetChild(0).gameObject);

                // use new model (if any)
                if (itemData.modelPrefab != null)
                {
                    // instantiate and parent
                    GameObject go = Instantiate(itemData.modelPrefab);
                    go.name = itemData.modelPrefab.name; // avoid "(Clone)"
                    go.transform.SetParent(location, false);

                    // is it a skinned mesh with an animator?
                    Animator anim = go.GetComponent<Animator>();
                    if (anim != null)
                    {
                        // assign main animation controller to it
                        anim.runtimeAnimatorController = animator.runtimeAnimatorController;

                        // restart all animators, so that skinned mesh equipment will be
                        // in sync with the main animation
                        RebindAnimators();
                    }
                }
            }
        }
        else
        {
            // empty now. delete old model (if any)
            if (location.childCount > 0)
                Destroy(location.GetChild(0).gameObject);
        }
    }

    void RefreshLocation(int index)
    {
        ItemSlot slot = slots[index];
        EquipmentInfo info = slotInfo[index];

        // valid category and valid location? otherwise don't bother
        if (info.requiredCategory != "" && info.location != null)
            RefreshLocation(info.location, slot);
    }

    void OnEquipmentChanged(SyncListItemSlot.Operation op, int index, ItemSlot slot)
    {
        // update the model
        RefreshLocation(index);
    }

    [Command]
    public void CmdSwapInventoryEquip(int inventoryIndex, int equipmentIndex)
    {
        // validate: make sure that the slots actually exist in the inventory
        // and in the equipment
        if (health.current > 0 &&
            0 <= inventoryIndex && inventoryIndex < inventory.slots.Count &&
            0 <= equipmentIndex && equipmentIndex < slots.Count)
        {
            // item slot has to be empty (unequip) or equipabable
            ItemSlot slot = inventory.slots[inventoryIndex];
            if (slot.amount == 0 ||
                slot.item.data is EquipmentItem &&
                ((EquipmentItem)slot.item.data).CanEquip(this, inventoryIndex, equipmentIndex))
            {
                // swap them
                ItemSlot temp = slots[equipmentIndex];
                slots[equipmentIndex] = slot;
                inventory.slots[inventoryIndex] = temp;
            }
        }
    }

    [Command]
    public void CmdMergeInventoryEquip(int inventoryIndex, int equipmentIndex)
    {
        // validate: make sure that the slots actually exist in the inventory
        // and in the equipment
        if (health.current > 0 &&
            0 <= inventoryIndex && inventoryIndex < inventory.slots.Count &&
            0 <= equipmentIndex && equipmentIndex < slots.Count)
        {
            // both items have to be valid
            ItemSlot slotFrom = inventory.slots[inventoryIndex];
            ItemSlot slotTo = slots[equipmentIndex];
            if (slotFrom.amount > 0 && slotTo.amount > 0)
            {
                // make sure that items are the same type
                // note: .Equals because name AND dynamic variables matter (petLevel etc.)
                if (slotFrom.item.Equals(slotTo.item))
                {
                    // merge from -> to
                    // put as many as possible into 'To' slot
                    int put = slotTo.IncreaseAmount(slotFrom.amount);
                    slotFrom.DecreaseAmount(put);

                    // put back into the lists
                    inventory.slots[inventoryIndex] = slotFrom;
                    slots[equipmentIndex] = slotTo;
                }
            }
        }
    }

    [Command]
    public void CmdMergeEquipInventory(int equipmentIndex, int inventoryIndex)
    {
        // validate: make sure that the slots actually exist in the inventory
        // and in the equipment
        if (health.current > 0 &&
            0 <= inventoryIndex && inventoryIndex < inventory.slots.Count &&
            0 <= equipmentIndex && equipmentIndex < slots.Count)
        {
            // both items have to be valid
            ItemSlot slotFrom = slots[equipmentIndex];
            ItemSlot slotTo = inventory.slots[inventoryIndex];
            if (slotFrom.amount > 0 && slotTo.amount > 0)
            {
                // make sure that items are the same type
                // note: .Equals because name AND dynamic variables matter (petLevel etc.)
                if (slotFrom.item.Equals(slotTo.item))
                {
                    // merge from -> to
                    // put as many as possible into 'To' slot
                    int put = slotTo.IncreaseAmount(slotFrom.amount);
                    slotFrom.DecreaseAmount(put);

                    // put back into the lists
                    slots[equipmentIndex] = slotFrom;
                    inventory.slots[inventoryIndex] = slotTo;
                }
            }
        }
    }

    // helpers for easier slot access //////////////////////////////////////////
    // GetEquipmentTypeIndex("Chest") etc.
    public int GetEquipmentTypeIndex(string category)
    {
        return slotInfo.ToList().FindIndex(slot => slot.requiredCategory == category);
    }

    // death & respawn /////////////////////////////////////////////////////////
    [Server]
    public void DropItemAndClearSlot(int index)
    {
        // drop and remove from inventory
        ItemSlot slot = slots[index];
        ((PlayerInventory)inventory).DropItem(slot.item, slot.amount);
        slot.amount = 0;
        slots[index] = slot;
    }

    // drop all equipment on death, so others can loot us
    [Server]
    public void OnDeath()
    {
        for (int i = 0; i < slots.Count; ++i)
            if (slots[i].amount > 0)
                DropItemAndClearSlot(i);
    }

    // we don't clear items on death so that others can still loot us. we clear
    // them on respawn.
    [Server]
    public void OnRespawn()
    {
        // for each slot: make empty slot or default item if any
        for (int i = 0; i < slotInfo.Length; ++i)
            slots[i] = slotInfo[i].defaultItem != null ? new ItemSlot(new Item(slotInfo[i].defaultItem)) : new ItemSlot();
    }

    // durability //////////////////////////////////////////////////////////////
    public void OnReceivedDamage(GameObject attacker, int damage)
    {
        // reduce durability in each item
        for (int i = 0; i < slots.Count; ++i)
        {
            if (slots[i].amount > 0)
            {
                ItemSlot slot = slots[i];
                slot.item.durability = Mathf.Clamp(slot.item.durability - damage, 0, slot.item.maxDurability);
                slots[i] = slot;
            }
        }
    }

    // drag & drop /////////////////////////////////////////////////////////////
    void OnDragAndDrop_InventorySlot_EquipmentSlot(int[] slotIndices)
    {
        // slotIndices[0] = slotFrom; slotIndices[1] = slotTo

        // merge? (just check equality, rest is done server sided)
        if (inventory.slots[slotIndices[0]].amount > 0 && slots[slotIndices[1]].amount > 0 &&
            inventory.slots[slotIndices[0]].item.Equals(slots[slotIndices[1]].item))
        {
            CmdMergeInventoryEquip(slotIndices[0], slotIndices[1]);
        }
        // swap?
        else
        {
            CmdSwapInventoryEquip(slotIndices[0], slotIndices[1]);
        }
    }

    void OnDragAndDrop_EquipmentSlot_InventorySlot(int[] slotIndices)
    {
        // slotIndices[0] = slotFrom; slotIndices[1] = slotTo

        // merge? (just check equality, rest is done server sided)
        if (slots[slotIndices[0]].amount > 0 && inventory.slots[slotIndices[1]].amount > 0 &&
            slots[slotIndices[0]].item.Equals(inventory.slots[slotIndices[1]].item))
        {
            CmdMergeEquipInventory(slotIndices[0], slotIndices[1]);
        }
        // swap?
        else
        {
            CmdSwapInventoryEquip(slotIndices[1], slotIndices[0]);
        }
    }
}