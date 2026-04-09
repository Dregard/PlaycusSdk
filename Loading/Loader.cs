using System;
using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using Playcus.Analytics;
using System.Collections;
using System.Linq;
using System.Threading;
using Playcus.Ads;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Playcus.Loading
{
    /// <summary>
    /// Control loading of any services on application started.
    /// Contains loaded service's GameObjects as transform chields
    /// Documentation https://docs.google.com/document/d/1A6Ce52Xh3iLBiVgYKnjTHNCm4g21k5jln3PdMnZE0To/edit#
    /// </summary>
    //[ExecuteAlways]
    public class Loader : MonoBehaviour
    {
        //DEPENDENCIES
        protected IAnalyticsManager _analyticsManager;

        public event Action<int, int> ChunkLoadedEvent;

        //EDITOR VARIABLES
        [HelpBox(@"SETUP INSTRUCTION 
- All childs is loaded objects
- 1 step - bind all childs to services
- 2 step - resolve all childs by services
- 3 step - setActive all childs
- 4 step - load all services by order and chunks
- Use wait object for timelimit control of loading", HelpBoxMessageType.Info)]
        [SerializeField] private bool _readMe;
        [SerializeField] protected bool _nextSceneAutoLoad = true;
        [SerializeField] private bool _autoLoadWaitsForAdsToEnd = false;
        [SerializeField] private bool sendStepAnalyticEvents = false;
        //PRIVATE VARIABLES
        private int _syncLoadingStep;
        private bool _syncLoadingStepReady;
        private int _asyncLoadingStep;
        private bool _asyncLoadingStepReady;
        protected System.Diagnostics.Stopwatch _stopwatch;
        protected bool _loadingStartTracked;

        protected List<GameObject> _loadedObjects = new List<GameObject>();

        static protected bool _loaderInstantiated;
        
        protected Service[] _allServices;
        private bool _allServicesLoaded;

        public bool CheckAllServicesLoaded(out string notLoadedService)
        {
            notLoadedService = string.Empty;
            if (_allServicesLoaded)
            {
                return _allServicesLoaded;
            }

            foreach (var service in _allServices)
            {
                if (service.State != ServiceState.Ready)
                {
                    notLoadedService = service.gameObject.name;
                    return false;
                }
            }

            _allServicesLoaded = true;
            return _allServicesLoaded;
            
        }

        private void Update()
        {
            if (!Application.isEditor || Application.isPlaying)
                return;

            for (int i = 0; i < gameObject.transform.childCount; i++)
            {
                GameObject nextLoadedObject = gameObject.transform.GetChild(i).gameObject;
                if (nextLoadedObject.GetComponent<Service>() || nextLoadedObject.GetComponent<LoaderWaitBarrierBehaviour>())
                {
                    nextLoadedObject.SetActive(false);
                }
            }
        }



        protected virtual void Awake()
        {
            if (Application.isEditor && !Application.isPlaying)
                return;

            ServiceLocator.Bind<Loader>(this);
            Debug.Log("Loader: Awake and self binded", gameObject);

            // Only one loader can be in app
            if (!_loaderInstantiated)
            {
                _loaderInstantiated = true;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                this.gameObject.SetActive(false);
                return;
            }

            // Begin loading
            _stopwatch = System.Diagnostics.Stopwatch.StartNew();

            Debug.Log("Loader: Find all loaded objects", gameObject);
            for (int i = 0; i < gameObject.transform.childCount; i++)
            {
                _loadedObjects.Add(gameObject.transform.GetChild(i).gameObject);
            }

            _allServices = gameObject.transform.GetComponentsInChildren<Service>(true);
            
            Debug.Log("Loader: Mark all services as loaded by loader", gameObject);
            foreach (var item in _loadedObjects)
            {
                // Service object
                Service[] servicesOnObject = item.GetComponents<Service>();
                foreach (Service service in servicesOnObject)
                {
                    service.MarkAsLoadedByLoader();
                }
            }

            Debug.Log("Loader: Bind all services", gameObject);
            foreach (var item in _loadedObjects)
            {
                if (Application.isEditor)
                {
                    Debug.Log($"Loader: Bind all from object {item.name}", gameObject);
                }
                ServiceLocator.BindServicesFromObject(item);
            }

            Debug.Log("Loader: Resolve all services", gameObject);
            foreach (var item in _loadedObjects)
            {
                if (Application.isEditor)
                {
                    Debug.Log($"Loader: Resolve all in object {item.name}", gameObject);
                }
                ServiceLocator.ResolveServicesInObject(item);
            }

            Debug.Log("Loader: Activate all objects", gameObject);
            foreach (var item in _loadedObjects)
            {
                item.SetActive(true);
            }

            // Load all services
            StartCoroutine(LoadServices());
        }

        protected virtual IEnumerator LoadServices()
        {
            Debug.Log($"Loader: LoadServices", gameObject);

            // Loading
            int loadStartCount = 0;
            int alreadyLoadedCount = 0;
            float waitTimeLimit;
            bool waitTimeLimitSetuped;
            bool firstSceneLoad = (SceneManager.GetActiveScene().buildIndex == 0);
            List<Service> servicesStartLoadingList;

            if (firstSceneLoad)
            {
                _analyticsManager = ServiceLocator.Get<IAnalyticsManager>(true);
                if (_analyticsManager != null && !_loadingStartTracked)
                {
                    _analyticsManager.LoadingStart();
                    _loadingStartTracked = true;
                }
            }
            
            // Calculate chunks count
            int chunksCount = 0;
            int loadedChunksCount = 0;
            foreach (var item in _loadedObjects)
            {
                if (item != null && item.GetComponent<LoaderWait>() != null)
                {
                    chunksCount++;
                }
            }

            // Load chunks
            while (alreadyLoadedCount < _loadedObjects.Count - 1)
            {
                // Load next chunk
                waitTimeLimit = 0;
                waitTimeLimitSetuped = false;
                servicesStartLoadingList = new List<Service>();

                // Load every object in chunk before found LoaderWait object or complete
                for (int i = loadStartCount; i < _loadedObjects.Count; i++)
                {
                    loadStartCount++;

                    if (_loadedObjects[i] == null)
                    {
                        Debug.Log($"Skip loadedObjects[{i}]");
                        continue;
                    }

                    // Wait object
                    if (_loadedObjects[i].GetComponent<LoaderWait>() != null)
                    {
                        waitTimeLimit = _loadedObjects[i].GetComponent<LoaderWait>().GetTimelimit;
                        waitTimeLimitSetuped = true;
                        // finish for cycle
                        i = _loadedObjects.Count;
                    }
                    else
                    {
                        // Service object
                        Service[] servicesNeedLoadingList = _loadedObjects[i].GetComponents<Service>();
                        foreach (Service service in servicesNeedLoadingList)
                        {
                            Debug.Log($"Loader object {service.gameObject.name} load started");
                            service.LoadAsync();
                            servicesStartLoadingList.Add(service);
                        }
                    }
                }

                // Wait loading of every started loading object
                while (servicesStartLoadingList.Count > 0 && waitTimeLimitSetuped)
                {
                    // Wait real loading and remove already loaded from waitlist
                    for (int i = servicesStartLoadingList.Count - 1; i >= 0; i--)
                    {
                        Service service = servicesStartLoadingList[i];
                        if (service.State == ServiceState.Ready)
                        {
                            Debug.Log($"Loader object {service.gameObject.name} load completed {GetCurrentLoadingTime()}");
                            servicesStartLoadingList.Remove(service);
                        }
                    }
                    // Time limit for loading
                    if (waitTimeLimitSetuped && waitTimeLimit > 0f)
                    {
                        waitTimeLimit -= Time.unscaledDeltaTime;
                        if (waitTimeLimit <= 0f)
                        {
                            waitTimeLimitSetuped = false;
                            Debug.LogWarning($"Loader wait limit, go to next chunk without wait loading completed!");
                        }
                    }

                    yield return new WaitForEndOfFrame();
                }

                if (sendStepAnalyticEvents)
                {
                    _analyticsManager?.LoadingStep(loadedChunksCount,Mathf.RoundToInt(GetCurrentLoadingTime()));
                }
                
                alreadyLoadedCount = loadStartCount;
                loadedChunksCount++;
                Debug.Log($"Loader next chunk loaded {GetCurrentLoadingTime()}", gameObject);
                RaiseChunkLoadedEvent(loadedChunksCount, chunksCount);

                yield return new WaitForEndOfFrame();
            }

            // Scene loading
            if (firstSceneLoad)
            {
                if (_nextSceneAutoLoad)
                {
                    var adsService = ServiceLocator.Get<IAdsManager>();
                    if (adsService != null && _autoLoadWaitsForAdsToEnd)
                    {
                        while (adsService.IsAdsShowedNow())
                        {
                            yield return new WaitForEndOfFrame();
                        }
                    }
                    Debug.Log($"Loader load next scene", gameObject);
                    SceneManager.LoadSceneAsync(1, LoadSceneMode.Single);
                }

                Debug.Log($"Loader track analytics about loading", gameObject);
                _stopwatch.Stop();
                
                _analyticsManager?.LoadingEnd(Mathf.RoundToInt(GetCurrentLoadingTime()));
            }

            Debug.Log($"Loader loading completed in {GetCurrentLoadingTime()} sec", gameObject);
            yield return null;
        }

        protected void RaiseChunkLoadedEvent(int loadedChunksCount, int chunksCount)
        {
            ChunkLoadedEvent?.Invoke(loadedChunksCount, chunksCount);
        }

        protected float GetCurrentLoadingTime()
        {
            return _stopwatch.ElapsedMilliseconds / 1000f;
        }



    }
}