using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using Playcus.Utils;
#if UNITY_EDITOR
using UnityEditor;
#endif
#if PL_SDK_PLAYCUSDATALAKE_ON
using PlaycusDL;
#endif
#if PL_SDK_DELTADNA_ON
using DeltaDNA;
#endif
#if PL_SDK_APPSFLYER_ON
using AppsFlyerSDK;
using Mistplay;
using Playcus.Ads;
#endif


namespace Playcus.Analytics
{
    /// <summary>
    /// For AnalyticsService used only! Don't use directly! (Simonenko Alexey)
    /// AppsFlyer Analytics https://support.appsflyer.com/hc/en-us/articles/213766183-AppsFlyer-SDK-Integration-Unity
    /// All playcus events list https://docs.google.com/spreadsheets/d/1JjSoB1pyAnKDIZ4Ir3PsFESau0e4rwD1WF_-PFT0bTg
    /// </summary>
    [ExecuteAlways]
#if PL_SDK_APPSFLYER_ON
    public partial class AppsFlyerAnalyticServicePlatform : AnalyticsServicePlatform, IAppsFlyerConversionData
#else
    public class AppsFlyerAnalyticServicePlatform : AnalyticsServicePlatform
#endif
    {
#region consent

        public static void SetConsent(bool consentGiven)
        {     
#if PL_SDK_APPSFLYER_ON
            AppsFlyer.setConsentData(AppsFlyerConsent.ForGDPRUser(consentGiven, consentGiven));
#endif
        }
        
#endregion
        public override bool CanTesterBeTracked()
        {
            return false;
        }

        // CONFIG
        [Header("Settings")] [SerializeField] protected string AppsflyerDevKey;

        [Header("Optional Settings")] [SerializeField]
        private string UwpAppID;

        [HelpBox(@"UwpAppID is case sensitive, so you need to define this ID exact like in Appsflyer")]
        [Tooltip(
            "Public key from Google Developer Console https://support.google.com/googleplay/android-developer/answer/186113?hl=en")]
        [SerializeField]
        private string GooglePublicKey;

        [SerializeField] private string IosAppNumberID;

        private bool _tokenSent;

#if PL_SDK_APPSFLYER_ON

        // PRIVATE
        private Action _initCallbackFunction;
        private IAnalyticsManager _analyticsManager;


        private void Awake()
        {
            ValidateSettings();
        }

        private void OnDestroy()
        {
            MaxSdkCallbacks.Rewarded.OnAdRevenuePaidEvent -= OnAdRevenuePaidEvent;
            MaxSdkCallbacks.Interstitial.OnAdRevenuePaidEvent -= OnAdRevenuePaidEvent;
            MaxSdkCallbacks.Banner.OnAdRevenuePaidEvent -= OnAdRevenuePaidEvent;
        }

        private bool ValidateSettings()
        {
            bool validated = true;

            if (string.IsNullOrEmpty(AppsflyerDevKey))
            {
                Debug.LogError("AppsFlyerAnalyticServicePlatform: AppsflyerDevKey is not setuped!", gameObject);
                validated = false;
            }

#if UNITY_EDITOR
            if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.iOS)
#else
            if (Application.platform == RuntimePlatform.IPhonePlayer)
#endif
            {
                if (string.IsNullOrEmpty(IosAppNumberID))
                {
                    Debug.LogError("AppsFlyerAnalyticServicePlatform: IosAppNumberID is not setuped!", gameObject);
                    validated = false;
                }
            }
#if STORE_GooglePlay
            if (string.IsNullOrEmpty(GooglePublicKey))
            {
                Debug.LogError(
                    "AppsFlyerAnalyticServicePlatform: GooglePublicKey is not setuped! https://support.google.com/googleplay/android-developer/answer/186113?hl=en",
                    gameObject);
                validated = false;
            }
#endif

            return validated;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public override void TryToInit()
        {
            if (IsInited)
                return;

            _analyticsManager = ServiceLocator.Get<IAnalyticsManager>();

            _analyticsManager.LogDebug("AppsFlyerAnalyticServicePlatform init");
            try
            {
                if (ValidateSettings())
                {
                    //AppsFlyer.setIsDebug(_analyticsManager.IsTesterUser());
                    AppsFlyer.setIsDebug(true);

#if PL_SDK_DELTADNA_ON
                    if (DDNA.Instance != null && !string.IsNullOrEmpty(DDNA.Instance.UserID))
                    {
                        setCustomerUserID(DDNA.Instance.UserID);
                    }
                    else
                    {
                        _analyticsManager.LogDebug(
                            "AppsFlyerAnalyticServicePlatform can't start, cause DDNA.Instance.UserID IsNullOrEmpty",
                            true);
                        Invoke("TryToInit", 1f);
                        return;
                    }
#elif PL_SDK_PLAYCUSDATALAKE_ON
                    
                    var userInfoService = ServiceLocator.Get<IUserInfoService>();

                    if (userInfoService != null)
                    {
                        setCustomerUserID(userInfoService.UserID);
                    }
                    else
                    {
                        _analyticsManager.LogDebug("AppsFlyerAnalyticServicePlatform can't start, cause IUserInfoService Is Null", true);
                        Invoke("TryToInit", 1f);
                        return;
                    }
                    // if (PDL.Instance != null && !string.IsNullOrEmpty(PDL.Instance.UserID))
                    // {
                    //     setCustomerUserID(PDL.Instance.UserID);
                    // }
                    // else
                    // {
                    //     _analyticsManager.LogDebug("AppsFlyerAnalyticServicePlatform can't start, cause PDL.Instance.UserID IsNullOrEmpty", true);
                    //     Invoke("TryToInit", 1f);
                    //     return;
                    // }
#else
                    setCustomerUserID(SystemInfo.deviceUniqueIdentifier);
#endif


#if UNITY_IOS
                    AppsFlyer.initSDK(AppsflyerDevKey, IosAppNumberID, this);
                    AppsFlyer.OnDeepLinkReceived += (_, args) => MistplayTimeTrackingAppsFlyer.OnDeepLinkValueReceived((args as DeepLinkEventsArgs).getDeepLinkValue());
#elif UNITY_ANDROID
                    AppsFlyer.initSDK(AppsflyerDevKey, Application.identifier, this);
#elif UNITY_WSA
                    AppsFlyer.initSDK(AppsflyerDevKey, UwpAppID, this);
#endif

#if PL_APPSFLYER_PURCHASE_CONNECTOR_ON && (UNITY_IOS || UNITY_ANDROID)
                    AppsFlyerPurchaseConnector.init(this, Store.GOOGLE);
#if DEVELOPMENT_BUILD 
                    AppsFlyerPurchaseConnector.setIsSandbox(true);   
 #else
                    AppsFlyerPurchaseConnector.setIsSandbox(false); 
 #endif
                    AppsFlyerPurchaseConnector.setAutoLogPurchaseRevenue(AppsFlyerAutoLogPurchaseRevenueOptions.AppsFlyerAutoLogPurchaseRevenueOptionsAutoRenewableSubscriptions, AppsFlyerAutoLogPurchaseRevenueOptions.AppsFlyerAutoLogPurchaseRevenueOptionsInAppPurchases);
                    AppsFlyerPurchaseConnector.setPurchaseRevenueValidationListeners(true);
                    AppsFlyerPurchaseConnector.build();
                    AppsFlyerPurchaseConnector.startObservingTransactions();      
                    
                   
                    _analyticsManager.CustomEvent("pl_startObservingTransactions",0);
#endif
                
                    AppsFlyer.startSDK();

                    MaxSdkCallbacks.Rewarded.OnAdRevenuePaidEvent += OnAdRevenuePaidEvent;
                    MaxSdkCallbacks.Interstitial.OnAdRevenuePaidEvent += OnAdRevenuePaidEvent;
                    MaxSdkCallbacks.Banner.OnAdRevenuePaidEvent += OnAdRevenuePaidEvent;
                    
                    // Invoke(nameof(StartAdRevenueConnectorIfNeed),1);
                    Debug.Log($"AppsFlyerAnalyticServicePlatform:: AdRevenueConnector: invoke StartAdRevenueConnectorIfNeed 1s");
                    
                    // Appsflyer uninstall tracking
// #if UNITY_IOS
//                     UnityEngine.iOS.NotificationServices.RegisterForNotifications(UnityEngine.iOS.NotificationType.Alert | UnityEngine.iOS.NotificationType.Badge | UnityEngine.iOS.NotificationType.Sound);
// #endif


                    InitComplete();

                    _analyticsManager.LogDebug("AppsFlyerAnalyticServicePlatform InitComplete  "
                                               + " AppsflyerDevKey: " + AppsflyerDevKey
                                               + " IosAppNumberID: " + IosAppNumberID
                                               + " UwpAppID: " + UwpAppID
                                               + " AppIdentifier: " + Application.identifier
                                               + " DebugLogging: " + _analyticsManager.IsTesterUser(),
                        true);
                }
                else
                {
                    Debug.LogError("AppsFlyerAnalyticServicePlatform: appsflyer ValidateSettings error", gameObject);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"AppsflyerAnalyticsServicePlatform:Init error {e.Message}");
            }
        }
        
// #if APPSFLYER_ADREVENUE_CONNECTOR
        private void OnAdRevenuePaidEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            // Dictionary<string, string> additionalParams = new Dictionary<string, string>();
            // additionalParams.Add(AFAdRevenueEvent.AD_UNIT, adInfo.AdUnitIdentifier);
            // additionalParams.Add(AFAdRevenueEvent.AD_TYPE, adInfo.AdFormat);
            // AppsFlyerAdRevenue.logAdRevenue(adInfo.NetworkName,
            //     AppsFlyerAdRevenueMediationNetworkType.AppsFlyerAdRevenueMediationNetworkTypeApplovinMax,
            //     adInfo.Revenue, "USD", additionalParams);
         
            
            Dictionary<string, string> additionalParams = new Dictionary<string, string>();
            additionalParams.Add(AdRevenueScheme.COUNTRY, MaxSdk.GetSdkConfiguration().CountryCode);
            additionalParams.Add(AdRevenueScheme.AD_UNIT, adInfo.AdUnitIdentifier);
            additionalParams.Add(AdRevenueScheme.AD_TYPE, adInfo.AdFormat);
            additionalParams.Add(AdRevenueScheme.PLACEMENT, adInfo.Placement);
            var logRevenue = new AFAdRevenueData(adInfo.NetworkName, MediationNetwork.ApplovinMax, "USD", adInfo.Revenue);
            AppsFlyer.logAdRevenue(logRevenue, additionalParams);
            
            Debug.Log($"AppsFlyerAnalyticServicePlatform:: AdRevenueConnector: OnAdRevenuePaidEvent");
        }
// #endif


#if PL_APPSFLYER_PURCHASE_CONNECTOR_ON
        public void didReceivePurchaseRevenueValidationInfo(string validationInfo)
        {
            // event (validationInfo)
            var parameters = new Dictionary<string, object>();
            parameters.Add(AnalyticsProperties.pr_content.ToString(), validationInfo);
            _analyticsManager.CustomEvent("pl_didReceivePurchaseRevenueValidationInfo",0,parameters);
            
            AppsFlyer.AFLog("didReceivePurchaseRevenueValidationInfo", validationInfo);
            // deserialize the string as a dictionnary, easy to manipulate
            Dictionary<string, object> dictionary = AFMiniJSON.Json.Deserialize(validationInfo) as Dictionary<string, object>;

            // if the platform is Android, you can create an object from the dictionnary 
// #if UNITY_ANDROID
//             if (dictionary.ContainsKey("productPurchase") && dictionary["productPurchase"] != null)
//             {
//                 // Create an object from the JSON string.
//                 InAppPurchaseValidationResult iapObject = JsonUtility.FromJson<InAppPurchaseValidationResult>(validationInfo);
//             } else if (dictionary.ContainsKey("subscriptionPurchase") && dictionary["subscriptionPurchase"] != null) {
//                 SubscriptionValidationResult iapObject = JsonUtility.FromJson<SubscriptionValidationResult>(validationInfo);
//             }
// #endif
        }
#endif

#if PKG_NOTIFICATIONS
        void Update()
        {
#if UNITY_IOS && !UNITY_EDITOR
            if (!_tokenSent)
            {
                byte[] token = UnityEngine.iOS.NotificationServices.deviceToken;
                if (token != null)
                {
                    AppsFlyer.registerUninstall(token);
                    _tokenSent = true;
                }
            }
#endif
        }
#endif

