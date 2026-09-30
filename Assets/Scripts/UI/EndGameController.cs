using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// The screen after the game (also the pause menu opened with Escape): the rest of the HUD is dimmed, the winner banner moves to the middle of the
// screen, under it come a summary of the game and the buttons (with a rematch that swaps the sides against the computer).
// Settings replaces the buttons with a small options panel. In the pause menu Play again, Back to Menu and Quit ask for confirmation first.
public class EndGameController : MonoBehaviour
{
    // Share of the settings panel's rect that its image really covers (792 of 812 pixels).
    private const float PanelVisibleWidth = 792.0f / 812.0f;

    [SerializeField] private Image _dim;
    [SerializeField] private CanvasGroup _buttons;
    [SerializeField] private Button _playAgainButton;
    [SerializeField] private Button _menuButton;
    [SerializeField] private Button _settingsButton;
    [SerializeField] private Button _quitButton;
    [Header("Summary of the game")]
    [SerializeField] private GameObject _summaryPanel;
    [SerializeField] private TMP_Text _summaryText;
    [SerializeField] private float _summaryFontSize = 26.0f;
    [Tooltip("Space between the labels and the first number of the summary, and the smallest space between its two numbers.")]
    [SerializeField] private float _summaryGap = 24.0f;
    [Tooltip("Rematch with the computer taking the other team; only in a game against the computer.")]
    [SerializeField] private Button _swapSidesButton;
    [SerializeField] private Color _superHotColor = new Color32(0xFF, 0x1B, 0x47, 0xFF);
    [SerializeField] private Color _superColdColor = new Color32(0x14, 0xC8, 0xD8, 0xFF);
    [Header("Confirmation in the pause menu")]
    [SerializeField] private GameObject _confirmPanel;
    [SerializeField] private TMP_Text _confirmDetail;
    [SerializeField] private Button _confirmYesButton;
    [SerializeField] private Button _confirmNoButton;
    [Tooltip("The pause menu has no banner and summary above the buttons, so they move up by this much; its panels are centred on the screen.")]
    [SerializeField] private float _pauseButtonsShift = 126.0f;
    [Header("Settings")]
    [SerializeField] private GameObject _settingsPanel;
    [SerializeField] private Slider _soundSlider;
    [SerializeField] private Slider _musicSlider;
    [SerializeField] private Button _resolutionButton;
    [SerializeField] private TMP_Text _resolutionLabel;
    [Tooltip("Difficulty of the computer; it applies to the next game.")]
    [SerializeField] private Button _difficultyButton;
    [SerializeField] private TMP_Text _difficultyLabel;
    [SerializeField] private Button _settingsBackButton;
    [Tooltip("Background of the winner banner on this screen, less transparent than in the HUD.")]
    [SerializeField] private Sprite _bannerSprite;
    [Tooltip("Size of the text of the winner banner (in the HUD the same text, \"Turn: ...\", is smaller).")]
    [SerializeField] private float _winnerFontSize = 40.0f;
    [Header("Layout")]
    [Tooltip("Where the winner banner stops, relative to the middle of the screen.")]
    [SerializeField] private Vector2 _bannerPosition = new Vector2(0.0f, 150.0f);
    [SerializeField] private float _dimAlpha = 0.7f;
    [SerializeField] private float _fadeTime = 0.5f;

    private bool _paused;

    // Whether the screen is open, as the pause menu or after the game.
    public bool IsOpen => gameObject.activeSelf;
    private Action _confirmedAction;
    private RectTransform _buttonsRect;
    private RectTransform _settingsRect;
    private RectTransform _confirmRect;
    private Vector2 _buttonsPosition;
    private Vector2 _settingsPosition;
    private Vector2 _confirmPosition;

    private void Awake()
    {
        _buttonsRect = (RectTransform)_buttons.transform;
        _settingsRect = (RectTransform)_settingsPanel.transform;
        _confirmRect = (RectTransform)_confirmPanel.transform;
        _buttonsPosition = _buttonsRect.anchoredPosition;
        _settingsPosition = _settingsRect.anchoredPosition;
        _confirmPosition = _confirmRect.anchoredPosition;
        _playAgainButton.onClick.AddListener(() => Ask("The current game will be lost and a new one starts.", PlayAgain));
        _menuButton.onClick.AddListener(() => Ask("The current game will be lost.", BackToMenu));
        _quitButton.onClick.AddListener(() => Ask("The game will close and the current game will be lost.", QuitGame));
        _swapSidesButton.onClick.AddListener(SwapSides);
        _confirmYesButton.onClick.AddListener(Confirmed);
        _confirmNoButton.onClick.AddListener(Declined);
        _settingsButton.onClick.AddListener(() => ShowSettings(true));
        _settingsBackButton.onClick.AddListener(() => ShowSettings(false));
        _resolutionButton.onClick.AddListener(CycleResolution);
        _difficultyButton.onClick.AddListener(CycleDifficulty);
        _soundSlider.onValueChanged.AddListener(value => SoundController.Instance.SoundVolume = value);
        _musicSlider.onValueChanged.AddListener(value => SoundController.Instance.MusicVolume = value);
        // A click after releasing the sound slider, so the new volume can be heard.
        EventTrigger.Entry released = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
        released.callback.AddListener(_ => SoundController.Instance?.PlayClick());
        _soundSlider.gameObject.AddComponent<EventTrigger>().triggers.Add(released);
        gameObject.SetActive(false);
    }

