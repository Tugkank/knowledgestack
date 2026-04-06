using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using KnowledgeStack.Game.Hangman;
using KnowledgeStack.Core;

namespace KnowledgeStack.Game
{
    public class GameController : MonoBehaviour
    {
        [Header("UI References")]
        public TextMeshProUGUI levelText;
        public TextMeshProUGUI questionCounterText;
        public TextMeshProUGUI questionText;
        public Image timerBar;
        public TextMeshProUGUI timerText;
        public Transform answersContainer;
        public GameObject exitPopup;
        public GameObject settingsPopup;
        public GameObject gameOverPopup;
        public GameObject levelUpPopup;
        public GameObject loadingPanel;
        public TextMeshProUGUI correctStatsText;
        public TextMeshProUGUI wrongStatsText;

        [Header("Game Settings")]
        public float timePerQuestion = 15f;

        [Header("Audio Settings")]
        public AudioClip correctSound;
        public AudioClip wrongSound;
        public AudioClip winSound;
        public AudioClip gameOverSound;

        [Header("Button Sprites")]
        public Sprite defaultButtonSprite;
        public Sprite correctButtonSprite;
        public Sprite wrongButtonSprite;

        [Header("Hangman System")]
        public HangmanManager hangmanManager;

        // State
        private int currentLevel = 1;
        private int currentQuestionIndex = 0; // 0 to 9
        private int correctAnswers = 0;
        private int wrongAnswers = 0;
        
        private List<QuestionData> currentLevelQuestions;
        private QuestionData activeQuestion;
        private bool isAnsweringAllowed = true;
        private Coroutine timerCoroutine;
        private AudioSource audioSource;

        private void Start()
        {
            // Load saved level or default to 1
            currentLevel = PlayerPrefs.GetInt("CurrentLevel", 1);

            // Setup Audio Source
            audioSource = gameObject.GetComponent<AudioSource>();
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;

            // Auto-find UI if not assigned (Since we generate UI at runtime/editor)
            InitializeUIReferences();

            // Wait for QuestionManager Data
            if (QuestionManager.Instance != null)
            {
                if (QuestionManager.Instance.IsDataLoaded)
                {
                    StartLevel(currentLevel);
                }
                else
                {
                    Debug.Log("Waiting for Questions to load...");
                    if (loadingPanel != null) loadingPanel.SetActive(true);
                    QuestionManager.Instance.OnQuestionsLoaded += HandleQuestionsLoaded;
                }
            }
            else
            {
                Debug.LogError("QuestionManager Not Found!");
            }
        }
        
        private void HandleQuestionsLoaded()
        {
            Debug.Log("Questions Loaded! Starting Level.");
            if(QuestionManager.Instance != null) 
                QuestionManager.Instance.OnQuestionsLoaded -= HandleQuestionsLoaded;
            
            if (loadingPanel != null) loadingPanel.SetActive(false);

            StartLevel(currentLevel);
        }

        private void OnDestroy()
        {
            if (QuestionManager.Instance != null)
                QuestionManager.Instance.OnQuestionsLoaded -= HandleQuestionsLoaded;
        }

        private Color defaultButtonColor = Color.white;

        // ... (Start, HandleQuestionsLoaded, OnDestroy methods unchanged) ...

