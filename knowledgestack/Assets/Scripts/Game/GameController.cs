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

        private Sprite[] blockSprites;

        private void Start()
        {
            // Auto-find UI if not assigned (Since we generate UI at runtime/editor)
            InitializeUIReferences();
            
            // Load Custom Block Sprites
            blockSprites = Resources.LoadAll<Sprite>("Blocks");
            if (blockSprites != null && blockSprites.Length > 0)
            {
                Debug.Log($"Loaded {blockSprites.Length} custom block sprites.");
            }
            else
            {
                Debug.LogWarning("No custom block sprites found in Resources/Blocks. Using default colors.");
            }
            
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
        
        // ... (HandleQuestionsLoaded, OnDestroy, InitializeUIReferences, StartLevel, LoadNextQuestion, SetupAnswerButtons, OnAnswerSelected, UpdateStatsUI, WaitAndNext unchanged) ...

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
            
            // Custom Sprite Logic
            if (blockSprites != null && blockSprites.Length > 0)
            {
                // Pick random custom sprite
                Sprite randomSprite = blockSprites[Random.Range(0, blockSprites.Length)];
                img.sprite = randomSprite;
                img.color = Color.white; // Show original sprite colors
                
                // Preserve Aspect Ratio? Maybe not, we want them to fill the block slot.
                // img.preserveAspect = true; 
            }
            else
            {
                // Fallback to Colored Blocks
                Color c = GetRandomNextColor();
                img.color = c;
                
                Sprite roundedSprite = Resources.Load<Sprite>("UI/Skin/Background");
                if (roundedSprite != null) 
                {
                    img.sprite = roundedSprite;
                    img.type = Image.Type.Sliced;
                }
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
