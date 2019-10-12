using UnityEngine;

sealed public class GameState : MonoBehaviour
{
    public delegate void EventHandler(GameState sender);

    public event EventHandler OnPausedChanged;

    [Header("Quest System")]
    [SerializeField]
    private QuestController questController = new QuestController();
    private bool paused = false;

    public QuestController QuestController { get { return questController; } }

    public bool Paused
    {
        get { return paused; }

        set
        {
            paused = value;

            OnPausedChanged(this);

            if (paused)
                Time.timeScale = 0f;
            else
                Time.timeScale = 1f;
        }
    }
}