        private void InitializeUIReferences()
        {
            if(GameObject.Find("GameCanvas") == null)
            {
                Debug.LogError("GameCanvas not found! run 'Generate Game UI' from Tools menu.");
                return;
            }

            // ... (Header finding logic unchanged) ...
            
            // Search globally within GameCanvas for the timer elements 
            // since they might be deeply nested in Responsive UI containers
            var canvasObj = GameObject.Find("GameCanvas");
            if (canvasObj != null)
            {
                // Find Texts
                var allTexts = canvasObj.GetComponentsInChildren<TextMeshProUGUI>(true);
                foreach (var t in allTexts)
                {
                    if (t.name == "LevelText") levelText = t;
                    else if (t.name == "QuestionCounter") questionCounterText = t;
                    else if (t.name == "QuestionText") questionText = t;
                    else if (t.name == "Sayaçtext" || t.name == "TimerText") timerText = t;
                }

                // Find Timer Bar (Image)
                var allImages = canvasObj.GetComponentsInChildren<Image>(true);
                foreach (var img in allImages)
                {
                    if (img.gameObject.name.Contains("Sayaçbar iç") || img.gameObject.name == "TimerFill")
                    {
                        timerBar = img;
                        break;
                    }
                }
            }

            var ac = GameObject.Find("AnswersContainer") ?? GameObject.Find("answer container");
            if(ac) 
            {
                answersContainer = ac.transform;
                // Save default button color from the first button found safely
                var buttons = answersContainer.GetComponentsInChildren<Button>(true);
                if(buttons.Length > 0)
                {
                    var firstBtnImg = buttons[0].GetComponent<Image>();
                    if(firstBtnImg != null) defaultButtonColor = firstBtnImg.color;
                }
            }
            else Debug.LogError("AnswersContainer not found!");
            
            // ... (Stats and Exit logic unchanged) ...
            
            // Stats
            var stats = GameObject.Find("ScoreStats");
            if (stats != null)
            {
                var cs = stats.transform.Find("CorrectStats");
                if(cs) correctStatsText = cs.GetComponent<TextMeshProUGUI>();
                
                var ws = stats.transform.Find("WrongStats");
                if(ws) wrongStatsText = ws.GetComponent<TextMeshProUGUI>();
            }
            else Debug.LogError("ScoreStats not found!");
            
            // Assign Button Listeners
            var exitBtnObj = GameObject.Find("TopBar/ExitButton");
            if(exitBtnObj != null)
            {
                exitBtnObj.GetComponent<Button>().onClick.AddListener(OnExitButtonClicked);
            }

            var settingsBtnObj = GameObject.Find("TopBar/SettingsButton");
            if(settingsBtnObj != null)
            {
                settingsBtnObj.GetComponent<Button>().onClick.AddListener(OpenSettings);
            }

            // Popup
            Transform popupPanel = GameObject.Find("GameCanvas").transform.Find("ExitPopupPanel");
            if(popupPanel != null) 
            {
                exitPopup = popupPanel.gameObject;
                var yesBtn = popupPanel.Find("PopupBox/Buttons/YesButton");
                if(yesBtn) yesBtn.GetComponent<Button>().onClick.AddListener(ConfirmExit);
                
                var noBtn = popupPanel.Find("PopupBox/Buttons/NoButton");
                if(noBtn) noBtn.GetComponent<Button>().onClick.AddListener(CancelExit);
            }

            // Loading Panel
            Transform loadPanel = GameObject.Find("GameCanvas").transform.Find("LoadingPanel");
            if(loadPanel != null) loadingPanel = loadPanel.gameObject;

            // Level Up Popup Listeners
            if (levelUpPopup != null)
            {
                var nextBtn = levelUpPopup.transform.Find("PopupBox/Buttons/YesButton");
                if (nextBtn) nextBtn.GetComponent<Button>().onClick.AddListener(OnLevelUpNextClicked);

                var menuBtn = levelUpPopup.transform.Find("PopupBox/Buttons/NoButton");
                if (menuBtn) menuBtn.GetComponent<Button>().onClick.AddListener(GoToMainMenu);
            }

            // Game Over Popup Listeners
            if (gameOverPopup != null)
            {
                var retryBtn = gameOverPopup.transform.Find("PopupBox/Buttons/YesButton");
                if (retryBtn) retryBtn.GetComponent<Button>().onClick.AddListener(OnGameOverRetryClicked);

                var menuBtn = gameOverPopup.transform.Find("PopupBox/Buttons/NoButton");
                if (menuBtn) menuBtn.GetComponent<Button>().onClick.AddListener(GoToMainMenu);
            }
        }

        private void StartLevel(int level)
        {
            currentLevel = level;
            currentQuestionIndex = 0;
            correctAnswers = 0;
            wrongAnswers = 0;
            UpdateStatsUI();
            
            // Get Questions from Manager
            if (QuestionManager.Instance != null)
            {
                currentLevelQuestions = QuestionManager.Instance.GetQuestionsForLevel(currentLevel);
            }
            else
            {
                Debug.LogError("QuestionManager not found!");
                return;
            }

            if (levelText != null)
            {
                if (KnowledgeStack.Core.LanguageManager.CurrentLanguage == KnowledgeStack.Core.Language.Turkish)
                {
                    levelText.text = "SEVİYE " + currentLevel;
                }
                else
                {
                    levelText.text = "LEVEL " + currentLevel;
                }
            }
            
            if (hangmanManager != null) hangmanManager.InitializeForLevel(currentLevel);

            LoadNextQuestion();
        }

