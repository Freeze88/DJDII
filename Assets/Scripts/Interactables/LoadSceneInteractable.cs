using UnityEngine;
using UnityEngine.SceneManagement;

sealed public class LoadSceneInteractable : Interactable
{
    [SerializeField]
    private int toId = 0;
    [SerializeField]
    private string sceneName = "Default";

    protected override void OnInteract(PlayerController controller)
    {
        GameInstance.Singleton.ToID = toId;
        SceneManager.LoadScene(sceneName);
    }
}
