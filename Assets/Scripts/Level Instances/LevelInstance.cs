using System.Collections;
using System.Collections.Generic;
using UnityEngine;

abstract public class LevelInstance : MonoBehaviour
{
    [SerializeField]
    private PlayerStart[] startPositions = new PlayerStart[0];
     
    public static LevelInstance Singleton { get; private set; }
    public PlayerStart[] StartPositions { get { return startPositions; } }

    protected virtual void Awake()
    {
        Singleton = this;
    }
}


