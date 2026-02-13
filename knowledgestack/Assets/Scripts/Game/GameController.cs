using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

namespace KnowledgeStack.Game
{
    public class GameController : MonoBehaviour
    {
        [Header("UI References")]
        public TextMeshProUGUI levelText;
        public TextMeshProUGUI questionCounterText;
        public TextMeshProUGUI questionText;
        public Transform blockArea;
        public Transform answersContainer;
        public GameObject exitPopup;
        public TextMeshProUGUI correctStatsText;
        public TextMeshProUGUI wrongStatsText;

        [Header("Game Settings")]
        public Color[] blockColors; // 10 Colors will be set in inspector or code
        public float blockDropSpeed = 1000f;

        // State
        private int currentLevel = 1;
        private int currentQuestionIndex = 0; // 0 to 9
        private int correctAnswers = 0;
        private int wrongAnswers = 0;
        private int lastColorIndex = -1;
        
        private Stack<GameObject> spawnedBlocks = new Stack<GameObject>();
        
        private List<QuestionData> currentLevelQuestions;
        private QuestionData activeQuestion;
        private bool isAnsweringAllowed = true;

        private void Start()
        {
            // Auto-find UI if not assigned (Since we generate UI at runtime/editor)
            InitializeUIReferences();
            
            // Define Palette if not set (Vibrant, Neon-like colors)
            if (blockColors == null || blockColors.Length == 0)
            {
                blockColors = new Color[] {
                    new Color(1f, 0.2f, 0.2f), // Red
                    new Color(0.2f, 1f, 0.2f), // Green
                    new Color(0.2f, 0.2f, 1f), // Blue
                    new Color(1f, 1f, 0.2f),   // Yellow
                    new Color(1f, 0.2f, 1f),   // Magenta
                    new Color(0.2f, 1f, 1f),   // Cyan
                    new Color(1f, 0.5f, 0f),   // Orange
                    new Color(0.5f, 0f, 1f),   // Purple
                    new Color(0f, 1f, 0.5f),   // Mint
                    new Color(1f, 0.2f, 0.6f)  // Pink
                };
            }

            StartLevel(currentLevel);
        }

        private void InitializeUIReferences()
        {
            if(GameObject.Find("GameCanvas") == null)
            {
                Debug.LogError("GameCanvas not found! run 'Generate Game UI' from Tools menu.");
                return;
            }

            Transform header = GameObject.Find("HeaderContainer").transform;
            levelText = header.Find("LevelText").GetComponent<TextMeshProUGUI>();
            questionCounterText = header.Find("QuestionCounter").GetComponent<TextMeshProUGUI>();
            questionText = header.Find("QuestionText").GetComponent<TextMeshProUGUI>();
            
            blockArea = GameObject.Find("BlockArea").transform;
            answersContainer = GameObject.Find("AnswersContainer").transform;
            
            // Stats
            Transform statsObj = GameObject.Find("ScoreStats").transform;
            correctStatsText = statsObj.Find("CorrectStats").GetComponent<TextMeshProUGUI>();
            wrongStatsText = statsObj.Find("WrongStats").GetComponent<TextMeshProUGUI>();
            
            // Assign Button Listeners
            Button exitBtn = GameObject.Find("TopBar/ExitButton").GetComponent<Button>();
            exitBtn.onClick.AddListener(OnExitButtonClicked);

            // Popup
            Transform popupPanel = GameObject.Find("GameCanvas").transform.Find("ExitPopupPanel");
            if(popupPanel != null) 
            {
                exitPopup = popupPanel.gameObject;
                popupPanel.Find("PopupBox/Buttons/YesButton").GetComponent<Button>().onClick.AddListener(ConfirmExit);
                popupPanel.Find("PopupBox/Buttons/NoButton").GetComponent<Button>().onClick.AddListener(CancelExit);
            }
        }

        private void StartLevel(int level)
        {
            currentLevel = level;
            currentQuestionIndex = 0;
            
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

            levelText.text = "SEVİYE " + currentLevel;
            
            // Clear Blocks
            foreach(Transform child in blockArea) Destroy(child.gameObject);
            spawnedBlocks.Clear();

            LoadNextQuestion();
        }

