using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DG.Tweening;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private Button _playButton;
    [SerializeField] private Button _instructionsButton;
    [SerializeField] private Button _optionsButton;
    [SerializeField] private Button _creditsButton;
    [SerializeField] private Button _exitButton;
    [SerializeField] private Button _backButton;
    [SerializeField] private Image _barImage;
    [SerializeField] private Image _creditsImage;
    [SerializeField] private Text _instructionText;
    [SerializeField] private GameObject _optionsPanel;
    [SerializeField] private Toggle _soundToggle;
    [SerializeField] private Toggle _musicToggle;

    public void StartGame()
    {
        DOTween.KillAll(false);
        SceneManager.LoadScene("MainScene");
    }

    public void DisplayCredits()
    {
        SoundController.Instance.PlayClick();
        _playButton.gameObject.SetActive(false);
        _instructionsButton.gameObject.SetActive(false);
        _optionsButton.gameObject.SetActive(false);
        _creditsButton.gameObject.SetActive(false);
        _exitButton.gameObject.SetActive(false);
        _backButton.gameObject.SetActive(true);
        _creditsImage.gameObject.SetActive(true);
        _barImage.transform.position = new Vector3(-1920.0f, 0.0f, 0.0f);
    }

    public void HideCredits()
    {
        SoundController.Instance.PlayClick();
        _playButton.gameObject.SetActive(true);
        _instructionsButton.gameObject.SetActive(true);
        _optionsButton.gameObject.SetActive(true);
        _creditsButton.gameObject.SetActive(true);
        _exitButton.gameObject.SetActive(true);
        _backButton.gameObject.SetActive(false);
        _creditsImage.gameObject.SetActive(false);
        _instructionText.gameObject.SetActive(false);
        _barImage.transform.position = new Vector3(-1920.0f, 0.0f, 0.0f);
    }

    public void DisplayOptions()
    {
        SoundController.Instance.PlayClick();
        _playButton.gameObject.SetActive(false);
        _instructionsButton.gameObject.SetActive(false);
        _optionsButton.gameObject.SetActive(false);
        _creditsButton.gameObject.SetActive(false);
        _exitButton.gameObject.SetActive(false);
        _optionsPanel.SetActive(true);
        _barImage.transform.position = new Vector3(-1920.0f, 0.0f, 0.0f);
        _soundToggle.isOn = SoundController.Instance.SoundOn;
        _musicToggle.isOn = SoundController.Instance.MusicOn;
    }

    public void HideOptions()
    {
        SoundController.Instance.PlayClick();
        _playButton.gameObject.SetActive(true);
        _instructionsButton.gameObject.SetActive(true);
        _optionsButton.gameObject.SetActive(true);
        _creditsButton.gameObject.SetActive(true);
        _exitButton.gameObject.SetActive(true);
        _optionsPanel.SetActive(false);
        _barImage.transform.position = new Vector3(-1920.0f, 0.0f, 0.0f);
    }

    public void DisplayInstructions()
    {
        SoundController.Instance.PlayClick();
        _playButton.gameObject.SetActive(false);
        _instructionsButton.gameObject.SetActive(false);
        _optionsButton.gameObject.SetActive(false);
        _creditsButton.gameObject.SetActive(false);
        _exitButton.gameObject.SetActive(false);
        _backButton.gameObject.SetActive(true);
        _instructionText.gameObject.SetActive(true);
        _barImage.transform.position = new Vector3(-1920.0f, 0.0f, 0.0f);
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
        _barImage.transform.position = new Vector3(-1920.0f, myButton.transform.position.y, 0.0f);
        // Only stop the highlight bar tween, KillAll also stopped every other running tween in the menu.
        _barImage.transform.DOKill();
        _barImage.transform.DOLocalMoveX(0.0f, 1.0f).SetEase(Ease.OutExpo).SetLink(_barImage.gameObject);
    }

    public void OnMybuttonExit(Button myButton)
    {
        _barImage.transform.DOKill();
        _barImage.transform.DOLocalMoveX(-1920.0f, 1.0f).SetEase(Ease.OutExpo).SetLink(_barImage.gameObject);
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
