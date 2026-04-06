using System;
using System.ComponentModel;
using System.Reflection;
using UnityEngine;
using Playcus.Loading;
using UnityEngine.Events;
#if UNITY_EDITOR
using UnityEditor;

#endif

namespace Playcus.RemoteConfig
{
    /// <summary>
    /// Load var from remote config manager and set values to choice fields
    /// </summary>
    public class RemoteJsonObject : RemoteComponent
    {
        // CONFIG
        [Tooltip(
            @"Remote config variable name what will override selected component by JsonUtility.FromJsonOverwrite.")]
        [SerializeField]
        private string RemoteConfigVar;
        
        [Header("Cache data in prefabs")]
        [SerializeField] private GameObject originalPrefab;
        [SerializeField] private bool RefreshOriginalPrefabInEditor = true;
        
        private UnityEvent valueLoadedSuccess;
        private UnityEvent valueLoadedNull;

        // PRIVATE
        private string remoteValue;
        

        protected override void SetVars()
        {
            if (targetComponent != null)
            {
                //Get remote var and set it value in component if value not null or empty
                remoteValue = remoteConfigManager.GetValue(RemoteConfigVar);
                if (!String.IsNullOrEmpty(remoteValue))
                {
                    JsonUtility.FromJsonOverwrite(remoteValue, targetComponent);
                    valueLoadedSuccess?.Invoke();
                }
                else
                {
                    valueLoadedNull?.Invoke();
                }
            }
        }

        public void AttachOriginalPrefab(GameObject originalPrefab)
        {
            this.originalPrefab = originalPrefab;
        }

        private void OnDisable()
        {
            if (RefreshOriginalPrefabInEditor)
            {
                UpdateOriginal();
            }
        }

        private void UpdateOriginal()
        {
            // Update original prefab if we are in editor
#if UNITY_EDITOR
            // Try to find original prefab
            
            if (originalPrefab == null)
            {
                Debug.LogWarning($"RemoteJsonObject original prefab not founded {gameObject.name}", gameObject);
                /*
                originalPrefab = PrefabUtility.GetCorrespondingObjectFromOriginalSource(this.gameObject);
                if (originalPrefab == null)
                {
                    Debug.LogWarning($"RemoteJsonObject original prefab not founded {gameObject.name}", gameObject);
                    Debug.LogWarning(
                        $"RemoteJsonObject GetCorrespondingObjectFromOriginalSource {PrefabUtility.GetCorrespondingObjectFromOriginalSource(this.gameObject) != null}",
                        gameObject);
                    Debug.LogWarning(
                        $"RemoteJsonObject GetCorrespondingObjectFromSource {PrefabUtility.GetCorrespondingObjectFromSource(this.gameObject) != null}",
                        gameObject);
                    Debug.LogWarning(
                        $"RemoteJsonObject GetPrefabInstanceHandle {PrefabUtility.GetPrefabInstanceHandle(this.gameObject) != null}",
                        gameObject);
                    Debug.LogWarning(
                        $"RemoteJsonObject IsPartOfAnyPrefab {PrefabUtility.IsPartOfAnyPrefab(this.gameObject)}",
                        gameObject);
                }
                */
            }
            

            if (!string.IsNullOrEmpty(remoteValue) && originalPrefab != null)
            {
                var original = originalPrefab.GetComponent(targetComponent.GetType());
                if (original != null)
                {
                    JsonUtility.FromJsonOverwrite(remoteValue, original);
                    EditorUtility.SetDirty(original);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                    Debug.Log($"RemoteJsonObject original prefab updated {gameObject.name}", gameObject);
                }
                else
                {
                    Debug.LogWarning($"RemoteJsonObject original component not founded {gameObject.name}", gameObject);
                }
            }
#endif
        }

        public void ParseObjectToJsonInLog()
        {
            Debug.Log($"RemoteJsonObject: {gameObject.name} {JsonUtility.ToJson(targetComponent)}", gameObject);
        }
    }
}