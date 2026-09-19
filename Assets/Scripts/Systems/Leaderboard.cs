using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
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
    private const int MaxEntries = 10;
    private const string PlayerPrefsKey = "SpeedrunLeaderboard";

    [SerializeField] private List<PlayerScore> players = new List<PlayerScore>();

    public IReadOnlyList<PlayerScore> Players => players;

    [Serializable]
    private class SavedScores
    {
        public List<PlayerScore> players = new List<PlayerScore>();
    }

    private void Awake()
    {
        Load();
    }

    public bool AddPlayer(string name, float time)
    {
        if (string.IsNullOrWhiteSpace(name) || float.IsNaN(time) || float.IsInfinity(time) || time < 0f)
            return false;

        PlayerScore addedScore = new PlayerScore(name.Trim(), time);
        players.Add(addedScore);
        SortLeaderboard();
        if (players.Count > MaxEntries)
            players.RemoveRange(MaxEntries, players.Count - MaxEntries);

        Save();
        return players.Contains(addedScore);
    }

    public void AddTestScores()
    {
        for (int i = 0; i < 11; i++)
            AddPlayer("Test " + (i + 1).ToString("00"), 21f - i);
    }

    public void ClearLeaderboard()
    {
        players.Clear();
        Save();
    }

    public void Load()
    {
        if (!PlayerPrefs.HasKey(PlayerPrefsKey))
        {
            players.Clear();
            return;
        }

        SavedScores saved = JsonUtility.FromJson<SavedScores>(PlayerPrefs.GetString(PlayerPrefsKey));
        players = saved != null && saved.players != null ? saved.players : new List<PlayerScore>();
        SortLeaderboard();
        if (players.Count > MaxEntries)
            players.RemoveRange(MaxEntries, players.Count - MaxEntries);
    }

    private void Save()
    {
        SavedScores saved = new SavedScores { players = players };
        PlayerPrefs.SetString(PlayerPrefsKey, JsonUtility.ToJson(saved));
        PlayerPrefs.Save();
    }

    private void SortLeaderboard()
    {
        players.Sort((a, b) => a.time.CompareTo(b.time));
    }
}
