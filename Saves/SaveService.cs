using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Playcus.Loading;
using Playcus.Utils;
using UnityEngine;
using UnityEngine.Networking;

namespace Playcus.Saves
{
    /// <summary>
    /// Universal hub to save and load game variables from saved data
    /// Recommended method to use: FromJsonOverwrite and ToJsonOverwrite
    /// Legacy methods (for back compability not recommended to use)
    /// </summary>
    [ServiceBind(typeof(ISaveService))]
    public class SaveService : ServiceWithConfig, ISaveService
    {
        [InjectService] private Loader _loader;

        private Dictionary<string, object> _savedObjects = new Dictionary<string, object>();
        private bool _needBeSaved;

        private List<BaseStorage> _storages = new List<BaseStorage>();
        private BaseStorage _currentStorage;

        protected override Type ConfigType => typeof(SaveServiceConfig);
        protected SaveServiceConfig Config => (SaveServiceConfig)_serviceConfig;

        public event Action<string> OnDebug;
        private Coroutine _waitServiceAndSave;

        #region Service loading

        protected override async UniTask LoadAsyncInternal(CancellationToken cancellationToken)
        {
            // await base.LoadAsyncInternal(cancellationToken);
            try
            {
                await LoadServiceConfigAsync(cancellationToken, false);
                
                foreach (var prefab in Config.CurrentStore.StoragesPrefabs)
                {
                    if (prefab != null)
                    {
                        var storage = Instantiate(prefab, transform);
                        _storages.Add(storage);
                    }
                    else
                    {
                        Debug.LogError("SaveService: Storage prefab is null");
                    }
                }

                foreach (var storage in _storages)
                {
                    storage.Synchronize(OnLoadStorageComplete);
                }

                if (_storages.Count <= 0)
                {
                    Debug.LogError("SaveService: No save storages");
                    // ServiceLoadingComplete();
                }
            }
            catch (OperationCanceledException)
            {
                State = ServiceState.Failed;
                return;
            }
            catch (Exception e)
            {
                State = ServiceState.Failed;
                Console.WriteLine(e);
                throw;
            }
        }

        private void OnLoadStorageComplete(BaseStorage storage, bool success)
        {
            if (!success)
                Debug.LogError($"SaveService: Failed loading storage {storage}");

            if (State!=ServiceState.Ready && IsAllStoragesSynchronize())
            {
                _currentStorage = GetCurrencyStorage();
                if (_currentStorage == null && _storages.Count > 0)
                {
                    _currentStorage = _storages[0];
                }

                ServiceLoadingComplete();
            }
        }

        private bool IsAllStoragesSynchronize()
        {
            foreach (var storage in _storages)
            {
                if (!storage.IsInit)
                    return false;
            }

            return true;
        }

        #endregion

        #region Interface Methods

        /// <summary>
        /// Registers a serializable value object to be saved and load it data with json overide immediately
        /// </summary>
        public void RegisterAndLoad(string uniqueKey, object saveVO)
        {
            if (_savedObjects.ContainsValue(saveVO) == false)
            {
                _savedObjects.Add(uniqueKey, saveVO);
            }

            FromJsonOverwrite(uniqueKey, saveVO);
        }

        public async UniTask RegisterAndLoadAsync(string uniqueKey, object saveVO)
        { 
            await UniTask.WaitUntil(()=>State==ServiceState.Ready);
            
            if (_savedObjects.ContainsValue(saveVO) == false)
            {
                _savedObjects.Add(uniqueKey, saveVO);
            }

            FromJsonOverwrite(uniqueKey, saveVO);
        }

