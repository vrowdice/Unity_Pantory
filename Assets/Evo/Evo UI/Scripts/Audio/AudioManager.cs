using UnityEngine;
using UnityEngine.Audio;
using System;

namespace Evo.UI
{
    [DisallowMultipleComponent]
    [HelpURL(Constants.HELP_URL)]
    [AddComponentMenu("Evo/UI/Audio/Audio Manager")]
    public class AudioManager : MonoBehaviour
    {
        public static Action<AudioClip, float> OnPlaySoundEvent;

        public static void PlayClip(AudioClip clip, float volume = 1, bool bypassEffects = true, AudioMixerGroup mixerGroup = null)
        {
            if (clip == null) return;

            if (OnPlaySoundEvent != null)
            {
                OnPlaySoundEvent.Invoke(clip, volume);
                return;
            }

            PlayClipLegacy(clip, volume);
        }

        private static void PlayClipLegacy(AudioClip clip, float volume)
        {
            GameObject go = new GameObject("TempAudio_Legacy");
            AudioSource source = go.AddComponent<AudioSource>();
            source.clip = clip;
            source.volume = volume;
            source.Play();
            Destroy(go, clip.length + 0.1f);
        }
    }
}
