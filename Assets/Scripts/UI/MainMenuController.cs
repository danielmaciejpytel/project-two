using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using DG.Tweening;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private Button _playButton;
    [Header("Play submenu")]
    [SerializeField] private Button _playerVsPlayerButton;
    [SerializeField] private Button _playVsAiButton;
    [SerializeField] private Button _playBackButton;
    [Header("Main menu")]
    [SerializeField] private Button _instructionsButton;
    [SerializeField] private Button _optionsButton;
    [SerializeField] private Button _creditsButton;
    [SerializeField] private Button _exitButton;
    [SerializeField] private Button _backButton;
    [SerializeField] private Image _barImage;
    [SerializeField] private Image _creditsImage;
    [SerializeField] private TMP_Text _instructionText;
    [SerializeField] private GameObject _optionsPanel;
    [SerializeField] private Slider _soundSlider;
    [SerializeField] private Slider _musicSlider;
    [SerializeField] private TMP_Text _difficultyLabel;
    [SerializeField] private TMP_Text _resolutionLabel;
    [SerializeField] private TMP_Text _languageLabel;

    private const float HiddenBarX = -1920.0f;

    private void OnEnable()
    {
        Loc.Changed += RefreshOptionTexts;
    }

    private void OnDisable()
    {
        Loc.Changed -= RefreshOptionTexts;
    }

    private void Start()
    {
        // Coming back from the unit draft: show the game mode choice right away.
        if (!GameSession.OpenPlayMenu) return;
        GameSession.OpenPlayMenu = false;
        SetMainButtonsActive(false);
        SetPlayButtonsActive(true);
    }

    private void HideBar()
    {
        _barImage.transform.DOKill();
        Vector3 localPosition = _barImage.transform.localPosition;
        localPosition.x = HiddenBarX;
        _barImage.transform.localPosition = localPosition;
    }

    private void SetMainButtonsActive(bool active)
    {
        _playButton.gameObject.SetActive(active);
        _instructionsButton.gameObject.SetActive(active);
        _optionsButton.gameObject.SetActive(active);
        _creditsButton.gameObject.SetActive(active);
        _exitButton.gameObject.SetActive(active);
    }

    private void SetPlayButtonsActive(bool active)
    {
        _playerVsPlayerButton.gameObject.SetActive(active);
        _playVsAiButton.gameObject.SetActive(active);
        _playBackButton.gameObject.SetActive(active);
    }

    // PLAY opens the choice between Player vs Player and Play vs AI in place of the main buttons.
    public void DisplayPlayMenu()
    {
        SoundController.Instance?.PlayClick();
        SetMainButtonsActive(false);
        SetPlayButtonsActive(true);
        HideBar();
    }

    public void HidePlayMenu()
    {
        SoundController.Instance?.PlayClick();
        SetPlayButtonsActive(false);
        SetMainButtonsActive(true);
        HideBar();
    }

    public void StartGame()
    {
        GameSession.AiPlayerId = GameSession.NoAi;
        LoadGame();
    }

    public void StartGameVsAI()
    {
        GameSession.AiPlayerId = GameSession.DefaultAiPlayer;
        LoadGame();
    }

    private void LoadGame()
    {
        DOTween.KillAll(false);
        SceneManager.LoadScene("MainScene");
    }

    public void DisplayCredits()
    {
        SoundController.Instance?.PlayClick();
        SetMainButtonsActive(false);
        _backButton.gameObject.SetActive(true);
        _creditsImage.gameObject.SetActive(true);
        HideBar();
    }

    public void HideCredits()
    {
        SoundController.Instance?.PlayClick();
        SetMainButtonsActive(true);
        _backButton.gameObject.SetActive(false);
        _creditsImage.gameObject.SetActive(false);
        _instructionText.gameObject.SetActive(false);
        HideBar();
    }

    public void DisplayOptions()
    {
        SoundController.Instance?.PlayClick();
        SetMainButtonsActive(false);
        _optionsPanel.SetActive(true);
        HideBar();
        _soundSlider.SetValueWithoutNotify(SoundController.Instance.SoundVolume);
        _musicSlider.SetValueWithoutNotify(SoundController.Instance.MusicVolume);
        RefreshOptionTexts();
    }

    public void HideOptions()
    {
        SoundController.Instance?.PlayClick();
        SetMainButtonsActive(true);
        _optionsPanel.SetActive(false);
        HideBar();
    }

    public void DisplayInstructions()
    {
        SoundController.Instance?.PlayClick();
        SetMainButtonsActive(false);
        _backButton.gameObject.SetActive(true);
        _instructionText.gameObject.SetActive(true);
        HideBar();
    }

    public void QuitGame()
    {
        SoundController.Instance?.PlayClick();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void OnMybuttonEnter(Button myButton)
    {
        SoundController.Instance?.PlayHover();
        // Follow the hovered button vertically, start off-screen on the left (canvas units).
        Vector3 barPosition = _barImage.transform.position;
        barPosition.y = myButton.transform.position.y;
        _barImage.transform.position = barPosition;
        HideBar();
        // Only stop the highlight bar tween, KillAll also stopped every other running tween in the menu.
        _barImage.transform.DOKill();
        _barImage.transform.DOLocalMoveX(0.0f, 1.0f).SetEase(Ease.OutExpo).SetLink(_barImage.gameObject);
    }

    public void OnMybuttonExit(Button myButton)
    {
        _barImage.transform.DOKill();
        _barImage.transform.DOLocalMoveX(HiddenBarX, 1.0f).SetEase(Ease.OutExpo).SetLink(_barImage.gameObject);
    }

    // Options: cycles the computer opponent's difficulty Easy -> Normal -> Hard.
    public void CycleDifficulty()
    {
        SoundController.Instance?.PlayClick();
        GameSession.Difficulty = (AiDifficulty)(((int)GameSession.Difficulty + 1) % 3);
        RefreshOptionTexts();
    }

    private void UpdateDifficultyLabel()
    {
        if (_difficultyLabel != null) _difficultyLabel.text = Loc.F("Difficulty: {0}", Loc.T(GameSession.Difficulty.ToString()));
    }

    // Options: cycles through the resolutions the monitor supports (native first).
    public void CycleResolution()
    {
        SoundController.Instance?.PlayClick();
        List<Vector2Int> resolutions = DisplaySettings.Available();
        int next = (resolutions.IndexOf(DisplaySettings.Current) + 1) % resolutions.Count;
        DisplaySettings.Set(resolutions[next]);
        RefreshOptionTexts();
    }

    private void UpdateResolutionLabel()
    {
        if (_resolutionLabel == null) return;
        Vector2Int current = DisplaySettings.Current;
        string size = current.x + "x" + current.y + (DisplaySettings.IsNative(current) ? " " + Loc.T("(native)") : "");
        _resolutionLabel.text = Loc.F("Resolution: {0}", size);
    }

    // Options: switches between the available languages; every text follows at once.
    public void CycleLanguage()
    {
        SoundController.Instance?.PlayClick();
        Loc.Current = Loc.Current == Language.English ? Language.Polish : Language.English;
    }

    private void UpdateLanguageLabel()
    {
        if (_languageLabel != null) _languageLabel.text = Loc.F("Language: {0}", Loc.LanguageName(Loc.Current));
    }

    // The option texts are built in code; the buttons around them follow the text width (left edge fixed).
    private void RefreshOptionTexts()
    {
        UpdateDifficultyLabel();
        UpdateResolutionLabel();
        UpdateLanguageLabel();
        foreach (TMP_Text label in new[] { _difficultyLabel, _resolutionLabel, _languageLabel })
        {
            if (label == null) continue;
            label.ForceMeshUpdate();
            ((RectTransform)label.transform.parent).SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, label.preferredWidth + 6.0f);
        }
    }

    // Sliders: the volume is applied and saved while dragging.
    public void SoundVolumeChanged(float value)
    {
        SoundController.Instance.SoundVolume = value;
    }

    public void MusicVolumeChanged(float value)
    {
        SoundController.Instance.MusicVolume = value;
    }

    public void SoundSliderReleasedData(UnityEngine.EventSystems.BaseEventData data) => SoundSliderReleased();

    // A click after releasing the sound slider, so the new volume can be heard.
    public void SoundSliderReleased()
    {
        SoundController.Instance?.PlayClick();
    }
}
