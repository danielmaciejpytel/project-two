using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// The screen after the game (also the pause menu opened with Escape): the rest of the HUD is dimmed, the winner banner moves to the middle of the
// screen and four buttons appear under it. Settings replaces the buttons with a small options panel.
public class EndGameController : MonoBehaviour
{
    private const string MenuSceneName = "MenuScene";
    // Share of the settings panel's rect that its image really covers (792 of 812 pixels).
    private const float PanelVisibleWidth = 792.0f / 812.0f;

    [SerializeField] private Image _dim;
    [SerializeField] private CanvasGroup _buttons;
    [SerializeField] private Button _playAgainButton;
    [SerializeField] private Button _menuButton;
    [SerializeField] private Button _settingsButton;
    [SerializeField] private Button _quitButton;
    [Header("Settings")]
    [SerializeField] private GameObject _settingsPanel;
    [SerializeField] private Slider _soundSlider;
    [SerializeField] private Slider _musicSlider;
    [SerializeField] private Button _resolutionButton;
    [SerializeField] private TMP_Text _resolutionLabel;
    [SerializeField] private Button _settingsBackButton;
    [Tooltip("Background of the winner banner on this screen, less transparent than in the HUD.")]
    [SerializeField] private Sprite _bannerSprite;
    [Header("Layout")]
    [Tooltip("Where the winner banner stops, relative to the middle of the screen.")]
    [SerializeField] private Vector2 _bannerPosition = new Vector2(0.0f, 150.0f);
    [SerializeField] private float _dimAlpha = 0.7f;
    [SerializeField] private float _fadeTime = 0.5f;

    private bool _paused;

    private void Awake()
    {
        _playAgainButton.onClick.AddListener(PlayAgain);
        _menuButton.onClick.AddListener(BackToMenu);
        _settingsButton.onClick.AddListener(() => ShowSettings(true));
        _settingsBackButton.onClick.AddListener(() => ShowSettings(false));
        _quitButton.onClick.AddListener(QuitGame);
        _resolutionButton.onClick.AddListener(CycleResolution);
        _soundSlider.onValueChanged.AddListener(value => SoundController.Instance.SoundVolume = value);
        _musicSlider.onValueChanged.AddListener(value => SoundController.Instance.MusicVolume = value);
        // A click after releasing the sound slider, so the new volume can be heard.
        EventTrigger.Entry released = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
        released.callback.AddListener(_ => SoundController.Instance?.PlayClick());
        _soundSlider.gameObject.AddComponent<EventTrigger>().triggers.Add(released);
        gameObject.SetActive(false);
    }

    private void OnEnable() => Loc.Changed += UpdateResolutionLabel;

    private void OnDisable() => Loc.Changed -= UpdateResolutionLabel;

    // The game must never stay frozen after this screen is gone.
    private void OnDestroy() => Time.timeScale = 1.0f;

    // Escape during the game: opens the pause menu; with the settings open it goes back to the buttons, otherwise it resumes.
    public void HandleEscape()
    {
        if (!gameObject.activeSelf) ShowPause();
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

    // Shows the screen; the banner is taken out of the HUD so it stays above the dimming.
    public void Show(RectTransform winnerBanner)
    {
        gameObject.SetActive(true);
        ShowSettings(false);
        _soundSlider.SetValueWithoutNotify(SoundController.Instance != null ? SoundController.Instance.SoundVolume : 1.0f);
        _musicSlider.SetValueWithoutNotify(SoundController.Instance != null ? SoundController.Instance.MusicVolume : 0.5f);
        UpdateResolutionLabel();

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
        // The banner is as wide as the settings panel; the panel image has a transparent margin on its left and right.
        float width = ((RectTransform)_settingsPanel.transform).rect.width * PanelVisibleWidth;
        // The banner image keeps its proportions, so the height follows the width.
        Image bannerImage = winnerBanner.GetComponent<Image>();
        float height = bannerImage != null && bannerImage.sprite != null && bannerImage.preserveAspect
            ? width * bannerImage.sprite.rect.height / bannerImage.sprite.rect.width
            : winnerBanner.sizeDelta.y;
        winnerBanner.DOSizeDelta(new Vector2(width, height), _fadeTime).SetEase(Ease.OutCubic).SetUpdate(true).SetLink(winnerBanner.gameObject);
    }

    private void ShowSettings(bool show)
    {
        _settingsPanel.SetActive(show);
        _buttons.gameObject.SetActive(!show);
    }

    private void PlayAgain()
    {
        SoundController.Instance?.PlayClick();
        LeaveScene(SceneManager.GetActiveScene().name);
    }

    private void BackToMenu()
    {
        SoundController.Instance?.PlayClick();
        LeaveScene(MenuSceneName);
    }

    private static void LeaveScene(string sceneName)
    {
        Time.timeScale = 1.0f;
        DOTween.KillAll(false);
        UnitAnimators.Release();
        SceneManager.LoadScene(sceneName);
    }

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

    private void UpdateResolutionLabel()
    {
        Vector2Int current = DisplaySettings.Current;
        string size = current.x + "x" + current.y + (DisplaySettings.IsNative(current) ? " " + Loc.T("(native)") : "");
        _resolutionLabel.text = Loc.F("Resolution: {0}", size);
    }
}