        /// <summary>
        /// Saves all registered saveables to the save file after 0.5f sec delay or immediately if forceSaveNow=true 
        /// </summary>
        public void Save(bool forceSaveNow = false)
        {
            if (_loader != null && _loader.CheckAllServicesLoaded(out var notLoadedService))
            {
                Debug.Log("SaveService: Save forceSaveNow=" + forceSaveNow.ToString(), gameObject);
                if (!_needBeSaved)
                {
                    _needBeSaved = true;
                    if (forceSaveNow)
                        InvokedSave();
                    else
                        Invoke(nameof(InvokedSave), 0.5f);
                }
            }
            else if (_loader == null)
            {
                Debug.LogWarning($"SaveService: failed to save because _loader is null", gameObject);
            }
            else if (!_loader.CheckAllServicesLoaded(out notLoadedService))
            {

                if (_waitServiceAndSave == null)
                {
                    Debug.LogWarning(
                        $"SaveService: Failed to save because service {notLoadedService} not loaded. Saving will be performed automatically as soon as all services are loaded.", gameObject);

                    _waitServiceAndSave = StartCoroutine(WaitAllServiceAndSave());
                }
                else
                {
                    Debug.LogWarning(
                        $"SaveService: Failed to save because service {notLoadedService} not loaded. Waiting all services are loaded.", gameObject);
                }
            }
        }


    private IEnumerator WaitAllServiceAndSave()
        {
            var waiter = new WaitForSecondsRealtime(0.5f);
            while (!_loader.CheckAllServicesLoaded(out var notLoadedService))
            {
                yield return waiter;
            }
            
            Save();
        }

        

        public async UniTask DeleteSaveData()
        {
            var taskArray = new List<UniTask>();

            OnDebugStorage("SaveService: Delete");

            foreach (var storage in _storages)
            {
                if (storage != null)
                {
                    var uniTask = storage.Delete();
                    taskArray.Add(uniTask);
                }
            }

            await UniTask.WhenAll(taskArray);
        }

        /// <summary>
        /// LEGACY Loads the save data for the given save id. Use RegisterAndLoad instead
        /// </summary>
        public JSONNode LoadSave(string saveId)
        {
            if (_currentStorage != null)
            {
                var loadedSave = _currentStorage.Load();

                // Check if the loaded save file has the given save id
                if (loadedSave == null || !loadedSave.AsObject.HasKey(saveId))
                    return null;

                // Return the JSONNode for the save id
                return loadedSave[saveId];
            }

            Debug.LogError("SaveService: CurrentStorage is null");
            return null;
        }

        #endregion

        #region Private Methods

