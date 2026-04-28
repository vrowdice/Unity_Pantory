using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public class SoundManager : Singleton<SoundManager>
{
    [Header("Settings")]
    [SerializeField] private AudioMixerGroup _bgmGroup;
    [SerializeField] private AudioMixerGroup _sfxGroup;
    [SerializeField] private int _initialPoolSize = 10;

    [Header("BGM Source")]
    [SerializeField] private AudioSource _bgmSource;

    private List<AudioSource> _sfxSources;
    
    private float _bgmVolume = 0.5f;
    private float _sfxVolume = 1.0f;
    
    private static System.Type _audioManagerType;
    private static FieldInfo _onPlaySoundEventField;
    private Action<AudioClip, float> _evoAudioEventHandler;

    protected override void Awake()
    {
        base.Awake();
        
        if (Instance != this) return;
        
        InitPool();
        SubscribeToEvoAudioManager();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        UnsubscribeFromEvoAudioManager();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SaveLoadManager saveLoad = SaveLoadManager.Instance;
        if (saveLoad != null)
        {
            saveLoad.TryApplyUserSettingsVolumesToSound();
        }
    }

    private void SubscribeToEvoAudioManager()
    {
        if (_audioManagerType == null)
        {
            _audioManagerType = System.Type.GetType("Evo.UI.AudioManager, Evo.UI");
            if (_audioManagerType == null)
            {
                _audioManagerType = System.Type.GetType("Evo.UI.AudioManager");
            }
            
            if (_audioManagerType != null)
            {
                _onPlaySoundEventField = _audioManagerType.GetField("OnPlaySoundEvent", BindingFlags.Public | BindingFlags.Static);
            }
        }

        if (_onPlaySoundEventField != null && _evoAudioEventHandler == null)
        {
            _evoAudioEventHandler = OnEvoUiPlaySound;
            object currentEvent = _onPlaySoundEventField.GetValue(null);
            
            if (currentEvent != null)
            {
                Action<AudioClip, float> currentAction = currentEvent as Action<AudioClip, float>;
                if (currentAction != null)
                {
                    currentAction = currentAction + _evoAudioEventHandler;
                    _onPlaySoundEventField.SetValue(null, currentAction);
                }
                else
                {
                    _onPlaySoundEventField.SetValue(null, _evoAudioEventHandler);
                }
            }
            else
            {
                _onPlaySoundEventField.SetValue(null, _evoAudioEventHandler);
            }
        }
    }

    private void UnsubscribeFromEvoAudioManager()
    {
        if (_onPlaySoundEventField != null && _evoAudioEventHandler != null)
        {
            object currentEvent = _onPlaySoundEventField.GetValue(null);
            
            if (currentEvent != null)
            {
                Action<AudioClip, float> currentAction = currentEvent as Action<AudioClip, float>;
                if (currentAction != null)
                {
                    currentAction -= _evoAudioEventHandler;
                    _onPlaySoundEventField.SetValue(null, currentAction);
                }
            }
        }
    }

    private void OnEvoUiPlaySound(AudioClip clip, float volume)
    {
        PlaySFX(clip, volume, 0f);
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
        
        LoadVolumeSettings();
    }
    
    private void LoadVolumeSettings()
    {
        _bgmVolume = 0.5f;
        _sfxVolume = 1f;
        ApplyBGMVolume();
        ApplySFXVolume();
    }

    public void ApplyVolumesFromUserSettings(float bgmVolume, float sfxVolume)
    {
        _bgmVolume = Mathf.Clamp01(bgmVolume);
        _sfxVolume = Mathf.Clamp01(sfxVolume);
        ApplyBGMVolume();
        ApplySFXVolume();
    }
    
    private void ApplyBGMVolume()
    {
        if (_bgmSource != null)
        {
            _bgmSource.volume = _bgmVolume;
        }
    }
    
    private void ApplySFXVolume()
    {
        foreach (AudioSource source in _sfxSources)
        {
            if (source != null)
            {
                if (!source.isPlaying)
                {
                    source.volume = _sfxVolume;
                }
                else
                {
                    source.volume = _sfxVolume;
                }
            }
        }
    }

    private AudioSource CreateNewSource()
    {
        GameObject obj = new GameObject($"SFX_Source_{_sfxSources.Count}");
        obj.transform.SetParent(transform);
        
        AudioSource source = obj.AddComponent<AudioSource>();
        source.outputAudioMixerGroup = _sfxGroup;
        source.playOnAwake = false;
        source.volume = _sfxVolume;
        
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
        source.volume = volume * _sfxVolume;
        source.pitch = 1.0f + UnityEngine.Random.Range(-pitchRandomness, pitchRandomness);
        
        source.Play();
    }

    public void PlaySFXAt(AudioClip clip, Vector3 position, float volume = 1.0f, float pitchRandomness = 0.1f)
    {
        if (clip == null) return;

        AudioSource source = GetAvailableSource();

        source.spatialBlend = 1.0f;
        source.transform.position = position;
        source.clip = clip;
        source.volume = volume * _sfxVolume;
        source.pitch = 1.0f + UnityEngine.Random.Range(-pitchRandomness, pitchRandomness);
        
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
        _bgmSource.volume = volume * _bgmVolume;
        _bgmSource.Play();
    }
    
    public void SetBGMVolume(float volume)
    {
        _bgmVolume = Mathf.Clamp01(volume);
        ApplyBGMVolume();
    }
    
    public void SetSFXVolume(float volume)
    {
        _sfxVolume = Mathf.Clamp01(volume);
        ApplySFXVolume();
    }
    
    public float GetBGMVolume()
    {
        return _bgmVolume;
    }
    
    public float GetSFXVolume()
    {
        return _sfxVolume;
    }
    
    public void SaveSettings()
    {
        SaveLoadManager saveLoad = SaveLoadManager.Instance;
        if (saveLoad != null)
        {
            saveLoad.SaveUserSettingsFromCurrentState();
        }
    }
}
