using System.IO;
using UnityEditor;
using UnityEngine;

namespace Playcus
{
    public static class PlaycusEditorUtils
    {
        private const string PATH_BASE = "Assets/Playcus/Resources";
        
        public static T CreatePrefabAsset<T>(string prefabName, string templateName, string resourcesSubfolder = null)
            where T : Object
        {
            var prefabPath = prefabName;
            if (string.IsNullOrEmpty(resourcesSubfolder) == false)
            {
                prefabPath = Path.Combine(resourcesSubfolder, prefabPath);
            }
            var prefab = Resources.Load<T>(prefabPath);
            if (prefab == null)
            {
                // load template
                var template = Resources.Load<GameObject>(System.IO.Path.Combine("Templates", templateName));
                
                var instance = Object.Instantiate(template);

                ValidateFolder();
                
                var path = PATH_BASE;
                if (string.IsNullOrEmpty(resourcesSubfolder) == false)
                {
                    var subfolderPath = System.IO.Path.Combine(path, resourcesSubfolder);
                    if (AssetDatabase.IsValidFolder(subfolderPath) == false)
                    {
                        AssetDatabase.CreateFolder(path, resourcesSubfolder);
                        AssetDatabase.SaveAssets();
                        AssetDatabase.Refresh();
                    }

                    path = subfolderPath;
                }
                path = System.IO.Path.Combine(path, $"{prefabName}.prefab");
                PrefabUtility.SaveAsPrefabAsset(instance, path);
                Object.DestroyImmediate(instance);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                prefab = Resources.Load<T>(prefabName);
            }

            return prefab;
        }
        
        public static T LoadOrCreateScriptableObject<T>(string assetName, string resourcesSubfolder = null)
            where T : ScriptableObject
        {
            var assetPath = assetName;
            if (string.IsNullOrEmpty(resourcesSubfolder) == false)
            {
                assetPath = Path.Combine(resourcesSubfolder, assetPath);
            }
            var scriptableObject = Resources.Load<T>(assetPath);
            if (scriptableObject == null)
            {
                scriptableObject = ScriptableObject.CreateInstance<T>();
                
                ValidateFolder();

                var path = PATH_BASE;
                if (string.IsNullOrEmpty(resourcesSubfolder) == false)
                {
                    var subfolderPath = System.IO.Path.Combine(path, resourcesSubfolder);
                    if (AssetDatabase.IsValidFolder(subfolderPath) == false)
                    {
                        AssetDatabase.CreateFolder(path, resourcesSubfolder);
                        AssetDatabase.SaveAssets();
                        AssetDatabase.Refresh();
                    }

                    path = subfolderPath;
                }
                path = System.IO.Path.Combine(path, $"{assetName}.asset");

                AssetDatabase.CreateAsset(scriptableObject, path);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            return scriptableObject;
        }

        private static void ValidateFolder()
        {
            if (AssetDatabase.IsValidFolder("Assets/Playcus") == false)
            {
                AssetDatabase.CreateFolder("Assets", "Playcus");
            }
            
            if (AssetDatabase.IsValidFolder("Assets/Playcus/Resources") == false)
            {
                AssetDatabase.CreateFolder("Assets/Playcus", "Resources");
            }
        }
    }
}