using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
sealed public class QuestController : object
{
    public delegate void EventHandler(QuestController sender);
    public delegate void QuestEventHandler(QuestController sender, Quest quest);

    public event QuestEventHandler  OnQuestAdded,
                                    OnQuestRemoved,
                                    OnQuestCompleted;

    [SerializeField]
    private List<Quest> quests = new List<Quest>();

    public List<Quest> Quest { get { return quests; } }

    public void Add (Quest quest)
    {
        if (quests.Contains(quest))
            return;

        quests.Add(quest);

        quest.OnCompleted += OnQuestHasBeenCompleted;
        quest.OnSubQuestAdded += OnSubQuestAdded;

        OnQuestAdded?.Invoke(this, quest);
    }

    private void OnSubQuestAdded (Quest sender, Quest subQuest)
    {

    }

    private void OnQuestHasBeenCompleted(Quest sender)
    {
        OnQuestCompleted?.Invoke(this, sender);
    }

    public void Remove(Quest quest)
    {
        if (quests.Remove(quest))
        {
            OnQuestRemoved?.Invoke(this, quest);
            quest.OnCompleted       -= OnQuestHasBeenCompleted;
            quest.OnSubQuestAdded   -= OnSubQuestAdded;
        }
    }

    public void CompleteQuest (string name)
    {
        string auxName = name.ToLower();

        foreach (Quest q in quests)
        {
            bool found = false;
            foreach (Quest sub in q.subQuests)
            {
                found = auxName.Equals(sub.name.ToLower());
                if (found)
                {
                    sub.Complete();
                    break;
                }
            }

            if (found)
                break;

            if (auxName.Equals(q.name.ToLower()))
            {
                q.Complete();
                break;
            }
        }
    }
}