        private void LoadNextQuestion()
        {
            if (currentLevelQuestions == null)
            {
                Debug.LogError("Current Level Questions list is null!");
                return;
            }

            if (currentQuestionIndex >= currentLevelQuestions.Count)
            {
                // Reached end of questions for this level without getting fully hung
                Debug.Log($"Level {currentLevel} Complete! Promoting to next level.");
                
                // Sync to Server BEFORE incrementing locally 
                // We send the level they just beat + 1 as their new server level. Score is correctAnswers * 1 for win.
                if (KnowledgeStack.Networking.NetworkManager.Instance != null && activeQuestion != null)
                {
                    string userId = PlayerPrefs.GetString("UserId", SystemInfo.deviceUniqueIdentifier);
                    KnowledgeStack.Networking.NetworkManager.Instance.SyncProgress(
                        userId, currentLevel + 1, correctAnswers, activeQuestion.id,
                        () => Debug.Log("Progress synced to server successfully!"),
                        (err) => Debug.LogWarning("Failed to sync progress: " + err)
                    );
                }

                // SAVE PROGRESS LOCALLY IMMEDIATELY
                PlayerPrefs.SetInt("CurrentLevel", currentLevel + 1);
                int currentTotalScore = PlayerPrefs.GetInt("TotalScore", 0);
                PlayerPrefs.SetInt("TotalScore", currentTotalScore + correctAnswers);
                PlayerPrefs.Save();

                // Play Win Sound
                if (winSound != null)
                {
                    if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(winSound);
                    else if (audioSource != null) audioSource.PlayOneShot(winSound, PlayerPrefs.GetFloat("SFXVolume", 1f));
                }

                // Show Level Up Popup instead of auto-starting next level
                if (levelUpPopup != null)
                {
                    levelUpPopup.SetActive(true);
                }
                else
                {
                    // Fallback if popup not assigned
                    currentLevel++;
                    StartLevel(currentLevel);
                }
                return;
            }

            activeQuestion = currentLevelQuestions[currentQuestionIndex];
            
            // UI Update
            if (questionCounterText != null) questionCounterText.text = $"{currentQuestionIndex + 1}/{currentLevelQuestions.Count}";
            
            if (questionText != null)
            {
                if (KnowledgeStack.Core.LanguageManager.CurrentLanguage == KnowledgeStack.Core.Language.Turkish)
                {
                    questionText.text = activeQuestion.text_tr;
                }
                else
                {
                    // Fallback to text_tr if text_en is empty, assuming QuestionData has text_en
                    questionText.text = string.IsNullOrEmpty(activeQuestion.text_en) ? activeQuestion.text_tr : activeQuestion.text_en; 
                }
            }
            
            SetupAnswerButtons(activeQuestion);
            isAnsweringAllowed = true;
            StartQuestionTimer();
        }

        private void SetupAnswerButtons(QuestionData q)
        {
            if (answersContainer == null) return;

            var options = QuestionManager.Instance.GetShuffledAnswers(q);
            
            // Get all buttons recursively (penetrates Row 1, Row 2 containers)
            Button[] buttons = answersContainer.GetComponentsInChildren<Button>(true);
            
            // Ensure we have 4 buttons to work with
            int buttonCount = Mathf.Min(4, buttons.Length);

            for (int i = 0; i < buttonCount; i++)
            {
                Button btn = buttons[i];
                TextMeshProUGUI txt = btn.GetComponentInChildren<TextMeshProUGUI>();
                
                if(txt != null) txt.text = options[i];
                
                // Reset sprite to default
                if (defaultButtonSprite != null)
                {
                    btn.GetComponent<Image>().sprite = defaultButtonSprite;
                }
                btn.GetComponent<Image>().color = Color.white; // Ensure no tint is left
                
                // Click Event
                string selectedAnswer = options[i];
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => OnAnswerSelected(btn, selectedAnswer));
            }
        }

