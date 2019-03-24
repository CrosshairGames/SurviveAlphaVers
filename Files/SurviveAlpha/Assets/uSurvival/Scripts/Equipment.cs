using UnityEngine;
using Mirror;
using System.Collections.Generic;
using System.Linq;

[DisallowMultipleComponent]
[RequireComponent(typeof(Health))]
[RequireComponent(typeof(Inventory))]
public abstract class Equipment : NetworkBehaviour, IHealthBonus, IHydrationBonus, INutritionBonus, ICombatBonus
{
    // Used components. Assign in Inspector. Easier than GetComponent caching.
    public Health health;
    public Inventory inventory;

    public SyncListItemSlot slots = new SyncListItemSlot();

    public int GetItemIndexByName(string itemName)
    {
        return slots.FindIndex(slot => slot.amount > 0 && slot.item.name == itemName);
    }

    // energy boni
    public int GetHealthBonus(int baseHealth)
    {
        return slots.Where(slot => slot.amount > 0).Sum(slot => ((EquipmentItem)slot.item.data).healthBonus);
    }
    public int GetHealthRecoveryBonus()
    {
        return 0;
    }
    public int GetHydrationBonus(int baseHydration)
    {
        return slots.Where(slot => slot.amount > 0).Sum(slot => ((EquipmentItem)slot.item.data).hydrationBonus);
    }
    public int GetHydrationRecoveryBonus()
    {
        return 0;
    }
    public int GetNutritionBonus(int baseNutrition)
    {
        return slots.Where(slot => slot.amount > 0).Sum(slot => ((EquipmentItem)slot.item.data).nutritionBonus);
    }
    public int GetNutritionRecoveryBonus()
    {
        return 0;
    }

    // combat boni
    public int GetDamageBonus()
    {
        return slots.Where(slot => slot.amount > 0).Sum(slot => ((EquipmentItem)slot.item.data).damageBonus);
    }
    public int GetDefenseBonus()
    {
        return slots.Where(slot => slot.amount > 0).Sum(slot => ((EquipmentItem)slot.item.data).defenseBonus);
    }
}