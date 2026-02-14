using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

namespace KnowledgeStack.Editor
{
    public class GameInterfaceGenerator : MonoBehaviour
    {
        [MenuItem("Tools/Knowledge Stack/Generate Game UI")]
        public static void Generate()
        {
            // 1. Canvas Setup (Create a separate canvas for Game UI)
            GameObject canvasObj = GameObject.Find("GameCanvas");
            if (canvasObj != null) DestroyImmediate(canvasObj); // Clear old one
            
            canvasObj = new GameObject("GameCanvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1; // Show above Main Menu if active
                
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920); 
            scaler.matchWidthOrHeight = 0.5f;
                
            canvasObj.AddComponent<GraphicRaycaster>();

            // 2. Background (Same logic as MainMenu)
            GameObject bgObj = CreateImage("Background", canvasObj.transform, Color.white);
            SetFullScreen(bgObj.GetComponent<RectTransform>());
            
            Sprite bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Background.jpg"); // Try direct load first
            if (bgSprite == null) bgSprite = Resources.Load<Sprite>("Background");
            
            if (bgSprite != null) bgObj.GetComponent<Image>().sprite = bgSprite;
            else bgObj.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f, 1f);

            // 3. Top Bar (Exit & Settings)
            GameObject topBar = new GameObject("TopBar");
            topBar.transform.SetParent(canvasObj.transform, false);
            RectTransform topRect = topBar.AddComponent<RectTransform>();
            topRect.anchorMin = new Vector2(0, 0.92f);
            topRect.anchorMax = new Vector2(1, 1);
            topRect.offsetMin = Vector2.zero;
            topRect.offsetMax = Vector2.zero;

            GameObject exitBtn = CreateButton("ExitButton", topBar.transform, "ÇIKIŞ", new Color(0.8f, 0.2f, 0.2f, 0.8f), 40);
            RectTransform exitRect = exitBtn.GetComponent<RectTransform>();
            exitRect.anchorMin = new Vector2(0.05f, 0.5f);
            exitRect.anchorMax = new Vector2(0.05f, 0.5f);
            exitRect.sizeDelta = new Vector2(200, 80);
            exitRect.anchoredPosition = new Vector2(100, 0); // Offset from left

            GameObject settingsBtn = CreateButton("SettingsButton", topBar.transform, "AYARLAR", new Color(0.2f, 0.2f, 0.2f, 0.8f), 40);
            RectTransform settingsRect = settingsBtn.GetComponent<RectTransform>();
            settingsRect.anchorMin = new Vector2(0.95f, 0.5f);
            settingsRect.anchorMax = new Vector2(0.95f, 0.5f);
            settingsRect.sizeDelta = new Vector2(200, 80);
            settingsRect.anchoredPosition = new Vector2(-100, 0); // Offset from right

            // 4. Header (Level & Question Info) - Center Top
            GameObject headerContainer = new GameObject("HeaderContainer");
            headerContainer.transform.SetParent(canvasObj.transform, false);
            RectTransform headerRect = headerContainer.AddComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0.1f, 0.75f);
            headerRect.anchorMax = new Vector2(0.9f, 0.9f);
            headerRect.offsetMin = Vector2.zero;
            headerRect.offsetMax = Vector2.zero;

            VerticalLayoutGroup headerLayout = headerContainer.AddComponent<VerticalLayoutGroup>();
            headerLayout.childAlignment = TextAnchor.MiddleCenter;
            headerLayout.spacing = 10;
            headerLayout.childControlHeight = false;
            headerLayout.childControlWidth = true;

            CreateText("LevelText", headerContainer.transform, "SEVİYE 1", 80, Color.white, true);
            CreateText("QuestionCounter", headerContainer.transform, "1/10", 50, new Color(0.8f, 0.8f, 0.8f, 1f));
            CreateText("QuestionText", headerContainer.transform, "Soru metni buraya gelecek...", 50, Color.white);

            // 5. Tetris/Block Area (Middle)
            GameObject tetrisArea = new GameObject("BlockArea");
            tetrisArea.transform.SetParent(canvasObj.transform, false);
            Image tetrisImg = tetrisArea.AddComponent<Image>();
            tetrisImg.color = new Color(0, 0, 0, 0.3f); // Semi-transparent black placeholder
            
            RectTransform tetrisRect = tetrisArea.GetComponent<RectTransform>();
            tetrisRect.anchorMin = new Vector2(0.5f, 0.5f);
            tetrisRect.anchorMax = new Vector2(0.5f, 0.5f);
            tetrisRect.sizeDelta = new Vector2(900, 600); // Big area
            tetrisRect.anchoredPosition = new Vector2(0, -50);
            
            // Add border outline
            Outline tetrisOutline = tetrisArea.AddComponent<Outline>();
            tetrisOutline.effectColor = new Color(0f, 1f, 1f, 0.5f); // Cyan glow
            tetrisOutline.effectDistance = new Vector2(3, -3);

            // 6. Answer Options (Bottom)
            GameObject answersContainer = new GameObject("AnswersContainer");
            answersContainer.transform.SetParent(canvasObj.transform, false);
            RectTransform answersRect = answersContainer.AddComponent<RectTransform>();
            answersRect.anchorMin = new Vector2(0.5f, 0.2f);
            answersRect.anchorMax = new Vector2(0.5f, 0.2f);
            answersRect.sizeDelta = new Vector2(900, 300); // Area for buttons
            
