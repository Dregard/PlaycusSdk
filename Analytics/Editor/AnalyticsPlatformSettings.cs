using System;
using System.Collections.Generic;
using Playcus.Analytics;
using Playcus.Analytics.Internal;
using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEngine;

namespace Playcus
{
    internal class AnalyticsPlatformSettings<T> where T : AnalyticsServicePlatform
    {
        private readonly AnalyticsServiceConfig _serviceConfig;
        private readonly DefineSymbolSwitch _platformSwitch;

        private T _appstorePlatform;
        private T _googlePlayPlatform;
        private SerializedObject _settingsObjectAppstore;
        private SerializedObject _settingsObjectGooglePlay;
        
        private readonly List<STORE> _platforms = new List<STORE>()
        {
            STORE.Appstore,
            STORE.GooglePlay
        };

        private List<string> _platformLabels;

        private STORE _currentPlatform;

        private AnimBool _anim = new AnimBool(false);
        private readonly string _label;
        private readonly string _prefabTemplateName;

        public AnalyticsPlatformSettings(
            string label,
            AnalyticsServiceConfig serviceConfig,
            DefineSymbolSwitch platformSwitch,
            string prefabTemplateName)
        {
            _label = label;
            _serviceConfig = serviceConfig;
            _platformSwitch = platformSwitch;
            _prefabTemplateName = prefabTemplateName;
            _currentPlatform = _platforms[0];
            
            _platformLabels = new List<string>(_platforms.Count);
            foreach (var platform in _platforms)
            {
                _platformLabels.Add(platform.ToString());
            }
        }

        public void Draw()
        {
            _platformSwitch.DrawToggle();

            if (_platformSwitch.IsEnabled)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                var headerRect = GUILayoutUtility.GetRect(16f, 28f, GUILayout.ExpandWidth(true));
                GUI.Box(headerRect, GUIContent.none); // Draws background

                var newFoldout = EditorGUI.Foldout(
                    new Rect(headerRect.x + 12f, headerRect.y + 4f, headerRect.width, headerRect.height),
                    _anim.target, 
                    _label, 
                    true, 
                    EditorStyles.foldoutHeader);

                if (newFoldout != _anim.target)
                {
                    _anim.target = newFoldout;
                }

                EditorGUILayout.BeginFadeGroup(_anim.faded);
                if (_anim.target)
                {
                    _currentPlatform = _platforms[GUILayout.Toolbar (_platforms.IndexOf(_currentPlatform), _platformLabels.ToArray())];
                    switch (_currentPlatform)
                    {
                        case STORE.Appstore:
                            DrawAppstore();
                            break;
                        case STORE.GooglePlay:
                            DrawGooglePlay();
                            break;
                    }
                }
                EditorGUILayout.EndFadeGroup();
                EditorGUILayout.EndVertical();

                AssetDatabase.SaveAssetIfDirty(_serviceConfig);

                if (_appstorePlatform != null)
                {
                    AssetDatabase.SaveAssetIfDirty(_appstorePlatform);
                }
                if (_googlePlayPlatform != null)
                {
                    AssetDatabase.SaveAssetIfDirty(_googlePlayPlatform);
                }
            }
        }

        private void DrawAppstore()
        {
            DrawPlatform(ref _appstorePlatform, ref _settingsObjectAppstore, STORE.Appstore);
        }

        private void DrawGooglePlay()
        {
            DrawPlatform(ref _googlePlayPlatform, ref _settingsObjectGooglePlay, STORE.GooglePlay);
        }
        
