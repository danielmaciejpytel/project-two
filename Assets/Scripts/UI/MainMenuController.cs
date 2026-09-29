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
    [SerializeField] private Toggle _soundToggle;
    [SerializeField] private Toggle _musicToggle;
    [SerializeField] private TMP_Text _difficultyLabel;

    private const float HiddenBarX = -1920.0f;

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
        SoundController.Instance.PlayClick();
        SetMainButtonsActive(false);
        SetPlayButtonsActive(true);
        HideBar();
    }

    public void HidePlayMenu()
    {
        SoundController.Instance.PlayClick();
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
        SoundController.Instance.PlayClick();
        SetMainButtonsActive(false);
        _backButton.gameObject.SetActive(true);
        _creditsImage.gameObject.SetActive(true);
        HideBar();
    }

    public void HideCredits()
    {
        SoundController.Instance.PlayClick();
        SetMainButtonsActive(true);
        _backButton.gameObject.SetActive(false);
        _creditsImage.gameObject.SetActive(false);
        _instructionText.gameObject.SetActive(false);
        HideBar();
    }

    public void DisplayOptions()
    {
        SoundController.Instance.PlayClick();
        SetMainButtonsActive(false);
        _optionsPanel.SetActive(true);
        HideBar();
        _soundToggle.isOn = SoundController.Instance.SoundOn;
        UpdateDifficultyLabel();
        _musicToggle.isOn = SoundController.Instance.MusicOn;
    }

    public void HideOptions()
    {
        SoundController.Instance.PlayClick();
        SetMainButtonsActive(true);
        _optionsPanel.SetActive(false);
        HideBar();
    }

    public void DisplayInstructions()
    {
        SoundController.Instance.PlayClick();
        SetMainButtonsActive(false);
        _backButton.gameObject.SetActive(true);
        _instructionText.gameObject.SetActive(true);
        HideBar();
    }

    public void QuitGame()
    {
        SoundController.Instance.PlayClick();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void OnMybuttonEnter(Button myButton)
    {
        SoundController.Instance.PlayHover();
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
        SoundController.Instance.PlayClick();
        GameSession.Difficulty = (AiDifficulty)(((int)GameSession.Difficulty + 1) % 3);
        UpdateDifficultyLabel();
    }

    private void UpdateDifficultyLabel()
    {
        if (_difficultyLabel != null) _difficultyLabel.text = "AI: " + GameSession.Difficulty;
    }

    public void SoundToggleClicked()
    {
        SoundController.Instance.PlayClick();
        SoundController.Instance.SoundOn = _soundToggle.isOn;
    }

    public void MusicToggleClicked()
    {
        SoundController.Instance.PlayClick();
        SoundController.Instance.MusicOn = _musicToggle.isOn;
    }
}
