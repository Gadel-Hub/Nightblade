using Nightblade;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public sealed class ProductionRunUI : MonoBehaviour
{
    private static readonly Color SelectedColor = new Color(0.55f, 0.78f, 0.45f, 0.45f);
    private static readonly Color SelectedHoverColor = new Color(0.7f, 0.95f, 0.6f, 0.6f);
    private static readonly Color HoverColor = new Color(1f, 0.82f, 0.42f, 0.35f);
    [SerializeField] private PlayerCharacter player;
    [SerializeField] private PlayerHealth health;
    [SerializeField] private TimeManager timer;
    [SerializeField] private ArenaRunManager arena;
    [SerializeField] private LeaderboardUI leaderboardUI;

    [Header("Panels")]
    [SerializeField] private GameObject selectionPanel;
    [SerializeField] private GameObject gameplayPanel;
    [SerializeField] private GameObject resultsPanel;

    [Header("Character selection")]
    [SerializeField] private Button[] characterButtons;
    [SerializeField] private Text selectionLabel;
    [SerializeField] private Button startButton;

    [Header("Gameplay HUD")]
    [SerializeField] private Text healthLabel;
    [SerializeField] private Text timerLabel;
    [SerializeField] private Button[] skillButtons;
    [SerializeField] private Text[] skillCooldownLabels;

    [Header("Results")]
    [SerializeField] private Text finalTimeLabel;
    [SerializeField] private Text resultsTitleLabel;
    [SerializeField] private Text restartButtonLabel;
    [SerializeField] private Text leaderboardHeadingLabel;
    [SerializeField] private Text leaderboardEntriesLabel;
    [SerializeField] private InputField playerNameInput;
    [SerializeField] private Button saveButton;
    [SerializeField] private Text saveStatusLabel;
    [SerializeField] private Button restartButton;

    private static readonly PlayerSkillSlot[] SkillSlots =
    {
        PlayerSkillSlot.NormalSkill1,
        PlayerSkillSlot.NormalSkill2,
        PlayerSkillSlot.DefensiveSkill,
        PlayerSkillSlot.Ultimate
    };

    private Vector2 startPosition;
    private bool resultSaved;
    private bool defeatShowing;
    private Image healthBarFill;
    private Sprite healthBarSprite;

    private void Awake()
    {
        if (player == null) player = FindFirstObjectByType<PlayerCharacter>();
        if (health == null && player != null) health = player.GetComponent<PlayerHealth>();
        if (timer == null) timer = FindFirstObjectByType<TimeManager>();
        if (arena == null) arena = FindFirstObjectByType<ArenaRunManager>();
        if (player != null) startPosition = player.transform.position;
        if (player != null) player.GetComponent<PlayerCombat>()?.SetAttackVisualEnabled(false);
        if (arena != null) arena.RunCompleted.AddListener(ShowResults);
        CreateHealthBar();

        for (int i = 0; i < characterButtons.Length; i++)
        {
            int index = i;
            characterButtons[i].onClick.AddListener(() => SelectCharacter(index));
        }

        if (startButton != null) startButton.onClick.AddListener(StartRun);
        for (int i = 0; i < skillButtons.Length && i < SkillSlots.Length; i++)
        {
            int index = i;
            skillButtons[i].onClick.AddListener(() => UseSkill(SkillSlots[index]));
        }

        if (saveButton != null) saveButton.onClick.AddListener(SaveResult);
        if (restartButton != null) restartButton.onClick.AddListener(RestartRun);
    }

    private void OnDestroy()
    {
        if (arena != null) arena.RunCompleted.RemoveListener(ShowResults);
        if (healthBarSprite != null) Destroy(healthBarSprite);
    }

    private void Start()
    {
        if (player != null && player.SelectedIndex < 0)
            player.SelectCharacter(0);

        ShowSelection();
    }

    private void Update()
    {
        if (!defeatShowing && player != null && player.RunInProgress && health != null && health.IsDepleted)
        {
            ShowDefeat();
            return;
        }

        if (selectionPanel != null && selectionPanel.activeSelf && Keyboard.current != null)
        {
            if (Keyboard.current.digit1Key.wasPressedThisFrame) SelectCharacter(0);
            else if (Keyboard.current.digit2Key.wasPressedThisFrame) SelectCharacter(1);
            else if (Keyboard.current.digit3Key.wasPressedThisFrame) SelectCharacter(2);

            if (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame)
                StartRun();
        }

        if (gameplayPanel == null || !gameplayPanel.activeSelf || player == null) return;

        if (healthLabel != null && health != null)
        {
            healthLabel.text = "Can " + health.CurrentHealth + "/" + health.MaxHealth;
            if (healthBarFill != null)
                healthBarFill.fillAmount = health.MaxHealth > 0
                    ? health.CurrentHealth / (float)health.MaxHealth
                    : 0f;
        }
        if (timerLabel != null && timer != null)
            timerLabel.text = TimeManager.FormatTime(timer.timePassed);

        for (int i = 0; i < skillCooldownLabels.Length && i < SkillSlots.Length; i++)
        {
            float remaining = player.GetCooldownRemaining(SkillSlots[i]);
            skillCooldownLabels[i].text = remaining > 0f ? remaining.ToString("F1") + " sn" : "HAZIR";
            if (i < skillButtons.Length) skillButtons[i].interactable = player.RunInProgress && remaining <= 0f;
        }
    }

    public void ShowResults()
    {
        if (defeatShowing) return;
        SetResultsMode(false);
        player?.EndRun();
        if (gameplayPanel != null) gameplayPanel.SetActive(false);
        if (selectionPanel != null) selectionPanel.SetActive(false);
        if (resultsPanel != null) resultsPanel.SetActive(true);
        if (finalTimeLabel != null && timer != null)
            finalTimeLabel.text = "Final time  " + TimeManager.FormatTime(timer.timePassed);
        if (saveStatusLabel != null) saveStatusLabel.text = string.Empty;
        if (saveButton != null) saveButton.interactable = !resultSaved;
        leaderboardUI?.ShowLeaderboard();
    }

    private void ShowDefeat()
    {
        defeatShowing = true;
        arena?.ResetRun();
        player?.EndRun();
        if (gameplayPanel != null) gameplayPanel.SetActive(false);
        if (selectionPanel != null) selectionPanel.SetActive(false);
        if (resultsPanel != null) resultsPanel.SetActive(true);
        SetResultsMode(true);
        if (saveStatusLabel != null) saveStatusLabel.text = string.Empty;
    }

    private void SetResultsMode(bool defeated)
    {
        if (resultsTitleLabel != null) resultsTitleLabel.text = defeated ? "Kaybettin" : "RUN COMPLETE";
        if (restartButtonLabel != null) restartButtonLabel.text = defeated ? "Tekrar Dene" : "RESTART";
        if (finalTimeLabel != null) finalTimeLabel.gameObject.SetActive(!defeated);
        if (playerNameInput != null) playerNameInput.gameObject.SetActive(!defeated);
        if (saveButton != null) saveButton.gameObject.SetActive(!defeated);
        if (saveStatusLabel != null) saveStatusLabel.gameObject.SetActive(!defeated);
        if (leaderboardHeadingLabel != null) leaderboardHeadingLabel.gameObject.SetActive(!defeated);
        if (leaderboardEntriesLabel != null) leaderboardEntriesLabel.gameObject.SetActive(!defeated);
    }

    private void SelectCharacter(int index)
    {
        if (player == null || player.RunInProgress || player.HasSelection || index < 0 || index >= player.Characters.Count)
            return;

        if (player.SelectCharacter(index)) UpdateSelection();
    }

    private void UpdateSelection()
    {
        if (player == null) return;

        int selected = player.SelectedIndex;
        if (selectionLabel != null)
            selectionLabel.text = selected >= 0 ? "Selected: " + player.Characters[selected].DisplayName : "Choose a character";

        for (int i = 0; i < characterButtons.Length; i++)
        {
            Button button = characterButtons[i];
            ColorBlock colors = button.colors;
            bool isSelected = i == selected;
            colors.normalColor = isSelected ? SelectedColor : Color.clear;
            colors.highlightedColor = isSelected ? SelectedHoverColor : HoverColor;
            colors.selectedColor = isSelected ? SelectedHoverColor : HoverColor;
            colors.pressedColor = isSelected
                ? new Color(0.42f, 0.65f, 0.35f, 0.7f)
                : new Color(0.62f, 0.72f, 0.86f, 0.5f);
            button.colors = colors;
            button.targetGraphic.color = colors.normalColor;
        }

        EventSystem.current?.SetSelectedGameObject(null);
    }

    private void StartRun()
    {
        if (player == null || player.SelectedIndex < 0 || player.RunInProgress) return;
        if (!player.HasSelection && !player.ConfirmSelection()) return;
        if (!player.BeginRun()) return;

        resultSaved = false;
        if (selectionPanel != null) selectionPanel.SetActive(false);
        if (resultsPanel != null) resultsPanel.SetActive(false);
        if (gameplayPanel != null) gameplayPanel.SetActive(true);
        if (playerNameInput != null) playerNameInput.text = string.Empty;
    }

    private void UseSkill(PlayerSkillSlot slot)
    {
        player?.TryUseSkill(slot);
    }

    private void CreateHealthBar()
    {
        if (healthLabel == null) return;

        RectTransform backgroundRect = CreateBarImage("Health Bar Background", healthLabel.transform,
            new Color(0.12f, 0.14f, 0.12f, 0.9f));
        backgroundRect.anchoredPosition = new Vector2(0f, -7f);
        backgroundRect.sizeDelta = new Vector2(56f, 4f);
        healthBarSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f),
            new Vector2(0.5f, 0.5f), 1f);
        backgroundRect.GetComponent<Image>().sprite = healthBarSprite;

        RectTransform fillRect = CreateBarImage("Health Bar Fill", backgroundRect,
            new Color(0.55f, 0.78f, 0.45f, 1f));
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.one;
        fillRect.offsetMax = -Vector2.one;

        healthBarFill = fillRect.GetComponent<Image>();
        healthBarFill.sprite = healthBarSprite;
        healthBarFill.type = Image.Type.Filled;
        healthBarFill.fillMethod = Image.FillMethod.Horizontal;
        healthBarFill.fillOrigin = (int)Image.OriginHorizontal.Left;
        healthBarFill.raycastTarget = false;
    }

    private static RectTransform CreateBarImage(string objectName, Transform parent, Color color)
    {
        GameObject barObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        barObject.transform.SetParent(parent, false);
        RectTransform rectTransform = (RectTransform)barObject.transform;
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        Image image = barObject.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return rectTransform;
    }

    private void SaveResult()
    {
        if (leaderboardUI == null) return;

        resultSaved = leaderboardUI.SaveScore(playerNameInput != null ? playerNameInput.text : string.Empty);
        if (saveStatusLabel != null)
            saveStatusLabel.text = resultSaved ? "Saved" : "Enter a name and place in the top 10";
        if (saveButton != null) saveButton.interactable = !resultSaved;
    }

    private void RestartRun()
    {
        arena?.ResetRun();
        player?.ReturnToSelection(startPosition);
        resultSaved = false;
        defeatShowing = false;
        if (playerNameInput != null) playerNameInput.text = string.Empty;
        if (saveStatusLabel != null) saveStatusLabel.text = string.Empty;
        ShowSelection();
    }

    private void ShowSelection()
    {
        SetResultsMode(false);
        if (selectionPanel != null) selectionPanel.SetActive(true);
        if (gameplayPanel != null) gameplayPanel.SetActive(false);
        if (resultsPanel != null) resultsPanel.SetActive(false);
        EventSystem.current?.SetSelectedGameObject(null);
        UpdateSelection();
    }
}
