using UnityEngine;
using UnityEngine.SceneManagement;

public class EntryLevelInstance : LevelInstance
{
    [SerializeField]
    private string sceneToLoad = "Showcase";

    protected override void Awake()
    {
        base.Awake();
        SceneManager.LoadScene(sceneToLoad);
    }
}
