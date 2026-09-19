using System.Text;
using TMPro;
using UnityEngine;

public class LeaderboardUI : MonoBehaviour
{
    [SerializeField] private TMP_InputField inputName;
    [SerializeField] private Leaderboard leaderboard;
    [SerializeField] private TimeManager timeManager;
    [SerializeField] private TMP_Text leaderboardText;
    private string testPlayerName = "Runner";

    private void Start()
    {
        ShowLeaderboard();
    }

    public void SaveScore()
    {
        if (leaderboard == null || timeManager == null || !timeManager.HasFinishedRun)
            return;

        string playerName = inputName != null ? inputName.text : testPlayerName;
        if (leaderboard.AddPlayer(playerName, timeManager.timePassed))
            ShowLeaderboard();
    }

    public void ShowLeaderboard()
    {
        if (leaderboard == null || leaderboardText == null) return;

        StringBuilder display = new StringBuilder("Leaderboard\n");
        for (int i = 0; i < leaderboard.Players.Count; i++)
        {
            PlayerScore score = leaderboard.Players[i];
            display.Append(i + 1).Append(". ")
                .Append(score.name).Append("  ")
                .AppendLine(TimeManager.FormatTime(score.time));
        }

        if (leaderboard.Players.Count == 0)
            display.Append("No runs yet");

        leaderboardText.text = display.ToString();
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private void OnGUI()
    {
        if (leaderboard == null || timeManager == null) return;

        GUILayout.BeginArea(new Rect(8f, 8f, 250f, 430f), GUI.skin.box);
        GUILayout.Label("Speedrun Test Panel");
        GUILayout.Label("Time: " + TimeManager.FormatTime(timeManager.timePassed));

        if (!timeManager.IsRunning)
        {
            if (GUILayout.Button("Start Run")) timeManager.StartRun();
        }
        else
        {
            if (GUILayout.Button("Pause")) timeManager.Pause();
        }

        if (GUILayout.Button("Resume")) timeManager.Resume();
        if (GUILayout.Button("Finish Run")) timeManager.FinishRun();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Name", GUILayout.Width(45f));
        testPlayerName = GUILayout.TextField(testPlayerName);
        GUILayout.EndHorizontal();
        if (GUILayout.Button("Save Finished Run")) SaveScore();
        if (GUILayout.Button("Add 11 Sorting Tests")) leaderboard.AddTestScores();
        if (GUILayout.Button("Clear Saved Times")) leaderboard.ClearLeaderboard();

        GUILayout.Space(6f);
        foreach (string line in BuildLeaderboardLines()) GUILayout.Label(line);
        GUILayout.EndArea();
    }

    private string[] BuildLeaderboardLines()
    {
        string[] lines = new string[leaderboard.Players.Count == 0 ? 1 : leaderboard.Players.Count];
        if (leaderboard.Players.Count == 0)
        {
            lines[0] = "No runs yet";
            return lines;
        }

        for (int i = 0; i < leaderboard.Players.Count; i++)
        {
            PlayerScore score = leaderboard.Players[i];
            lines[i] = string.Format("{0}. {1}  {2}", i + 1, score.name, TimeManager.FormatTime(score.time));
        }
        return lines;
    }
#endif
}
