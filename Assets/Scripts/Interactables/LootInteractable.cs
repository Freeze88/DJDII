using UnityEngine;

sealed public class LootInteractable : Interactable
{
    [SerializeField]
    private Item[] items = new Item[0];
    private Inventory inventory;

    public Inventory Inventory { get { return inventory; } }

    private void Awake()
    {
        inventory = new Inventory();
        foreach (Item item in items)
            inventory.Add(item);
    }

    protected override void OnInteract (PlayerController controller)
    {
        if (Locked && controller.Inventory.Contains("Bobby Pin"))
            GameInstance.HUD.EnableLockPick(true, this, controller);
        else if (!Locked)
            GameInstance.HUD.EnableObjectInventory(this, controller);
    }
}
