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
// The pause menu has a title and Resume instead of the banner, the summary and Quit. Settings replaces the buttons with a small options panel.
// In the pause menu Play again and Back to Menu ask for confirmation first.
public class EndGameController : MonoBehaviour
{
    // Layout of the mockup (1920x1080, top left corner): the pause title, the grid of four buttons and the rows under it.
    private const float GridX = 580.0f;
    private const float GridWidth = 760.0f;
    private const float ButtonWidth = 372.0f;
    private const float ButtonHeight = 72.0f;
    private const float GridGap = 16.0f;
    private const float PauseGridY = 410.0f;
    private const float EndGridY = 624.0f;
    private const float PauseTitleY = 330.0f;
    private const float PauseHintY = 596.0f;
    private const float BannerY = 190.0f;
    private const float BannerHeight = 112.0f;
    private const float SummaryY = 318.0f;
    private const float SummaryHeight = 240.0f;
    private const float SummaryPaddingX = 40.0f;
    private const float RecordY = 574.0f;
    // The rows of the summary: the label and the numbers in the two team columns (the right edges of the columns).
    private const float SummaryRowHeight = 44.0f;
    private const float SummaryColumnWidth = 150.0f;

    [SerializeField] private Image _dim;
    [SerializeField] private CanvasGroup _buttons;
    [SerializeField] private Button _playAgainButton;
    [SerializeField] private Button _menuButton;
    [SerializeField] private Button _settingsButton;
    [SerializeField] private Button _quitButton;
    [Tooltip("Back to the game; only in the pause menu.")]
    [SerializeField] private Button _resumeButton;
    [Header("Pause menu")]
    [SerializeField] private TMP_Text _pauseTitle;
    [SerializeField] private TMP_Text _pauseHint;
    [Header("Summary of the game")]
    [SerializeField] private GameObject _summaryPanel;
    [SerializeField] private TMP_Text _summaryText;
    [Tooltip("Size of the numbers of the summary; the labels are smaller.")]
    [SerializeField] private float _summaryFontSize = 32.0f;
    [SerializeField] private float _summaryLabelSize = 16.0f;
    [Tooltip("The record under the summary: a new high score and the place on the leaderboard.")]
    [SerializeField] private TMP_Text _recordText;
    [Tooltip("Rematch with the computer taking the other team; only in a game against the computer.")]
    [SerializeField] private Button _swapSidesButton;
    [SerializeField] private Color _superHotColor = new Color32(0xFF, 0x1B, 0x47, 0xFF);
    [SerializeField] private Color _superColdColor = new Color32(0x14, 0xC8, 0xD8, 0xFF);
    [Header("Menu buttons")]
    [Tooltip("The first button of the menu is the selected one: pink text and a pink edge.")]
    [SerializeField] private Sprite _buttonSprite;
    [SerializeField] private Sprite _buttonSelectedSprite;
    [SerializeField] private Color _accentColor = new Color32(0xD7, 0x2E, 0x66, 0xFF);
    [Header("Confirmation in the pause menu")]
    [SerializeField] private GameObject _confirmPanel;
    [SerializeField] private TMP_Text _confirmDetail;
    [SerializeField] private Button _confirmYesButton;
    [SerializeField] private Button _confirmNoButton;
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
    [Tooltip("Background of the winner banner on this screen (the panel of the end screen).")]
    [SerializeField] private Sprite _bannerSprite;
    [Tooltip("Size of the text of the winner banner.")]
    [SerializeField] private float _winnerFontSize = 48.0f;
    [Header("Layout")]
    [SerializeField] private float _dimAlpha = 1.0f;
    [SerializeField] private float _fadeTime = 0.5f;

    private bool _paused;

    // Whether the screen is open, as the pause menu or after the game.
    public bool IsOpen => gameObject.activeSelf;
    private Action _confirmedAction;

