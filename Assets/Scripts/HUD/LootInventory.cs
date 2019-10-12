using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

public class LootInventory : MonoBehaviour
{
    public Text objectLabel;
    private LootInteractable interactable;
    private Inventory playerInventory;
    public Scrollbar playerInventoryListScrollBar;
    public Scrollbar lootableInventoryListScrollBar;
    public GameObject lootButton;
    public GameObject lootContent;
    public GameObject playerContent;
    public GameObject lootQuantityPanel;
    public Image itemIconImage;
    public TextMeshProUGUI itemDescriptionLabel;
    [Header("Audio")]
    public AudioSource takeAll;

    private bool active = true;

    public LootInteractable Interactable
    {
        get { return interactable; }

        set
        {
            interactable = value;

            if (objectLabel == null)
                return;

            objectLabel.text = interactable.Message;
            InitializeItems(lootContent.transform, interactable.Inventory);
        }
    }
    public Inventory PlayerInventory
    {
        get { return playerInventory; }

        set
        {
            playerInventory = value;

            if (playerInventory != null)
                InitializeItems(playerContent.transform, playerInventory);
        }
    }

    public void Initialize ()
    {
        if (lootableInventoryListScrollBar != null)
            lootableInventoryListScrollBar.value = 1;

        if (playerInventoryListScrollBar != null)
            playerInventoryListScrollBar.value = 1;
    }

    public void Close ()
    {
        itemIconImage.gameObject.SetActive(false);
        itemDescriptionLabel.text = "";

        GameInstance.GameState.Paused = false;
        gameObject.SetActive(false);
    }

    public void TakeAll ()
    {
        LootQuantityPanelController controller = lootQuantityPanel.GetComponent<LootQuantityPanelController>();
        if (controller != null)
        {
           while (lootContent.transform.childCount > 0)
            {
                LootButton button = lootContent.transform.GetChild(0).gameObject.GetComponent<LootButton>();
                if (button != null)
                {
                    controller.Initialize(button, interactable.Inventory, playerInventory);
                    if (controller.Take(uint.Parse(button.quantityLabel.text.Replace("x", ""))))
                        takeAll.Play();
                }
            }
        }

        Close();
    }

    private void Start()
    {
        if (lootQuantityPanel != null)
        {
            LootQuantityPanelController controller = lootQuantityPanel.GetComponent<LootQuantityPanelController>();
            if (controller != null)
                controller.OnClose += (LootQuantityPanelController sender) =>
                {
                    float playerScrollbarValue = playerInventoryListScrollBar != null ? playerInventoryListScrollBar.value : 0;
                    float lootableScrollbarValue = lootableInventoryListScrollBar != null ? lootableInventoryListScrollBar.value : 0;

                    if (playerInventory != null)
                    {
                        InitializeItems(playerContent.transform, playerInventory);

                        if (playerInventoryListScrollBar != null)
                            playerInventoryListScrollBar.value = playerScrollbarValue;
                    }

                    InitializeItems(lootContent.transform, interactable.Inventory);

                    if (lootableInventoryListScrollBar != null)
                        lootableInventoryListScrollBar.value = lootableScrollbarValue;

                    active = true;
                };
        }
    }

    private void InitializeItems(Transform contentHolder, Inventory inventory)
    {
        while (contentHolder.childCount > 0)
            DestroyImmediate(contentHolder.GetChild(0).gameObject);

        List<Item> items = inventory.Items.Select(o => o).Distinct().ToList();
        for (int i = 0; i < items.Count; i++)
        {
            GameObject go = Instantiate(lootButton, contentHolder);
            LootButton item = go.GetComponent<LootButton>();
            if (item != null)
            {
                item.Initialize(items[i], inventory.Items.Where(x => x.name.Contains(items[i].name)).Count());

                item.OnHoverEnter += OnItemHoverEnter;
                item.OnHoverExit += OnItemHoverExit;
                item.OnClicked += (LootButton sender) =>
                {
                    uint count = uint.Parse(sender.quantityLabel.text.Replace("x", ""));
                    LootQuantityPanelController controller = lootQuantityPanel.GetComponent<LootQuantityPanelController>();
                    if (controller != null)
                    {
                        controller.Initialize(sender, inventory == playerInventory ? playerInventory : interactable.Inventory, inventory == playerInventory ? interactable.Inventory : playerInventory);

                        if (count > 1)
                        {
                            if (lootQuantityPanel != null)
                            {
                                active = false;
                                lootQuantityPanel.gameObject.SetActive(true);
                            }
                        }
                        else
                            controller.Take(1);
                    }
                };
            }
        }
    }

    private void OnItemHoverEnter (LootButton sender)
    {
        if (itemIconImage != null)
        {
            itemIconImage.gameObject.SetActive(true);
            itemIconImage.sprite = sender.Item.icon;
        }

        if (itemDescriptionLabel != null)
            itemDescriptionLabel.text = sender.Item.description;
    }

    private void OnItemHoverExit(LootButton sender)
    {
        if (itemIconImage != null)
            itemIconImage.gameObject.SetActive(false);

        if (itemDescriptionLabel == null)
            return;

        itemDescriptionLabel.text = "";
    }

    private void Update()
    {
        if (!active)
            return;

        if (Input.GetKeyDown(KeyCode.C))
            Close();

        if (Input.GetKeyDown(KeyCode.X))
            TakeAll();
    }
}
