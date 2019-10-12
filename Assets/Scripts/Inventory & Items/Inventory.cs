using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
sealed public class Inventory
{
    [SerializeField]
    private List<Item> items = new List<Item>();

    public List<Item> Items { get { return items; } }

    public void Add (Item item)
    {
        Items.Add(item);
    }

    public bool Contains (string name)
    {
        string auxName = name.ToLower();
        foreach (Item i in Items)
            if (auxName.Equals (i.name.ToLower()))
                return true;

        return false;
    }
}