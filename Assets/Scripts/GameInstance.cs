using UnityEngine;

public class GameInstance : MonoBehaviour
{
    public static GameInstance Singleton    { get; private set; }
    public static HUD HUD                   { get; private set; }
    public static GameState GameState       { get; private set; }
    public int ToID                         { get; set; }

    private void Awake()
    {
        if (Singleton != null)
        {
            DestroyImmediate(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);

        Singleton   = this;
        HUD         = GetComponentInChildren<HUD>();
        GameState   = GetComponentInChildren<GameState>();

        HUD.Initialize();

        GameState.OnPausedChanged += (GameState sender) =>
        {
            if (sender.Paused)
                SetMouseCursorState(true, CursorLockMode.None);
            else
                SetMouseCursorState(false, CursorLockMode.Locked);
        };
    }

    public void SetMouseCursorState(bool visible, CursorLockMode lockMode)
    {
        Cursor.visible = visible;
        Cursor.lockState = lockMode;
    }
}
