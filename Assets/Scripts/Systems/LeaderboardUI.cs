using TMPro;
using UnityEngine;

public class LeaderboardUI : MonoBehaviour
{
    [SerializeField]
    private TMP_InputField inputName;
    [SerializeField]
    private Leaderboard leaderboard;

    public TextMeshProUGUI winnerText;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI timeText;

    public void SaveScore()
    {
        string name = inputName.text;
        if (name == "")
        {
            return;
        }
        TimeManager timeManager = FindFirstObjectByType<TimeManager>();

        if (leaderboard.AddPlayer(name, timeManager.timePassed))
        {
            ShowLeaderboard();
        }
    }
    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return) && inputName.isFocused)
        {
            SaveScore();
        }
    }
    public void ShowLeaderboard()
    {
        winnerText.text = "";
        nameText.text = "";
        timeText.text = "";

        for (int i = 0; i < leaderboard.players.Count; i++)
        {
            winnerText.text += (i + 1).ToString() + "\n\n";
            nameText.text += leaderboard.players[i].name + "\n\n";
            timeText.text += leaderboard.players[i].time.ToString("F2") + "\n\n";

        }

    }

}
