using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Actor : MonoBehaviour
{
    public delegate void EventHandler(Actor sender);

    [Header("Actor")]
    [SerializeField]
    protected bool canBeDamaged   = true;
    [SerializeField]
    protected bool canBeHealed    = true;
    [SerializeField]
    protected bool canBeRevived   = true;

    public bool ApplyDamage(Damage damage)
    {
        if (canBeDamaged)
            return OnApplyDamage(damage);

        return false;
    }

    protected abstract bool OnApplyDamage(Damage damage);

    public bool ApplyHeal(Heal heal)
    {
        if (canBeHealed)
            return OnApplyHeal(heal);

        return false;
    }

    protected abstract bool OnApplyHeal(Heal heal);
}
