using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerStart : MonoBehaviour
{
    [SerializeField]
    private uint id = 0;

    public uint ID { get { return id; } }

    public void Spawn(PlayerController controller)
    {
        CharacterController characterController = controller.gameObject.GetComponent<CharacterController>();
        if (characterController != null)
            characterController.enabled = false;

        controller.gameObject.transform.position = transform.position;
        controller.gameObject.transform.rotation = transform.rotation;

        if (characterController != null)
            characterController.enabled = true;
    }
}
