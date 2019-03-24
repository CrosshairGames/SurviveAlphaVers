// the crafting system is designed to work with all kinds of commonly known
// crafting options:
// - item combinations: wood + stone = axe
// - weapon upgrading: axe + gem = strong axe
// - recipe items: axerecipe(item) + wood(item) + stone(item) = axe(item)
//
// players can craft at all times, not just at npcs, because that's the most
// realistic option
using UnityEngine;
using Mirror;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

public enum CraftingState {None, InProgress, Success, Failed}

[DisallowMultipleComponent]
[RequireComponent(typeof(Inventory))]
public class Crafting : NetworkBehaviour
{
    // Used components. Assign in Inspector. Easier than GetComponent caching.
    public Health health;
    public Inventory inventory;

    public List<int> indices = Enumerable.Repeat(-1, ScriptableRecipe.recipeSize).ToList();
    [HideInInspector] public CraftingState craftingState = CraftingState.None; // // client sided

    // craft the current combination of items and put result into inventory
    [Command]
    public void CmdCraft(int[] indices)
    {
        // validate: between 1 and 6, all valid, no duplicates?
        if (health.current > 0 &&
            0 < indices.Length && indices.Length <= ScriptableRecipe.recipeSize &&
            indices.All(index => 0 <= index && index < inventory.slots.Count && inventory.slots[index].amount > 0) &&
            !indices.ToList().HasDuplicates())
        {
            // build list of item templates from indices
            List<ScriptableItem> items = indices.Select(index => inventory.slots[index].item.data).ToList();

            // find recipe
            ScriptableRecipe recipe = ScriptableRecipe.dict.Values.ToList().Find(r => r.CanCraftWith(items)); // good enough for now
            if (recipe != null && recipe.result != null)
            {
                // enough space?
                Item result = new Item(recipe.result);
                if (inventory.CanAdd(result, 1))
                {
                    // remove the ingredients from inventory in any case
                    foreach (int index in indices)
                    {
                        // decrease item amount
                        ItemSlot slot = inventory.slots[index];
                        slot.DecreaseAmount(1);
                        inventory.slots[index] = slot;
                    }

                    // roll the dice to decide if we add the result or not
                    // IMPORTANT: we use rand() < probability to decide.
                    // => UnityEngine.Random.value is [0,1] inclusive:
                    //    for 0% probability it's fine because it's never '< 0'
                    //    for 100% probability it's not because it's not always '< 1', it might be == 1
                    //    and if we use '<=' instead then it won't work for 0%
                    // => C#'s Random value is [0,1) exclusive like most random
                    //    functions. this works fine.
                    if (new System.Random().NextDouble() < recipe.probability)
                    {
                        // add result item to inventory
                        inventory.Add(result, 1);
                        TargetCraftingSuccess(connectionToClient);
                    }
                    else
                    {
                        TargetCraftingFailed(connectionToClient);
                    }
                }
            }
        }
    }

    // two rpcs for results to save 1 byte for the actual result
    [TargetRpc] // only send to one client
    public void TargetCraftingSuccess(NetworkConnection target)
    {
        craftingState = CraftingState.Success;
    }

    [TargetRpc] // only send to one client
    public void TargetCraftingFailed(NetworkConnection target)
    {
        craftingState = CraftingState.Failed;
    }

    // drag & drop /////////////////////////////////////////////////////////////
    void OnDragAndDrop_InventorySlot_CraftingIngredientSlot(int[] slotIndices)
    {
        // slotIndices[0] = slotFrom; slotIndices[1] = slotTo
        // only if not crafting right now
        if (craftingState != CraftingState.InProgress)
        {
            if (!indices.Contains(slotIndices[0]))
            {
                indices[slotIndices[1]] = slotIndices[0];
                craftingState = CraftingState.None; // reset state
            }
        }
    }

    void OnDragAndDrop_CraftingIngredientSlot_CraftingIngredientSlot(int[] slotIndices)
    {
        // slotIndices[0] = slotFrom; slotIndices[1] = slotTo
        // only if not crafting right now
        if (craftingState != CraftingState.InProgress)
        {
            // just swap them clientsided
            int temp = indices[slotIndices[0]];
            indices[slotIndices[0]] = indices[slotIndices[1]];
            indices[slotIndices[1]] = temp;
            craftingState = CraftingState.None; // reset state
        }
    }

    void OnDragAndClear_CraftingIngredientSlot(int slotIndex)
    {
        // only if not crafting right now
        if (craftingState != CraftingState.InProgress)
        {
            indices[slotIndex] = -1;
            craftingState = CraftingState.None; // reset state
        }
    }
}