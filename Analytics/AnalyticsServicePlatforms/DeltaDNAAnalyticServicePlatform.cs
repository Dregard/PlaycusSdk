using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using Playcus.Utils;
#if PL_SDK_DELTADNA_ON
using DeltaDNA;
#endif
#if UNITY_EDITOR
using UnityEditor;

#endif

namespace Playcus.Analytics
{
    /// <summary>
    /// For AnalyticsService used only! Don't use directly! (Simonenko Alexey)
    /// DeltaDNA Analytics https://docs.deltadna.com/advanced-integration/unity-sdk/
    /// All playcus events list https://docs.google.com/spreadsheets/d/1JjSoB1pyAnKDIZ4Ir3PsFESau0e4rwD1WF_-PFT0bTg
    /// </summary>
    [ExecuteAlways]
    public class DeltaDNAAnalyticServicePlatform : AnalyticsServicePlatform
    {
        public override bool CanTesterBeTracked()
        {
            return true;
        }

        [Serializable]
        public sealed class Configuration
        {
            public string environmentKeyDev;
            public string environmentKeyLive;
            public string collectUrl;
            public string engageUrl;
            public string hashSecret;
        }

        // CONFIG
        [Header("Settings")] [SerializeField] private DeltaDNAAnalyticServicePlatform.Configuration configuration;


#if PL_SDK_DELTADNA_ON


        // PRIVATE
        private Action initCallbackFunction;
        private IAnalyticsManager _analyticsManager;

        private void Awake()
        {
            ValidateSettings();
        }

