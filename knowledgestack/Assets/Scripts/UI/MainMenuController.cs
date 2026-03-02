using UnityEngine;
using TMPro;
using KnowledgeStack.Networking;

namespace KnowledgeStack.UI
{
    public class MainMenuController : MonoBehaviour
    {
        [Header("UI References")]
        public TextMeshProUGUI levelText;
        public TextMeshProUGUI scoreText;
        public GameObject settingsPopup;
        public GameObject comingSoonPopup;

        // Mock User ID - In production, get this from Google Play Games / Game Center
        private string currentUserId = "mock_user_123"; 
        private Coroutine popupCoroutine;

        private void Start()
        {
            // Find UI elements if not assigned
            if (levelText == null) levelText = GameObject.Find("LevelText")?.GetComponent<TextMeshProUGUI>();
            if (scoreText == null) scoreText = GameObject.Find("ScoreText")?.GetComponent<TextMeshProUGUI>();
            
            // Find Play Button
            var playBtnObj = GameObject.Find("PlayButton");
            if (playBtnObj != null)
            {
                var uBtn = playBtnObj.GetComponent<UnityEngine.UI.Button>();
                if(uBtn != null)
                {
                    uBtn.onClick.RemoveAllListeners();
                    uBtn.onClick.AddListener(StartGame);
                }
            }

            // Find Settings Button
            var settingsBtnObj = GameObject.Find("SettingsButton");
            if (settingsBtnObj != null)
            {
                var sBtn = settingsBtnObj.GetComponent<UnityEngine.UI.Button>();
                if(sBtn != null)
                {
                    sBtn.onClick.RemoveAllListeners();
                    sBtn.onClick.AddListener(OpenSettings);
                }
            }

            RefreshStats();
        }

        public void OpenSettings()
        {
            if (settingsPopup != null)
            {
                settingsPopup.SetActive(true);
            }
            else
            {
                Debug.LogWarning("Settings Popup is not assigned in MainMenuController!");
            }
        }

        public void StartGame()
        {
            Debug.Log("Starting Game...");
            UnityEngine.SceneManagement.SceneManager.LoadScene("Game"); // Ensure scene is named "Game"
        }

        public void RefreshStats()
        {
            // First, show loading state
            // First, show loading state
            if (KnowledgeStack.Core.LanguageManager.CurrentLanguage == KnowledgeStack.Core.Language.Turkish)
            {
                if(levelText) levelText.text = "SEVİYE: ...";
                if(scoreText) scoreText.text = "PUAN: ...";
            }
            else
            {
                if(levelText) levelText.text = "LEVEL: ...";
                if(scoreText) scoreText.text = "SCORE: ...";
            }

            // If NetworkManager exists, try to fetch data
            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.LoginOrRegister(currentUserId, 
                    (data) => {
                        PlayerPrefs.SetInt("CurrentLevel", data.level);
                        PlayerPrefs.Save();
                        UpdateUI(data.level, data.totalScore);
                    },
                    (error) => {
                        Debug.LogWarning("Could not fetch stats (Offline Mode): " + error);
                        PlayerPrefs.SetInt("CurrentLevel", 1);
                        PlayerPrefs.Save();
                        UpdateUI(1, 0); // Default/Offline values
                    }
                );
            }
            else
            {
                // For testing without NetworkManager in scene, show dummy data
                Debug.LogWarning("NetworkManager instance not found. Using dummy data.");
                PlayerPrefs.SetInt("CurrentLevel", 5);
                PlayerPrefs.Save();
                UpdateUI(5, 1250); 
            }
        }

        private void UpdateUI(int level, int score)
        {
            if (KnowledgeStack.Core.LanguageManager.CurrentLanguage == KnowledgeStack.Core.Language.Turkish)
            {
                if (levelText) levelText.text = $"SEVİYE: {level}";
                if (scoreText) scoreText.text = $"PUAN: {score}";
            }
            else
            {
                if (levelText) levelText.text = $"LEVEL: {level}";
                if (scoreText) scoreText.text = $"SCORE: {score}";
            }
        }

        // --- Coming Soon Popup Logic ---
        public void OnComingSoonButtonClicked()
        {
            if (comingSoonPopup == null)
            {
                Debug.LogWarning("Coming Soon Popup is not assigned in MainMenuController!");
                return;
            }

            if (popupCoroutine != null)
            {
                StopCoroutine(popupCoroutine);
            }

            popupCoroutine = StartCoroutine(ShowComingSoonRoutine());
        }

        private System.Collections.IEnumerator ShowComingSoonRoutine()
        {
            comingSoonPopup.SetActive(true);
            yield return new WaitForSeconds(3f);
            comingSoonPopup.SetActive(false);
            popupCoroutine = null;
        }
    }
}
