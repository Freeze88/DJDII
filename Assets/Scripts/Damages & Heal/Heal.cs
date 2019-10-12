using UnityEngine;


public abstract class Heal : InstigatedObject
{
    public sealed class Point : Heal
    {
        public Point(Actor instigator, float baseHeal)
            : base(instigator)
        {
            BaseHeal = baseHeal;
        }
    }

    public sealed class Radial : Heal
    {
        public Radial(Actor instigator, Vector3 origin, float area)
            : base(instigator)
        {
            Origin = origin;
            Area = area;
        }

        public override float Get(Actor actor)
        {
            float distance = (Origin - actor.transform.position).magnitude;
            if (distance > Area)
                return 0;

            float influence = 1 - (distance / Area);
            return base.Get(actor) * influence;
        }

        public Vector3 Origin { get; }
        public float Area { get; }
    }

    public Heal(Actor instigator)
        : base(instigator)
    {
    }

    public virtual float Get(Actor actor)
    {
        return BaseHeal;
    }

    public virtual float BaseHeal { get; set; } = 10;
}
