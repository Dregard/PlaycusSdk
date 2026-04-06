using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using Cysharp.Threading.Tasks;
using Playcus.Analytics.Internal;
// using Playcus.Currency;
#if GDPR
using Playcus.GDPR;
#endif
using UnityEngine;
using UnityEngine.Purchasing;
using Playcus;
#if GDPR
using Playcus.GDPR;
#endif

namespace Playcus.Analytics
{
    /// <summary>
    /// Must be used only as IAnalyticsService. You can find full documentation in IAnalyticsService.
    /// </summary>
    [ServiceBind(typeof(IAnalyticsManager))]
    public class AnalyticsService : ServiceWithConfig, IAnalyticsManager
    {
        private readonly string[] _eventsForNonTesters = new[]
        {
            AnalyticsEvents.pl_purchase_success.ToString(),
            AnalyticsEvents.pl_purchase_checkout.ToString(),
            AnalyticsEvents.pl_ads_revenue.ToString(),
        };
        
        // CONFIG
        [HelpBox(
            @"SETUP INSTRUCTION 
1. Add all Stores where analytics need (or can) be tracked.
2. Link prefab with attached AnalyticsServicePlatform realizations to each Store.
3. Analytics events must be tracked only by ServiceLocator.Get<IAnalyticsManager>()"
            , HelpBoxMessageType.Info)]
        [SerializeField]
        private bool _readme;

        protected override Type ConfigType => typeof(AnalyticsServiceConfig);
        protected AnalyticsServiceConfig Config => (AnalyticsServiceConfig) _serviceConfig;


        // PRIVATE
        private List<AnalyticsServicePlatform> _analyticsPlatforms = new List<AnalyticsServicePlatform>();
        private DateTime _lastTutorialStepTime = DateTime.Now;

        /// <summary>
        /// Parameters that register game and that need be tracked in every possible event
        /// </summary>
        private Dictionary<string, object> _generalEventParameters = new Dictionary<string, object>();

        /// <summary>
        /// The queue for sending events that are waiting for the service to load
        /// </summary>
        private Queue<EventData> _eventQueue = new Queue<EventData>();

        /// <summary>
        /// Some events should be not tracked in analytics for tester users
        /// </summary>

        private bool _isGdprAccepted = false;
        private bool isDebug;
        public virtual bool IsTesterUser()
        {
            bool isTester = false;
            // TODO testers uids list or segment
            if (isDebug || Application.isEditor)
                isTester = true;
            return isTester;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
#if GDPR
            var usercentricsService = ServiceLocator.Get<UsercentricsConsentService>();
                    
            if (usercentricsService != null && usercentricsService.IsGDPRAccepted())
                _isGdprAccepted = true;

            // GDPRService.GDPRAcceptedEvent += OnGdprAccepted;
            UsercentricsConsentService.GDPRAcceptedEvent += OnGdprAccepted;
#endif
            
            isDebug = Debug.isDebugBuild;
#if PL_USERCENTRICS_CONSENT_ON
            // With Usercentrics settings, now user can only ACCEPT ALL or leave app, so no need to check if it accepted
            _isGdprAccepted = true;
#endif
        }

        protected override void OnDisable()
        {
            base.OnDisable();
#if GDPR
            UsercentricsConsentService.GDPRAcceptedEvent -= OnGdprAccepted;
#endif
        }

        private void OnGdprAccepted()
        {
            _isGdprAccepted = true;
        }

        public void LogDebug(string newMessage, bool force = false)
        {
            if (force || IsTesterUser())
            {
                Debug.Log($"AnalyticsService: {newMessage}");
            }
        }

        public bool IsTestEnvironment()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            using (var actClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                var context = actClass.GetStatic<AndroidJavaObject>("currentActivity");
                var systemGlobal = new AndroidJavaClass("android.provider.Settings$System");
                var testLab =
 systemGlobal.CallStatic<string>("getString", context.Call<AndroidJavaObject>("getContentResolver"), "firebase.test.lab");
                return testLab == "true";
            }
#else
            return false;
#endif
        }