        public virtual void setCustomerUserID(string userID)
        {
            AppsFlyer.setCustomerUserId(userID);
            _analyticsManager.LogDebug($"AppsFlyerAnalyticServicePlatform::setCustomerUserID - User ID is {userID}",
                true);
        }


        public override void TrackEvent(AnalyticsEvent currentEvent)
        {
            try
            {
                //Main parameter is value (amount)
                int amount = 0;
                if (currentEvent.Parameters.ContainsKey(AnalyticsProperties.pr_amount.ToString()))
                {
                    Int32.TryParse(currentEvent.Parameters[AnalyticsProperties.pr_amount.ToString()].ToString(),
                        out amount);
                }
                currentEvent.Parameters.Add(AFInAppEvents.QUANTITY, amount);

                // Prepare revenue value
                if (currentEvent.Parameters.ContainsKey(AnalyticsProperties.pr_revenue.ToString()))
                {
                    currentEvent.Parameters[AnalyticsProperties.pr_revenue.ToString()] =
                        currentEvent.pr_revenue.Replace(",", ".");
                }

                //Convert to array appsflyer parameters
                Dictionary<string, string> eventParameters = new Dictionary<string, string>();
                int i = 0;
                foreach (KeyValuePair<string, object> kvp in currentEvent.Parameters)
                {
                    eventParameters.Add(kvp.Key, kvp.Value.ToString());
                    i++;
                }
                
                // Additional SDK predefined events block track of custom events!
                if (currentEvent.EventKey == AnalyticsEvents.pl_tutorial_completed.ToString())
                {
                    TutorialCompleted();
                }
                else if (currentEvent.EventKey == AnalyticsEvents.pl_user_level.ToString())
                {
                    UserLevelAchieved(currentEvent.pr_level);
                }
                else if (currentEvent.EventKey == AnalyticsEvents.pl_achievement.ToString())
                {
                    AchievementUnlocked(currentEvent.pr_content_id);
                }
                else if (currentEvent.EventKey == AnalyticsEvents.pl_social_signup.ToString())
                {
                    SocialSignUp();
                }
                else if (currentEvent.EventKey == AnalyticsEvents.pl_purchase_checkout.ToString())
                {
                    _analyticsManager.LogDebug($"AppsFlyerAnalyticServicePlatform: TrackPurchase : {currentEvent.EventKey}", true);
                    PurchaseInitiatedCheckout(currentEvent.pr_currency, currentEvent.pr_revenue,
                        currentEvent.pr_content_id, currentEvent.pr_placement);
                }
                else if (currentEvent.EventKey == AnalyticsEvents.pl_purchase_success.ToString())
                {
                    _analyticsManager.LogDebug($"AppsFlyerAnalyticServicePlatform: TrackPurchase : {currentEvent.EventKey}", true);
                    PurchaseSuccess(currentEvent.pr_currency, currentEvent.pr_revenue,
                        currentEvent.pr_content_id, currentEvent.pr_placement, currentEvent.pr_receipt);
                }else if (currentEvent.EventKey == AnalyticsEvents.pl_ads_revenue.ToString())
                {
                    // Block pl_ads_revenue event
                    return;
                }
                else
                {
                    Func<AnalyticsEvent, bool> handler = null;
                    HandleCustomEvent(ref handler);

                    if (handler == null || handler.Invoke(currentEvent) == false)
                    {
                        // Track custom event
                        AppsFlyer.sendEvent(currentEvent.EventKey, eventParameters);
                    }
                }
                // Logs
                _analyticsManager.LogDebug(
                    $"AppsFlyerAnalyticServicePlatform: TrackEvent : {currentEvent.EventKey}", true);
            }
            catch (Exception e)
            {
                Debug.Log("AppsFlyerAnalyticServicePlatform: Exception", gameObject);
                Debug.Log(e.ToString(), gameObject);
                throw;
            }
        }

