using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class LeaderboardUI : MonoBehaviour
{
    [SerializeField] private InputField inputName;
    [SerializeField] private Leaderboard leaderboard;
    [SerializeField] private TimeManager timeManager;
    [SerializeField] private Text leaderboardText;
    [SerializeField] private bool showTestPanel = true;
    private string testPlayerName = "Koşucu";

    private void Start()
    {
        ShowLeaderboard();
    }

    public void SaveScore()
    {
        string playerName = inputName != null ? inputName.text : testPlayerName;
        SaveScore(playerName);
    }

    public bool SaveScore(string playerName)
    {
        if (leaderboard == null || timeManager == null || !timeManager.HasFinishedRun)
            return false;

        if (!leaderboard.AddPlayer(playerName, timeManager.timePassed)) return false;
        ShowLeaderboard();
        return true;
    }

    public void ShowLeaderboard()
    {
        if (leaderboard == null || leaderboardText == null) return;

        StringBuilder display = new StringBuilder();
        for (int i = 0; i < leaderboard.Players.Count; i++)
        {
            PlayerScore score = leaderboard.Players[i];
            display.Append(i + 1).Append(". ")
                .Append(score.name).Append("  ")
                .AppendLine(TimeManager.FormatTime(score.time));
        }

        if (leaderboard.Players.Count == 0)
            display.Append("Henüz kayıt yok");

        leaderboardText.text = display.ToString();
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private void OnGUI()
    {
        if (!showTestPanel || leaderboard == null || timeManager == null) return;

        GUILayout.BeginArea(new Rect(8f, 8f, 250f, 430f), GUI.skin.box);
        GUILayout.Label("Süre Test Paneli");
        GUILayout.Label("Süre: " + TimeManager.FormatTime(timeManager.timePassed));

        if (!timeManager.IsRunning)
        {
            if (GUILayout.Button("Koşuyu Başlat")) timeManager.StartRun();
        }
        else
        {
            if (GUILayout.Button("Duraklat")) timeManager.Pause();
        }

        if (GUILayout.Button("Sürdür")) timeManager.Resume();
        if (GUILayout.Button("Koşuyu Bitir")) timeManager.FinishRun();

        GUILayout.BeginHorizontal();
        GUILayout.Label("İsim", GUILayout.Width(45f));
        testPlayerName = GUILayout.TextField(testPlayerName);
        GUILayout.EndHorizontal();
        if (GUILayout.Button("Süreyi Kaydet")) SaveScore();
        if (GUILayout.Button("11 Sıralama Kaydı Ekle")) leaderboard.AddTestScores();
        if (GUILayout.Button("Kayıtlı Süreleri Temizle")) leaderboard.ClearLeaderboard();

        GUILayout.Space(6f);
        foreach (string line in BuildLeaderboardLines()) GUILayout.Label(line);
        GUILayout.EndArea();
    }

    private string[] BuildLeaderboardLines()
    {
        string[] lines = new string[leaderboard.Players.Count == 0 ? 1 : leaderboard.Players.Count];
        if (leaderboard.Players.Count == 0)
        {
            lines[0] = "Henüz kayıt yok";
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
