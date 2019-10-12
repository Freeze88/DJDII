using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LootQuantityPanelController : MonoBehaviour
{
    public delegate void EventHandler(LootQuantityPanelController sender);

    public event EventHandler OnClose;

    public TextMeshProUGUI  minLabel;
    public TextMeshProUGUI  maxLabel;
    public Slider           quantitySlider;
    private Inventory       fromInventory,
                            toInventory;
    private LootButton      lootButton;
    [Header("Audio")]
    public AudioSource      takeAll;

    public void Initialize(LootButton lootButton, Inventory from, Inventory to)
    {
        this.lootButton = lootButton;

        if (minLabel != null)
            minLabel.text = "1";

        if (maxLabel != null)
            maxLabel.text = lootButton.quantityLabel.text.Replace("x", "");

        quantitySlider.minValue = 0;
        quantitySlider.maxValue = uint.Parse(maxLabel.text);
        quantitySlider.value = 1;

        fromInventory = from;
        toInventory = to;
    }

    public void OnSliderChanged ()
    {
        if (minLabel != null)
            minLabel.text = quantitySlider.value.ToString("f0");
    }

    private void Close ()
    {
        OnClose?.Invoke(this);
        gameObject.SetActive(false);
    }

    public void Accept ()
    {
        Take(uint.Parse(minLabel.text));
    }

    public void TakeAll ()
    {
        if (Take(uint.Parse(maxLabel.text)))
            takeAll.Play();
    }

    public bool Take (uint count)
    {
        string itemName = lootButton.Item.name;
        bool taken = false;

        for (int i = 0; i < count; i++)
            foreach (Item item in fromInventory.Items)
            {
                if (item.name == itemName)
                {
                    fromInventory.Items.Remove(item);
                    toInventory.Items.Add(item);
                    taken = true;
                    break;
                }
            }

        Close();
        return taken;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))
            Accept();

        if (Input.GetKeyDown(KeyCode.X))
            TakeAll();
    }
}
