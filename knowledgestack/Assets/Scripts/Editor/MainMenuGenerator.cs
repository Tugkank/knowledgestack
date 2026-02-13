using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

namespace KnowledgeStack.Editor
{
    public class MainMenuGenerator : MonoBehaviour
    {
        [MenuItem("Tools/Knowledge Stack/Generate Main Menu")]
        public static void Generate()
        {
            // 1. Canvas Setup
            GameObject canvasObj = GameObject.Find("Canvas");
            if (canvasObj == null)
            {
                canvasObj = new GameObject("Canvas");
                Canvas canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                
                CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080, 1920); // Portrait Reference
                scaler.matchWidthOrHeight = 0.5f;
                
                canvasObj.AddComponent<GraphicRaycaster>();
            }

            // 2. Background
            GameObject bgObj = CreateImage("Background", canvasObj.transform, Color.white);
            RectTransform bgRect = bgObj.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            
            // Auto-fix texture import settings
            string bgPath = CheckAndFixBackgroundImport();
            
            Sprite bgSprite = null;
            if (!string.IsNullOrEmpty(bgPath))
            {
                bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>(bgPath);
            }
            
            if (bgSprite != null)
            {
                bgObj.GetComponent<Image>().sprite = bgSprite;
                // Preserve aspect ratio or stretch? Stretch is usually better for backgrounds if they match aspect ratio
                // bgObj.GetComponent<Image>().preserveAspect = true; 
            }
            else
            {
                // Fallback
                 bgObj.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f, 1f); 
            }