        partial void HandleCustomEvent(ref Func<AnalyticsEvent, bool> handler);

        private void TutorialCompleted()
        {
            AppsFlyer.sendEvent(AFInAppEvents.TUTORIAL_COMPLETION, new Dictionary<string, string>
                {
                    {AFInAppEvents.SUCCESS, "true"}
                }
            );
        }


        private void UserLevelAchieved(int level)
        {
            AppsFlyer.sendEvent(AFInAppEvents.LEVEL_ACHIEVED, new Dictionary<string, string>
                {
                    {AFInAppEvents.LEVEL, level.ToString()}
                }
            );
        }


        private void AchievementUnlocked(string achievementID)
        {
            AppsFlyer.sendEvent(AFInAppEvents.ACHIEVEMENT_UNLOCKED, new Dictionary<string, string>
                {
                    {AFInAppEvents.CONTENT_ID, achievementID}
                }
            );
        }


        private void SocialSignUp()
        {
            AppsFlyer.sendEvent(AFInAppEvents.LOGIN, new Dictionary<string, string>());
        }


        private void PurchaseInitiatedCheckout(string currency, string price, string itemID, string placement)
        {
#if PL_APPSFLYER_PURCHASE_CONNECTOR_ON && (UNITY_IOS || UNITY_ANDROID)
            return;
#endif
            AppsFlyer.sendEvent(AFInAppEvents.INITIATED_CHECKOUT, new Dictionary<string, string>
                {
                    {AFInAppEvents.CONTENT_ID, itemID},
                    {AFInAppEvents.PRICE, price},
                    {AFInAppEvents.CURRENCY, currency}
                }
            );
            _analyticsManager.LogDebug($"AppsFlyerAnalyticServicePlatform: PurchaseInitiatedCheckout sent", true);
        }


