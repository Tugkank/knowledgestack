using UnityEngine;
using UnityEditor;
using System.IO;

namespace KnowledgeStack.Editor
{
    public class BlockImporter : AssetPostprocessor
    {
        // Automatically fix textures when added to Resources/Blocks
        void OnPreprocessTexture()
        {
            if (assetPath.Contains("Resources/Blocks"))
            {
                TextureImporter importer = (TextureImporter)assetImporter;
                if (importer.textureType != TextureImporterType.Sprite)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.alphaIsTransparency = true;
                    Debug.Log($"[BlockImporter] Automatically set {assetPath} to Sprite.");
                }
            }
        }

        // Manual Fix Tool
        [MenuItem("Tools/Knowledge Stack/Fix Block Images")]
        public static void FixBlockImages()
        {
            string folderPath = "Assets/Resources/Blocks";
            if (!Directory.Exists(folderPath))
            {
                Debug.LogError("Resources/Blocks folder not found!");
                return;
            }

            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folderPath });
            int fixedCount = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                
                if (importer != null)
                {
                    bool changed = false;
                    if (importer.textureType != TextureImporterType.Sprite)
                    {
                        importer.textureType = TextureImporterType.Sprite;
                        changed = true;
                    }
                    if (importer.spriteImportMode != SpriteImportMode.Single)
                    {
                        importer.spriteImportMode = SpriteImportMode.Single;
                        changed = true;
                    }

                    if (changed)
                    {
                        importer.SaveAndReimport();
                        fixedCount++;
                    }
                }
            }
            
            AssetDatabase.Refresh();
            Debug.Log($"Block Image Fix Complete. Fixed {fixedCount} images.");
            EditorUtility.DisplayDialog("Blok Görselleri Onarıldı", $"{fixedCount} adet görsel Sprite formatına çevrildi.\n\nArtık oyunu başlatabilirsiniz.", "Tamam");
        }
    }
}
