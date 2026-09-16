using TMPro;
using Unity.VisualScripting;
using UnityEngine;
public class ScoreManager : MonoBehaviour
{
    public int score {  get; private set; }
    [SerializeField] 
    private TextMeshProUGUI scoreText;
    void Start()
    {
       UpdateScoreUI(); 
    }
    public void ResetScore()
    {
        score = 0;
        UpdateScoreUI();
    }
    public void UpdateScore(int amount)
    {
        score += amount;
        UpdateScoreUI();
    }
    private void UpdateScoreUI()
    {
        scoreText.text = "Score: " + score;
    }
}