        private void PurchaseSuccess(string currency, string price, string itemID, string placement, string receipt)
        {
#if PL_APPSFLYER_PURCHASE_CONNECTOR_ON && (UNITY_IOS || UNITY_ANDROID)
            return;
#endif
            
            // SDK validated event
#if STORE_Appstore

            _analyticsManager.LogDebug($"Appsflyer receipt validation IOS currency:{currency} price:{price} itemID:{itemID}");

            // Deserialize ios receipt
            JSONNode receiptParsed = JSON.Parse(receipt);
            // Send data to Appsflyer (appsflyer backend will track Purchase event if receipt will validated)
           AppsFlyer.validateAndSendInAppPurchase(
                itemID,
                price,
                currency,
                receiptParsed["TransactionID"],
                null,
                this);
            _analyticsManager.LogDebug(
                $"AppsFlyerAnalyticServicePlatform: PurchaseSuccess sent with TransactionID : {receiptParsed["TransactionID"]}"
                , true);

#elif STORE_GooglePlay
            _analyticsManager.LogDebug($"Appsflyer receipt validation ANDROID currency:{currency} price:{price} itemID:{itemID}");

            // Deserialize android receipt
            JSONNode receiptParsed = JSON.Parse(receipt);
            JSONNode payload = JSON.Parse(receiptParsed["Payload"]);
            // Send data to Appsflyer (appsflyer backend will track Purchase event if receipt will validated)
            AppsFlyer.validateAndSendInAppPurchase(
                GooglePublicKey,
                payload["signature"],
                payload["json"],
                price,
                currency,
                null,
                this);

#else
            _analyticsManager.LogDebug($"Appsflyer AFInAppEvents.PURCHASE without validation currency:{currency} price:{price} itemID:{itemID}");

            AppsFlyer.sendEvent(AFInAppEvents.PURCHASE, new Dictionary<string, string>
                {
                    {AFInAppEvents.CONTENT_ID, itemID},
                    {AFInAppEvents.REVENUE, price},
                    {AFInAppEvents.CURRENCY, currency}
                }
            );

#endif
        }

