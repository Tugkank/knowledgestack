using UnityEngine;
using TMPro; // Assuming you are using TextMeshPro for UI texts
using KnowledgeStack.Core;

namespace KnowledgeStack.UI
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class LocalizeText : MonoBehaviour
    {
        [Header("Translations")]
        [TextArea(2, 5)]
        public string turkishText;
        
        [TextArea(2, 5)]
        public string englishText;

        private TextMeshProUGUI textComponent;

        private void Awake()
        {
            textComponent = GetComponent<TextMeshProUGUI>();
        }

        private void Start()
        {
            // Subscribe to the language change event so this updates automatically
            LanguageManager.OnLanguageChanged += UpdateText;
            
            // Set the initial text based on the current language
            UpdateText(LanguageManager.CurrentLanguage);
        }

        private void OnDestroy()
        {
            // Always unsubscribe from static events to prevent memory leaks
            LanguageManager.OnLanguageChanged -= UpdateText;
        }

        public void UpdateText(Language lang)
        {
            if (textComponent == null) return;

            switch (lang)
            {
                case Language.English:
                    // Fallback to Turkish if English is completely empty
                    textComponent.text = string.IsNullOrEmpty(englishText) ? turkishText : englishText;
                    break;
                case Language.Turkish:
                default:
                    // Fallback to English if Turkish is completely empty
                    textComponent.text = string.IsNullOrEmpty(turkishText) ? englishText : turkishText;
                    break;
            }
        }

        // Optional: Method to set text dynamically from code (e.g., for data loaded from server)
        public void SetDynamicText(string tr, string en)
        {
            turkishText = tr;
            englishText = en;
            UpdateText(LanguageManager.CurrentLanguage);
        }
    }
}
