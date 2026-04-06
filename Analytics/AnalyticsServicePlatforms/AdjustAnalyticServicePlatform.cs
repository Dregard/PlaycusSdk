using System;
using System.Collections.Generic;
using System.Globalization;
#if GDPR
using Playcus.GDPR;
#endif
using UnityEngine;
using Playcus.Utils;
#if UNITY_EDITOR
using UnityEditor;
#endif
#if PL_SDK_ADJUST_ON
using com.adjust.sdk;
using com.adjust.sdk.purchase;
#endif
#if PL_SDK_PLAYCUSDATALAKE_ON
using PlaycusDL;
#endif

namespace Playcus.Analytics
{
    /// <summary>
    /// For AnalyticsService used only! Don't use directly! (Simonenko Alexey)
    /// Adjust Analytics https://github.com/adjust/unity_sdk
    /// All playcus events list https://docs.google.com/spreadsheets/d/1JjSoB1pyAnKDIZ4Ir3PsFESau0e4rwD1WF_-PFT0bTg
    /// </summary>
    [ExecuteAlways]
    public class AdjustAnalyticServicePlatform : AnalyticsServicePlatform
    {
        public override bool CanTesterBeTracked()
        {
            return false;
        }

        // CONFIG
        [Header("Settings")] [SerializeField] protected string _appToken;
        [SerializeField] private List<BaseEventToken> _baseEventsTokens;
        [SerializeField] private List<CustomEventToken> _customEventsTokens;

        [Serializable]
        private class BaseEventToken
        {
            public AnalyticsEvents EventKey;
            public string EventToken;
        }

        [Serializable]
        private class CustomEventToken
        {
            public string EventKey;
            public string EventToken;
        }

        private bool _tokenSent;

#if PL_SDK_ADJUST_ON

        // PRIVATE
        private Action _initCallbackFunction;
        private IAnalyticsManager _analyticsManager;

        private AdjustEvent _purchaseAdjustEvent;

        private void Awake()
        {
            ValidateSettings();
        }

        private bool ValidateSettings()
        {
            bool validated = true;

            if (string.IsNullOrEmpty(_appToken))
            {
                Debug.LogError("AdjustAnalyticServicePlatform: AppToken is not setuped!", gameObject);
                validated = false;
            }

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

            _analyticsManager.LogDebug("AdjustAnalyticServicePlatform init");
            try
            {
                if (ValidateSettings())
                {
#if GDPR
                    // GDPR flow
                    AdjustThirdPartySharing adjustThirdPartySharing =
                        new AdjustThirdPartySharing(ServiceLocator.Get<GDPRService>().IsGDPRAccepted());
                    Adjust.trackThirdPartySharing(adjustThirdPartySharing);
#endif

                    // Attribution solution
                    AdjustEnvironment environment = _analyticsManager.IsTesterUser()
                        ? AdjustEnvironment.Sandbox
                        : AdjustEnvironment.Production;
                    AdjustConfig config = new AdjustConfig(_appToken, environment, true);
                    config.setLogLevel(environment == AdjustEnvironment.Sandbox
                        ? AdjustLogLevel.Debug
                        : AdjustLogLevel.Error);
                    //config.setSendInBackground(this.sendInBackground);
                    //config.setEventBufferingEnabled(this.eventBuffering);
                    //config.setLaunchDeferredDeeplink(this.launchDeferredDeeplink);
                    Adjust.start(config);

                    // Purchase validation solution
                    ADJPConfig configValidation = new ADJPConfig(_appToken, _analyticsManager.IsTesterUser()
                        ? ADJPEnvironment.Sandbox
                        : ADJPEnvironment.Production);
                    configValidation.SetLogLevel(ADJPLogLevel.Error);
                    AdjustPurchase.Init(configValidation);

                    // Custom uid
#if PL_SDK_PLAYCUSDATALAKE_ON
                    if (PDL.Instance != null && !string.IsNullOrEmpty(PDL.Instance.UserID))
                    {
                        setCustomerUserID(PDL.Instance.UserID);
                    }
                    else
                    {
                        _analyticsManager.LogDebug("AdjustAnalyticServicePlatform can't start, cause PDL.Instance.UserID IsNullOrEmpty", true);
                        Invoke("TryToInit", 1f);
                        return;
                    }
#else
                    setCustomerUserID(SystemInfo.deviceUniqueIdentifier);
#endif

                    // Complete
                    InitComplete();
                    _analyticsManager.LogDebug("AdjustAnalyticServicePlatform InitComplete  "
                                               + " AppToken: " + _appToken
                                               + " DebugLogging: " + _analyticsManager.IsTesterUser(),
                        true);
                }
                else
                {
                    Debug.LogError("AdjustAnalyticServicePlatform: ValidateSettings error", gameObject);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"AdjustAnalyticServicePlatform: Init error {e.Message}");
            }
        }

