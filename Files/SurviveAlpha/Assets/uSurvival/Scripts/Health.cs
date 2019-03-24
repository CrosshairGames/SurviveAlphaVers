using System.Linq;
using UnityEngine;
using Mirror;

// inventory, attributes etc. can influence max health
public interface IHealthBonus
{
    int GetHealthBonus(int baseHealth);
    int GetHealthRecoveryBonus();
}

[DisallowMultipleComponent]
public class Health : Energy
{
    public int baseRecoveryPerTick = 0;
    public int baseHealth = 100;

    // cache components that give a bonus (attributes, inventory, etc.)
    IHealthBonus[] bonusComponents;
    void Awake()
    {
        bonusComponents = GetComponentsInChildren<IHealthBonus>();
    }

    // calculate max
    public override int max
    {
        get
        {
            int bonus = bonusComponents.Sum(b => b.GetHealthBonus(baseHealth));
            return baseHealth + bonus;
        }
    }

    public override int recoveryPerTick
    {
        get
        {
            int bonus = bonusComponents.Sum(b => b.GetHealthRecoveryBonus());
            return baseRecoveryPerTick + bonus;
        }
    }
}