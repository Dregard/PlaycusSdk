using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
#if UNITY_ANDROID && MOBILECLOUD_PLUGIN_ENABLED
using Cysharp.Threading.Tasks;
using GooglePlayGames;
using GooglePlayGames.Android;
using GooglePlayGames.BasicApi;
using GooglePlayGames.BasicApi.SavedGame;
#endif
using UnityEngine;
using Playcus.Utils;

namespace Playcus.Saves
{
    public class GooglePlaySaveStorage :  BaseStorage
    {
        public const string SAVE_DATA_KEY = "saveDataKey";
        
        [SerializeField] private float _loadingTimeout = 5f;

#if UNITY_ANDROID && MOBILECLOUD_PLUGIN_ENABLED
        private ISavedGameMetadata _savedGameMetadata;
#endif
        private string _savedJson;
        private bool _isSavedProgress;
        private string _queueSavedJson;
        protected bool _isSaveDeleted;
   
        public override void Synchronize(Action<BaseStorage, bool> callbackComplete)
        {
            base.Synchronize(callbackComplete);
            
            StartCoroutine(SynchronizeTimeout(_loadingTimeout));
            
            SignIn();
        }
        
        private IEnumerator SynchronizeTimeout(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            
            Debug.Log($"{name}: SynchronizeTimeout is end,   game time = {Time.realtimeSinceStartup}");
            SynchronizeComplete(false);
        }

        #region Init
        private void SignIn()
        {
#if UNITY_ANDROID && MOBILECLOUD_PLUGIN_ENABLED
            // Debug.Log("GooglePlaySaveStorage: PlayGamesClientConfiguration");
            // PlayGamesClientConfiguration config = new PlayGamesClientConfiguration.Builder()
            //     // enables saving game progress.
            //     .EnableSavedGames()
            //     .Build();            
            //
            // Debug.Log("GooglePlaySaveStorage: init config = " + config);
            // PlayGamesPlatform.InitializeInstance(config);
            //
            // PlayGamesPlatform.DebugLogEnabled = true;
            // PlayGamesPlatform.Activate();
            //
            // Debug.Log("PlayGamesPlatform.Instance = " + PlayGamesPlatform.Instance);
            //
            // PlayGamesPlatform.Instance.Authenticate(SignInInteractivity.CanPromptOnce, (result) =>
            // {
            //     Debug.Log($"GooglePlaySaveStorage: sign in result = {result}");
            //     
            //     if (result == SignInStatus.Success)
            //     {
            //         OpenSaveData(true);
            //     }
            //     else
            //     {
            //         SynchronizeComplete(false);
            //     }
            // });
            PlayGamesPlatform.Instance.Authenticate(ProcessAuthentication);
#endif
        }

#if  UNITY_ANDROID && MOBILECLOUD_PLUGIN_ENABLED
        
        internal void ProcessAuthentication(SignInStatus status) {
            if (status == SignInStatus.Success) {
                OpenSaveData(true);
            } else {
                SynchronizeComplete(false);
            }
        }
        
        private void OpenSaveData(bool withLoad = false)
        {
            try
            {
                ISavedGameClient savedGameClient = PlayGamesPlatform.Instance.SavedGame;
                Debug.Log("GooglePlaySaveStorage: savedGameClient = " + savedGameClient);
                savedGameClient.OpenWithAutomaticConflictResolution(SAVE_DATA_KEY, DataSource.ReadCacheOrNetwork,
                    ConflictResolutionStrategy.UseLongestPlaytime, (status, game) =>
                    {
                        Debug.Log($"GooglePlaySaveStorage: OnSavedGameOpened status = {status}");
            
                        if (status == SavedGameRequestStatus.Success)
                        {
                            _savedGameMetadata = game;
                            
                            if (withLoad)
                                LoadGameData(_savedGameMetadata);
                            
                            CheckQueueSaved();
                        }
                        else
                        {
                            Debug.LogError("GooglePlaySaveStorage: not opened saved game");
                        }
                    });
                    
                Debug.Log("GooglePlaySaveStorage: OpenWithAutomaticConflictResolution");
            }
            catch (Exception e)
            {
                Console.WriteLine("GooglePlaySaveStorage:" + e);
                throw;
            }
        }

        private void LoadGameData(ISavedGameMetadata game)
        {
            ISavedGameClient savedGameClient = PlayGamesPlatform.Instance.SavedGame;
            savedGameClient.ReadBinaryData(game, (status, data) =>
            {
                Debug.Log($"GooglePlaySaveStorage: OnSavedGameDataRead status = {status}");
            
                if (status == SavedGameRequestStatus.Success)
                {
                    _savedJson = Encoding.UTF8.GetString(data);
                    SynchronizeComplete(true);
                }
                else
                {
                    Debug.LogError("GooglePlaySaveStorage: not read saved game");
                }
            });
        }

