using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class PlayerScore
{
    public string name;
    public float time;

    public PlayerScore(string name, float time)
    {
        this.name = name;
        this.time = time;
    }
}
public class Leaderboard : MonoBehaviour
{
    public List<PlayerScore> players = new List<PlayerScore>();

    private void Start()
    {
        LoadPlayers();
    }

    public bool AddPlayer(string name, float time)
    {
        foreach (PlayerScore player in players)
        {
            if (player.name == name)
            {
                return false;
            }
        }
        players.Add(new PlayerScore(name, time));
        SortLeaderboard();
        SavePlayers();
        return true;
    }


    private void SortLeaderboard()
    {
        players.Sort((a, b) => {
            return a.time.CompareTo(b.time);
        });
    }

    private void SavePlayers()
    {
        PlayerPrefs.SetInt("PlayerCount", players.Count);
        for (int i = 0; i < players.Count; i++)
        {
            PlayerPrefs.SetString("PlayerName_" + i, players[i].name);
            PlayerPrefs.SetFloat("PlayerTime_" + i, players[i].time);
        }

        PlayerPrefs.Save();
    }
    private void LoadPlayers()
    {
        players.Clear();

        int playerCount = PlayerPrefs.GetInt("PlayerCount", 0);

        for (int i = 0; i < playerCount; i++)
        {
            string name = PlayerPrefs.GetString("PlayerName_" + i, "");
            float time = PlayerPrefs.GetFloat("PlayerTime_" + i, 0f);

            if (name != "")
            {
                players.Add(new PlayerScore(name, time));
            }

        }
        SortLeaderboard();
    }
}
