using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SoundController : MonoBehaviour
{
    [SerializeField] private AudioClip _clickClip;
    [SerializeField] private AudioClip _hoverClip;
    [SerializeField] private AudioClip _callUnitClip;
    [SerializeField] private AudioClip _endTurnClip;
    [SerializeField] private AudioClip[] _gameMusic;
    private AudioSource _myAudioSource;
    private AudioSource _myMusicSource;
    private bool _myMusicOn;
    public static SoundController _instance;
    public bool _soundOn { get; set; }
    public bool _musicOn
    {
        get { return _myMusicOn; }
        set
        {
            _myMusicOn = value;
            if (_myMusicOn) PlayNextClip();
            else _myMusicSource.Pause();
        }
    }

    private void Awake()
    {
        Application.targetFrameRate = 60;
        DontDestroyOnLoad(this.gameObject);
        if (_instance == null)
            _instance = this;
        else
        {
            Destroy(this);
        }
    }

    private void Start()
    {
        _myAudioSource = gameObject.AddComponent<AudioSource>(); 
        _myMusicSource = gameObject.AddComponent<AudioSource>();
        _myMusicSource.loop = false;
        _myMusicSource.volume = 0.2f;
        _soundOn = true;
        _musicOn = true;
    }

    private void PlayNextClip()
    {
        if (_musicOn)
        {
            int song = Random.Range(1, _gameMusic.Length);
            if (SceneManager.GetActiveScene().name == "MenuScene") song = 0;
            _myMusicSource.clip = _gameMusic[song];
            _myMusicSource.Play();
            Invoke("PlayNextClip", _myMusicSource.clip.length);
        }
    }

    public void PlayClick()
    {
        if (_soundOn) _myAudioSource.PlayOneShot(_clickClip);
    }

    public void PlayHover()
    {
        if (_soundOn) _myAudioSource.PlayOneShot(_hoverClip);
    }

    public void PlayCall()
    {
        if (_soundOn) _myAudioSource.PlayOneShot(_callUnitClip);
    }

    public void PlayEndTurn()
    {
        if (_soundOn) _myAudioSource.PlayOneShot(_endTurnClip);
    }
}
