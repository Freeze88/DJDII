using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
[CreateAssetMenu(fileName = "New Quest", menuName = "Quests/New Quest")]
public class Quest : ScriptableObject
{
    public delegate void EventHandler(Quest sender);
    public delegate void SubQuestEventHandler(Quest sender, Quest subQuest);

    public event EventHandler OnCompleted;
    public event SubQuestEventHandler OnSubQuestAdded;

    public new string       name;
    public string           description;
    public List<Quest>      subQuests;
    public bool             completed;
    public bool             hide;
    public Vector3[]        position;

    public void Complete()
    {
        if (completed)
            return;

        completed = true;

        OnCompleted?.Invoke(this);
    }

    public void Add(Quest quest)
    {
        if (subQuests.Contains(quest))
            return;

        subQuests.Add(quest);

        OnSubQuestAdded?.Invoke(this, quest);
    }
}
