using UnityEngine;
public class InstigatedObject : object
{
    public Actor Instigator { get; }

    public InstigatedObject(Actor instigator)
    {
        Instigator = instigator;
    }
}

