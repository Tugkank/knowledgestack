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
            
            Transform header = GameObject.Find("HeaderContainer").transform;
            if (header != null)
            {
                var lvl = header.Find("LevelText");
                if(lvl) levelText = lvl.GetComponent<TextMeshProUGUI>();
                
                var qc = header.Find("QuestionCounter");
                if(qc) questionCounterText = qc.GetComponent<TextMeshProUGUI>();
                
                var qt = header.Find("QuestionText");
                if(qt) questionText = qt.GetComponent<TextMeshProUGUI>();
            }
            else Debug.LogError("HeaderContainer not found!");

            var ac = GameObject.Find("AnswersContainer");
            if(ac) 
            {
                answersContainer = ac.transform;
                // Save default button color from the first button found
                if(answersContainer.childCount > 0)
                {
                    var firstBtn = answersContainer.GetChild(0).GetComponent<Image>();
                    if(firstBtn != null) defaultButtonColor = firstBtn.color;
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

            if(levelText != null) levelText.text = "SEVİYE " + currentLevel;
            
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
                
                // Play Win Sound
                if (winSound != null)
                {
                    if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(winSound);
                    else if (audioSource != null) audioSource.PlayOneShot(winSound, PlayerPrefs.GetFloat("SFXVolume", 1f));
                }

                currentLevel++;
                
                StartLevel(currentLevel);
                return;
            }

            activeQuestion = currentLevelQuestions[currentQuestionIndex];
            
            // UI Update
            if(questionCounterText != null) questionCounterText.text = $"{currentQuestionIndex + 1}/{currentLevelQuestions.Count}";
            if(questionText != null) questionText.text = activeQuestion.text_tr; 
            
            SetupAnswerButtons(activeQuestion);
            isAnsweringAllowed = true;
            StartQuestionTimer();
        }

        private void SetupAnswerButtons(QuestionData q)
        {
            var options = QuestionManager.Instance.GetShuffledAnswers(q);
            
            // Ensure we have 4 buttons in container
            for (int i = 0; i < 4; i++)
            {
                Transform btnTrans = answersContainer.GetChild(i);
                Button btn = btnTrans.GetComponent<Button>();
                TextMeshProUGUI txt = btnTrans.GetComponentInChildren<TextMeshProUGUI>();
                
                txt.text = options[i];
                
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

            bool isCorrect = (answer == activeQuestion.answer);

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
            Debug.Log($"Level Failed ({correctAnswers}/{currentLevelQuestions.Count} Correct). Retrying Level {currentLevel}...");
            yield return new WaitForSeconds(2.5f); // Wait to see the full hangman before restart
            StartLevel(currentLevel);
        }

        private void HighlightCorrectAnswer()
        {
            foreach (Transform child in answersContainer)
            {
                Button btn = child.GetComponent<Button>();
                TextMeshProUGUI txt = child.GetComponentInChildren<TextMeshProUGUI>();
                
                if (btn != null && txt != null)
                {
                    if (txt.text == activeQuestion.answer)
                    {
                        if (correctButtonSprite != null) 
                            btn.GetComponent<Image>().sprite = correctButtonSprite;
                        else 
                            btn.GetComponent<Image>().color = Color.green;
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
            correctStatsText.text = "Doğru: " + correctAnswers;
            wrongStatsText.text = "Yanlış: " + wrongAnswers;
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
    }
}