        protected override async UniTask LoadAsyncInternal(CancellationToken cancellationToken)
        {
            await base.LoadAsyncInternal(cancellationToken);

            if (IsTestEnvironment())
            {
                ServiceLoadingComplete();
                return;
            }

            // Try find and init ads network config
            if (Config.Stores.Length > 0)
            {
                foreach (var store in Config.Stores)
                {
                    if (store.Store == StoreConstants.GetCurrentStore())
                    {
                        if (store.AnalyticsPlatformConfigPrefabs == null ||
                            store.AnalyticsPlatformConfigPrefabs.Count == 0)
                        {
                            Debug.LogError(
                                $"AnalyticsService::Load - Store {store.Store.ToString()} added but hasn't any config");
                            return;
                        }

                        for (var i = 0; i < store.AnalyticsPlatformConfigPrefabs.Count; i++)
                        {
                            var AnalyticsPlatformPrefub = store.AnalyticsPlatformConfigPrefabs[i];
                            if (AnalyticsPlatformPrefub == null)
                            {
                                Debug.LogError(
                                    $"AnalyticsService::Load - Store {store.Store.ToString()} has empty GameObject in position {i}");
                                break;
                            }

                            GameObject analyticsPlatformPrefab = Instantiate(AnalyticsPlatformPrefub, transform);
                            analyticsPlatformPrefab.SetActive(true);
                            AnalyticsServicePlatform aplatform =
                                analyticsPlatformPrefab.GetComponent<AnalyticsServicePlatform>();
                            if (aplatform != null && !aplatform.IsInited)
                            {
                                _analyticsPlatforms.Add(aplatform);
                                LogDebug($"{aplatform} added to list of analytics platforms", true);
                                aplatform.TryToInit();
                            }
                            else
                            {
                                Debug.LogError(
                                    $"AnalyticsService::Load - can't get component with analytics platform for {analyticsPlatformPrefab}");
                            }
                        }
                    }
                }
            }

            ServiceLoadingComplete();
        }

        private void CheckLoaded()
        {
            if (State != ServiceState.Ready)
            {
                Debug.LogWarning(
                    $"AnalyticsService not loaded yet. Use IAnalyticsManager.IsLoaded for check service loading status before tracking events.");
            }
        }

        private void MergeParameters(Dictionary<string, object> parameters, Dictionary<string, object>
            additionalParameters)
        {
            if (parameters != null && additionalParameters != null)
            {
                foreach (KeyValuePair<string, object> pair in additionalParameters)
                {
                    if (!parameters.ContainsKey(pair.Key))
                    {
                        parameters.Add(pair.Key, pair.Value);
                    }
                }
            }
        }

        //SOCIAL & VIRAL
        /*
        public void ShareCheckout(string id, PLACE placement, Dictionary<string, object> customParameters = null)
        {
            ShareCheckout(id, placement.ToString());
        }

        public void ShareCheckout(string id, string placement, Dictionary<string, object> customParameters = null)
        {
            LogDebug("ShareCheckout");
            CheckLoaded();
            foreach (AnalyticsServicePlatform aplatform in _analyticsPlatforms)
            {
                Dictionary<string, object> eventParameters = new Dictionary<string, object>();
                eventParameters.Add(AnalyticsProperties.pr_content_id.ToString(), id);
                eventParameters.Add(AnalyticsProperties.pr_placement.ToString(), placement);

                MergeParameters(eventParameters, _generalEventParameters);
                MergeParameters(eventParameters, customParameters);

                aplatform.RegisterEvent(AnalyticsEvents.pl_share_checkout.ToString(), eventParameters);
            }
        }


        public void ShareSuccess(string id, PLACE placement, Dictionary<string, object> customParameters = null)
        {
            ShareSuccess(id, placement.ToString());
        }
        
        public void ShareFailed(string id, PLACE placement, Dictionary<string, object> customParameters = null)
        {
            ShareFailed(id, placement.ToString());
        }


        public void ShareSuccess(string id, string placement, Dictionary<string, object> customParameters = null)
        {
            LogDebug("ShareSuccess");
            CheckLoaded();
            foreach (AnalyticsServicePlatform aplatform in _analyticsPlatforms)
            {
                Dictionary<string, object> eventParameters = new Dictionary<string, object>();
                eventParameters.Add(AnalyticsProperties.pr_content_id.ToString(), id);
                eventParameters.Add(AnalyticsProperties.pr_placement.ToString(), placement);

                MergeParameters(eventParameters, _generalEventParameters);
                MergeParameters(eventParameters, customParameters);

                aplatform.RegisterEvent(AnalyticsEvents.pl_share_success.ToString(), eventParameters);
            }
        }
        
        public void ShareFailed(string id, string placement, Dictionary<string, object> customParameters = null)
        {
            LogDebug("ShareSuccess");
            CheckLoaded();
            foreach (AnalyticsServicePlatform aplatform in _analyticsPlatforms)
            {
                Dictionary<string, object> eventParameters = new Dictionary<string, object>();
                eventParameters.Add(AnalyticsProperties.pr_content_id.ToString(), id);
                eventParameters.Add(AnalyticsProperties.pr_placement.ToString(), placement);

                MergeParameters(eventParameters, _generalEventParameters);
                MergeParameters(eventParameters, customParameters);

                aplatform.RegisterEvent(AnalyticsEvents.pl_share_failed.ToString(), eventParameters);
            }
        }


        public void RequestCheckout(string id, PLACE placement, Dictionary<string, object> customParameters = null)
        {
            RequestCheckout(id, placement.ToString());
        }

        public void RequestCheckout(string id, string placement, Dictionary<string, object> customParameters = null)
        {
            LogDebug("RequestCheckout");
            CheckLoaded();
            foreach (AnalyticsServicePlatform aplatform in _analyticsPlatforms)
            {
                Dictionary<string, object> eventParameters = new Dictionary<string, object>();
                eventParameters.Add(AnalyticsProperties.pr_content_id.ToString(), id);
                eventParameters.Add(AnalyticsProperties.pr_placement.ToString(), placement);

                MergeParameters(eventParameters, _generalEventParameters);
                MergeParameters(eventParameters, customParameters);

                aplatform.RegisterEvent(AnalyticsEvents.pl_request_checkout.ToString(), eventParameters);
            }
        }


        public void RequestSuccess(string id, PLACE placement, Dictionary<string, object> customParameters = null)
        {
            RequestSuccess(id, placement.ToString());
        }


        public void RequestSuccess(string id, string placement, Dictionary<string, object> customParameters = null)
        {
            LogDebug("RequestSuccess");
            CheckLoaded();
            foreach (AnalyticsServicePlatform aplatform in _analyticsPlatforms)
            {
                Dictionary<string, object> eventParameters = new Dictionary<string, object>();
                eventParameters.Add(AnalyticsProperties.pr_content_id.ToString(), id);
                eventParameters.Add(AnalyticsProperties.pr_placement.ToString(), placement);

                MergeParameters(eventParameters, _generalEventParameters);
                MergeParameters(eventParameters, customParameters);

                aplatform.RegisterEvent(AnalyticsEvents.pl_request_success.ToString(), eventParameters);
            }
        }

        public void SocialSignUp(Dictionary<string, object> customParameters = null)
        {
            LogDebug("SocialSignUp");
            CheckLoaded();
            foreach (AnalyticsServicePlatform aplatform in _analyticsPlatforms)
            {
                Dictionary<string, object> eventParameters = new Dictionary<string, object>();
                MergeParameters(eventParameters, _generalEventParameters);
                MergeParameters(eventParameters, customParameters);

                aplatform.RegisterEvent(AnalyticsEvents.pl_social_signup.ToString(), eventParameters);
            }
        }
        */

