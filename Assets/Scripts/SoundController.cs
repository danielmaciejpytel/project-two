using UnityEngine;
using UnityEngine.SceneManagement;

public class SoundController : MonoBehaviour
{
    private const string SoundVolumePrefKey = "Options.SoundVolume";
    private const string MusicVolumePrefKey = "Options.MusicVolume";
    // Older versions saved on/off switches instead of volumes.
    private const string OldSoundPrefKey = "Options.SoundOn";
    private const string OldMusicPrefKey = "Options.MusicOn";
    // Music volume at the slider's maximum (the slider's default of 0.5 gives the old fixed volume of 0.2).
    private const float MusicMaxVolume = 0.4f;

    [SerializeField] private AudioClip _clickClip;
    [SerializeField] private AudioClip _hoverClip;
    [SerializeField] private AudioClip _callUnitClip;
    [SerializeField] private AudioClip _endTurnClip;
    [Tooltip("Index 0 is the menu theme, the rest are played in game.")]
    [SerializeField] private AudioClip[] _gameMusic;

    private AudioSource _myAudioSource;
    private AudioSource _myMusicSource;
    private float _soundVolume;
    private float _musicVolume;

    public static SoundController Instance { get; private set; }

    // Volumes go from 0 (silent) to 1 and are saved as soon as they change.
    public float SoundVolume
    {
        get => _soundVolume;
        set
        {
            _soundVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(SoundVolumePrefKey, _soundVolume);
        }
    }

    public float MusicVolume
    {
        get => _musicVolume;
        set
        {
            _musicVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(MusicVolumePrefKey, _musicVolume);
            if (_myMusicSource != null) _myMusicSource.volume = _musicVolume * MusicMaxVolume;
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
        _soundVolume = PlayerPrefs.GetFloat(SoundVolumePrefKey, PlayerPrefs.GetInt(OldSoundPrefKey, 1) == 1 ? 1.0f : 0.0f);
        _musicVolume = PlayerPrefs.GetFloat(MusicVolumePrefKey, PlayerPrefs.GetInt(OldMusicPrefKey, 1) == 1 ? 0.5f : 0.0f);
        _myMusicSource.volume = _musicVolume * MusicMaxVolume;
    }

    private void Start()
    {
        PlayNextClip();
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
        CancelInvoke(nameof(PlayNextClip));
        PlayNextClip();
    }

    private void PlayNextClip()
    {
        if (_gameMusic == null || _gameMusic.Length == 0) return;

        int song = 0;
        if (SceneManager.GetActiveScene().name != GameController.MenuSceneName && _gameMusic.Length > 1)
            song = Random.Range(1, _gameMusic.Length);
        _myMusicSource.clip = _gameMusic[song];
        _myMusicSource.Play();
        Invoke(nameof(PlayNextClip), _myMusicSource.clip.length);
    }

    private void PlayOneShot(AudioClip clip)
    {
        if (clip != null && _soundVolume > 0.0f) _myAudioSource.PlayOneShot(clip, _soundVolume);
    }

    // Sound effects played by other objects (unit attacks, deaths) follow the same volume.
    public void PlayClip(AudioClip clip) => PlayOneShot(clip);

    public void PlayClick() => PlayOneShot(_clickClip);

    public void PlayHover() => PlayOneShot(_hoverClip);

    public void PlayCall() => PlayOneShot(_callUnitClip);

    public void PlayEndTurn() => PlayOneShot(_endTurnClip);
}
