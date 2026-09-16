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
        Time.timeScale = 1;
        pauseButton.SetActive(true);
        resumeButton.SetActive(false);
        StateManager.Paused();
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

        timerText.text =string.Format("Time: {0:00}:{1:00}", minutes, seconds);
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
        isRunning = true;
        StateManager.ResumeGame();
    }
}
