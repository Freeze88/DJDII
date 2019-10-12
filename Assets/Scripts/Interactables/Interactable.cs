using UnityEngine;
abstract public class Interactable : MonoBehaviour
{
    public delegate void EventHandler(Interactable sender);

    public event EventHandler OnUnlocked;

    [SerializeField]
    private bool canInteract = true;
    [SerializeField]
    private bool locked = false;
    [SerializeField]
    private string message = "Interact";

    public bool Locked
    {
        get { return locked; }

        set
        {
            locked = value;

            if (!locked)
                OnUnlocked?.Invoke(this);
        }
    }

    public string Message { get { return message; } }

    public void Interact (PlayerController controller)
    {
        if (!canInteract)
            return;

        OnInteract(controller);
    }

    protected abstract void OnInteract(PlayerController controller);
}