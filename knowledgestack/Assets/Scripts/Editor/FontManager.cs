using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using System.Collections.Generic;

namespace KnowledgeStack.Editor
{
    public class FontManager : EditorWindow
    {
        private TMP_FontAsset targetFont;

        [MenuItem("Tools/Knowledge Stack/Font Manager")]
        public static void ShowWindow()
        {
            GetWindow<FontManager>("Font Manager");
        }

        private void OnGUI()
        {
            GUILayout.Label("Font Değiştirme Aracı", EditorStyles.boldLabel);

            GUILayout.Space(10);
            
            EditorGUILayout.HelpBox("1. Önce Window -> TextMeshPro -> Font Asset Creator menüsünden TTF dosyanızı seçip 'Generate Font Atlas' diyerek bir Font Asset oluşturun ve kaydedin.\n\n2. Oluşturduğunuz Font Asset dosyasını aşağıya sürükleyin.", MessageType.Info);

            GUILayout.Space(10);

            targetFont = (TMP_FontAsset)EditorGUILayout.ObjectField("Yeni Font Asset:", targetFont, typeof(TMP_FontAsset), false);

            GUILayout.Space(20);

            if (GUILayout.Button("Açık Sahnedeki Tüm Yazıları Güncelle"))
            {
                if (targetFont == null)
                {
                    EditorUtility.DisplayDialog("Hata", "Lütfen önce bir Font Asset seçin!", "Tamam");
                    return;
                }
                UpdateCurrentScene();
            }

            if (GUILayout.Button("TÜM Sahneleri Güncelle (MainMenu + Game)"))
            {
                if (targetFont == null)
                {
                    EditorUtility.DisplayDialog("Hata", "Lütfen önce bir Font Asset seçin!", "Tamam");
                    return;
                }
                UpdateAllScenes();
            }
        }

        private void UpdateCurrentScene()
        {
            TextMeshProUGUI[] texts = FindObjectsOfType<TextMeshProUGUI>();
            Undo.RecordObjects(texts, "Change Font");

            foreach (var txt in texts)
            {
                txt.font = targetFont;
                EditorUtility.SetDirty(txt);
            }
            
            Debug.Log($"Updated {texts.Length} text objects in current scene.");
        }

        private void UpdateAllScenes()
        {
            string[] scenePaths = { "Assets/Scenes/MainMenu.unity", "Assets/Scenes/Game.unity" };
            
            // Save current scene if modified
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            foreach (string path in scenePaths)
            {
                UnityEngine.SceneManagement.Scene scene = EditorSceneManager.OpenScene(path);
                UpdateCurrentScene();
                EditorSceneManager.SaveScene(scene);
            }

            Debug.Log("All scenes updated!");
            EditorUtility.DisplayDialog("Başarılı", "Tüm sahnelerdeki fontlar güncellendi!", "Harika");
        }
    }
}
