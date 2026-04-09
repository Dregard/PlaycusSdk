using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Playcus.Analytics;
using Playcus.GDPR;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

namespace Playcus.Loading
{
    public class LoaderAsync : Loader
    {
        /*
         * LoaderAsync inherited from Loader to preserve binding logics.
         * This class does not show ads automatically
         */
        
        // //EDITOR VARIABLES
        // [FormerlySerializedAs("_initializationTimeout")]
        // [HelpBox(@"Initialization timeout in seconds.", HelpBoxMessageType.Info)]
        // [SerializeField] private int _globalInitializationTimeout = 15;

        [SerializeField] private bool _waitUsercentrics = true;

        protected override void Awake()
        {
            base.Awake();
        }
        
        protected override IEnumerator LoadServices()
        {
            var task = LoadServicesAsync();
            
            while (!task.IsCompleted)
            {
                yield return null;
            }

            // Check for exceptions in the async task
            if (task.Exception != null)
            {
                Debug.LogError($"Async initialization failed: {task.Exception}");
            }
        }

        private async Task LoadServicesAsync()
        {
            Debug.Log($"Loader: LoadServices", gameObject);

            LoadSceneAfterUsercentricsReady();
            
            bool firstSceneLoad = (SceneManager.GetActiveScene().buildIndex == 0);

            if (firstSceneLoad)
            {
                _analyticsManager = ServiceLocator.Get<IAnalyticsManager>(true);
                if (_analyticsManager != null && !_loadingStartTracked)
                {
                    _analyticsManager.LoadingStart();
                    _loadingStartTracked = true;
                }
            }

            var serviceGroups = new List<List<IService>>();
            var currentGroup = new List<IService>();
            serviceGroups.Add(currentGroup);

            foreach (var obj in _loadedObjects)
            {
                if (obj.TryGetComponent<IService>(out var service))
                {
                    currentGroup.Add(service);
                }
                else if (obj.TryGetComponent<LoaderWaitBarrierBehaviour>(out _))
                {
                    currentGroup = new List<IService>();
                    serviceGroups.Add(currentGroup);
                }
            }

            for (var i = 0; i < serviceGroups.Count; i++)
            {
                var group = serviceGroups[i];
                if (group.Count == 0)
                    continue;

                var loadingTasks = group.Select(service => service.LoadAsync()).ToList();

                await UniTask.WhenAll(loadingTasks);
                await UniTask.WaitWhile(() =>
                    group.Any(service => service.State == ServiceState.Initializing));

                RaiseChunkLoadedEvent(i + 1, serviceGroups.Count);
            }
            
            
            // foreach (var loadedObject in _loadedObjects)
            // {
            //     if (loadedObject.TryGetComponent<IService>(out var service))
            //     {
            //         services.Add(service);
            //     }
            //     else if (loadedObject.TryGetComponent<LoaderWaitBarrierBehaviour>(out var wait))
            //     {
            //         foreach (var serviceToLoad in services)
            //         {
            //             var loadingTask = serviceToLoad.LoadAsync();
            //             loadingTasks.Add(loadingTask);
            //         }
            //         await UniTask.WaitWhile(() =>
            //             services.Any(s => s.State == ServiceState.Initializing));
            //     }
            //     var servicesNeedLoadingList = loadedObject.GetComponents<Service>();
            //     // if (servicesNeedLoadingList != null)
            //     // {
            //     //     foreach (Service service in servicesNeedLoadingList)
            //     //     {
            //     //         Debug.Log($"Loader object {service.gameObject.name} load started");
            //     //         services.Add(service);
            //     //         var loadingTask = service.LoadAsync();
            //     //         loadingTasks.Add(loadingTask);
            //     //     }
            //     // }
            // }
            
            // foreach (var loadedObject in _loadedObjects)
            // {
            //     var servicesNeedLoadingList = loadedObject.GetComponents<Service>();
            //     if (servicesNeedLoadingList != null)
            //     {
            //         foreach (Service service in servicesNeedLoadingList)
            //         {
            //             Debug.Log($"Loader object {service.gameObject.name} load started");
            //             services.Add(service);
            //             var loadingTask = service.LoadAsync();
            //             loadingTasks.Add(loadingTask);
            //         }
            //     }
            // }

            // try
            // {
            //     await UniTask.WhenAll(loadingTasks);
            // }
            // catch (OperationCanceledException e)
            // {
            //    Debug.LogError($"[{nameof(LoaderAsync)}] Exception while initializing services: {e}");
            // }
            //
            // Debug.Log($"[{nameof(LoaderAsync)}] Services initializtion result:");
            // foreach (var service in services)
            // {
            //     Debug.Log($"[{nameof(LoaderAsync)}] Service '{service.GetType().Name}' initialized: {service.State==ServiceState.Ready}");
            // }

            // Scene loading
            if (firstSceneLoad)
            {
                // if (_nextSceneAutoLoad)
                // {
                //     if (_waitUsercentrics)
                //     {
                //         var _usercentricsConsentService = ServiceLocator.Get<UsercentricsConsentService>();
                //         await UniTask.WaitWhile(() => _usercentricsConsentService.State == ServiceState.Initializing);
                //     }
                //     SceneManager.LoadSceneAsync(1, LoadSceneMode.Single);
                // }

                Debug.Log($"Loader track analytics about loading", gameObject);
                _stopwatch.Stop();
                
                _analyticsManager?.LoadingEnd(Mathf.RoundToInt(GetCurrentLoadingTime()));
            }

            Debug.Log($"Loader loading completed in {GetCurrentLoadingTime()} sec", gameObject);
        }

        private async UniTaskVoid LoadSceneAfterUsercentricsReady()
        {
            if (_nextSceneAutoLoad)
            {
                if (_waitUsercentrics)
                {
                    var usercentricsConsentService = ServiceLocator.Get<UsercentricsConsentService>();
                    
                    await UniTask.WaitUntil(() =>
                        usercentricsConsentService.State is ServiceState.Ready or ServiceState.Failed);
                    
                    SceneManager.LoadSceneAsync(1, LoadSceneMode.Single);
                }
            }
        }
    }
}