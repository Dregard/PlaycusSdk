using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ScenesWindow : EditorWindow
{
    private const string EditorResourceFolder = "Editor Default Resources";

    private ScenesWindowCache _cache;

    private List<EditorBuildSettingsScene> _scenes;

    private string _sceneSearchMask;
    private SearchField _searchField;

    private GUIStyle style;
    private GUIStyle foldStyle;
    private GUIStyle foldInnerStyle;

    private bool _othersFold;
    private bool _favoriteFold;
    private bool _gameScenesFold = true;

    [MenuItem("Window/Scenes", false, -2000)]
    public static void Init()
    {
        var wnd = GetWindow<ScenesWindow>("Scenes");
        wnd.minSize = new Vector2(300, 100);
        wnd.ShowPopup();
    }

    private void CreateNewCacheAsset()
    {
        var path = $"{Application.dataPath}/{EditorResourceFolder}";
        if (!System.IO.Directory.Exists(path))
        {
            System.IO.Directory.CreateDirectory(path);
        }

        var assetPath = $"Assets/{EditorResourceFolder}/ScenesWindowCacheAsset.asset";
        AssetDatabase.CreateAsset(_cache, assetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private void OnFocus()
    {
        Repaint();
    }

    private void OnLostFocus()
    {
        _createStyles = false;
    }

    private void Awake()
    {
        _searchField = _searchField ?? new SearchField();
        _cache = EditorGUIUtility.Load("ScenesWindowCacheAsset.asset") as ScenesWindowCache;
    }

    private void OnEnable()
    {
        _searchField = _searchField ?? new SearchField();

        FillScenes();
    }

    private void OnDisable()
    {
        _scenes = null;
        foldStyle = null;
        foldInnerStyle = null;
        style = null;
        _createStyles = false;
    }

    private void FillScenes()
    {
        _cache = _cache != null ? _cache : EditorGUIUtility.Load("ScenesWindowCacheAsset.asset") as ScenesWindowCache;

        if (_cache == null)
        {
            _cache = ScriptableObject.CreateInstance<ScenesWindowCache>();
            _cache.scenes = new List<SceneCacheData>();
            CreateNewCacheAsset();
        }

        var guids = AssetDatabase.FindAssets("t:Scene");
        var paths = Array.ConvertAll<string, string>(guids, AssetDatabase.GUIDToAssetPath);

        if (_scenes != null)
        {
            EditorBuildSettings.scenes = _scenes.ToArray();
            AssetDatabase.SaveAssets();
        }

        _scenes = EditorBuildSettings.scenes.ToList();

        _cache.scenes.Clear();

        foreach (var path in paths)
        {
            var folder = Path.GetDirectoryName(path);
            var data = _cache.scenes.Find(cacheData => cacheData.sceneFolder == folder);

            if (data == null)
            {
                data = new SceneCacheData {sceneFolder = folder, scenesPath = new List<string>()};
                _cache.scenes.Add(data);
            }

            data.scenesPath.Add(path);
        }

        _favoriteFold = _cache.favoriteScenesData.Count > 0;
    }

    private void AddToFavorite()
    {
        var scene = SceneManager.GetActiveScene();
        AddToFavorite(scene.path);
    }

    private void AddToFavorite(string sceneName)
    {
        if (!_cache.favoriteScenesData.Contains(sceneName))
        {
            _cache.favoriteScenesData.Add(sceneName);
            _favoriteFold = true;
            SaveCacheData();
        }
    }

    private void SaveCacheData()
    {
        if (_cache != null)
        {
            EditorUtility.SetDirty(_cache);
            AssetDatabase.SaveAssets();
        }
    }

    private void RemoveFromFavorite(string sceneName)
    {
        if (_cache.favoriteScenesData.Contains(sceneName))
        {
            _cache.favoriteScenesData.Remove(sceneName);
            _favoriteFold = _cache.favoriteScenesData.Count > 0;
            SaveCacheData();
        }
    }

    private void AddCurrentToBuild()
    {
        var scenes = EditorBuildSettings.scenes;
        var scene = SceneManager.GetActiveScene();

        foreach (var settingsScene in scenes)
        {
            if (settingsScene.path == scene.path)
            {
                Debug.LogWarning("Scene already added to Build Scenes");
                return;
            }
        }

        Array.Resize(ref scenes, scenes.Length + 1);
        scenes[scenes.Length - 1] = new EditorBuildSettingsScene(scene.path, true);

        _scenes = scenes.ToList();
        FillScenes();
    }

    private void RemoveCurrentFromBuild()
    {
        var scene = SceneManager.GetActiveScene();

        foreach (var settingsScene in _scenes)
        {
            if (settingsScene.path == scene.path)
            {
                _scenes.Remove(settingsScene);
                FillScenes();
                break;
            }
        }
    }

    private Vector2 _vScrollBar;

    private bool _createStyles;

    private void OnGUI()
    {
        if (_scenes == null && _cache.scenes.Count == 0) return;

        _sceneSearchMask = _searchField.OnGUI(_sceneSearchMask);

        if (!_createStyles)
        {
            foldInnerStyle = new GUIStyle(EditorStyles.foldout);
            foldInnerStyle.name = EditorStyles.foldout.name;
            foldInnerStyle.fontSize = 14;
            foldInnerStyle.margin = new RectOffset(5, 0, 0, 0);

            foldStyle = new GUIStyle(EditorStyles.foldout);
            foldStyle.name = EditorStyles.foldout.name;
            foldStyle.fontSize = 16;
        
            style = GUIStyle.none;
            style.fontSize = 16;

            _createStyles = true;
        }


        if (!string.IsNullOrEmpty(_sceneSearchMask))
        {
            _othersFold = true;
        }

        if (GUILayout.Button("Reload Window"))
        {
            this.Close();
            Init();
            return;
        }

        if (GUILayout.Button("Refresh & Save"))
        {
            FillScenes();
            Repaint();
            return;
        }

        if (GUILayout.Button(" Add Current to Build"))
        {
            AddCurrentToBuild();
            return;
        }

        if (GUILayout.Button("Remove Current from Build"))
        {
            RemoveCurrentFromBuild();
            return;
        }

        if (GUILayout.Button("Add Current to Favorite"))
        {
            AddToFavorite();
            return;
        }

        _vScrollBar = GUILayout.BeginScrollView(_vScrollBar, false, true);

        if (_cache.favoriteScenesData != null)
        {
            _favoriteFold = EditorGUILayout.Foldout(_favoriteFold, "Favorite", foldStyle);
            //GUILayout.Label("Favorite", _style);
            if (_favoriteFold)
            {
                foreach (var favoriteScene in _cache.favoriteScenesData)
                {
                    var sceneName = Path.GetFileNameWithoutExtension(favoriteScene);

                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button(sceneName))
                    {
                        if (!OpenScene(favoriteScene))
                        {
                            if (_cache.favoriteScenesData.Contains(favoriteScene))
                            {
                                _cache.favoriteScenesData.Remove(favoriteScene);
                            }
                        }
                    }

                    if (GUILayout.Button(EditorGUIUtility.IconContent("d_TreeEditor.Trash"), GUILayout.Width(30f)))
                    {
                        RemoveFromFavorite(favoriteScene);
                        break;
                    }

                    GUILayout.EndHorizontal();
                }
            }
        }

        if (_scenes != null && _scenes.Count > 0)
        {
            _gameScenesFold = EditorGUILayout.Foldout(_gameScenesFold, "GameScenes", foldStyle);
            if (_gameScenesFold)
            {
                foreach (var scene in _scenes)
                {
                    GUILayout.BeginHorizontal();
                    scene.enabled = GUILayout.Toggle(scene.enabled, "", GUILayout.Width(15f));

                    // GUI.enabled = scene.enabled;     
                    if (GUILayout.Button(Path.GetFileName(scene.path)))
                    {
                        OpenScene(scene.path);
                    }

                    DrawFavoriteButton(scene.path);

                    //GUI.enabled = true;
                    GUILayout.EndHorizontal();
                }
            }
        }

        _othersFold = EditorGUILayout.Foldout(_othersFold, "Others", foldStyle);

        if (_othersFold)
        {
            foreach (var data in _cache.scenes)
            {
                if (string.IsNullOrEmpty(_sceneSearchMask) || data.ContainScene(_sceneSearchMask))
                {
                    data.foldOut = EditorGUILayout.Foldout(data.foldOut, data.sceneFolder, foldInnerStyle);

                    if (data.foldOut)
                    {
                        foreach (var path in data.scenesPath)
                        {
                            var sceneName = Path.GetFileNameWithoutExtension(path);

                            if (_scenes.Count > 0 && _scenes.Exists(scene => scene.path == path)) continue;

                            GUILayout.BeginHorizontal();
                            if (GUILayout.Button(sceneName))
                            {
                                OpenScene(path);
                            }

                            DrawFavoriteButton(path);
                            GUILayout.EndHorizontal();
                        }
                    }
                }
            }
        }

        GUILayout.EndScrollView();
    }

    private void DrawFavoriteButton(string scene)
    {
        if (GUILayout.Button(EditorGUIUtility.IconContent("d_Favorite"), GUILayout.Width(30f)))
        {
            AddToFavorite(scene);
        }
    }


    private bool OpenScene(string scenePath)
    {
        try
        {
            if (SceneManager.GetActiveScene().isDirty)
            {
                var option =
                    EditorUtility.DisplayDialogComplex("Save scenes", "Save current scene?", "yes", "no", "cancel");

                if (option == 0)
                {
                    EditorSceneManager.SaveOpenScenes();
                }
            }

            EditorSceneManager.OpenScene(scenePath);

            var obj = AssetDatabase.LoadMainAssetAtPath(scenePath);
            if (obj != null)
            {
                Selection.activeObject = obj;
                EditorGUIUtility.PingObject(obj);
            }

            return true;
        }
        catch (Exception e)
        {
            Debug.LogError("Scene cannot open! " + e.Message);
        }

        return false;
    }
}