using UnityEngine;

abstract public class Controller : Actor
{
    public event EventHandler   OnDeath,
                                OnHealed,
                                OnDamaged,
                                OnRevive;

    [SerializeField]
    private ScaledValue hp = new ScaledValue(1, 100);
    [SerializeField]
    private ScaledValue armour = new ScaledValue(1, 0);

    public ScaledValue Hp { get { return hp; } }
    public ScaledValue Armour { get { return armour; } }

    protected override bool OnApplyDamage (Damage damage)
    {
        bool isAlive = !Hp.IsEmpty;

        if (!isAlive)
            return false;

        hp.Subtract(damage.Get(this));

        if (isAlive && Hp.IsEmpty)
            OnDeath?.Invoke(this);
        else
            OnDamaged?.Invoke(this);

        return true;
    }

    protected override bool OnApplyHeal(Heal heal)
    {
        bool isAlive = !Hp.IsEmpty;

        if (isAlive)
        {
            hp.Add(heal.Get(this));
            OnHealed?.Invoke(this);
            return true;
        }
        else if (canBeRevived)
        {
            hp.Add(heal.Get(this));
            OnRevive?.Invoke(this);
            return true;
        }

        return false;
    }
}
