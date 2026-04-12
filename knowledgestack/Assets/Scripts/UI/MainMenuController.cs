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

        // In production, get this from Google Play Games / Game Center
        private string currentUserId; 
        private Coroutine popupCoroutine;
        private UnityEngine.UI.Button playButton;

        private void Awake()
        {
            if (!PlayerPrefs.HasKey("UserId"))
            {
                PlayerPrefs.SetString("UserId", SystemInfo.deviceUniqueIdentifier);
                PlayerPrefs.Save();
            }
            currentUserId = PlayerPrefs.GetString("UserId");
        }

        private void Start()
        {
            // Find UI elements if not assigned
            if (levelText == null) levelText = GameObject.Find("LevelText")?.GetComponent<TextMeshProUGUI>();
            if (scoreText == null) scoreText = GameObject.Find("ScoreText")?.GetComponent<TextMeshProUGUI>();
            
            // Find Play Button
            var playBtnObj = GameObject.Find("PlayButton");
            if (playBtnObj != null)
            {
                playButton = playBtnObj.GetComponent<UnityEngine.UI.Button>();
                if(playButton != null)
                {
                    playButton.onClick.RemoveAllListeners();
                    playButton.onClick.AddListener(StartGame);
                    // Disable until stats refresh is complete
                    playButton.interactable = false;
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

            // Prevent LocalizeText from overwriting our dynamic numbers
            if (levelText != null && levelText.GetComponent<LocalizeText>() != null) 
                Destroy(levelText.GetComponent<LocalizeText>());
                
            if (scoreText != null && scoreText.GetComponent<LocalizeText>() != null) 
                Destroy(scoreText.GetComponent<LocalizeText>());

            KnowledgeStack.Core.LanguageManager.OnLanguageChanged += OnLanguageChanged;

            RefreshStats();
        }

        private void OnDestroy()
        {
            KnowledgeStack.Core.LanguageManager.OnLanguageChanged -= OnLanguageChanged;
        }

        private void OnLanguageChanged(KnowledgeStack.Core.Language lang)
        {
            int savedLevel = PlayerPrefs.GetInt("CurrentLevel", 1);
            int savedScore = PlayerPrefs.GetInt("TotalScore", 0);
            UpdateUI(savedLevel, savedScore);
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
                        int localLevel = PlayerPrefs.GetInt("CurrentLevel", 1);
                        int localScore = PlayerPrefs.GetInt("TotalScore", 0);

                        // Prevent server from overwriting local progress if local is higher
                        // (e.g. if the user played offline and server is outdated)
                        int agreedLevel = Mathf.Max(localLevel, data.level);
                        int agreedScore = Mathf.Max(localScore, data.totalScore);

                        PlayerPrefs.SetInt("CurrentLevel", agreedLevel);
                        PlayerPrefs.SetInt("TotalScore", agreedScore); 
                        PlayerPrefs.Save();
                        
                        // Push to server if local was ahead (e.g. they played offline)
                        if (localLevel > data.level)
                        {
                            NetworkManager.Instance.SyncProgress(currentUserId, agreedLevel, 0, -1, null, null);
                        }

                        UpdateUI(agreedLevel, agreedScore);
                        if (playButton != null) playButton.interactable = true;
                    },
                    (error) => {
                        Debug.LogWarning("Could not fetch stats (Offline Mode): " + error);
                        // OFFLINE MODE / API FAILED: DO NOT reset to 1! Use the cached local values so player doesn't lose progress.
                        int savedLevel = PlayerPrefs.GetInt("CurrentLevel", 1);
                        int savedScore = PlayerPrefs.GetInt("TotalScore", 0);
                        UpdateUI(savedLevel, savedScore); 
                        
                        if (playButton != null) playButton.interactable = true;
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
                
                if (playButton != null) playButton.interactable = true;
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
