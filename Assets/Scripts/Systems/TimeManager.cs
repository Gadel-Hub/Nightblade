using System.Collections;
using TMPro;
using UnityEngine;

public class TimeManager : MonoBehaviour
{
    public GameStateManager StateManager;
    public float timePassed { get; private set; }
    public bool IsRunning => isRunning;
    public bool HasFinishedRun { get; private set; }

    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI countdownText;
    public GameObject pauseButton;
    public GameObject resumeButton;

    private bool isRunning;
    private bool hasStartedRun;
    private Coroutine countdown;

    private void Start()
    {
        UpdateTimer();
        SetCountdownVisible(false);
    }

    private void Update()
    {
        if (!isRunning) return;

        timePassed += Time.deltaTime;
        UpdateTimer();
    }

    public void StartRun()
    {
        if (countdown != null) StopCoroutine(countdown);
        Time.timeScale = 0f;
        isRunning = false;
        timePassed = 0f;
        hasStartedRun = true;
        HasFinishedRun = false;
        UpdateTimer();
        SetPauseControlsVisible(false);
        SetGameState(GameState.Paused);
        countdown = StartCoroutine(RunCountdown());
    }

    public void StartRunImmediately()
    {
        if (countdown != null) StopCoroutine(countdown);
        countdown = null;
        SetCountdownVisible(false);
        Time.timeScale = 1f;
        isRunning = true;
        timePassed = 0f;
        hasStartedRun = true;
        HasFinishedRun = false;
        UpdateTimer();
        SetPauseControlsVisible(true);
        SetGameState(GameState.Playing);
    }

    public void Pause()
    {
        if (!isRunning) return;

        isRunning = false;
        Time.timeScale = 0f;
        SetPauseControlsVisible(true);
        SetGameState(GameState.Paused);
    }

    public void Resume()
    {
        if (isRunning || countdown != null || !hasStartedRun) return;

        Time.timeScale = 0f;
        SetPauseControlsVisible(false);
        countdown = StartCoroutine(RunCountdown());
    }

    public void FinishRun()
    {
        if (countdown != null)
        {
            StopCoroutine(countdown);
            countdown = null;
        }

        isRunning = false;
        Time.timeScale = 1f;
        SetCountdownVisible(false);
        SetPauseControlsVisible(false);
        SetGameState(GameState.GameOver);
        if (hasStartedRun)
        {
            HasFinishedRun = true;
            hasStartedRun = false;
        }
        UpdateTimer();
    }

    public void StopTimer() => FinishRun();

    public void ResetTime() => StartRun();

    public static string FormatTime(float seconds)
    {
        int totalMilliseconds = Mathf.Max(0, Mathf.FloorToInt(seconds * 1000f));
        int minutes = totalMilliseconds / 60000;
        int remainingMilliseconds = totalMilliseconds % 60000;
        int wholeSeconds = remainingMilliseconds / 1000;
        int milliseconds = remainingMilliseconds % 1000;
        return string.Format("{0:00}:{1:00}.{2:000}", minutes, wholeSeconds, milliseconds);
    }

    private IEnumerator RunCountdown()
    {
        isRunning = false;
        SetCountdownVisible(true);

        for (int i = 3; i > 0; i--)
        {
            if (countdownText != null) countdownText.text = i.ToString();
            yield return new WaitForSecondsRealtime(1f);
        }

        if (countdownText != null) countdownText.text = "GO!";
        yield return new WaitForSecondsRealtime(0.5f);

        SetCountdownVisible(false);
        Time.timeScale = 1f;
        isRunning = true;
        SetPauseControlsVisible(true);
        SetGameState(GameState.Playing);
        countdown = null;
    }

    private void UpdateTimer()
    {
        if (timerText != null) timerText.text = "Time: " + FormatTime(timePassed);
    }

    private void SetCountdownVisible(bool visible)
    {
        if (countdownText != null) countdownText.gameObject.SetActive(visible);
    }

    private void SetPauseControlsVisible(bool visible)
    {
        if (pauseButton != null) pauseButton.SetActive(visible && isRunning);
        if (resumeButton != null) resumeButton.SetActive(visible && !isRunning);
    }

    private void SetGameState(GameState state)
    {
        if (StateManager == null) return;

        if (state == GameState.Playing) StateManager.StartGame();
        else if (state == GameState.Paused) StateManager.Paused();
        else StateManager.GameOver();
    }
}
