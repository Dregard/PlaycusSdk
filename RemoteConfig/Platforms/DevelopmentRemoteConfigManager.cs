using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using Playcus.Assets;
using UnityEngine;

namespace Playcus.FirebaseSDK
{
    public enum DevRemoteConfigLoadStatus
    {
        None = 0,
        Loading,
        Failed,
        Complete
    }

    [Serializable]
    public class ConfigVar
    {
        public const string ManifestFileName = "manifest.json";

        public string configVar;
        public bool takeOnce;
    }
    
    [CreateAssetMenu(fileName = "DevelopmentRemoteConfigManager", menuName = "Playcus/CreateDevelopmentRemoteConfig")]
    public class DevelopmentRemoteConfigManager:ScriptableObject
    {
        public event Action<DevRemoteConfigLoadStatus> OnLoadComplete;

        [SerializeField] private string _url;
        [HideInInspector] [SerializeField] private string _login;
        [HideInInspector] [SerializeField] private string _password;
        [SerializeField] private ConfigVar[] _removeVars;

        private DevRemoteConfigLoadStatus _status = DevRemoteConfigLoadStatus.None;

        public DevRemoteConfigLoadStatus Status => _status;

        private int _loadCount;
        private readonly Dictionary<string, string> _configs = new Dictionary<string, string>();
            
        // #if UNITY_EDITOR
//         [Button("CopyManifest to Clipboard")]
//         private void CopyManifestJson()
//         {
//             var json = JsonUtility.ToJson(_removeVars, true);
//             UnityEditor.EditorGUIUtility.systemCopyBuffer = json;
//             Debug.Log($"DevRemoteConfig manifest: {json}");
//         }
// #endif

        public void ClearConfigs()
        {
            _configs.Clear();
        }

        public void LoadAllDevConfigs()
        {
            if (_status == DevRemoteConfigLoadStatus.Loading) return;
            _loadCount = 0;
            var fileLoader = ServiceLocator.Get<IFilesLoader>();
            if (fileLoader != null && !string.IsNullOrEmpty(_url))
            {
                if (_removeVars.Length > 0)
                {
                    _configs.Clear();
                    _status = DevRemoteConfigLoadStatus.Loading;
                    foreach (var remoteVar in _removeVars)
                    {
                        var url = $"{_url}/{remoteVar.configVar}.json";
                        LoadDevConfig(remoteVar.configVar, url, fileLoader).Forget();
                    }
                }
            }
            else
            {
                if (fileLoader == null)
                {
                    Debug.Log("<color='red'>DevRemoteConfig: FileLoader service not found!</color>");
                }
                _status = DevRemoteConfigLoadStatus.Failed;
                OnLoadComplete?.Invoke(_status);
            }
        }

        private async UniTaskVoid LoadDevConfig(string configVar, string url, IFilesLoader fileLoader)
        {
            var bytes = await fileLoader.GetFileFromUrl(url, "",
                new CancellationTokenSource());

            if (bytes != null)
            {
                var json = Encoding.UTF8.GetString(bytes);

                if (!string.IsNullOrEmpty(json))
                {
                    if (_configs.ContainsKey(configVar))
                    {
                        _configs[configVar] = json;
                    }
                    else
                    {
                        _configs.Add(configVar, json);
                    }

                    Debug.Log($"DevRemoteConfig: {configVar} loaded {json} ");
                }
            }
            else
            {
                Debug.Log($"<color='red'>DevRemoteConfig: {configVar} not loaded!</color>");
            }

            _loadCount++;

            if (_loadCount == _removeVars.Length)
            {
                _status = _configs.Count == _removeVars.Length
                    ? DevRemoteConfigLoadStatus.Complete
                    : DevRemoteConfigLoadStatus.Failed;
                OnLoadComplete?.Invoke(_status);
            }
        }

        public string GetValue(string remoteVarId)
        {
            if (_configs.ContainsKey(remoteVarId))
            {
                var configJson = _configs[remoteVarId];

                foreach (var removeVar in _removeVars)
                {
                    if (removeVar.configVar == remoteVarId && removeVar.takeOnce)
                    {
                        _configs.Remove(remoteVarId);
                        break;
                    }
                }

                return configJson;
            }

            return null;
        }
    }
}