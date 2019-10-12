using UnityEngine;

public abstract class Damage : InstigatedObject
{
    public sealed class Point : Damage
    {
        public Point(Actor instigator, bool trueDamage, uint baseDamage)
            : base(instigator, trueDamage)
        {
            BaseDamage = baseDamage;
        }
    }

    public sealed class Radial : Damage
    {
        public Radial(Actor instigator, Vector3 origin, float area, bool trueDamage, uint baseDamage)
            : base(instigator, trueDamage)
        {
            Origin = origin;
            Area = area;
            BaseDamage = baseDamage;
        }

        public override float Get(Actor actor)
        {
            float distance = (Origin - actor.transform.position).magnitude;
            if (distance > Area)
                return 0;

            float influence = 1 - (distance / Area);

            if (!TrueDamage && actor is Controller character)
                return base.Get(actor) * influence * (100 / (100 + character.Armour.Value * 2));
            else
                return base.Get(actor) * influence;
        }

        public Vector3 Origin { get; }
        public float Area { get; }
    }

    public Damage(Actor instigator, bool trueDamage)
        : base(instigator)
    {
        TrueDamage = trueDamage;
    }

    public virtual float Get(Actor actor)
    {
        return BaseDamage;
    }

    public virtual float BaseDamage { get; set; } = 10;
    public bool TrueDamage { get; }
}