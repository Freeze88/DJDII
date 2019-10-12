using UnityEngine;

public class NewsInteractable : Interactable
{
    protected override void OnInteract(PlayerController controller)
    {
        GameInstance.HUD.EnableDigitalNewsPaper(true);
    }
}