        public override async UniTask Delete()
        {
            await base.Delete();
            
            _isSaveDeleted = false;

            // Open the file to get the metadata.
            if (PlayGamesPlatform.Instance != null && PlayGamesPlatform.Instance.IsAuthenticated())
            {
                ISavedGameClient savedGameClient = PlayGamesPlatform.Instance.SavedGame;
                savedGameClient.OpenWithAutomaticConflictResolution(SAVE_DATA_KEY, DataSource.ReadCacheOrNetwork,
                    ConflictResolutionStrategy.UseLongestPlaytime, DeleteSavedGame);
            
                await UniTask.WaitUntil(() => _isSaveDeleted == true); 
            }
        }
       
        private void DeleteSavedGame(SavedGameRequestStatus status, ISavedGameMetadata game) 
        {
            if (status == SavedGameRequestStatus.Success) 
            {
                ISavedGameClient savedGameClient = PlayGamesPlatform.Instance.SavedGame;
                savedGameClient.Delete(game);
            } 
            else 
            {
                Debug.LogError("GooglePlaySaveStorage: Delete error exception");
            }

            _isSaveDeleted = true;
        }
#endif
        #endregion

        #region Save / Load
        public override JSONNode Load()
        {
            if (IsSynchronize && !string.IsNullOrEmpty(_savedJson))
            {
                Debug.Log("GooglePlaySaveStorage: Load");
                
                try
                {
                    if (string.IsNullOrEmpty(_savedJson) == false)
                    {
                        try
                        {
                            return JSON.Parse(_savedJson);
                        }
                        catch (Exception e)
                        {
                            Debug.LogError(e.Message);
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError("GooglePlaySaveStorage: Load error exception");
                }
            }
            else
                Debug.Log("GooglePlaySaveStorage: Load failed - not synchronize");

            return null;
        }
        
        public override void Save(string json)
        {
#if  UNITY_ANDROID && MOBILECLOUD_PLUGIN_ENABLED
            if (IsSynchronize)
            {
                if (_savedGameMetadata != null && _savedGameMetadata.IsOpen)
                {
                    Debug.Log("GooglePlaySaveStorage: Save data = " + json);

                    if (_isSavedProgress)
                    {
                        _queueSavedJson = json;
                    }
                    else
                    {
                        _isSavedProgress = true;

                        try
                        {
                            ISavedGameClient savedGameClient = PlayGamesPlatform.Instance.SavedGame;
                            SavedGameMetadataUpdate updatedMetadata = new SavedGameMetadataUpdate.Builder()
                                .WithUpdatedPlayedTime(new TimeSpan(DateTime.Now.Ticks))
                                .Build();

                            var savedData = Encoding.UTF8.GetBytes(json);
                            savedGameClient.CommitUpdate(_savedGameMetadata, updatedMetadata, savedData,
                                (SavedGameRequestStatus status, ISavedGameMetadata game) =>
                                {
                                    if (status == SavedGameRequestStatus.Success)
                                    {
                                        _savedJson = json;
                                        Debug.Log("GooglePlaySaveStorage: save complete");
                                    }
                                    else
                                    {
                                        // handle error
                                        Debug.LogError("GooglePlaySaveStorage: write save game failed. Error = " +
                                                       status);
                                    }

                                    _savedGameMetadata = null;
                                    _isSavedProgress = false;
                                    OpenSaveData();
//                                    CheckQueueSaved();
                                });
                        }
                        catch (Exception e)
                        {
                            Debug.LogError("GooglePlaySaveStorage: Save error exception");

                            _isSavedProgress = false;
                            CheckQueueSaved();
                        }
                    }
                }
                else
                {
                    _queueSavedJson = json;
                    
                    OpenSaveData();
                }
            }
            else
                Debug.Log("GooglePlaySaveStorage: Save failed - not synchronize");
#endif
        }

        private void CheckQueueSaved()
        {
#if UNITY_ANDROID && MOBILECLOUD_PLUGIN_ENABLED
            _isSavedProgress = false;
            
            if (_savedGameMetadata != null && !string.IsNullOrEmpty(_queueSavedJson))
            {
                Save(_queueSavedJson);
                _queueSavedJson = null;
            }
#endif
        }
        #endregion
    }
}