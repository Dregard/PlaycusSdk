using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

[Serializable]
public class SceneCacheData
{
    public string sceneFolder;
    public List<string> scenesPath;
    public bool foldOut;

    public bool ContainScene(string name)
    {
        foreach (var scenePath in scenesPath)
        {
            var sceneName = Path.GetFileNameWithoutExtension(scenePath);
            if (sceneName.Contains(name)) return true;
        }

        return false;
    }
}

/// <summary>
/// Create asset file in  EditorDefaultResources folder
/// </summary>
//[CreateAssetMenu(fileName = "ScenesWindowCacheAsset", menuName = "Playcus/CreateScenesCacheAsset", order = 0)]
public class ScenesWindowCache : ScriptableObject
{
    public List<string> favoriteScenesData;

    public List<SceneCacheData> scenes;
    
   
    
    private void OnValidate()
    {
      
        // if (buttonInnerStyle == null)
        // {
        //     buttonInnerStyle = new GUIStyle(EditorStyles.toolbarButton);
        //     buttonInnerStyle.margin = new RectOffset(15, 0, 0, 0);
        // }
    }
}