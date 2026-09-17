using UnityEngine;
using TMPro;
using System.Collections;
public class TimeManager : MonoBehaviour
{
    public GameStateManager StateManager;
    public float timePassed {  get; private set; }
    [SerializeField]
    private TextMeshProUGUI timerText;
    [SerializeField] 
    private TextMeshProUGUI countdownText;
    public GameObject pauseButton;
    public GameObject resumeButton;
    private bool isRunning = true;
    void Start()
    {
        UpdateTimer();
    }
    void Update()
    {
        if (isRunning)
        {
            timePassed += Time.deltaTime;   
            UpdateTimer();
        }
    }
    public void Pause()
    {
        Time.timeScale = 0;
        pauseButton.SetActive(false);
        resumeButton.SetActive(true);
        isRunning = false;
        StateManager.Paused();
    }
    public void Resume()
    {
        pauseButton.SetActive(true);
        resumeButton.SetActive(false);
        StartCoroutine(CountdownResume());
        
    }

    public void StopTimer()
    {
        isRunning = false;
    }
    private void UpdateTimer()
    {
        int minutes = Mathf.FloorToInt(timePassed / 60);
        int seconds = Mathf.FloorToInt(timePassed % 60);
        int ms = Mathf.FloorToInt((timePassed*1000)%1000);

        timerText.text =string.Format("Time: {0:00}:{1:00}:{2:000}", minutes, seconds,ms);
    }

    public void ResetTime()
    {
        Time.timeScale = 1;
        timePassed = 0;
        isRunning = false;
        StartCoroutine(CountdownStart());
        pauseButton.SetActive(true);
        resumeButton.SetActive(false);
    }

    IEnumerator CountdownStart()
    {
        isRunning=false;
        countdownText.gameObject.SetActive(true);

        for (int i = 3;i>0; i--)
        {
            countdownText.text=i.ToString();
            yield return new WaitForSecondsRealtime(1);
        }
        countdownText.text = "Start!";

        yield return new WaitForSecondsRealtime(.5f);
        countdownText.gameObject.SetActive(false);
        isRunning = true;
        StateManager.StartGame();
        
    }

    IEnumerator CountdownResume()
    {
        isRunning = false;
        StateManager.Paused();
        countdownText.gameObject.SetActive(true);

        for (int i = 3; i > 0; i--)
        {
            countdownText.text = i.ToString();
            yield return new WaitForSecondsRealtime(1);
        }
        countdownText.text = "GO!";

        yield return new WaitForSecondsRealtime(.5f);
        countdownText.gameObject.SetActive(false);
        Time.timeScale = 1;
        isRunning = true;
        StateManager.ResumeGame();
        pauseButton.SetActive(true);
        resumeButton.SetActive(false);
    }
}
