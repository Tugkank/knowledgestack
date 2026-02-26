using UnityEngine;
using System.Collections;

namespace KnowledgeStack.Core
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Music Settings")]
        [Tooltip("Oyun boyunca çalacak arka plan müziğini buraya sürükleyin")]
        public AudioClip backgroundMusicClip;

        private AudioSource bgmSource;
        private AudioSource sfxSource;

        private Coroutine resumeBGMRoutine;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeAudioSources();
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }

        private void InitializeAudioSources()
        {
            if (bgmSource == null)
            {
                bgmSource = gameObject.AddComponent<AudioSource>();
                bgmSource.loop = true;
                bgmSource.playOnAwake = true;
                
                if (backgroundMusicClip != null)
                {
                    bgmSource.clip = backgroundMusicClip;
                    bgmSource.Play();
                }
            }

            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
                sfxSource.loop = false;
                sfxSource.playOnAwake = false;
            }

            // Load initial volumes
            UpdateVolumes();
        }

        public void UpdateVolumes()
        {
            float musicVol = PlayerPrefs.GetFloat("MusicVolume", 1f);
            float sfxVol = PlayerPrefs.GetFloat("SFXVolume", 1f);

            if (bgmSource != null) bgmSource.volume = musicVol;
            if (sfxSource != null) sfxSource.volume = sfxVol;
        }

        public void PlaySFX(AudioClip clip)
        {
            if (clip == null || sfxSource == null) return;

            // Stop any existing resume routine
            if (resumeBGMRoutine != null)
            {
                StopCoroutine(resumeBGMRoutine);
            }

            // Pause BGM
            if (bgmSource != null && bgmSource.isPlaying)
            {
                bgmSource.Pause();
            }

            // Play the SFX
            sfxSource.PlayOneShot(clip, PlayerPrefs.GetFloat("SFXVolume", 1f));

            // Start routine to resume BGM after clip finishes
            resumeBGMRoutine = StartCoroutine(ResumeBGMAfterClip(clip.length));
        }

        private IEnumerator ResumeBGMAfterClip(float delay)
        {
            // Wait for the SFX to finish (realtime to ignore Time.timeScale if paused)
            yield return new WaitForSecondsRealtime(delay);

            if (bgmSource != null)
            {
                bgmSource.UnPause();
            }
        }

        public void SetMusicVolume(float volume)
        {
            PlayerPrefs.SetFloat("MusicVolume", volume);
            PlayerPrefs.Save();
            if (bgmSource != null) bgmSource.volume = volume;
        }

        public void SetSFXVolume(float volume)
        {
            PlayerPrefs.SetFloat("SFXVolume", volume);
            PlayerPrefs.Save();
            if (sfxSource != null) sfxSource.volume = volume;
        }
    }
}
