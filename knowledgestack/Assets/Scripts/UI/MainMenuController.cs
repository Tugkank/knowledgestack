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

        // Mock User ID - In production, get this from Google Play Games / Game Center
        private string currentUserId = "mock_user_123"; 

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

            RefreshStats();
        }

        public void StartGame()
        {
            Debug.Log("Starting Game...");
            UnityEngine.SceneManagement.SceneManager.LoadScene("Game"); // Ensure scene is named "Game"
        }

        public void RefreshStats()
        {
            // First, show loading state
            if(levelText) levelText.text = "SEVİYE: ...";
            if(scoreText) scoreText.text = "PUAN: ...";

            // If NetworkManager exists, try to fetch data
            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.LoginOrRegister(currentUserId, 
                    (data) => {
                        UpdateUI(data.level, data.totalScore);
                    },
                    (error) => {
                        Debug.LogWarning("Could not fetch stats (Offline Mode): " + error);
                        // Fallback or offline mode could be handled here
                        UpdateUI(1, 0); // Default/Offline values
                    }
                );
            }
            else
            {
                // For testing without NetworkManager in scene, show dummy data
                Debug.LogWarning("NetworkManager instance not found. Using dummy data.");
                UpdateUI(5, 1250); 
            }
        }

        private void UpdateUI(int level, int score)
        {
            if (levelText) levelText.text = $"SEVİYE: {level}";
            if (scoreText) scoreText.text = $"PUAN: {score}";
        }
    }
}
