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
        SoundController._instance.PlayClick();
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
        SoundController._instance.PlayClick();
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
        SoundController._instance.PlayClick();
        _playButton.gameObject.SetActive(false);
        _instructionsButton.gameObject.SetActive(false);
        _optionsButton.gameObject.SetActive(false);
        _creditsButton.gameObject.SetActive(false);
        _exitButton.gameObject.SetActive(false);
        _optionsPanel.SetActive(true);
        _barImage.transform.position = new Vector3(-1920.0f, 0.0f, 0.0f);
        _soundToggle.isOn = SoundController._instance._soundOn;
        _musicToggle.isOn = SoundController._instance._musicOn;
    }

    public void HideOptions()
    {
        SoundController._instance.PlayClick();
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
        SoundController._instance.PlayClick();
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
        SoundController._instance.PlayClick();
        Application.Quit();
    }

    public void OnMybuttonEnter(Button myButton)
    {
        SoundController._instance.PlayHover();
        _barImage.transform.position = new Vector3(-1920.0f, myButton.transform.position.y, 0.0f);
        DOTween.KillAll();
        _barImage.transform.DOLocalMoveX(0.0f, 1.0f).SetEase(Ease.OutExpo);
    }

    public void OnMybuttonExit(Button myButton)
    {
        DOTween.KillAll();
        _barImage.transform.DOLocalMoveX(-1920.0f, 1.0f).SetEase(Ease.OutExpo);
    }

    public void SoundToggleClicked()
    {
        SoundController._instance.PlayClick();
        SoundController._instance._soundOn = _soundToggle.isOn;
    }

    public void MusicToggleClicked()
    {
        SoundController._instance.PlayClick();
        SoundController._instance._musicOn = _musicToggle.isOn;
    }
}
