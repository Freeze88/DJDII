using UnityEngine;
using UnityEngine.Playables;

sealed public class CyberCityLevelInstance : LevelInstance
{
    [Header("Cinametic")]
    [SerializeField]
    private bool play = true;
    public PlayableDirector timeline;

    private void Start()
    {
        if (!play)
            return;

        timeline.gameObject.SetActive(true);
        timeline.Play();
    }
}
