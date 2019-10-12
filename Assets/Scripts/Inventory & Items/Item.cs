using UnityEngine;

[System.Serializable]
[CreateAssetMenu(fileName = "New Item", menuName = "Item")]
public class Item : ScriptableObject
{
    public Sprite       icon;
    public new string   name;
    public string       description;
    public float        weight;
    public float        cost;
}