        private void DrawPlatform(ref T platformSettings, ref SerializedObject settingsObject, STORE store)
        {
            var prefabName = $"{_prefabTemplateName}{store}";
            if (platformSettings == null)
            {
                platformSettings = Resources.Load<T>(prefabName);
            }
            
            if (platformSettings == null)
            {
                // Prevent creating prefab while Unity is compiling
                if (EditorApplication.isCompiling)
                {
                    EditorGUILayout.HelpBox("Wait for compilation to complete before adding platform...", MessageType.Warning);
                    GUI.enabled = false;
                }

                if (GUILayout.Button($"Add {_prefabTemplateName} for {store}..", GUILayout.Height(30)))
                {
                    if (EditorApplication.isCompiling)
                    {
                        EditorUtility.DisplayDialog("Compilation in progress",
                            "Please wait for Unity to finish compiling scripts before creating the prefab.", "OK");
                        GUI.enabled = true;
                        return;
                    }

                    try
                    {
                        platformSettings = PlaycusEditorUtils.CreatePrefabAsset<T>(
                            prefabName,
                            _prefabTemplateName);

                        var platforms = new List<AnalyticsServiceConfig.AnalyticsServiceStoreConfig>(_serviceConfig.Stores);
                        var platform = default(AnalyticsServiceConfig.AnalyticsServiceStoreConfig);

                        foreach (var existingPlatform in platforms)
                        {
                            if (existingPlatform.Store == store)
                            {
                                platform = existingPlatform;
                                break;
                            }
                        }

                        if (platform == null)
                        {
                            platform = new AnalyticsServiceConfig.AnalyticsServiceStoreConfig();
                            platform.Store = store;
                            platform.AnalyticsPlatformConfigPrefabs = new List<GameObject>();

                            platforms.Add(platform);
                        }
                        
                        platform.AnalyticsPlatformConfigPrefabs.Add(platformSettings.gameObject);
                        _serviceConfig.Stores = platforms.ToArray();
                        
                        EditorUtility.SetDirty(_serviceConfig);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(e);
                    }
                }

                GUI.enabled = true;
            }
            else
            {
                if (settingsObject == null)
                {
                    settingsObject = new SerializedObject(platformSettings);
                }
                
                EditorGUI.indentLevel++;
                EditorGUI.BeginChangeCheck();
                
                platformSettings.EventsMaskMode = (AnalyticsServicePlatform.EventsMaskType)EditorGUILayout.EnumPopup(platformSettings.EventsMaskMode);
                EditorGUILayout.PropertyField(settingsObject.FindProperty("EventsMask"));
                EditorGUILayout.PropertyField(settingsObject.FindProperty("EventsMaskCustom"));
                EditorGUILayout.PropertyField(settingsObject.FindProperty("AdditionalEventSettings"));

                DrawCustomSettings(settingsObject, store);
                
                settingsObject.ApplyModifiedProperties();
                EditorGUI.indentLevel--;
                
                if (EditorGUI.EndChangeCheck())
                {
                    settingsObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(platformSettings);
                }
                
                AssetDatabase.SaveAssetIfDirty(platformSettings);
            }
        }

        private void DrawCustomSettings(SerializedObject settingsObject, STORE store)
        {
            if (typeof(T) == typeof(AppsFlyerAnalyticServicePlatform))
            {
                EditorGUILayout.PropertyField(settingsObject.FindProperty("AppsflyerDevKey"));

                switch (store)
                {
                    case STORE.Appstore:
                        EditorGUILayout.PropertyField(settingsObject.FindProperty("IosAppNumberID"));
                        break;
                    case STORE.GooglePlay:
                        EditorGUILayout.PropertyField(settingsObject.FindProperty("GooglePublicKey"));
                        break;
                }

            }
            else if (typeof(T) == typeof(GameAnalyticsAnalyticServicePlatform))
            {
                if (GUILayout.Button("Select GameAnalytics Settings"))
                {
#if PL_SDK_GA_ON
                    Selection.activeObject = GameAnalyticsSDK.GameAnalytics.SettingsGA;
#endif
                }
            }
            else if (typeof(T) == typeof(D2DAnalyticsPlatform))
            {
                switch (store)
                {
                    case STORE.Appstore:
                        EditorGUILayout.PropertyField(settingsObject.FindProperty("iOSAppID"));
                        break;
                    case STORE.GooglePlay:
                        EditorGUILayout.PropertyField(settingsObject.FindProperty("androidAppID"));
                        break;
                }
            }
        }
    }
}