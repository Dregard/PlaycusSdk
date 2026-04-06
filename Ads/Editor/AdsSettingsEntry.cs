using System;
using System.Collections.Generic;
using AppLovinMax.Scripts.IntegrationManager.Editor;
using Playcus.Ads;
using UnityEditor;
using UnityEngine;

namespace Playcus
{
    public class AdsSettingsEntry : SettingsEntry
    {
        public override string Label => "Advertisements";
        private DefineSymbolSwitch _applovinSwitch = new DefineSymbolSwitch("Applovin enabled", "PL_SDK_APPLOVIN_ON");
        private DefineSymbolSwitch _tamSwitch = new DefineSymbolSwitch("Amazon TAM enabled", "PL_AMAZON_TAM_ON");
        
        private readonly List<STORE> _platforms = new List<STORE>()
        {
            STORE.Appstore,
            STORE.GooglePlay
        };

        private List<string> _platformLabels;

        private STORE _currentPlatform;
        
        protected override void OnInitialize()
        {
#if PL_SDK_APPLOVIN_ON
            _config = PlaycusEditorUtils.LoadOrCreateScriptableObject<AdsApiConfig>("AdsApi", "ServiceConfigs");
            _configObject = new SerializedObject(_config);
            _currentPlatform = _platforms[0];
            _platformLabels = new List<string>(_platforms.Count);
            foreach (var platform in _platforms)
            {
                _platformLabels.Add(platform.ToString());
            }
#endif
        }
        
