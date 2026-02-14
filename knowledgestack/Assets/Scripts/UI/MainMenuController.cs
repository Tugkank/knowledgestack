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
            // Find UI elements if not assigned (Since we generate them via Editor script)
            if (levelText == null) levelText = GameObject.Find("LevelText")?.GetComponent<TextMeshProUGUI>();
            if (scoreText == null) scoreText = GameObject.Find("ScoreText")?.GetComponent<TextMeshProUGUI>();

            RefreshStats();
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
                        Debug.LogError("Failed to fetch stats: " + error);
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
