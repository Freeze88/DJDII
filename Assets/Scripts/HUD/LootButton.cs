using UnityEngine;
using TMPro;

public class LootButton : MonoBehaviour
{
    public delegate void EventHandler(LootButton sender);

    public event EventHandler OnClicked;
    public event EventHandler OnHoverEnter,
                              OnHoverExit;

    public TextMeshProUGUI nameLabel;
    public TextMeshProUGUI quantityLabel;

    public Item Item { get; private set; }

    public void Initialize (Item item, int quantity)
    {
        Item = item;

        if (nameLabel != null)
            nameLabel.text = item.name;

        if (quantityLabel != null)
            quantityLabel.text = "x" + quantity.ToString();
    }

    public void HoverEnter ()
    {
        OnHoverEnter?.Invoke(this);
    }

    public void HoverExit ()
    {
        OnHoverExit?.Invoke(this);
    }

    public void Click ()
    {
        OnClicked?.Invoke(this);
    }
}
