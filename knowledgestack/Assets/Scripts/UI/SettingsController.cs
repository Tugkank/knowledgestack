using UnityEngine;
using UnityEngine.UI;
using KnowledgeStack.Core;

namespace KnowledgeStack.UI
{
    public class SettingsController : MonoBehaviour
    {
        [Header("UI References")]
        public Slider musicSlider;
        public Slider sfxSlider;
        public Button btnTurkish;
        public Button btnEnglish;
        public Button btnClose;

        [Header("Audio Settings")]
        [Tooltip("Eğer işaretliyse, Music Slider ana sesi (AudioListener.volume) kontrol eder.")]
        public bool controlMasterVolume = true;

        private void Start()
        {
            // Load saved volumes (default to 1.0 / 100%)
            float musicVol = PlayerPrefs.GetFloat("MusicVolume", 1f);
            float sfxVol = PlayerPrefs.GetFloat("SFXVolume", 1f);

            // Setup Music Slider
            if (musicSlider != null)
            {
                musicSlider.value = musicVol;
                musicSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
            }

            // Setup SFX Slider
            if (sfxSlider != null)
            {
                sfxSlider.value = sfxVol;
                sfxSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
            }

            // Setup Buttons
            if (btnTurkish != null) btnTurkish.onClick.AddListener(SetTurkish);
            if (btnEnglish != null) btnEnglish.onClick.AddListener(SetEnglish);
            if (btnClose != null) btnClose.onClick.AddListener(CloseSettings);

            // Apply initial volume
            if (controlMasterVolume)
            {
                AudioListener.volume = musicVol;
            }
        }

        private void OnMusicVolumeChanged(float value)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetMusicVolume(value);
            }
            else
            {
                PlayerPrefs.SetFloat("MusicVolume", value);
                PlayerPrefs.Save();
            }
            
            if (controlMasterVolume)
            {
                AudioListener.volume = value;
            }
        }

        private void OnSFXVolumeChanged(float value)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetSFXVolume(value);
            }
            else
            {
                PlayerPrefs.SetFloat("SFXVolume", value);
                PlayerPrefs.Save();
            }
        }

        public void SetTurkish()
        {
            LanguageManager.CurrentLanguage = Language.Turkish;
            Debug.Log("Dil Türkçe olarak ayarlandı.");
        }

        public void SetEnglish()
        {
            LanguageManager.CurrentLanguage = Language.English;
            Debug.Log("Language set to English.");
        }

        public void CloseSettings()
        {
            // Popup'ı gizler
            gameObject.SetActive(false);
        }
    }
}
