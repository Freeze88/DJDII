using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class QuestUIController : MonoBehaviour
{
    public GameObject panel;
    public TextMeshProUGUI titleLabel;
    public TextMeshProUGUI descriptionLabel; 
    private float timer = 0f;

    private void Awake()
    {
        gameObject.transform.position = new Vector3(-100, gameObject.transform.position.y, 0f);
    }

    private void Start()
    {
        GameInstance.GameState.QuestController.OnQuestAdded += OnQuestAdded;
        GameInstance.GameState.OnPausedChanged += (GameState sender) =>
        {
            if (sender.Paused)
                panel.SetActive(false);
            else
                panel.SetActive(true);
        };
    }

    private void OnQuestAdded(QuestController sender, Quest quest)
    {
        timer = 15f;
        if (titleLabel != null)
            titleLabel.text = quest.name;

        if (descriptionLabel != null)
            descriptionLabel.text = quest.description;
    }

    private void Update()
    {
        timer = Mathf.Max(timer - Time.deltaTime, 0);

        if (timer == 0)
            gameObject.transform.position = Vector3.Lerp(gameObject.transform.position, new Vector3(-100, gameObject.transform.position.y, 0f), Time.unscaledDeltaTime);
        else
            gameObject.transform.position = Vector3.Lerp(gameObject.transform.position, new Vector3(0, gameObject.transform.position.y, 0f), Time.deltaTime);
    }
}
