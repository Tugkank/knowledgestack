using System;
using UnityEngine;

namespace KnowledgeStack.Core
{
    public enum Language
    {
        Turkish,
        English
    }

    public static class LanguageManager
    {
        // Event triggered when the language is changed
        public static event Action<Language> OnLanguageChanged;

        private static Language _currentLanguage = Language.Turkish;
        private static bool _isInitialized = false;

        public static Language CurrentLanguage
        {
            get
            {
                if (!_isInitialized) LoadLanguage();
                return _currentLanguage;
            }
            set
            {
                if (_currentLanguage != value)
                {
                    _currentLanguage = value;
                    // Save to PlayerPrefs so it persists across sessions
                    PlayerPrefs.SetInt("AppLanguage", (int)_currentLanguage);
                    PlayerPrefs.Save();
                    
                    // Notify all listening UI elements to update their text
                    OnLanguageChanged?.Invoke(_currentLanguage);
                }
            }
        }

        public static void LoadLanguage()
        {
            if (PlayerPrefs.HasKey("AppLanguage"))
            {
                _currentLanguage = (Language)PlayerPrefs.GetInt("AppLanguage");
            }
            else
            {
                // Default to system language or Turkish
                _currentLanguage = Application.systemLanguage == SystemLanguage.English ? Language.English : Language.Turkish;
            }
            _isInitialized = true;
        }
    }
}
