using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TalkInteractable : Interactable
{
    protected override void OnInteract(PlayerController controller)
    {
        GameInstance.HUD.EnableConversation(true, this);
    }
}
