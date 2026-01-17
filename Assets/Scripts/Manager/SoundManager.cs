using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private AudioMixerGroup _bgmGroup;
    [SerializeField] private AudioMixerGroup _sfxGroup;
    [SerializeField] private int _initialPoolSize = 10;

    [Header("BGM Source")]
    [SerializeField] private AudioSource _bgmSource;

    private List<AudioSource> _sfxSources;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitPool();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitPool()
    {
        _sfxSources = new List<AudioSource>();

        for (int i = 0; i < _initialPoolSize; i++)
        {
            CreateNewSource();
        }

        if (_bgmSource == null)
        {
            _bgmSource = gameObject.AddComponent<AudioSource>();
        }
        _bgmSource.outputAudioMixerGroup = _bgmGroup;
        _bgmSource.loop = true;
    }

    private AudioSource CreateNewSource()
    {
        GameObject obj = new GameObject($"SFX_Source_{_sfxSources.Count}");
        obj.transform.SetParent(transform);
        
        AudioSource source = obj.AddComponent<AudioSource>();
        source.outputAudioMixerGroup = _sfxGroup;
        source.playOnAwake = false;
        
        _sfxSources.Add(source);
        return source;
    }

    private AudioSource GetAvailableSource()
    {
        foreach (AudioSource source in _sfxSources)
        {
            if (!source.isPlaying)
            {
                return source;
            }
        }

        return CreateNewSource();
    }

    public void PlaySFX(AudioClip clip, float volume = 1.0f, float pitchRandomness = 0f)
    {
        if (clip == null) return;

        AudioSource source = GetAvailableSource();
        
        source.spatialBlend = 0f;
        source.transform.position = transform.position;
        source.clip = clip;
        source.volume = volume;
        source.pitch = 1.0f + Random.Range(-pitchRandomness, pitchRandomness);
        
        source.Play();
    }

    public void PlaySFXAt(AudioClip clip, Vector3 position, float volume = 1.0f, float pitchRandomness = 0.1f)
    {
        if (clip == null) return;

        AudioSource source = GetAvailableSource();

        source.spatialBlend = 1.0f;
        source.transform.position = position;
        source.clip = clip;
        source.volume = volume;
        source.pitch = 1.0f + Random.Range(-pitchRandomness, pitchRandomness);
        
        source.minDistance = 5f;
        source.maxDistance = 50f;
        source.rolloffMode = AudioRolloffMode.Linear;

        source.Play();
    }

    public void PlayBGM(AudioClip clip, float volume = 0.5f)
    {
        if (_bgmSource.clip == clip) return;

        _bgmSource.Stop();
        _bgmSource.clip = clip;
        _bgmSource.volume = volume;
        _bgmSource.Play();
    }
}
