using System;
using System.Collections.Generic;
using Playcus.Services.Unity;
using Unity.Services.Core;
using UnityEngine;
#if PL_UNITY_ANALYTICS_ON
using Cysharp.Threading.Tasks;
using Unity.Services.Analytics;
#endif

namespace Playcus.Analytics
{
    public class UnityAnalyticServicePlatform : AnalyticsServicePlatform
    {
        public override bool CanTesterBeTracked()
        {
            return false;
        }

#if PL_UNITY_ANALYTICS_ON
        // PRIVATE
        private Action _initCallbackFunction;
        private IAnalyticsManager _analyticsManager;
        private IAnalyticsService _unityAnalyticsService;
        private AdProvider _adProvider;

        public override void TryToInit()
        {
            if (IsInited)
                return;

            TryToInitAsync();
        }

        private async UniTask TryToInitAsync()
        {
            if (ServiceLocator.Get<UnityServicesInitializer>() == null)
            {
                Debug.LogError($"IapManager: It is required to add a UnityServicesInitializer to the loader so that it is higher in the hierarchy than the IapManagerOffline", gameObject);
            }
            
            // wait & initialize services
            Debug.Log("UnityAnalyticServicePlatform: Waiting For Initializing UnityServices...", gameObject);

            if (UnityServices.State != ServicesInitializationState.Initialized)
            {
                await UniTask.WaitUntil(() => UnityServices.State == ServicesInitializationState.Initialized);
            }

            Debug.Log("UnityAnalyticServicePlatform: UnityServices initialization complete", gameObject);
            
            
            _analyticsManager = ServiceLocator.Get<IAnalyticsManager>();
            _analyticsManager.LogDebug("UnityAnalyticsServicePlatform init");
            _unityAnalyticsService = Unity.Services.Analytics.AnalyticsService.Instance;
            try
            {
                InitComplete();
                _analyticsManager.LogDebug("UnityAnalyticsServicePlatform InitCompleted");
            }
            catch (Exception e)
            {
                Debug.LogError($"UnityAnalyticServicePlatform:Init error {e.Message}");
            }
        }

        private string PrepareEventName(string eventName)
        {
            // GA reserved : symbol to separate events level
            eventName.Replace(' ', '_');
            eventName.Replace('.', '_');
            return eventName;
        }

        private string PrepareAdsPlatformName(string platformName)
        {
            platformName.ToLower();
            return platformName;
        }

        private AdProvider GetCurrentAdProvider(string platformName)
        {
            platformName.ToLower();
            switch (platformName)
            {
                case "applovin": return AdProvider.AppLovin;
                case "vungle": return AdProvider.Vungle;
                case "admob": return AdProvider.AdMob;
            }
            return AdProvider.AppLovin;
        }
        public override void TrackEvent(AnalyticsEvent currentEvent)
        {
            string preparedEventKey = PrepareEventName(currentEvent.EventKey);
            int amount = 0;
            if (currentEvent.Parameters.ContainsKey(AnalyticsProperties.pr_amount.ToString()))
            {
                Int32.TryParse(currentEvent.Parameters[AnalyticsProperties.pr_amount.ToString()].ToString(), out amount);
            }

            // PREDEFINED EVENTS
            if (currentEvent.EventKey == AnalyticsEvents.pl_purchase_success.ToString())
            {
                SendTransitionEvent(currentEvent);
            }
            else if (currentEvent.EventKey == AnalyticsEvents.pl_ads_rewarded_complete.ToString())
            {
                var parameters = new AdImpressionParameters();
                parameters.AdProvider = GetCurrentAdProvider(currentEvent.pr_ad_network);
                parameters.PlacementID = currentEvent.pr_placement;
                parameters.AdCompletionStatus = AdCompletionStatus.Completed;
                parameters.PlacementType = AdPlacementType.REWARDED;
                parameters.PlacementName = currentEvent.pr_placement;
                _unityAnalyticsService.AdImpression(parameters);
            }
            else if (currentEvent.EventKey == AnalyticsEvents.pl_ads_rewarded_canceled.ToString())
            {
                var parameters = new AdImpressionParameters();
                parameters.AdProvider = GetCurrentAdProvider(currentEvent.pr_ad_network);
                parameters.PlacementID = currentEvent.pr_placement;
                parameters.AdCompletionStatus = AdCompletionStatus.Partial;
                parameters.PlacementType = AdPlacementType.REWARDED;
                parameters.PlacementName = currentEvent.pr_placement;
                _unityAnalyticsService.AdImpression(parameters);
            }else if (currentEvent.EventKey == AnalyticsEvents.pl_ads_insterstitial_closed.ToString())
            {
                var parameters = new AdImpressionParameters();
                parameters.AdProvider = GetCurrentAdProvider(currentEvent.pr_ad_network);
                parameters.PlacementID = currentEvent.pr_placement;
                parameters.AdCompletionStatus = AdCompletionStatus.Completed;
                parameters.PlacementType = AdPlacementType.INTERSTITIAL;
                parameters.PlacementName = currentEvent.pr_placement;
                _unityAnalyticsService.AdImpression(parameters);
            }else if (currentEvent.EventKey == AnalyticsEvents.pl_ads_banner_showed.ToString())
            {
               var parameters = new AdImpressionParameters();
              parameters.AdProvider = GetCurrentAdProvider(currentEvent.pr_ad_network);
              parameters.AdCompletionStatus = AdCompletionStatus.Completed;
              parameters.PlacementID = currentEvent.pr_placement;
              parameters.PlacementType = AdPlacementType.BANNER;
              parameters.PlacementName = currentEvent.pr_placement;
              _unityAnalyticsService.AdImpression(parameters);
            }
            else
            {
                // CUSTOM EVENTS
                _unityAnalyticsService.CustomData(preparedEventKey, currentEvent.Parameters);
                // ATTENTION - GameAnalytics does not support additional parameters for events
            }
        }

        private void SendTransitionEvent(AnalyticsEvent currentEvent)
        {

            long priceCents = currentEvent.pr_currency.Equals("USD")
                ? Mathf.CeilToInt(float.Parse(currentEvent.pr_revenue,
                    System.Globalization.CultureInfo.InvariantCulture) * 100f)
                : 0;

            var productsReceived = new Product()
            {
                Items = new List<Item>()
                {
                    new Item() { ItemName = currentEvent.pr_content_id, ItemType = "ShopItem", ItemAmount = 1 },
                }
            };

            var productsSpent = new Product()
            {
                RealCurrency = new RealCurrency() { RealCurrencyType = "USD", RealCurrencyAmount = priceCents }
            };

            var transactionServer = TransactionServer.GOOGLE;
#if UNITY_IOS
                transactionServer = TransactionServer.APPLE;
#endif
            _unityAnalyticsService.Transaction(new TransactionParameters()
            {
                ProductsReceived = productsReceived,
                ProductsSpent = productsSpent,
                TransactionName = "IAP - " + currentEvent.pr_content_id,
                TransactionType = TransactionType.PURCHASE,
                TransactionServer = transactionServer,
                TransactionReceipt = currentEvent.pr_receipt
            });

        }

#else
        public override void TryToInit()
        {
            Debug.LogError("AnalyticsServicePlatform: Unity Cloud Analytics is Disabled", gameObject);
        }
        
        public override void TrackEvent(AnalyticsEvent currentEvent){}


        [HelpBox(@"Insert SDK_GA scriptable symbol in project settings", HelpBoxMessageType.Warning)]
#endif
        [SerializeField] private bool _iSeeThisAlert;
    }
}