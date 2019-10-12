using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Data;
using Mono.Data.Sqlite;
using System.IO;

public class TalkUIController : MonoBehaviour
{
    private struct Answer
    {
        private string text;
        private int toId;

        public string Text { get { return text; } }
        public int ToID { get { return toId; } }

        public Answer (string text, int toId)
        {
            this.text = text;
            this.toId = toId;
        }
    }

    public GameObject conversationPanel;
    public GameObject answersPanel;
    private IDbConnection connection;

    public TalkInteractable Interactable { get; set; }

    public void Initialize()
    {
        string connectionPath = "URI=file:" + Application.persistentDataPath + "/Talk.db";
        connection = new SqliteConnection(connectionPath);
        connection.Open();
    }

    private string SearchConversation (int id)
    {
        IDbCommand cmnd_read = connection.CreateCommand();
        IDataReader reader;
        string query = "SELECT * FROM Conversations";
        cmnd_read.CommandText = query;
        reader = cmnd_read.ExecuteReader();
        while (reader.Read())
        {
            Debug.Log("id: " + reader[0].ToString());
            Debug.Log("val: " + reader[1].ToString());
        }

        return reader[3] as string;
    }

    private List<Answer> SearchAnswers (int id)
    {
        IDbCommand cmnd_read = connection.CreateCommand();
        IDataReader reader;
        string query = "SELECT * FROM Conversations";
        cmnd_read.CommandText = query;
        reader = cmnd_read.ExecuteReader();

        List<Answer> answers = new List<Answer>();

        while (reader.Read())
        {
            answers.Add(new Answer(reader[0].ToString(), id));
        }

        return answers;
    }

    public void Close ()
    {
        connection.Close();
    }

    public void SwitchToAnswers ()
    {
        if (conversationPanel != null)
            conversationPanel.SetActive(false);

        if(answersPanel != null)
            answersPanel.SetActive(true);
    }

    public void SwitchToConversation ()
    {
        if (conversationPanel != null)
            conversationPanel.SetActive(true);

        if (answersPanel != null)
            answersPanel.SetActive(false);
    }
}