            // 3. Title: KNOWLEDGE STACK
            GameObject titleObj = CreateText("TitleText", canvasObj.transform, "KNOWLEDGE STACK", 100, Color.white, true); // Adjusted font size
            RectTransform titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 0.75f); // 0.85 -> 0.75
            titleRect.anchorMax = new Vector2(0.5f, 0.75f);
            titleRect.anchoredPosition = new Vector2(0, 0);
            titleRect.sizeDelta = new Vector2(900, 200); // 1800 -> 900 to fit 1080 width

            // 4. Play Button (OYNA) - Neon Style
            // Dark Navy Blue Background
            GameObject playBtnObj = CreateButton("PlayButton", canvasObj.transform, "OYNA", new Color(0.02f, 0.02f, 0.2f, 0.95f));
            RectTransform playRect = playBtnObj.GetComponent<RectTransform>();
            playRect.anchorMin = new Vector2(0.5f, 0.45f); // 0.6 -> 0.45
            playRect.anchorMax = new Vector2(0.5f, 0.45f);
            playRect.anchoredPosition = new Vector2(0, 0);
            playRect.sizeDelta = new Vector2(600, 180); // Adjusted size
            
            // Add Neon Effect (Outline)
            Image playImg = playBtnObj.GetComponent<Image>();
            Outline outline = playBtnObj.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0.8f, 1f, 1f); // Bright Cyan
            outline.effectDistance = new Vector2(4, -4);
            
            // Add Glow/Shadow to Text for readibility
            GetTextComponent(playBtnObj).fontStyle = FontStyles.Bold;
            GetTextComponent(playBtnObj).fontSize = 110; // Make play text larger



            // 6. Bottom Menu Buttons (Settings, Shop, Leaderboard)
            GameObject bottomContainer = new GameObject("BottomMenuContainer");
            bottomContainer.transform.SetParent(canvasObj.transform, false);
            RectTransform bottomRect = bottomContainer.AddComponent<RectTransform>();
            bottomRect.anchorMin = new Vector2(0.5f, 0.25f); // 0.15 -> 0.25
            bottomRect.anchorMax = new Vector2(0.5f, 0.25f);
            bottomRect.sizeDelta = new Vector2(900, 320); // 250 -> 320 height

            Color frostedColor = new Color(0.1f, 0.1f, 0.1f, 0.6f); // Move frostedColor definition here since modes block removed
            
            GameObject settingsBtn = CreateBottomMenuButton("SettingsButton", bottomContainer.transform, "AYARLAR", frostedColor, "UI/Skin/Knob.psd");
            AddFrostedEffect(settingsBtn);
            
            GameObject shopBtn = CreateBottomMenuButton("ShopButton", bottomContainer.transform, "MAĞAZA", frostedColor, "UI/Skin/Background.psd");
            AddFrostedEffect(shopBtn);
            
            GameObject leaderBtn = CreateBottomMenuButton("LeaderboardButton", bottomContainer.transform, "LİDERLİK", frostedColor, "UI/Skin/Checkmark.psd");
            AddFrostedEffect(leaderBtn);



            HorizontalLayoutGroup bottomLayout = bottomContainer.AddComponent<HorizontalLayoutGroup>();
            bottomLayout.childAlignment = TextAnchor.MiddleCenter;
            bottomLayout.spacing = 40; // Reduced spacing slightly
            bottomLayout.childControlWidth = false; 
            bottomLayout.childControlHeight = false;
            
            // Set square sizes for bottom buttons
            Vector2 squareSize = new Vector2(250, 250); // 200 -> 250
            settingsBtn.GetComponent<RectTransform>().sizeDelta = squareSize;
            shopBtn.GetComponent<RectTransform>().sizeDelta = squareSize;
            leaderBtn.GetComponent<RectTransform>().sizeDelta = squareSize;

            // 7. Stats Container (Level & Score) - Bottom of screen
            GameObject statsContainer = new GameObject("StatsContainer");
            statsContainer.transform.SetParent(canvasObj.transform, false);
            RectTransform statsRect = statsContainer.AddComponent<RectTransform>();
            statsRect.anchorMin = new Vector2(0.5f, 0.08f); // Very bottom
            statsRect.anchorMax = new Vector2(0.5f, 0.08f);
            statsRect.sizeDelta = new Vector2(900, 100);
            
            HorizontalLayoutGroup statsLayout = statsContainer.AddComponent<HorizontalLayoutGroup>();
            statsLayout.childAlignment = TextAnchor.MiddleCenter;
            statsLayout.spacing = 100;

            // Level Text
            GameObject levelTextObj = CreateText("LevelText", statsContainer.transform, "SEVİYE: -", 40, Color.white, true);
            // Score Text
            GameObject scoreTextObj = CreateText("ScoreText", statsContainer.transform, "PUAN: -", 40, Color.white, true);

            // Add simple shadow/glow to stats
            Outline levelOutline = levelTextObj.AddComponent<Outline>();
            levelOutline.effectColor = new Color(0, 0, 0, 0.8f);
            levelOutline.effectDistance = new Vector2(2, -2);
            
            Outline scoreOutline = scoreTextObj.AddComponent<Outline>();
            scoreOutline.effectColor = new Color(0, 0, 0, 0.8f);
            scoreOutline.effectDistance = new Vector2(2, -2);

            Debug.Log("Main Menu Generated Successfully with Responsive Settings & Icons!");
        }

        private static GameObject CreateBottomMenuButton(string name, Transform parent, string buttonText, Color bgColor, string iconResourcePath)
        {
            GameObject btnObj = CreateButton(name, parent, "", bgColor); // Button background
            
            // 1. Icon Image (Top)
            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(btnObj.transform, false);
            Image iconImg = iconObj.AddComponent<Image>();
            iconImg.color = Color.white; // Icon color
            
            Sprite iconSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>(iconResourcePath);
            if (iconSprite != null) iconImg.sprite = iconSprite;
            
            RectTransform iconRect = iconObj.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.5f, 0.6f); // Slightliy above center
            iconRect.anchorMax = new Vector2(0.5f, 0.6f);
            iconRect.anchoredPosition = Vector2.zero;
            iconRect.sizeDelta = new Vector2(110, 110); // Icon size 80->110

            // 2. Text (Bottom)
            GameObject textObj = CreateText("Label", btnObj.transform, buttonText, 34, Color.white); // 28 -> 34
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0f, 0.1f);
            textRect.anchorMax = new Vector2(1f, 0.4f); // Bottom area
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            
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

        private static GameObject CreateText(string name, Transform parent, string content, int fontSize, Color color, bool bold = false)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
            tmp.text = content;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            if (bold) tmp.fontStyle = FontStyles.Bold;
            return obj;
        }

        private static GameObject CreateButton(string name, Transform parent, string buttonText, Color bgColor)
        {
            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);
            
            Image img = btnObj.AddComponent<Image>();
            img.color = bgColor;
            
            // Add rounded corners using built-in Background sprite
            Sprite roundedSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
            if (roundedSprite != null)
            {
                img.sprite = roundedSprite;
                img.type = Image.Type.Sliced;
            }

            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = img;

            GameObject textObj = CreateText("Text", btnObj.transform, buttonText, 48, Color.white); // 36 -> 48
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            
            return btnObj;
        }

        private static TextMeshProUGUI GetTextComponent(GameObject btnObj)
        {
            return btnObj.GetComponentInChildren<TextMeshProUGUI>();
        }

        private static string CheckAndFixBackgroundImport()
        {
            // Find asset with name "Background" inside Assets/Resources
            string[] guids = AssetDatabase.FindAssets("Background", new[] { "Assets/Resources" });
            if (guids.Length == 0) return null;

            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            
            // Prepare importer
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                bool changed = false;
                if (importer.textureType != TextureImporterType.Sprite)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    changed = true;
                }
                
                // Ensure full rect is used
                if (importer.spriteImportMode != SpriteImportMode.Single)
                {
                    importer.spriteImportMode = SpriteImportMode.Single;
                    changed = true;
                }

                if (changed)
                {
                    importer.SaveAndReimport();
                    AssetDatabase.Refresh();
                    Debug.Log("Fixed Background Texture Type to Sprite automatically.");
                }
            }
            
            return path;
        }

        private static void AddFrostedEffect(GameObject btnObj)
        {
            // Add subtle outline
            Outline outline = btnObj.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 1f, 1f, 0.2f); // Faint white outline
            outline.effectDistance = new Vector2(2, -2);
            
            // Make text bold
            var txt = GetTextComponent(btnObj);
            if(txt != null) txt.fontStyle = FontStyles.Bold;
        }
    }
}