    private void OnEnable() => Loc.Changed += UpdateSettingsLabels;

    private void OnDisable() => Loc.Changed -= UpdateSettingsLabels;

    // The game must never stay frozen after this screen is gone.
    private void OnDestroy() => Time.timeScale = 1.0f;

    // Escape during the game: opens the pause menu; with the settings or a question open it goes back to the buttons, otherwise it resumes.
    public void HandleEscape()
    {
        if (!gameObject.activeSelf) ShowPause();
        else if (_confirmPanel.activeSelf) Declined();
        else if (_settingsPanel.activeSelf) ShowSettings(false);
        else if (_paused) Resume();
    }

    // The same screen as after the game, without the winner banner; the game stops behind it.
    private void ShowPause()
    {
        _paused = true;
        Time.timeScale = 0.0f;
        Show(null);
    }

    private void Resume()
    {
        _paused = false;
        Time.timeScale = 1.0f;
        gameObject.SetActive(false);
    }

    // Shows the screen; the banner is taken out of the HUD so it stays above the dimming. Without a banner it is the pause menu.
    public void Show(RectTransform winnerBanner, GameResult result = null)
    {
        gameObject.SetActive(true);
        ShowSettings(false);
        _confirmedAction = null;
        bool endOfGame = winnerBanner != null;
        _buttonsRect.anchoredPosition = _buttonsPosition + (endOfGame ? Vector2.zero : new Vector2(0.0f, _pauseButtonsShift));
        _settingsRect.anchoredPosition = endOfGame ? _settingsPosition : new Vector2(_settingsPosition.x, 0.0f);
        _confirmRect.anchoredPosition = endOfGame ? _confirmPosition : new Vector2(_confirmPosition.x, 0.0f);
        _summaryPanel.SetActive(endOfGame && result != null);
        _swapSidesButton.gameObject.SetActive(endOfGame && GameSession.AiPlayerId != GameSession.NoAi);
        _soundSlider.SetValueWithoutNotify(SoundController.Instance != null ? SoundController.Instance.SoundVolume : 1.0f);
        _musicSlider.SetValueWithoutNotify(SoundController.Instance != null ? SoundController.Instance.MusicVolume : 0.5f);
        UpdateSettingsLabels();

        Color dim = _dim.color;
        dim.a = 0.0f;
        _dim.color = dim;
        _dim.DOFade(_dimAlpha, _fadeTime).SetUpdate(true).SetLink(_dim.gameObject);
        _buttons.alpha = 0.0f;
        _buttons.DOFade(1.0f, _fadeTime).SetDelay(_fadeTime).SetUpdate(true).SetLink(_buttons.gameObject);

        if (winnerBanner == null) return;
        RectTransform overlay = (RectTransform)transform;
        if (_bannerSprite != null && winnerBanner.TryGetComponent(out Image bannerBackground)) bannerBackground.sprite = _bannerSprite;
        winnerBanner.SetParent(overlay, true);
        winnerBanner.SetAsLastSibling();
        winnerBanner.anchorMin = winnerBanner.anchorMax = new Vector2(0.5f, 0.5f);
        winnerBanner.anchoredPosition = overlay.InverseTransformPoint(winnerBanner.position);
        winnerBanner.DOAnchorPos(_bannerPosition, _fadeTime).SetEase(Ease.OutCubic).SetUpdate(true).SetLink(winnerBanner.gameObject);
        // The banner is as wide as the panels under it (the summary and the buttons, which are as wide as the settings panel;
        // its image has a transparent margin on the left and right). The size is set at once, not tweened: the banner slides
        // into place, but it is never seen narrower than the panels below it.
        float width = ((RectTransform)_settingsPanel.transform).rect.width * PanelVisibleWidth;
        Image bannerImage = winnerBanner.GetComponent<Image>();
        float height = bannerImage != null && bannerImage.sprite != null
            ? width * bannerImage.sprite.rect.height / bannerImage.sprite.rect.width
            : winnerBanner.rect.height;
        if (bannerImage != null) bannerImage.preserveAspect = false;
        winnerBanner.sizeDelta = new Vector2(width, height);
        // A larger text, on one line across the banner.
        TMP_Text winnerText = winnerBanner.GetComponentInChildren<TMP_Text>();
        if (winnerText != null)
        {
            winnerText.fontSize = _winnerFontSize;
            winnerText.rectTransform.sizeDelta = new Vector2(width - 40.0f, winnerText.rectTransform.sizeDelta.y);
            if (result != null) FillSummary(result, winnerText);
        }
    }

    private void ShowSettings(bool show)
    {
        _settingsPanel.SetActive(show);
        _confirmPanel.SetActive(false);
        _buttons.gameObject.SetActive(!show);
    }

