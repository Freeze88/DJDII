using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StartPlacer : MonoBehaviour
{
    private void Start()
    {
        PlayerStart[] playerStartPositions = LevelInstance.Singleton.StartPositions;
        PlayerStart startPosition = null;

        foreach (PlayerStart startPos in playerStartPositions)
            if (startPos.ID == GameInstance.Singleton.ToID)
            {
                startPosition = startPos;
                break;
            }

        if (startPosition == null)
        {
            Debug.LogWarning("No Player Start Position for " + gameObject.name);
            return;
        }

        startPosition.Spawn(gameObject.GetComponent<PlayerController>());
    }
}