        public void didFinishValidateReceipt(string result)
        {
            AppsFlyer.AFLog("didFinishValidateReceipt", result);
            Debug.Log($"Appsflyer didFinishValidateReceipt result:{result}");
        }

        public void didFinishValidateReceiptWithError(string error)
        {
            AppsFlyer.AFLog("didFinishValidateReceiptWithError", error);
            Debug.LogError($"Appsflyer didFinishValidateReceiptWithError result:{error}");
        }


        public void onConversionDataSuccess(string conversionData)
        {
            AppsFlyer.AFLog("onConversionDataSuccess", conversionData);
            Debug.Log($"AppsFlyerAnalyticServicePlatform: onConversionDataSuccess = {conversionData}");
            Dictionary<string, object> conversionDataDictionary = AppsFlyer.CallbackStringToDictionary(conversionData);
            // add deferred deeplink logic here
        }

        public void onConversionDataFail(string error)
        {
            AppsFlyer.AFLog("onConversionDataFail", error);
        }

        public void onAppOpenAttribution(string attributionData)
        {
            AppsFlyer.AFLog("onAppOpenAttribution", attributionData);
            Debug.Log($"AppsFlyerAnalyticServicePlatform: onAppOpenAttribution = {attributionData}");
            Dictionary<string, object> attributionDataDictionary =
                AppsFlyer.CallbackStringToDictionary(attributionData);
            // add direct deeplink logic here
        }

