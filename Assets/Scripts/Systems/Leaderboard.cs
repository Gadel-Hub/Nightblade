using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class PlayerScore
{
    public string name;
    public float time;

    public PlayerScore(string name,float time)
    {
        this.name = name;
        this.time = time;
    }
}
public class Leaderboard : MonoBehaviour
{
    public List<PlayerScore> players =new List<PlayerScore>();


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
        return true;
    }

    private void SortLeaderboard()
    {
        players.Sort((a, b) => {
            return a.time.CompareTo(b.time);
            });
    }
}