    private void Awake()
    {
        _resumeButton.onClick.AddListener(() =>
        {
            SoundController.Instance?.PlayClick();
            Resume();
        });
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
        ShowMenuFor(endOfGame);
        _summaryPanel.SetActive(endOfGame && result != null);
        _recordText.gameObject.SetActive(false);
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
        // The banner slides to its place in the mockup; its size is set at once, it is never seen narrower than the panels below it.
        Vector2 target = HudLayoutPosition(GridX, BannerY, GridWidth, BannerHeight);
        winnerBanner.DOAnchorPos(target, _fadeTime).SetEase(Ease.OutCubic).SetUpdate(true).SetLink(winnerBanner.gameObject);
        Image bannerImage = winnerBanner.GetComponent<Image>();
        if (bannerImage != null) bannerImage.preserveAspect = false;
        winnerBanner.sizeDelta = new Vector2(GridWidth, BannerHeight);
        // The winner, large, on one line in the middle of the banner.
        TMP_Text winnerText = winnerBanner.GetComponentInChildren<TMP_Text>();
        if (winnerText != null)
        {
            RectTransform textRect = winnerText.rectTransform;
            textRect.anchorMin = textRect.anchorMax = textRect.pivot = new Vector2(0.5f, 0.5f);
            textRect.anchoredPosition = Vector2.zero;
            textRect.sizeDelta = new Vector2(GridWidth - 40.0f, textRect.sizeDelta.y);
            winnerText.fontSize = _winnerFontSize;
            winnerText.alignment = TextAlignmentOptions.Center;
            if (result != null) FillSummary(result);
        }
    }

    // Where the rect of a layout place goes when it is a child of this screen (its origin is the middle of the layout).
    private static Vector2 HudLayoutPosition(float x, float y, float width, float height) =>
        new Vector2(x + width * 0.5f - ScreenFit.ReferenceWidth * 0.5f, ScreenFit.ReferenceHeight * 0.5f - y - height * 0.5f);

    // The pause menu has a title, Resume, Play again, Settings and Back to Menu; the end screen Play again, Back to Menu, Settings and Quit.
    // The first button is the selected one.
    private void ShowMenuFor(bool endOfGame)
    {
        _pauseTitle.gameObject.SetActive(!endOfGame);
        _pauseHint.gameObject.SetActive(!endOfGame);
        _resumeButton.gameObject.SetActive(!endOfGame);
        _quitButton.gameObject.SetActive(endOfGame);
        Button[] grid = endOfGame
            ? new[] { _playAgainButton, _menuButton, _settingsButton, _quitButton }
            : new[] { _resumeButton, _playAgainButton, _settingsButton, _menuButton };
        float top = endOfGame ? EndGridY : PauseGridY;
        for (int i = 0; i < grid.Length; i++)
        {
            float x = GridX + (i % 2) * (ButtonWidth + GridGap);
            float y = top + (i / 2) * (ButtonHeight + GridGap);
            HudLayout.Place((RectTransform)grid[i].transform, x, y, ButtonWidth, ButtonHeight);
            StyleButton(grid[i], i == 0);
        }
        // Below the grid, the rematch with the sides swapped, as wide as the grid.
        HudLayout.Place((RectTransform)_swapSidesButton.transform, GridX, top + 2.0f * (ButtonHeight + GridGap), GridWidth, ButtonHeight);
        StyleButton(_swapSidesButton, false);
        if (!endOfGame)
        {
            HudLayout.Place((RectTransform)_pauseTitle.transform, 0.0f, PauseTitleY, ScreenFit.ReferenceWidth, 48.0f);
            HudLayout.Place((RectTransform)_pauseHint.transform, 0.0f, PauseHintY, ScreenFit.ReferenceWidth, 24.0f);
        }
        else
        {
            HudLayout.Place((RectTransform)_summaryPanel.transform, GridX, SummaryY, GridWidth, SummaryHeight);
            HudLayout.Place((RectTransform)_recordText.transform, GridX, RecordY, GridWidth, 24.0f);
        }
    }

