using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace KnowledgeStack.Editor
{
    public class SceneSetupTool : MonoBehaviour
    {
        [MenuItem("Tools/Knowledge Stack/Setup Full Project (One-Click)")]
        public static void SetupFullProject()
        {
            try
            {
                string scenesFolder = "Assets/Scenes";
                if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
                {
                    AssetDatabase.CreateFolder("Assets", "Scenes");
                }

                // --- 1. Setup Main Menu Scene ---
                string mainMenuPath = Path.Combine(scenesFolder, "MainMenu.unity");
                // Create New Scene
                Scene mainMenuScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                
                // Generate UI
                if(!Application.isPlaying) MainMenuGenerator.Generate();
                
                // Add Managers
                SetupManagers();
                
                // Add Controller
                if (GameObject.Find("MainMenuController") == null)
                {
                    GameObject controllerObj = new GameObject("MainMenuController");
                    controllerObj.AddComponent<KnowledgeStack.UI.MainMenuController>();
                }

                EditorSceneManager.SaveScene(mainMenuScene, mainMenuPath);
                Debug.Log("MainMenu Scene Setup Complete.");

                // --- 2. Setup Game Scene ---
                string gameScenePath = Path.Combine(scenesFolder, "Game.unity");
                Scene gameScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                
                // Generate UI
                if(!Application.isPlaying) GameInterfaceGenerator.Generate();
                
                // Add GameController
                if (GameObject.Find("GameController") == null)
                {
                    GameObject gameCtrlObj = new GameObject("GameController");
                    gameCtrlObj.AddComponent<KnowledgeStack.Game.GameController>();
                }
                
                EditorSceneManager.SaveScene(gameScene, gameScenePath);
                Debug.Log("Game Scene Setup Complete.");

                // --- 3. Add to Build Settings ---
                List<EditorBuildSettingsScene> buildScenes = new List<EditorBuildSettingsScene>();
                buildScenes.Add(new EditorBuildSettingsScene(mainMenuPath, true));
                buildScenes.Add(new EditorBuildSettingsScene(gameScenePath, true));
                EditorBuildSettings.scenes = buildScenes.ToArray();

                // Return to Main Menu
                EditorSceneManager.OpenScene(mainMenuPath);

                EditorUtility.DisplayDialog("Full Setup Complete", 
                    "Proje kurulumu tamamlandı!\n\n" +
                    "1. MainMenu.unity (NetworkManager, QuestionManager ve UI)\n" +
                    "2. Game.unity (GameController ve UI)\n" +
                    "3. Build Settings güncellendi.\n\n" +
                    "Lütfen şimdi 'Play' tuşuna basarak test edin.", "Harika");
            }
            catch(System.Exception e)
            {
                Debug.LogError("Setup Failed: " + e.Message);
                EditorUtility.DisplayDialog("Error", "Kurulum sırasında hata oluştu: " + e.Message, "Tamam");
            }
        }

        private static void SetupManagers()
        {
            // NetworkManager
            if (GameObject.Find("NetworkManager") == null)
            {
                GameObject netObj = new GameObject("NetworkManager");
                var netScript = netObj.AddComponent<KnowledgeStack.Networking.NetworkManager>();
                netScript.serverIpAddress = "46.101.199.48";
            }

            // QuestionManager
            if (GameObject.Find("QuestionManager") == null)
            {
                GameObject qObj = new GameObject("QuestionManager");
                qObj.AddComponent<QuestionManager>();
            }
        }
    }
}
