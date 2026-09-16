using TMPro;
using UnityEngine;

public class LeaderboardUI : MonoBehaviour
{
    [SerializeField]
    private TMP_InputField inputName;
    [SerializeField]
    private Leaderboard leaderboard;
   
    public void SaveScore()
    {
        string name = inputName.text;
        if (name=="")
        {
            return;
        }
        TimeManager timeManager = FindFirstObjectByType<TimeManager>();

        if (leaderboard.AddPlayer(name,timeManager.timePassed))
        {
            ShowLeaderboard();
        }
    }

    public void ShowLeaderboard()
    {

    }

}