        public virtual void setCustomerUserID(string userID)
        {
            Adjust.addSessionCallbackParameter(AnalyticsProperties.pr_user_id.ToString(), userID);
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

                //Convert to dictionary of string parameters
                Dictionary<string, string> eventParameters = new Dictionary<string, string>();
                int i = 0;
                foreach (KeyValuePair<string, object> kvp in currentEvent.Parameters)
                {
                    eventParameters.Add(kvp.Key, kvp.Value.ToString());
                    i++;
                }

                // Additional SDK predefined events block track of custom events!
                if (currentEvent.EventKey == AnalyticsEvents.pl_purchase_success.ToString())
                {
                    PurchaseSuccess(currentEvent.pr_currency, currentEvent.pr_revenue,
                        currentEvent.pr_content_id, currentEvent.pr_placement, currentEvent.pr_receipt);
                }
                else
                {
                    // Track custom event
                    _analyticsManager.LogDebug(
                        $"AdjustAnalyticServicePlatform: TrackEvent : {currentEvent.EventKey}", true);

                    AdjustEvent adjustEvent = new AdjustEvent(GetEventToken(currentEvent.EventKey));
                    foreach (var parameterPair in eventParameters)
                    {
                        adjustEvent.addCallbackParameter(parameterPair.Key, parameterPair.Value);
                    }

                    Adjust.trackEvent(adjustEvent);
                }
            }
            catch (Exception e)
            {
                Debug.Log("AdjustAnalyticServicePlatform: Exception", gameObject);
                Debug.Log(e.ToString(), gameObject);
                throw;
            }
        }

        private string GetEventToken(string eventKey)
        {
            foreach (BaseEventToken eventToken in _baseEventsTokens)
            {
                if (eventToken.EventKey.ToString() == eventKey)
                {
                    return eventToken.EventToken;
                }
            }

            foreach (CustomEventToken eventToken in _customEventsTokens)
            {
                if (eventToken.EventKey == eventKey)
                {
                    return eventToken.EventToken;
                }
            }

            Debug.LogError(
                $"AdjustAnalyticServicePlatform : you not setup event tokens maps in Adjust config prefab for event = {eventKey}");
            return "";
        }


        private void PurchaseSuccess(string currency, string price, string itemID, string placement, string receipt)
        {
            _purchaseAdjustEvent = new AdjustEvent(GetEventToken(AnalyticsEvents.pl_purchase_success.ToString()));
            _purchaseAdjustEvent.addPartnerParameter(AnalyticsProperties.pr_content_id.ToString(), itemID);
            _purchaseAdjustEvent.setRevenue(Convert.ToDouble(price), currency);

            // TODO Android 3.0 Validation

#if STORE_Appstore
            _analyticsManager.LogDebug($"AdjustAnalyticServicePlatform receipt validation IOS");
            // Deserialize ios receipt
            Playcus.Utils.JSONNode receiptParsed = Playcus.Utils.JSON.Parse(receipt);
            // Purchase verification request on iOS.
            AdjustPurchase.VerifyPurchaseiOS(receipt, receiptParsed["TransactionID"], itemID, VerificationInfoDelegate);

#elif STORE_GooglePlay
            _analyticsManager.LogDebug($"AdjustAnalyticServicePlatform receipt validation ANDROID");

           // Deserialize android receipt
            Playcus.Utils.JSONNode receiptParsed = Playcus.Utils.JSON.Parse(receipt);
            Playcus.Utils.JSONNode payload = Playcus.Utils.JSON.Parse(receiptParsed["Payload"]);

            // Purchase verification request on Android.
            //AdjustPurchase.VerifyPurchaseAndroid(itemID, "{ItemToken}", receiptParsed["Payload"], VerificationInfoDelegate);
            // TODO Android 3.0 Validation          
            Adjust.trackEvent(_purchaseAdjustEvent);
#else
            _analyticsManager.LogDebug($"AdjustAnalyticServicePlatform : AFInAppEvents.PURCHASE without validation");
            Adjust.trackEvent(_purchaseAdjustEvent);
#endif
        }


        private void VerificationInfoDelegate(ADJPVerificationInfo verificationInfo)
        {
            Debug.Log("Verification info arrived to unity callback!");
            Debug.Log("Message: " + verificationInfo.Message);
            Debug.Log("Status code: " + verificationInfo.StatusCode);
            Debug.Log("Verification state: " + verificationInfo.VerificationState);

            if (verificationInfo.VerificationState == ADJPVerificationState.ADJPVerificationStatePassed)
            {
                Adjust.trackEvent(_purchaseAdjustEvent);
            }
            else
            {
                Debug.LogWarning("AdjustAnalyticServicePlatform : purchase can't be validated");
            }
        }


#else
        
        public override void TryToInit()
        {
            Debug.LogError("Insert PL_SDK_ADJUST_ON scriptable symbol in project settings");
        }

        public override void TrackEvent(AnalyticsEvent currentEvent)
        {
        }

        [HelpBox(@"Insert PL_SDK_ADJUST_ON scriptable symbol in project settings", HelpBoxMessageType.Warning)]
#endif
        [SerializeField] private bool _iSeeThisAlert;
    }
}