using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Playcus.Utils;

namespace Playcus.Saves
{
    public class LocalSavedStorage : BaseStorage
    {
        public string SaveFilePath { get { return Application.persistentDataPath + "/save.json"; } }

        public override void Synchronize(Action<BaseStorage, bool> callbackComplete)
        {
            Debug.Log("LocalSavedStorage: Synchronize");
            base.Synchronize(callbackComplete);
            
            SynchronizeComplete(true);
        }
        
        public override void Save(string json)
        {
            Debug.Log("LocalSavedStorage: Save");
            System.IO.File.WriteAllText(SaveFilePath, json);
#if UNITY_WEBGL
            // https://discussions.unity.com/t/webgl-flushing-data-to-indexdb/240698
            Application.ExternalEval("FS.syncfs(false, function (err) {})");
#endif
        }

        public override JSONNode Load()
        {
            if (System.IO.File.Exists(SaveFilePath))
            {
                Debug.Log("LocalSavedStorage: Load success");
                return JSON.Parse(System.IO.File.ReadAllText(SaveFilePath));
            }

            Debug.Log("LocalSavedStorage: Load failed");
            return null;
        }

        public override async UniTask  Delete()
        {
            await base.Delete();
            
            Debug.Log("LocalSavedStorage: Delete");
            try
            {
                if (System.IO.File.Exists(SaveFilePath))
                {
                    System.IO.File.Delete(SaveFilePath);
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
            }
        }

        
    }
}