            GridLayoutGroup answersGrid = answersContainer.AddComponent<GridLayoutGroup>();
            answersGrid.cellSize = new Vector2(400, 120);
            answersGrid.spacing = new Vector2(50, 40);
            answersGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            answersGrid.constraintCount = 2; // 2 columns, 2 rows
            answersGrid.childAlignment = TextAnchor.MiddleCenter;

            for (int i = 1; i <= 4; i++)
            {
                CreateButton($"Answer_{i}", answersContainer.transform, $"Şık {i}", new Color(0.1f, 0.1f, 0.1f, 0.8f), 45);
            }

            // 7. Stats (Correct/Wrong) - Very Bottom
            GameObject statsContainer = new GameObject("ScoreStats");
            statsContainer.transform.SetParent(canvasObj.transform, false);
            RectTransform statsRect = statsContainer.AddComponent<RectTransform>();
            statsRect.anchorMin = new Vector2(0.5f, 0.05f);
            statsRect.anchorMax = new Vector2(0.5f, 0.05f);
            statsRect.sizeDelta = new Vector2(900, 80);

            HorizontalLayoutGroup statsLayout = statsContainer.AddComponent<HorizontalLayoutGroup>();
            statsLayout.childAlignment = TextAnchor.MiddleCenter;
            statsLayout.spacing = 150;

            CreateText("CorrectStats", statsContainer.transform, "Doğru: 0", 45, Color.green, true);
            CreateText("WrongStats", statsContainer.transform, "Yanlış: 0", 45, Color.red, true);


            // 8. Exit Popup (Hidden by default)
            GenerateExitPopup(canvasObj.transform);

            Debug.Log("Game Interface Generated!");
        }

        private static void GenerateExitPopup(Transform parent)
        {
            GameObject popupPanel = new GameObject("ExitPopupPanel");
            popupPanel.transform.SetParent(parent, false);
            // Full screen overlay
            SetFullScreen(popupPanel.AddComponent<RectTransform>());
            
            Image bg = popupPanel.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.9f); // Dark overlay

            // Popup Box
            GameObject box = new GameObject("PopupBox");
            box.transform.SetParent(popupPanel.transform, false);
            RectTransform boxRect = box.AddComponent<RectTransform>();
            boxRect.sizeDelta = new Vector2(1000, 600); // Even wider box
            
            Image boxImg = box.AddComponent<Image>();
            boxImg.color = new Color(0.15f, 0.15f, 0.2f, 1f);
            box.AddComponent<Outline>().effectColor = Color.white;

            // Text
            GameObject textObj = CreateText("Message", box.transform, "Oyundan çıkarsanız bu seviyeye tekrar başlamanız gerekmektedir.\n\nÇıkmak istiyor musunuz?", 40, Color.white);
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.1f, 0.4f); // Top part of box
            textRect.anchorMax = new Vector2(0.9f, 0.9f);
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            // Buttons
            GameObject btnContainer = new GameObject("Buttons");
            btnContainer.transform.SetParent(box.transform, false);
            RectTransform btnRect = btnContainer.AddComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0.5f, 0.15f); // Lowered
            btnRect.anchorMax = new Vector2(0.5f, 0.15f);
            btnRect.sizeDelta = new Vector2(900, 120);
            
            HorizontalLayoutGroup grp = btnContainer.AddComponent<HorizontalLayoutGroup>();
            grp.spacing = 100;
            grp.childAlignment = TextAnchor.MiddleCenter;
            grp.childControlWidth = false;
            grp.childControlHeight = false;

            GameObject yesBtn = CreateButton("YesButton", btnContainer.transform, "EVET", Color.red, 40);
            yesBtn.GetComponent<RectTransform>().sizeDelta = new Vector2(300, 90);
            
            GameObject noBtn = CreateButton("NoButton", btnContainer.transform, "HAYIR", Color.gray, 40);
            noBtn.GetComponent<RectTransform>().sizeDelta = new Vector2(300, 90);

            popupPanel.SetActive(false); // Hide initially
        }

        // --- Helpers ---

        private static void SetFullScreen(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static GameObject CreateText(string name, Transform parent, string content, int fontSize, Color color, bool bold = false)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
            tmp.text = content;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.enableWordWrapping = true;
            if (bold) tmp.fontStyle = FontStyles.Bold;
            return obj;
        }

        private static GameObject CreateButton(string name, Transform parent, string text, Color color, int fontSize)
        {
            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);
            
            Image img = btnObj.AddComponent<Image>();
            img.color = color;
            // Frosted/Rounded look
            Sprite roundedSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
            if (roundedSprite != null) { img.sprite = roundedSprite; img.type = Image.Type.Sliced; }

            btnObj.AddComponent<Button>().targetGraphic = img;

            GameObject textObj = CreateText("Text", btnObj.transform, text, fontSize, Color.white);
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            SetFullScreen(textRect);
            
            return btnObj;
        }

        private static GameObject CreateImage(string name, Transform parent, Color color)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            Image img = obj.AddComponent<Image>();
            img.color = color;
            return obj;
        }
    }
}