    // The summary lines up with the name of the winner above it: the labels start at its left edge; the first number of a line
    // (the Super Hot one, in its color) is in a column of its own and the second (Super Cold's) ends at its right edge.
    private void FillSummary(GameResult result, TMP_Text winnerText)
    {
        winnerText.ForceMeshUpdate();
        float width = winnerText.preferredWidth;
        _summaryText.fontSize = _summaryFontSize;
        _summaryText.enableAutoSizing = false;
        _summaryText.alignment = TextAlignmentOptions.MidlineLeft;
        _summaryText.textWrappingMode = TextWrappingModes.NoWrap;
        RectTransform rect = _summaryText.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(width, rect.sizeDelta.y);

        GameStats stats = result.Stats;
        GameRecord record = result.Record;
        string turns = Label("Turns played: {0}");
        string called = Label("Units called: {0}");
        string killed = Label("Units killed: {0}");
        string score = Label("Score: {0}");
        // The column of the first numbers: after the longest label.
        float column = Mathf.Max(Measure(turns), Measure(called), Measure(killed), Measure(score)) + _summaryGap;

        StringBuilder text = new StringBuilder();
        text.Append(turns).Append("<pos=").Append(column.ToString("0.#", CultureInfo.InvariantCulture)).Append("px>").Append(stats.Turns);
        AppendRow(text, called, stats.CalledBy(1), stats.CalledBy(2), column, width);
        AppendRow(text, killed, stats.KilledBy(1), stats.KilledBy(2), column, width);
        AppendRow(text, score, record.team1.score, record.team2.score, column, width);
        if (result.IsHighScore) text.Append('\n').Append(Loc.T("New high score!"));
        else if (result.LeaderboardPlace > 0) text.Append('\n').Append(Loc.F("Leaderboard place {0}", result.LeaderboardPlace));
        _summaryText.text = text.ToString();
    }

    // "Units called: " without the number, from the text used with a number.
    private static string Label(string format) => Loc.F(format, "").TrimEnd();

    private float Measure(string text) => _summaryText.GetPreferredValues(text).x;

    private void AppendRow(StringBuilder text, string label, int hot, int cold, float column, float width)
    {
        string secondNumber = cold.ToString();
        // The second number is written so that its right end is at the right edge of the line.
        float secondStart = width - Measure(secondNumber);
        text.Append('\n').Append(label)
            .Append("<pos=").Append(column.ToString("0.#", CultureInfo.InvariantCulture)).Append("px><color=#").Append(ColorUtility.ToHtmlStringRGB(_superHotColor)).Append('>').Append(hot).Append("</color>")
            .Append("<pos=").Append(secondStart.ToString("0.#", CultureInfo.InvariantCulture)).Append("px><color=#").Append(ColorUtility.ToHtmlStringRGB(_superColdColor)).Append('>').Append(secondNumber).Append("</color>");
    }

    // In the pause menu the game is still running behind it, so leaving needs a second click; after the game it does not.
    private void Ask(string detailKey, Action action)
    {
        if (!_paused)
        {
            action();
            return;
        }
        _confirmedAction = action;
        _confirmDetail.text = Loc.T(detailKey);
        _confirmPanel.SetActive(true);
        _buttons.gameObject.SetActive(false);
    }

    private void Confirmed()
    {
        Action action = _confirmedAction;
        Declined();
        action?.Invoke();
    }

    private void Declined()
    {
        _confirmedAction = null;
        _confirmPanel.SetActive(false);
        _buttons.gameObject.SetActive(true);
    }

    private void SwapSides()
    {
        GameSession.SwapSides();
        GameController.Instance.RestartGame();
    }

    private void PlayAgain() => GameController.Instance.RestartGame();

    private void BackToMenu() => GameController.Instance.ReturnToMenu();

    private void QuitGame()
    {
        SoundController.Instance?.PlayClick();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void CycleResolution()
    {
        SoundController.Instance?.PlayClick();
        List<Vector2Int> resolutions = DisplaySettings.Available();
        int next = (resolutions.IndexOf(DisplaySettings.Current) + 1) % resolutions.Count;
        DisplaySettings.Set(resolutions[next]);
        UpdateResolutionLabel();
    }

    // Easy -> Normal -> Hard, like in the options of the menu.
    private void CycleDifficulty()
    {
        SoundController.Instance?.PlayClick();
        GameSession.Difficulty = (AiDifficulty)(((int)GameSession.Difficulty + 1) % 3);
        UpdateDifficultyLabel();
    }

    private void UpdateSettingsLabels()
    {
        UpdateResolutionLabel();
        UpdateDifficultyLabel();
    }

    private void UpdateDifficultyLabel() => _difficultyLabel.text = Loc.F("Difficulty: {0}", Loc.T(GameSession.Difficulty.ToString()));

    private void UpdateResolutionLabel()
    {
        Vector2Int current = DisplaySettings.Current;
        string size = current.x + "x" + current.y + (DisplaySettings.IsNative(current) ? " " + Loc.T("(native)") : "");
        _resolutionLabel.text = Loc.F("Resolution: {0}", size);
    }
}