        protected override void DrawLayout()
        {
            EditorGUILayout.BeginHorizontal();
            _applovinSwitch.DrawToggle();
#if PL_SDK_APPLOVIN_ON
            if (GUILayout.Button("Open AppLovin Integration Manager..."))
            {
                AppLovinIntegrationManagerWindow.ShowManager();
            }
            EditorGUILayout.EndHorizontal();
            
            var rewardedEnabled = EditorGUILayout.ToggleLeft(
                "Rewarded Video enabled", 
                _config.RewardedEnabled);

            if (rewardedEnabled != _config.RewardedEnabled)
            {
                SetPrivateProperty(_config, nameof(_config.RewardedEnabled), rewardedEnabled);
            }
            
            var interstitialEnabled = EditorGUILayout.ToggleLeft(
                "Interstitial enabled", 
                _config.InterstitialEnabled);
            
            if (interstitialEnabled != _config.InterstitialEnabled)
            {
                SetPrivateProperty(_config, nameof(_config.InterstitialEnabled), interstitialEnabled);
            }
            
            var bannerEnabled = EditorGUILayout.ToggleLeft(
                "Banner enabled", 
                _config.BannerEnabled);
            
            if (bannerEnabled != _config.BannerEnabled)
            {
                SetPrivateProperty(_config, nameof(_config.BannerEnabled), bannerEnabled);
            }
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Default Banner Position");
            var defaultBannerPosition = (BANNER_POS)EditorGUILayout.EnumPopup(_config.DefaultBannerPosition);

            if (defaultBannerPosition != _config.DefaultBannerPosition)
            {
                SetPrivateProperty(_config, nameof(_config.DefaultBannerPosition), defaultBannerPosition);
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            
            _currentPlatform = _platforms[GUILayout.Toolbar (_platforms.IndexOf(_currentPlatform), _platformLabels.ToArray())];
            switch (_currentPlatform)
            {
                case STORE.Appstore:
                    DrawIosPlatforms();
                    break;
                case STORE.GooglePlay:
                    DrawAndroidPlatforms();
                    break;
            }

            if (GUI.changed)
            {
                EditorUtility.SetDirty(_config);
                if (_adsPlatformApplovinAppsStore != null)
                {
                    EditorUtility.SetDirty(_adsPlatformApplovinAppsStore);
                }
                if (_adsPlatformApplovinGooglePlay != null)
                {
                    EditorUtility.SetDirty(_adsPlatformApplovinGooglePlay);
                }
            }

            AssetDatabase.SaveAssetIfDirty(_config);
            if (_adsPlatformApplovinAppsStore != null)
            {
                AssetDatabase.SaveAssetIfDirty(_adsPlatformApplovinAppsStore);
            }
            if (_adsPlatformApplovinGooglePlay != null)
            {
                AssetDatabase.SaveAssetIfDirty(_adsPlatformApplovinGooglePlay);
            }
#else
            EditorGUILayout.EndHorizontal();
#endif
        }

#if PL_SDK_APPLOVIN_ON
        private AdsApiConfig _config;
        private AdsPlatformApplovin _adsPlatformApplovinAppsStore;
        private AdsPlatformApplovin _adsPlatformApplovinGooglePlay;
        private SerializedObject _configObject;

        private void DrawIosPlatforms()
        {
            DrawApplovinSettings(ref _adsPlatformApplovinAppsStore, STORE.Appstore);
        }

        private void DrawAndroidPlatforms()
        {
            DrawApplovinSettings(ref _adsPlatformApplovinGooglePlay, STORE.GooglePlay);
        }
        
        private void DrawApplovinSettings(ref AdsPlatformApplovin applovinSettings, STORE store)
        {
            var prefabName = $"AdsPlatformApplovin{store}";
            if (applovinSettings == null)
            {
                applovinSettings = Resources.Load<AdsPlatformApplovin>(prefabName);
            }

            if (applovinSettings == null)
            {
                if (GUILayout.Button($"Add Applovin for {store}..", GUILayout.Height(30)))
                {
                    try
                    {
                        applovinSettings = PlaycusEditorUtils.CreatePrefabAsset<AdsPlatformApplovin>(
                            prefabName,
                            "AdsPlatformApplovin");

                        var platforms = new List<AdsApiConfig.AdsServiceStoreConfig>(_config.Stores ?? new AdsApiConfig.AdsServiceStoreConfig[] {});
                        platforms.Add(new AdsApiConfig.AdsServiceStoreConfig()
                        {
                            Store = store,
                            AdsPlatformConfigPrefab = applovinSettings.gameObject
                        });

                        _config.Stores = platforms.ToArray();
                        EditorUtility.SetDirty(_config);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(e);
                    }
                }
            }
            else
            {
                EditorGUI.indentLevel++;
                EditorGUI.BeginChangeCheck();
                var settingsObject = new SerializedObject(applovinSettings);
                EditorGUILayout.PropertyField(settingsObject.FindProperty("SDKKey"), false);
                EditorGUILayout.PropertyField(settingsObject.FindProperty("_rewardedUnitID"), false);
                EditorGUILayout.PropertyField(settingsObject.FindProperty("_interstitialUnitID"), false);
                EditorGUILayout.PropertyField(settingsObject.FindProperty("_bannerUnitID"), false);
                EditorGUILayout.PropertyField(settingsObject.FindProperty("_bannerBackgroundColor"), false);
            
                EditorGUILayout.Space();
                
                _tamSwitch.DrawToggle();
                EditorGUI.BeginDisabledGroup(_tamSwitch.IsEnabled == false);
                EditorGUILayout.PropertyField(settingsObject.FindProperty("appId"), new GUIContent("Amazon App ID"));
                EditorGUILayout.PropertyField(settingsObject.FindProperty("amazonRewardedVideoSlotId"));
                EditorGUILayout.PropertyField(settingsObject.FindProperty("amazonInterstitialSlotId"));
                EditorGUILayout.PropertyField(settingsObject.FindProperty("amazonInterstitialVideoSlotId"));
                EditorGUILayout.PropertyField(settingsObject.FindProperty("amazonBannerSlotId"));

                EditorGUI.EndDisabledGroup();
                EditorGUI.indentLevel--;
                
                settingsObject.ApplyModifiedProperties();
            }
        }
#endif
    }
}