        //CUSTOM

        public void CustomEvent(string eventKey, int amount = 0, Dictionary<string, object> parameters = null, Dictionary<string, object> additionalParameters = null)
        {
            MergeParameters(parameters, _generalEventParameters);
            MergeParameters(parameters, additionalParameters);
            
            if (State == ServiceState.Ready && _isGdprAccepted && _eventQueue.Count == 0)

            {
                SendCustomEvent(eventKey, amount, parameters);
            }
            else
            {
                if (_eventQueue.Count == 0)
                {
                    LogDebug("Started waiting for the service to load to send the event in order.");
                    
                    SendEventsAfterServiceLoad();
                }
                
                LogDebug("Added an event to the queue. Event: "+ eventKey);

                _eventQueue.Enqueue(new EventData(eventKey, amount, parameters));
                // StartCoroutine(CustomEventAsync(eventKey, value, parameters));
            }
        }

        private void SendCustomEvent(string eventKey, int amount = 0,Dictionary<string, object> parameters = null)
        {
            LogDebug("CustomEvent " + eventKey + " " + amount.ToString());
            foreach (AnalyticsServicePlatform aplatform in _analyticsPlatforms)
            {
                if (Array.IndexOf(_eventsForNonTesters, eventKey) == -1 || // if the event can be sent for any type of user
                    (!IsTesterUser() || // or user is not a tester
                     aplatform.CanTesterBeTracked())) // or platform allow sending this event from the tester
                {
                    Dictionary<string, object> eventParameters = new Dictionary<string, object>();
                    eventParameters.Add(AnalyticsProperties.pr_amount.ToString(), amount.ToString());

                    MergeParameters(eventParameters, _generalEventParameters);
                    MergeParameters(eventParameters, parameters);

                    aplatform.RegisterEvent(eventKey, eventParameters);
                }
            }
        }
        
        private async UniTask SendEventsAfterServiceLoad()
        {
            await UniTask.WaitUntil(() => State == ServiceState.Ready && _isGdprAccepted);
            LogDebug("The service is loaded, start sending events from the queue.");

            while (_eventQueue.Count > 0)
            {
                var eventData = _eventQueue.Dequeue();
                SendCustomEvent(eventData.EventKey, eventData.Value, eventData.Parameters);
            }
        }

        public void SetGeneralParameterToAllEvents(string parameterKey, object parameterValue)
        {
            if (_generalEventParameters.ContainsKey(parameterKey))
            {
                _generalEventParameters[parameterKey] = parameterValue;
            }
            else
            {
                _generalEventParameters.Add(parameterKey, parameterValue);
            }
        }
    }

    public class EventData
    {
        public string EventKey { get; }
        public int Value { get; }
        public Dictionary<string, object> Parameters { get; }

        public EventData(string eventKey, int value = 0, Dictionary<string, object> parameters = null)
        {
            EventKey = eventKey;
            Value = value;
            Parameters = parameters;
        }

    }
}