        public void onAppOpenAttributionFailure(string error)
        {
            AppsFlyer.AFLog("onAppOpenAttributionFailure", error);
        }
        
        /// <summary>
        /// Used to accept deeplink callback from UnitySendMessage on native side.
        /// </summary>
        public void onDeepLinking(string response)
        {
            Debug.Log($"AppsFlyerAnalyticServicePlatform: onDeepLinking = {response}");
        }
#else
        public override void TryToInit()
        {
            Debug.LogError("Insert SDK_APPSFLYER scriptable symbol in project settings");
        }

        public override void TrackEvent(AnalyticsEvent currentEvent){}

        [HelpBox(@"Insert SDK_APPSFLYER scriptable symbol in project settings", HelpBoxMessageType.Warning)]
#endif
        [SerializeField] private bool _iSeeThisAlert;

// #if APPSFLYER_ADREVENUE_CONNECTOR           
//         private void AdRevenue(AnalyticsEvent currentEvent)
//         {
//             var monetizationNetwork = ServiceLocator.Get<IAdsManager>().AdsPlatformName;
//             var mediationNetworkName = GetNetworkType(currentEvent.pr_ad_network);
//
//             double eventRevenue = 0;
//             double.TryParse(currentEvent.pr_revenue,out eventRevenue);
//
//             var revenueCurrency = currentEvent.pr_currency;
//
//             var dic = new Dictionary<string, string>();
//             dic.Add("pr_ad_network",currentEvent.pr_ad_network);
//             dic.Add(AFAdRevenueEvent.AD_TYPE, currentEvent.pr_ad_format);
//             AppsFlyerAdRevenue.logAdRevenue(monetizationNetwork, mediationNetworkName, eventRevenue, revenueCurrency, dic);
//         }
//
//         private AppsFlyerAdRevenueMediationNetworkType GetNetworkType(string newtwork)
//         {
//             newtwork = newtwork.ToLower().Replace(" ","");
//             switch (newtwork)
//             {
//                 case "googleadmob":
//                     return AppsFlyerAdRevenueMediationNetworkType.AppsFlyerAdRevenueMediationNetworkTypeGoogleAdMob;
//                 case "ironsource":
//                     return AppsFlyerAdRevenueMediationNetworkType.AppsFlyerAdRevenueMediationNetworkTypeIronSource;
//                 case "applovin":
//                     return AppsFlyerAdRevenueMediationNetworkType.AppsFlyerAdRevenueMediationNetworkTypeApplovinMax;
//                 case "fyber":
//                     return AppsFlyerAdRevenueMediationNetworkType.AppsFlyerAdRevenueMediationNetworkTypeFyber;
//                 case "appodeal":
//                     return AppsFlyerAdRevenueMediationNetworkType.AppsFlyerAdRevenueMediationNetworkTypeAppodeal;
//                 case "admost":
//                     return AppsFlyerAdRevenueMediationNetworkType.AppsFlyerAdRevenueMediationNetworkTypeAdmost;
//                 case "topon":
//                     return AppsFlyerAdRevenueMediationNetworkType.AppsFlyerAdRevenueMediationNetworkTypeTopon;
//                 case "tradplus":
//                     return AppsFlyerAdRevenueMediationNetworkType.AppsFlyerAdRevenueMediationNetworkTypeTradplus;
//                 case "yandex":
//                     return AppsFlyerAdRevenueMediationNetworkType.AppsFlyerAdRevenueMediationNetworkTypeYandex;
//                 case "chartboost":
//                     return AppsFlyerAdRevenueMediationNetworkType.AppsFlyerAdRevenueMediationNetworkTypeChartBoost;
//                 case "unityads":
//                     return AppsFlyerAdRevenueMediationNetworkType.AppsFlyerAdRevenueMediationNetworkTypeUnity;
//                 default:
//                     return AppsFlyerAdRevenueMediationNetworkType.AppsFlyerAdRevenueMediationNetworkTypeCustomMediation;
//             }
//         }
// #endif
    }
}