        private bool ValidateSettings()
        {
            bool validated = true;

            if (string.IsNullOrEmpty(configuration.collectUrl))
            {
                Debug.LogError("DeltaDNAAnalyticServicePlatform: collectUrl is not setuped!", gameObject);
                validated = false;
            }

            if (string.IsNullOrEmpty(configuration.engageUrl))
            {
                Debug.LogError("DeltaDNAAnalyticServicePlatform: engageUrl is not setuped!", gameObject);
                validated = false;
            }

            if (string.IsNullOrEmpty(configuration.environmentKeyDev))
            {
                Debug.LogError("DeltaDNAAnalyticServicePlatform: environmentKeyDev is not setuped!", gameObject);
                validated = false;
            }

            if (string.IsNullOrEmpty(configuration.environmentKeyLive))
            {
                Debug.LogError("DeltaDNAAnalyticServicePlatform: environmentKeyLive is not setuped!", gameObject);
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

            _analyticsManager.LogDebug("DeltaDNAAnalyticServicePlatform init", true);
            try
            {
                if (ValidateSettings())
                {
#if UNITY_EDITOR_WIN
                    DDNA.Instance.Platform = DeltaDNA.Platform.PC_CLIENT;
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
                    DDNA.Instance.Platform = DeltaDNA.Platform.MAC_CLIENT;
#elif UNITY_WSA
                    DDNA.Instance.Platform = DeltaDNA.Platform.WINDOWS_MOBILE;
#elif STORE_Amazon
                    DDNA.Instance.Platform = DeltaDNA.Platform.AMAZON;
#endif
                    DeltaDNA.Configuration sdkConfiguration = new DeltaDNA.Configuration();
                    sdkConfiguration.environmentKey = _analyticsManager.IsTesterUser() ? 0 : 1;
                    sdkConfiguration.useApplicationVersion = true;
                    sdkConfiguration.collectUrl = configuration.collectUrl;
                    sdkConfiguration.engageUrl = configuration.engageUrl;
                    sdkConfiguration.environmentKeyDev = configuration.environmentKeyDev;
                    sdkConfiguration.environmentKeyLive = configuration.environmentKeyLive;
                    sdkConfiguration.hashSecret = configuration.hashSecret;
                    DDNA.Instance.StartSDK(sdkConfiguration);

                    InitComplete();
                    _analyticsManager.LogDebug(
                        $"DeltaDNAAnalyticServicePlatform InitComplete. userID:{DDNA.Instance.UserID}, sessionID:{DDNA.Instance.SessionID}",
                        true);
                }
                else
                {
                    Debug.LogError("DeltaDNAAnalyticServicePlatform: ValidateSettings error", gameObject);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"DeltaDNAAnalyticsServicePlatform:Init error {e.Message}");
            }
        }

        public override void TrackEvent(AnalyticsEvent currentEvent)
        {
            try
            {
                //Main parameter is amount
                int amount = 0;
                if (currentEvent.Parameters.ContainsKey(AnalyticsProperties.pr_amount.ToString()))
                {
                    Int32.TryParse(currentEvent.Parameters[AnalyticsProperties.pr_amount.ToString()].ToString(), out amount);
                    currentEvent.Parameters.Remove(AnalyticsProperties.pr_amount.ToString());
                }

                currentEvent.Parameters.Add("amount", amount);

                // Purchase is special event
                if (currentEvent.EventKey == AnalyticsEvents.pl_purchase_success.ToString())
                {
                    PurchaseSuccess(
                        currentEvent.pr_currency,
                        currentEvent.pr_revenue,
                        currentEvent.pr_content_id,
                        currentEvent.pr_content_type,
                        currentEvent.pr_placement,
                        currentEvent.pr_receipt
                    );
                }
                else
                {
                    //Convert to deltadna event 
                    var gameEvent = new GameEvent(currentEvent.EventKey);
                    Dictionary<string, string> eventParameters = new Dictionary<string, string>();
                    int i = 0;
                    foreach (KeyValuePair<string, object> kvp in currentEvent.Parameters)
                    {
                        gameEvent.AddParam(kvp.Key, kvp.Value);
                        i++;
                    }

                    DDNA.Instance.RecordEvent(gameEvent).Run();
                }
            }
            catch (Exception e)
            {
                Debug.Log("DeltaDNAAnalyticServicePlatform: Exception", gameObject);
                Debug.Log(e.ToString(), gameObject);
                throw;
            }
        }


        private void PurchaseSuccess(string currency, string price, string itemID, string itemType, string placement,
            string receipt)
        {
            // SDK validated events
#if UNITY_IOS && !UNITY_EDITOR
            /*
            IOS (Apple App Store)
            To validate in-app purchases made through the Apple App Store the following parameters should be added to the transaction event:

            transactionServer - the server for which the receipt should be validated against, in this case 'APPLE'
            transactionReceipt - the purchase data as a string not as nested JSON
            transactionID - the ID of the in-app purchase e.g 100000576198248
            */

            // Deserialize ios receipt
            JSONNode receiptParsed = JSON.Parse(receipt);
            // Prepare event
            var gameEvent = new Transaction(
                itemID,
                "PURCHASE",
                new Product()
                    .AddItem(itemID, string.IsNullOrEmpty(itemType) ? "item" : itemType, 1),
                new Product()
                    .SetRealCurrency(currency, Product.ConvertCurrency(currency, Convert.ToDecimal(float.Parse(price, System.Globalization.CultureInfo.InvariantCulture)))))
            .SetServer("APPLE")
            .SetReceipt(receipt)
            .SetTransactionId(receiptParsed["TransactionID"])
            .SetProductId(itemID);
#elif UNITY_ANDROID && STORE_GooglePlay && !UNITY_EDITOR
            /*
             Android (Google Play Store)
             To validate in-app purchases made through the Google Play Store the following parameters should be added to the transaction event:

             transactionServer - the server for which the receipt should be validated against, in this case 'GOOGLE'
             transactionReceipt - the purchase data as a string
             transactionReceiptSignature - the in-app data signature
             iOS (Apple App Store)
             */

            // Deserialize android receipt
            JSONNode receiptParsed = JSON.Parse(receipt);
            JSONNode payload = JSON.Parse(receiptParsed["Payload"]);
            // Prepare event
            var gameEvent = new Transaction(
                itemID,
                "PURCHASE",
                new Product()
                    .AddItem(itemID, string.IsNullOrEmpty(itemType) ? "item" : itemType, 1),
                new Product()
                    .SetRealCurrency(currency, Product.ConvertCurrency(currency, Convert.ToDecimal(float.Parse(price, System.Globalization.CultureInfo.InvariantCulture)))))
            .SetServer("GOOGLE")
            .SetReceipt(receipt)
            .SetReceiptSignature(payload["signature"])
            .SetProductId(itemID);
#else

            // Prepare event without validation 
            var gameEvent = new Transaction(
                        itemID,
                        "PURCHASE",
                        new Product()
                            .AddItem(itemID, string.IsNullOrEmpty(itemType) ? "item" : itemType, 1),
                        new Product()
                            .SetRealCurrency(currency,
                                Product.ConvertCurrency(currency,
                                    Convert.ToDecimal(float.Parse(price,
                                        System.Globalization.CultureInfo.InvariantCulture)))))
                    .SetProductId(itemID)
                ;

#endif

            DDNA.Instance.RecordEvent(gameEvent).Run();
        }


#else
        public override void TryToInit()
        {
            Debug.LogError("Insert PL_SDK_DELTADNA_ON scriptable symbol in project settings");
        }
        
        public override void TrackEvent(AnalyticsEvent currentEvent){}

        [HelpBox(@"Insert PL_SDK_DELTADNA_ON scriptable symbol in project settings", HelpBoxMessageType.Warning)]
#endif
        [SerializeField] private bool _iSeeThisAlert;
    }
}