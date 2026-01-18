using System.Reflection;
using UnityEngine;
using UnityEngine.Audio;

namespace Evo.UI
{
    /// <summary>
    /// Manages audio-related methods for runtime usage.
    /// </summary>
    [DisallowMultipleComponent]
    [HelpURL(Constants.HELP_URL)]
    [AddComponentMenu("Evo/UI/Audio/Audio Manager")]
    public class AudioManager : MonoBehaviour
    {
        // Static Instances
        public static AudioManager instance;

        [Header("Audio")]
        public AudioSource audioSource;
        public AudioMixerGroup audioMixerGroup;

        [Header("Settings")]
        [SerializeField] private bool dontDestroyOnLoad = true;

        // Cached reflection for SoundManager (to avoid assembly dependency)
        private static System.Type _soundManagerType;
        private static PropertyInfo _soundManagerInstanceProperty;
        private static MethodInfo _soundManagerPlaySFXMethod;

        void Awake()
        {
            if (instance == null) 
            { 
                instance = this;

                if (dontDestroyOnLoad) 
                {
                    if (instance.transform.parent != null) { instance.transform.parent = null; }
                    DontDestroyOnLoad(instance);
                }
            }
        }

        static void CreateInstance()
        {
            if (instance != null)
                return;

            var go = new GameObject { name = "[Evo UI - Audio]" };
            go.AddComponent<AudioManager>();
        }

        /// <summary>
        /// Initializes reflection cache for SoundManager (called once on first use).
        /// </summary>
        static void InitializeSoundManagerReflection()
        {
            if (_soundManagerType != null) return; // Already initialized

            _soundManagerType = System.Type.GetType("SoundManager");
            if (_soundManagerType != null)
            {
                _soundManagerInstanceProperty = _soundManagerType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
                _soundManagerPlaySFXMethod = _soundManagerType.GetMethod("PlaySFX", new System.Type[] { typeof(AudioClip), typeof(float), typeof(float) });
            }
        }

        /// <summary>
        /// Plays an audio clip via AudioSource.
        /// Uses SoundManager if available (via reflection to avoid assembly dependency), otherwise falls back to legacy AudioSource.
        /// </summary>
        public static void PlayClip(AudioClip clip, float volume = 1, bool bypassEffects = true, AudioMixerGroup mixerGroup = null)
        {
            if (clip == null) { return; }

            // Try to use SoundManager via reflection (preferred for pooling and performance)
            InitializeSoundManagerReflection();
            if (_soundManagerType != null && _soundManagerInstanceProperty != null && _soundManagerPlaySFXMethod != null)
            {
                object soundManagerInstance = _soundManagerInstanceProperty.GetValue(null);
                if (soundManagerInstance != null)
                {
                    _soundManagerPlaySFXMethod.Invoke(soundManagerInstance, new object[] { clip, Mathf.Clamp01(volume), 0f });
                    return;
                }
            }

            // Fallback to legacy AudioSource method
            if (instance == null) { CreateInstance(); }
            if (instance.audioSource == null) { instance.audioSource = instance.gameObject.AddComponent<AudioSource>(); }

            instance.audioSource.outputAudioMixerGroup = mixerGroup == null ? instance.audioMixerGroup : mixerGroup;
            instance.audioSource.bypassEffects = !bypassEffects;
            instance.audioSource.bypassReverbZones = !bypassEffects;
            instance.audioSource.PlayOneShot(clip, Mathf.Clamp01(volume));
        }
    }
}