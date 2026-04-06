using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;

#endif

namespace Playcus
{
    /// <summary>
    /// Service with config that can be loaded from remote config and/or cached in scriptable object in editor.
    /// Documentation https://docs.google.com/document/d/1A6Ce52Xh3iLBiVgYKnjTHNCm4g21k5jln3PdMnZE0To/edit#
    /// </summary>
    public abstract class ServiceWithConfig : Service
    {
        // DEPENDENCIES
        [InjectService] protected IRemoteConfigManager _remoteConfigManager;

        [Header("Editor Only")] [SerializeField]
        private bool _doNotOverwriteFromRemote;

        // VARIABLES
        protected abstract Type ConfigType { get; }

        protected ServiceConfig _serviceConfig;

        private const string PATH_RESOURCES = "Playcus/Resources";
        private const string PATH_CONFIGS = "ServiceConfigs";
        string ServiceName => this.GetType().Name;
        string FolderResourcesPath => $"{Application.dataPath}/{PATH_RESOURCES}";
        string FolderResourcesConfigPath => $"{FolderResourcesPath}/{PATH_CONFIGS}";
        string ResourcePath => $"{PATH_CONFIGS}/{ServiceName}";
        string AssetPath => $"Assets/{PATH_RESOURCES}/{PATH_CONFIGS}/{ServiceName}.asset";
        
        protected override async UniTask LoadAsyncInternal(CancellationToken cancellationToken)
        {
            await LoadServiceConfigAsync(cancellationToken);
            
            ServiceLoadingComplete();
        }

        protected async UniTask LoadServiceConfigAsync(CancellationToken cancellationToken, bool requireRemoteConfigFetch = true)
        {
            FindConfigAsset();
#if PL_SDK_FIREBASE_ON
            string platformVarID = _serviceConfig.RemoteVarID;
            foreach(var p in _serviceConfig.RemoteVarIDByPlatform)
            {
                if (p.Platform == StoreConstants.GetCurrentStore())
                {
                    platformVarID = p.Name;
                    Debug.Log("Load ServiceWithConfig find spercial config " + platformVarID + " for platform " + StoreConstants.GetCurrentStore());
                }
            }
                    
            if (!string.IsNullOrEmpty(platformVarID))
            {
                // Try to load remote value of config
                if (_remoteConfigManager != null)
                {
                    // workaround for RemoteConfigManagerFirebase so it would not stuck waiting for itself
                    if (requireRemoteConfigFetch)
                    {
                        await UniTask.WaitWhile(() => _remoteConfigManager.State == ServiceState.Initializing, cancellationToken: cancellationToken);
                    }
                        
                    if (_remoteConfigManager.State != ServiceState.Ready)
                    {
                        Debug.LogWarning(
                            $"{gameObject.name}: Load ServiceWithConfig failed: IRemoteConfigManager service is not loaded yet! Will be get blank or cached value from previous session.");
                        
                    }

                    if (Application.isEditor && _doNotOverwriteFromRemote)
                    {
                        return;
                    }
                    
                    string remoteConfigValue = _remoteConfigManager.GetValue(platformVarID);
                    if (!string.IsNullOrEmpty(remoteConfigValue))
                    {
                        // Override config with remote values
                        ParseServiceConfig(remoteConfigValue);

                    }
                    else
                    {
                        Debug.LogWarning(
                            $"{gameObject.name}: Load ServiceWithConfig failed: can't find var {platformVarID} in remote config");
                    }
                }
                else
                {
                    Debug.LogWarning(
                        $"{gameObject.name}: Load ServiceWithConfig failed: Can't resolve IRemoteConfigManager");
                }
            }
            Debug.Log($"{gameObject.name} ConfigID " + platformVarID);
#endif
        }

        protected virtual void ParseServiceConfig(string json)
        {
            JsonUtility.FromJsonOverwrite(json,_serviceConfig);
        }

        private void FindConfigAsset()
        {
            // Find scriptable object or create new
            _serviceConfig = Resources.Load(ResourcePath, ConfigType) as ServiceConfig;

            // If no config
            if (_serviceConfig == null)
            {
                _serviceConfig = (ServiceConfig) ScriptableObject.CreateInstance(ConfigType);
#if UNITY_EDITOR
                CreateNewConfigAsset();
#endif
            }
        }

        private void CreateNewConfigAsset()
        {
#if UNITY_EDITOR
            if (!System.IO.Directory.Exists(FolderResourcesPath))
            {
                System.IO.Directory.CreateDirectory(FolderResourcesPath);
            }

            if (!System.IO.Directory.Exists(FolderResourcesConfigPath))
            {
                System.IO.Directory.CreateDirectory(FolderResourcesConfigPath);
            }

            _serviceConfig.RemoteVarID = ServiceName;
            AssetDatabase.CreateAsset(_serviceConfig, AssetPath);
            if (!Application.isPlaying)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
#endif
        }

        protected virtual void OnDisable()
        {
            // IN EDITOR Override original config object
#if UNITY_EDITOR
            if (_serviceConfig != null)
            {
                EditorUtility.SetDirty(_serviceConfig);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
#endif
        }

        public void SelectConfigAsset()
        {
#if UNITY_EDITOR
            FindConfigAsset();
            Selection.activeObject =
                AssetDatabase.LoadAssetAtPath(AssetDatabase.GetAssetPath(_serviceConfig), ConfigType) as
                    UnityEngine.Object;
#endif
        }
    }
}