using UnityEngine;
using UnityEngine.SceneManagement;

public class SoundController : MonoBehaviour
{
    private const string MenuSceneName = "MenuScene";
    private const string SoundPrefKey = "Options.SoundOn";
    private const string MusicPrefKey = "Options.MusicOn";

    [SerializeField] private AudioClip _clickClip;
    [SerializeField] private AudioClip _hoverClip;
    [SerializeField] private AudioClip _callUnitClip;
    [SerializeField] private AudioClip _endTurnClip;
    [Tooltip("Index 0 is the menu theme, the rest are played in game.")]
    [SerializeField] private AudioClip[] _gameMusic;
    [SerializeField, Range(0.0f, 1.0f)] private float _musicVolume = 0.2f;

    private AudioSource _myAudioSource;
    private AudioSource _myMusicSource;
    private bool _soundOn;
    private bool _musicOn;

    public static SoundController Instance { get; private set; }

    public bool SoundOn
    {
        get => _soundOn;
        set
        {
            _soundOn = value;
            PlayerPrefs.SetInt(SoundPrefKey, value ? 1 : 0);
        }
    }

    public bool MusicOn
    {
        get => _musicOn;
        set
        {
            _musicOn = value;
            PlayerPrefs.SetInt(MusicPrefKey, value ? 1 : 0);
            CancelInvoke(nameof(PlayNextClip));
            if (_musicOn) PlayNextClip();
            else _myMusicSource.Pause();
        }
    }

    private void Awake()
    {
        // The menu scene contains its own SoundController, so returning to the menu would otherwise create duplicates.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Application.targetFrameRate = 60;

        _myAudioSource = gameObject.AddComponent<AudioSource>();
        _myMusicSource = gameObject.AddComponent<AudioSource>();
        _myMusicSource.loop = false;
        _myMusicSource.volume = _musicVolume;
        _soundOn = PlayerPrefs.GetInt(SoundPrefKey, 1) == 1;
    }

    private void Start()
    {
        MusicOn = PlayerPrefs.GetInt(MusicPrefKey, 1) == 1;
        SceneManager.activeSceneChanged += OnActiveSceneChanged;
    }

    private void OnDestroy()
    {
        if (Instance != this) return;
        SceneManager.activeSceneChanged -= OnActiveSceneChanged;
        PlayerPrefs.Save();
        Instance = null;
    }

    private void OnActiveSceneChanged(Scene previous, Scene next)
    {
        // Switch between the menu theme and the in-game playlist without waiting for the current track to finish.
        if (_musicOn)
        {
            CancelInvoke(nameof(PlayNextClip));
            PlayNextClip();
        }
    }

    private void PlayNextClip()
    {
        if (!_musicOn || _gameMusic == null || _gameMusic.Length == 0) return;

        int song = 0;
        if (SceneManager.GetActiveScene().name != MenuSceneName && _gameMusic.Length > 1)
            song = Random.Range(1, _gameMusic.Length);
        _myMusicSource.clip = _gameMusic[song];
        _myMusicSource.Play();
        Invoke(nameof(PlayNextClip), _myMusicSource.clip.length);
    }

    private void PlayOneShot(AudioClip clip)
    {
        if (_soundOn && clip != null) _myAudioSource.PlayOneShot(clip);
    }

    public void PlayClick() => PlayOneShot(_clickClip);

    public void PlayHover() => PlayOneShot(_hoverClip);

    public void PlayCall() => PlayOneShot(_callUnitClip);

    public void PlayEndTurn() => PlayOneShot(_endTurnClip);
}