        private void InvokedSave()
        {
            Debug.Log("SaveService: InvokedSave", gameObject);
            _needBeSaved = false;
            Dictionary<string, object> saveJson = new Dictionary<string, object>();
            // saved data (new method)
            foreach (var item in _savedObjects)
            {
                if (!saveJson.ContainsKey(item.Key))
                    saveJson.Add(item.Key, ToJsonOverwrite(item.Key, item.Value));
            }

            // saveables (legacy method)
            for (int i = 0; i < saveables.Count; i++)
            {
                if (!saveJson.ContainsKey(saveables[i].SaveId))
                    saveJson.Add(saveables[i].SaveId, saveables[i].Save());
            }
            
            try
            {
                var json = JSON.ConvertToJsonString(saveJson);
                OnDebugStorage("SaveService: Save data = " + json);

                // @TODO need check save for 
                foreach (var storage in _storages)
                {
                    if (storage.IsSynchronize)
                        storage.Save(json);
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        /// <summary>
        /// Loads and overwrite object from json value
        /// </summary>
        private void FromJsonOverwrite(string uniqueKey, object objectToOverwrite)
        {
            JSONNode json = LoadSave(uniqueKey);
            if (json != null && json.AsObject.HasKey(uniqueKey))
            {
                string savedJsonString = UnityWebRequest.UnEscapeURL(json[uniqueKey]);
                JsonUtility.FromJsonOverwrite(savedJsonString, objectToOverwrite);
            }
            else
            {
                Debug.Log("SaveService : key not founded : " + uniqueKey, gameObject);
            }
        }

        /// <summary>
        /// Loads and overwrite object from json value
        /// </summary>
        private Dictionary<string, object> ToJsonOverwrite(string uniqueKey, object objectToOverwrite)
        {
            Dictionary<string, object> returned = new Dictionary<string, object>();
            returned.Add(uniqueKey, UnityWebRequest.EscapeURL(JsonUtility.ToJson(objectToOverwrite)));
            return returned;
        }

        [ContextMenu("GetCurrencyStorage")]
        private BaseStorage GetCurrencyStorage()
        {
            BaseStorage result = null;

            foreach (var storage in _storages)
            {
                if (storage.IsInit && storage.IsSynchronize)
                {
                    if (result == null)
                    {
                        result = storage;
                    }
                    else
                    {
                        var currencyValue = GetJsonCurrency(result.Load(), Config.CompareProgressKey);
                        var storageValue = GetJsonCurrency(storage.Load(), Config.CompareProgressKey);

                        Debug.Log("SaveService.GetCurrencyStorage: currencyValue = " + currencyValue);
                        Debug.Log("SaveService.GetCurrencyStorage: storageValue = " + storageValue);

                        if (storageValue > currencyValue)
                            result = storage;
                    }
                }
            }

            Debug.Log("SaveService: current storage = " + result);

            return result;
        }

        private int GetJsonCurrency(JSONNode jsonNode, string currencyKey)
        {
            if (jsonNode != null)
            {
                Debug.Log("jsonNode = " + jsonNode.ToString());

                var keys = currencyKey.Split(new[] { '/' });
                if (keys != null && keys.Length == 3)
                {
                    try
                    {
                        var node = jsonNode[keys[0]];
                        if (node != null)
                        {
                            var nodeArray = node[keys[1]];

                            var isIndexParsed = int.TryParse(keys[2], out var currencyIndex);
                            if (nodeArray != null && nodeArray.IsArray && isIndexParsed && nodeArray.Count > currencyIndex)
                            {
                                return nodeArray.AsArray[currencyIndex].AsInt;
                            }
                            else
                            {
                                Debug.LogError($"SaveService: currencyKey not find key {keys[1]} or not array");
                            }
                        }
                        else
                        {
                            Debug.LogError($"SaveService: currencyKey not find key {keys[0]}");
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"SaveService: GetJsonCurrency exception - {e.Message}");
                    }
                }
                else
                {
                    Debug.LogError("SaveService: currencyKey wrong path");
                }
            }
            else
                Debug.Log("SaveService: GetJsonCurrency jsonNode is null");


            return 0;
        }

        #endregion

        #region (LEGACY) methods for back compability

        private List<ISaveable> saveables = new List<ISaveable>();

        /// <summary>
        /// LEGACY Registers a saveable to be saved. Use RegisterAndLoad instead
        /// </summary>
        public void Register(ISaveable saveable)
        {
            Debug.Log("SaveService: Register " + saveable.SaveId, gameObject);
            saveables.Add(saveable);
        }

        /// <summary>
        /// Prepare string to json (LEGACY)
        /// </summary>
        public string SaveKeyToJson(object obj)
        {
            return UnityWebRequest.EscapeURL(JsonUtility.ToJson(obj));
        }

        /// <summary>
        /// Loads key as json ready string (LEGACY)
        /// </summary>
        public string LoadKeyAsJson(ISaveable saveable, string key)
        {
            string returned = "";
            JSONNode json = LoadSave(saveable.SaveId);
            if (json != null && json.AsObject.HasKey(key))
            {
                returned = UnityWebRequest.UnEscapeURL(json[key]);
            }
            else
            {
                Debug.Log("SaveManager : key not founded : " + key, gameObject);
            }

            return returned;
        }

        #endregion

        #region For test

        public void Register(string uniqueKey, object saveVO)
        {
            if (_savedObjects.ContainsKey(uniqueKey) == false)
                _savedObjects.Add(uniqueKey, saveVO);
        }

        public void Load(string uniqueKey, object objectToOverwrite)
        {
            FromJsonOverwrite(uniqueKey, objectToOverwrite);
        }

        private void OnDebugStorage(string text)
        {
            OnDebug?.Invoke(text);
        }

        #endregion
    }
}