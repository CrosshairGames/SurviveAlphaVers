using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using Mirror;

// inventory, attributes etc. can influence max health
public interface ICombatBonus
{
    int GetDamageBonus();
    int GetDefenseBonus();
}

[Serializable] public class UnityEventGameObjectInt : UnityEvent<GameObject, int> {}

public class Combat : NetworkBehaviour
{
    // invincibility is useful for GMs etc.
    public int baseDamage;
    public int baseDefense;
    public GameObject onDamageEffect;

    // events
    public UnityEventGameObjectInt onReceivedDamage;

    // cache components that give a bonus (attributes, inventory, etc.)
    ICombatBonus[] bonusComponents;
    void Awake()
    {
        bonusComponents = GetComponentsInChildren<ICombatBonus>();
    }

    // calculate damage
    public int damage
    {
        get
        {
            return baseDamage + bonusComponents.Sum(b => b.GetDamageBonus());
        }
    }

    // calculate defense
    public int defense
    {
        get
        {
            return baseDefense + bonusComponents.Sum(b => b.GetDefenseBonus());
        }
    }

    // deal damage while acknowledging the target's defense etc.
    public void DealDamageAt(GameObject other, int amount, Vector3 hitPoint, Vector3 hitNormal, Collider hitCollider)
    {
        if (other != null)
        {
            Health otherHealth = other.GetComponent<Health>();
            Combat otherCombat = other.GetComponent<Combat>();
            if (otherHealth != null && otherCombat != null)
            {
                // not dead yet?
                if (otherHealth.current > 0)
                {
                    // extra damage on that collider? (e.g. on head)
                    DamageArea damageArea = hitCollider.GetComponent<DamageArea>();
                    float multiplier = damageArea != null ? damageArea.multiplier : 1;
                    int amountMultiplied = Mathf.RoundToInt(amount * multiplier);

                    // subtract defense (but leave at least 1 damage, otherwise
                    // it may be frustrating for weaker players)
                    int damageDealt = Mathf.Max(amountMultiplied - otherCombat.defense, 1);

                    // deal the damage
                    otherHealth.current -= damageDealt;

                    // call OnReceiveDamage event on the target
                    // -> can be used for monsters to pull aggro
                    // -> can be used by equipment to decrease durability etc.
                    otherCombat.onReceivedDamage.Invoke(gameObject, damageDealt);

                    // show effects on clients
                    otherCombat.RpcOnDamageReceived(damageDealt, hitPoint, hitNormal);
                }
            }
        }
    }

    [ClientRpc]
    public void RpcOnDamageReceived(int amount, Vector3 hitPoint, Vector3 hitNormal)
    {
        if (onDamageEffect)
            Instantiate(onDamageEffect, hitPoint, Quaternion.LookRotation(-hitNormal));
    }
}