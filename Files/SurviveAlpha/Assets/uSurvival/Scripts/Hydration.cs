using System.Linq;
using UnityEngine;
using Mirror;

// inventory, attributes etc. can influence max
public interface IHydrationBonus
{
    int GetHydrationBonus(int baseHydration);
    int GetHydrationRecoveryBonus();
}

[DisallowMultipleComponent]
public class Hydration : Energy
{
    public int baseRecoveryPerTick = -1;
    public int baseHydration = 100;

    // cache components that give a bonus (attributes, inventory, etc.)
    IHydrationBonus[] bonusComponents;
    void Awake()
    {
        bonusComponents = GetComponentsInChildren<IHydrationBonus>();
    }

    // calculate max
    public override int max
    {
        get
        {
            int bonus = bonusComponents.Sum(b => b.GetHydrationBonus(baseHydration));
            return baseHydration + bonus;
        }
    }

    public override int recoveryPerTick
    {
        get
        {
            int bonus = bonusComponents.Sum(b => b.GetHydrationRecoveryBonus());
            return baseRecoveryPerTick + bonus;
        }
    }
}