        private void OnAnswerSelected(Button btn, string answer)
        {
            if (!isAnsweringAllowed) return;
            isAnsweringAllowed = false;
            StopQuestionTimer();

            bool isCorrect = false;
            
            if (KnowledgeStack.Core.LanguageManager.CurrentLanguage == KnowledgeStack.Core.Language.Turkish)
            {
                isCorrect = (answer == activeQuestion.answer_tr);
            }
            else
            {
                string engAns = string.IsNullOrEmpty(activeQuestion.answer_eng) ? activeQuestion.answer_tr : activeQuestion.answer_eng;
                isCorrect = (answer == engAns);
            }

            // Get current SFX volume from Settings
            float currentSFXVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);

            if (isCorrect)
            {
                // Only play generic 'correct' sound if it's NOT the last question of the level
                if (currentQuestionIndex < currentLevelQuestions.Count)
                {
                    if (correctSound != null)
                    {
                        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(correctSound);
                        else if (audioSource != null) audioSource.PlayOneShot(correctSound, currentSFXVolume);
                    }
                }
                
                if (correctButtonSprite != null) btn.GetComponent<Image>().sprite = correctButtonSprite;
                else btn.GetComponent<Image>().color = Color.green; // Fallback
                
                correctAnswers++;
                UpdateStatsUI();
                StartCoroutine(WaitAndNext(1.5f));
            }
            else
            {
                if (wrongButtonSprite != null) btn.GetComponent<Image>().sprite = wrongButtonSprite;
                else btn.GetComponent<Image>().color = Color.red; // Fallback
                
                HighlightCorrectAnswer();

                wrongAnswers++;
                UpdateStatsUI();

                if (hangmanManager != null)
                {
                    bool isGameOver = hangmanManager.HandleMistake();
                    if (isGameOver)
                    {
                        if (gameOverSound != null)
                        {
                            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(gameOverSound);
                            else if (audioSource != null) audioSource.PlayOneShot(gameOverSound, currentSFXVolume);
                        }
                        Debug.Log("Game Over! The character was fully hung.");
                        StartCoroutine(LevelFailedRoutine());
                        return; // Stop to prevent loading next question
                    }
                }
                
                // Play standard wrong sound if we didn't die
                if (wrongSound != null)
                {
                    if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(wrongSound);
                    else if (audioSource != null) audioSource.PlayOneShot(wrongSound, currentSFXVolume);
                }
                
                StartCoroutine(WaitAndNext(2.5f)); // Wait longer on wrong answer to see part
            }
        }

        private IEnumerator LevelFailedRoutine()
        {
            Debug.Log($"Level Failed ({correctAnswers}/{currentLevelQuestions.Count} Correct).");
            yield return new WaitForSeconds(2.5f); // Wait to see the full hangman
            
            if (gameOverPopup != null)
            {
                gameOverPopup.SetActive(true);
            }
            else
            {
                // Fallback
                StartLevel(currentLevel);
            }
        }

        private void HighlightCorrectAnswer()
        {
            if (answersContainer == null) return;

            Button[] buttons = answersContainer.GetComponentsInChildren<Button>(true);

            foreach (Button btn in buttons)
            {
                TextMeshProUGUI txt = btn.GetComponentInChildren<TextMeshProUGUI>();
                
                if (btn != null && txt != null)
                {
                    string correctAns = "";
                    if (KnowledgeStack.Core.LanguageManager.CurrentLanguage == KnowledgeStack.Core.Language.Turkish)
                    {
                        correctAns = activeQuestion.answer_tr;
                    }
                    else
                    {
                        correctAns = string.IsNullOrEmpty(activeQuestion.answer_eng) ? activeQuestion.answer_tr : activeQuestion.answer_eng;
                    }

                    if (txt.text == correctAns)
                    {
                        if (correctButtonSprite != null) 
                            btn.GetComponent<Image>().sprite = correctButtonSprite;
                        else 
                            btn.GetComponent<Image>().color = Color.green;
                        break; // Stop after highlighting the first correct match
                    }
                }
            }
        }

        // --- Timer Logic ---
        private void StartQuestionTimer()
        {
            StopQuestionTimer(); // Ensure no previous timer is running
            if (timerBar != null)
            {
                timerBar.fillAmount = 1f;
                timerCoroutine = StartCoroutine(QuestionTimerRoutine());
            }
        }

        private void StopQuestionTimer()
        {
            if (timerCoroutine != null)
            {
                StopCoroutine(timerCoroutine);
                timerCoroutine = null;
            }
        }

        private IEnumerator QuestionTimerRoutine()
        {
            float timeRemaining = timePerQuestion;

            while (timeRemaining > 0)
            {
                yield return null;
                timeRemaining -= Time.deltaTime;
                
                if (timerBar != null)
                {
                    timerBar.fillAmount = timeRemaining / timePerQuestion;
                    // Optional: Change color to red when time is low
                    if (timeRemaining < (timePerQuestion * 0.3f))
                        timerBar.color = Color.Lerp(Color.red, Color.yellow, Mathf.PingPong(Time.time * 5, 1));
                    else
                        timerBar.color = Color.green;
                }

                if (timerText != null)
                {
                    timerText.text = Mathf.CeilToInt(timeRemaining).ToString();
                }
            }

            TimeUp();
        }

        private void TimeUp()
        {
            if (!isAnsweringAllowed) return;
            isAnsweringAllowed = false;

            Debug.Log("Time is UP!");
            
            float currentSFXVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);
            
            // Treat as wrong answer
            wrongAnswers++;
            UpdateStatsUI();

            if (timerBar != null) timerBar.fillAmount = 0;
            if (timerText != null) timerText.text = "0";

            if (hangmanManager != null)
            {
                bool isGameOver = hangmanManager.HandleMistake();
                if (isGameOver)
                {
                    if (gameOverSound != null)
                    {
                        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(gameOverSound);
                        else if (audioSource != null) audioSource.PlayOneShot(gameOverSound, currentSFXVolume);
                    }
                    Debug.Log("Game Over! Time ran out and character was fully hung.");
                    StartCoroutine(LevelFailedRoutine());
                    return; 
                }
            }

            // Play standard wrong sound if we didn't die
            if (wrongSound != null)
            {
                if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(wrongSound);
                else if (audioSource != null) audioSource.PlayOneShot(wrongSound, currentSFXVolume);
            }

            StartCoroutine(WaitAndNext(2.5f));
        }

        private void UpdateStatsUI()
        {
            if (KnowledgeStack.Core.LanguageManager.CurrentLanguage == KnowledgeStack.Core.Language.Turkish)
            {
                if (correctStatsText != null) correctStatsText.text = "Doğru: " + correctAnswers;
                if (wrongStatsText != null) wrongStatsText.text = "Yanlış: " + wrongAnswers;
            }
            else
            {
                if (correctStatsText != null) correctStatsText.text = "Correct: " + correctAnswers;
                if (wrongStatsText != null) wrongStatsText.text = "Wrong: " + wrongAnswers;
            }
        }

        private IEnumerator WaitAndNext(float delay)
        {
            yield return new WaitForSeconds(delay);
            currentQuestionIndex++;
            LoadNextQuestion();
        }

        // --- Exit Logic ---
        private void OnExitButtonClicked()
        {
            if (exitPopup) exitPopup.SetActive(true);
        }

        private void ConfirmExit()
        {
            Debug.Log("Exiting to Main Menu...");
            SceneManager.LoadScene("MainMenu");
        }

        private void CancelExit()
        {
            if (exitPopup) exitPopup.SetActive(false);
        }

        public void OpenSettings()
        {
            if (settingsPopup != null)
            {
                settingsPopup.SetActive(true);
            }
            else
            {
                Debug.LogWarning("Settings Popup is not assigned in GameController!");
            }
        }

        // --- Popup Action Methods ---
        public void OnGameOverRetryClicked()
        {
            if (gameOverPopup != null) gameOverPopup.SetActive(false);
            StartLevel(currentLevel); // Retry current level
        }

        public void OnLevelUpNextClicked()
        {
            if (levelUpPopup != null) levelUpPopup.SetActive(false);
            
            currentLevel++; // Increment level here instead of earlier
            PlayerPrefs.SetInt("CurrentLevel", currentLevel);
            PlayerPrefs.Save();

            StartLevel(currentLevel); 
        }

        public void GoToMainMenu()
        {
            Time.timeScale = 1f; // Ensure time is running
            SceneManager.LoadScene("MainMenu"); // Adjust scene name if needed
        }
    }
}
