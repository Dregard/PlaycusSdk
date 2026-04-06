using Playcus.Saves;
using UnityEditor;
using UnityEngine;

namespace Playcus
{
    [HideInSettingsWindow]
    public class SaveSettingEntry : SettingsEntry
    {
        private SaveServiceConfig _config;
        public override string Label => "Saves";
        
        protected override void OnInitialize()
        {
            _config = PlaycusEditorUtils.LoadOrCreateScriptableObject<SaveServiceConfig>("SaveService", "ServiceConfigs");
            
            // must be loaded from core resources
            var prefab = Resources.Load<LocalSavedStorage>("LocalSaveStorage");
            if (prefab == null)
            {
                prefab = PlaycusEditorUtils.CreatePrefabAsset<LocalSavedStorage>("LocalSaveStorage", "LocalSaveStorage");
            }

            if (_config.Stores == null || _config.Stores.Length == 0)
            {
                _config.Stores = new[]
                {
                    new SaveServiceConfig.SaveServiceStoreConfig()
                    {
                        Store = STORE.Appstore,
                        StoragesPrefabs = new[]
                        {
                            prefab
                        }
                    },
                    new SaveServiceConfig.SaveServiceStoreConfig()
                    {
                        Store = STORE.GooglePlay,
                        StoragesPrefabs = new[]
                        {
                            prefab
                        }
                    }
                };
                
                EditorUtility.SetDirty(_config);
            }
            
            AssetDatabase.SaveAssetIfDirty(_config);
        }

        protected override void DrawLayout() { }
    }
}