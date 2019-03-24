using System.Linq;
using UnityEngine;
using Mirror;

// inventory, attributes etc. can influence max
public interface INutritionBonus
{
    int GetNutritionBonus(int baseNutrition);
    int GetNutritionRecoveryBonus();
}

[DisallowMultipleComponent]
public class Nutrition : Energy
{
    public int baseRecoveryPerTick = -1;
    public int baseNutrition = 100;

    // cache components that give a bonus (attributes, inventory, etc.)
    INutritionBonus[] bonusComponents;
    void Awake()
    {
        bonusComponents = GetComponentsInChildren<INutritionBonus>();
    }

    // calculate max
    public override int max
    {
        get
        {
            int bonus = bonusComponents.Sum(b => b.GetNutritionBonus(baseNutrition));
            return baseNutrition + bonus;
        }
    }

    public override int recoveryPerTick
    {
        get
        {
            int bonus = bonusComponents.Sum(b => b.GetNutritionRecoveryBonus());
            return baseRecoveryPerTick + bonus;
        }
    }
}