    private void StyleButton(Button button, bool selected)
    {
        if (button.TryGetComponent(out Image image) && _buttonSprite != null)
        {
            image.sprite = selected ? _buttonSelectedSprite : _buttonSprite;
            image.preserveAspect = false;
        }
        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
        if (label != null) label.color = selected ? _accentColor : Color.white;
    }

    private void ShowSettings(bool show)
    {
        _settingsPanel.SetActive(show);
        _confirmPanel.SetActive(false);
        _buttons.gameObject.SetActive(!show);
    }

    // The summary in the panel: the labels on the left, the numbers of Super Hot and Super Cold in two columns in their colors (right-aligned),
    // the number of turns between the two columns. Under the panel, the record.
    private void FillSummary(GameResult result)
    {
        float width = GridWidth - 2.0f * SummaryPaddingX;
        float rows = 4.0f * SummaryRowHeight;
        _summaryText.fontSize = _summaryLabelSize;
        _summaryText.enableAutoSizing = false;
        _summaryText.alignment = TextAlignmentOptions.MidlineLeft;
        _summaryText.textWrappingMode = TextWrappingModes.NoWrap;
        RectTransform rect = _summaryText.rectTransform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(width, rows);

        GameStats stats = result.Stats;
        GameRecord record = result.Record;
        float hotRight = width - SummaryColumnWidth;
        float coldRight = width;
        StringBuilder text = new StringBuilder();
        text.Append("<line-height=").Append(SummaryRowHeight.ToString("0.#", CultureInfo.InvariantCulture)).Append('>');
        // Turns played: one number, centred between the two columns.
        float turnsStart = (hotRight + coldRight) * 0.5f - Measure(stats.Turns.ToString()) * 0.5f;
        text.Append(Label("Turns played: {0}")).Append(Number(stats.Turns.ToString(), turnsStart, Color.white));
        AppendRow(text, Label("Units called: {0}"), stats.CalledBy(1), stats.CalledBy(2), hotRight, coldRight);
        AppendRow(text, Label("Units killed: {0}"), stats.KilledBy(1), stats.KilledBy(2), hotRight, coldRight);
        AppendRow(text, Label("Score: {0}"), record.team1.score, record.team2.score, hotRight, coldRight);
        _summaryText.text = text.ToString();

        string recordLine = result.IsHighScore ? Loc.T("New high score!") : "";
        if (result.LeaderboardPlace > 0) recordLine += (recordLine.Length > 0 ? " - " : "") + Loc.F("Leaderboard place {0}", result.LeaderboardPlace);
        _recordText.text = recordLine;
        _recordText.gameObject.SetActive(recordLine.Length > 0);
    }

    // "Units called: " without the number and the colon, from the text used with a number.
    private static string Label(string format) => Loc.F(format, "").TrimEnd().TrimEnd(':');

    // The width of a number in the size of the summary numbers.
    private float Measure(string number) => _summaryText.GetPreferredValues("<size=" + _summaryFontSize.ToString("0.#", CultureInfo.InvariantCulture) + ">" + number + "</size>").x;

    private string Number(string number, float start, Color color) =>
        "<pos=" + start.ToString("0.#", CultureInfo.InvariantCulture) + "px><size=" + _summaryFontSize.ToString("0.#", CultureInfo.InvariantCulture) + "><color=#" +
        ColorUtility.ToHtmlStringRGB(color) + ">" + number + "</color></size>";

    private void AppendRow(StringBuilder text, string label, int hot, int cold, float hotRight, float coldRight)
    {
        string hotNumber = hot.ToString();
        string coldNumber = cold.ToString();
        // The numbers are written so that their right ends are at the right edges of their columns.
        text.Append('\n').Append(label)
            .Append(Number(hotNumber, hotRight - Measure(hotNumber), _superHotColor))
            .Append(Number(coldNumber, coldRight - Measure(coldNumber), _superColdColor));
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