        private void LoadNextQuestion()
        {
            if (currentQuestionIndex >= currentLevelQuestions.Count)
            {
                Debug.Log("Level Complete!");
                currentLevel++;
                StartLevel(currentLevel);
                return;
            }

            activeQuestion = currentLevelQuestions[currentQuestionIndex];
            
            // UI Update
            questionCounterText.text = $"{currentQuestionIndex + 1}/10";
            questionText.text = activeQuestion.text_tr; 
            
            SetupAnswerButtons(activeQuestion);
            isAnsweringAllowed = true;
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
                
                // Reset colors
                btn.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
                
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

            bool isCorrect = (answer == activeQuestion.answer);

            if (isCorrect)
            {
                btn.GetComponent<Image>().color = Color.green; // Correct -> Green
                correctAnswers++;
                SpawnBlock(); 
            }
            else
            {
                btn.GetComponent<Image>().color = Color.red; // Wrong -> Red
                wrongAnswers++;
                SpawnAcidEffect(); // Trigger Acid
            }
            
            UpdateStatsUI();

            // Wait longer if acid effect is playing
            float waitTime = isCorrect ? 1.5f : 2.5f; 
            StartCoroutine(WaitAndNext(waitTime));
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
        
        // --- Block Logic ---
        
        private void SpawnBlock()
        {
            // Calculate based on STACK HEIGHT not Question Index to prevent gaps
            int stackIndex = spawnedBlocks.Count; 
            
            if (stackIndex >= 10) return; // Cap at 10

            float areaHeight = blockArea.GetComponent<RectTransform>().rect.height;
            float areaWidth = blockArea.GetComponent<RectTransform>().rect.width; // 900
            
            float blockHeight = areaHeight / 10f;
            
            // Width calculation
            float widthRatio = 0.9f - (stackIndex * 0.06f); 
            float blockWidth = areaWidth * widthRatio;

            // Create Object
            GameObject blockObj = new GameObject("Block_" + stackIndex);
            blockObj.transform.SetParent(blockArea, false);
            
            Image img = blockObj.AddComponent<Image>();
            Color c = GetRandomNextColor();
            img.color = c;
            
            Sprite roundedSprite = Resources.Load<Sprite>("UI/Skin/Background");
            if (roundedSprite != null) 
            {
                img.sprite = roundedSprite;
                img.type = Image.Type.Sliced;
            }

            Outline outline = blockObj.AddComponent<Outline>();
            outline.effectColor = new Color(1,1,1,0.5f);
            outline.effectDistance = Vector2.one * 2;

            RectTransform rect = blockObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(blockWidth, blockHeight);
            
            float targetY = (-areaHeight / 2f) + (stackIndex * blockHeight) + (blockHeight / 2f);
            float startY = (areaHeight / 2f) + blockHeight; 
            
            rect.anchoredPosition = new Vector2(0, startY);
            
            // Add to stack
            spawnedBlocks.Push(blockObj);

            StartCoroutine(AnimateBlockDrop(rect, targetY));
        }

        // --- Acid Effect Logic ---
        
        private void SpawnAcidEffect()
        {
            if (spawnedBlocks.Count == 0) return; // Nothing to destroy

            GameObject targetBlock = spawnedBlocks.Peek(); // Don't pop yet
            RectTransform targetRect = targetBlock.GetComponent<RectTransform>();
            
            // Create Acid Blob
            GameObject acidObj = new GameObject("AcidBlob");
            acidObj.transform.SetParent(blockArea, false);
            SetTopInHierarchy(acidObj);

            Image img = acidObj.AddComponent<Image>();
            img.color = new Color(0.2f, 1f, 0f, 0.9f); // Toxic Green
            
            // Use Knob/Circle sprite if possible
            Sprite knob = Resources.Load<Sprite>("UI/Skin/Knob");
            if(knob != null) img.sprite = knob;

            RectTransform acidRect = acidObj.GetComponent<RectTransform>();
            acidRect.sizeDelta = new Vector2(80, 80);
            
            // Start from top, horizontally aligned with target block
            float startY = (blockArea.GetComponent<RectTransform>().rect.height / 2f);
            acidRect.anchoredPosition = new Vector2(0, startY);

            StartCoroutine(AnimateAcidDrop(acidObj, targetBlock));
        }

        private void SetTopInHierarchy(GameObject obj) { obj.transform.SetAsLastSibling(); }

        private IEnumerator AnimateAcidDrop(GameObject acidObj, GameObject targetBlock)
        {
            if(targetBlock == null) { Destroy(acidObj); yield break; }
            
            RectTransform acidRect = acidObj.GetComponent<RectTransform>();
            RectTransform targetRect = targetBlock.GetComponent<RectTransform>();
            float targetY = targetRect.anchoredPosition.y + (targetRect.rect.height/2f);

            // Drop Animation
            while (acidRect.anchoredPosition.y > targetY)
            {
                float newY = acidRect.anchoredPosition.y - (1500f * Time.deltaTime);
                acidRect.anchoredPosition = new Vector2(0, newY);
                yield return null;
            }

            // Impact!
            // 1. Destroy Acid
            Destroy(acidObj);

            // 2. Melt Block Animation (Scale down? Fade?)
            float duration = 0.5f;
            float elapsed = 0;
            Vector3 startScale = targetBlock.transform.localScale;
            Image blockImg = targetBlock.GetComponent<Image>();
            Color startColor = blockImg.color;
            Color acidColor = new Color(0.2f, 1f, 0f, 0f); // Green transparent

            while (elapsed < duration)
            {
                if(targetBlock == null) break;
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                
                targetBlock.transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
                blockImg.color = Color.Lerp(startColor, acidColor, t);
                yield return null;
            }

            if(targetBlock != null) Destroy(targetBlock);
            
            // Remove from stack logic
            if(spawnedBlocks.Count > 0) spawnedBlocks.Pop();
        }

        private Color GetRandomNextColor()
        {
            int index = Random.Range(0, blockColors.Length);
            // Retry if same as last (and we have enough colors)
            if (blockColors.Length > 1) 
            {
                while (index == lastColorIndex)
                {
                    index = Random.Range(0, blockColors.Length);
                }
            }
            lastColorIndex = index;
            return blockColors[index];
        }

        private IEnumerator AnimateBlockDrop(RectTransform rect, float targetY)
        {
            while (rect.anchoredPosition.y > targetY)
            {
                float newY = rect.anchoredPosition.y - (blockDropSpeed * Time.deltaTime);
                if (newY < targetY) newY = targetY;
                
                rect.anchoredPosition = new Vector2(0, newY);
                yield return null;
            }
            rect.anchoredPosition = new Vector2(0, targetY);
        }

        // --- Exit Logic ---
        private void OnExitButtonClicked()
        {
            if (exitPopup) exitPopup.SetActive(true);
        }

        private void ConfirmExit()
        {
            // Return to Main Menu Logic
            // UnityEngine.SceneManagement.SceneManager.LoadScene(0);
            Debug.Log("Exiting to Main Menu...");
            // For now just close popup
            if (exitPopup) exitPopup.SetActive(false);
        }

        private void CancelExit()
        {
            if (exitPopup) exitPopup.SetActive(false);
        }
    }
}
