using System;
using System.Collections.Generic;
using UnityEngine;
using Playcus.Utils;
#if PL_SDK_D2D_ON && !UNITY_EDITOR
using DevToDev.Analytics;
#endif

namespace Playcus.Analytics
{
    /// <summary>
    /// For AnalyticsService used only! Don't use directly!
    /// DevToDev Analytics https://docs.devtodev.com/integration/integration-of-sdk-v2/sdk-integration/unity
    /// All playcus events list https://docs.google.com/spreadsheets/d/1JjSoB1pyAnKDIZ4Ir3PsFESau0e4rwD1WF_-PFT0bTg
    /// </summary>
    public class D2DAnalyticsPlatform : AnalyticsServicePlatform
    {
        [Header("D2D App Keys")]
        public string androidAppID;
        public string iOSAppID;

        public override bool CanTesterBeTracked()
        {
            return false;
        }

#if PL_SDK_D2D_ON && !UNITY_EDITOR

        private IAnalyticsManager _analyticsManager;

        public override void TryToInit()
        {
            if (IsInited)
            {
                return;
            }

            _analyticsManager = ServiceLocator.Get<IAnalyticsManager>();
            _analyticsManager.LogDebug("D2DAnalyticsPlatform init");
            Debug.Log($"{nameof(D2DAnalyticsPlatform)}: TryToInit called.");

            try
            {
                var config = new DTDAnalyticsConfiguration();
                config.LogLevel = DTDLogLevel.Error;
                config.ApplicationVersion = Application.version;

                DTDAnalytics.SetInitializationCompleteCallback(() =>
                {
                    Debug.Log("D2DAnalyticsPlatform InitializationCompleteCallback");
                });

#if UNITY_ANDROID
                if (!string.IsNullOrEmpty(androidAppID))
                {
                    DTDAnalytics.Initialize(androidAppID);
                }
#elif UNITY_IOS
                if (!string.IsNullOrEmpty(iOSAppID))
                {
                    DTDAnalytics.Initialize(iOSAppID);
                }
#endif

                InitComplete();
                _analyticsManager.LogDebug("D2DAnalyticsPlatform InitCompleted");
            }
            catch (Exception e)
            {
                Debug.LogError($"D2DAnalyticsPlatform:Init error {e.Message}");
            }
        }

        private string PrepareEventName(string eventName)
        {
            eventName = eventName.Replace(' ', '_');
            eventName = eventName.Replace('.', '_');
            return eventName;
        }

        private string PrepareAdsPlatformName(string platformName)
        {
            return platformName.ToLower();
        }

        public override void TrackEvent(AnalyticsEvent currentEvent)
        {
            var preparedEventKey = PrepareEventName(currentEvent.EventKey);
            var amount = 0;
            if (currentEvent.Parameters.ContainsKey(AnalyticsProperties.pr_amount.ToString()))
            {
                int.TryParse(currentEvent.Parameters[AnalyticsProperties.pr_amount.ToString()].ToString(), out amount);
            }

            // PREDEFINED EVENTS
            if (currentEvent.EventKey == AnalyticsEvents.pl_user_level.ToString())
            {
                DTDAnalytics.LevelUp(currentEvent.pr_level);
            }
            else if (currentEvent.EventKey == AnalyticsEvents.pl_level_opened.ToString())
            {
                var param = new DTDCustomEventParameters();
                param.Add("level_info", $"{PrepareEventName(currentEvent.pr_level_type)}:{currentEvent.pr_level}");
                DTDAnalytics.CustomEvent(preparedEventKey, param);
            }
            else if (currentEvent.EventKey == AnalyticsEvents.pl_level_started.ToString())
            {
                var paramProgr = new DTDStartProgressionEventParameters();
                paramProgr.Source = $"{{pr_level_type:{currentEvent.pr_level_type},pr_level:{currentEvent.pr_level}}}";
                DTDAnalytics.StartProgressionEvent(preparedEventKey, paramProgr);
            }
            else if (currentEvent.EventKey == AnalyticsEvents.pl_level_failed.ToString())
            {
                var paramProgr = new DTDFinishProgressionEventParameters();
                paramProgr.SuccessfulCompletion = false;
                paramProgr.Duration = 0;
                DTDAnalytics.FinishProgressionEvent(PrepareEventName(AnalyticsEvents.pl_level_started.ToString()), paramProgr);
            }
            else if (currentEvent.EventKey == AnalyticsEvents.pl_level_completed.ToString())
            {
                var paramProgr = new DTDFinishProgressionEventParameters();
                paramProgr.SuccessfulCompletion = true;
                paramProgr.Duration = 0;
                DTDAnalytics.FinishProgressionEvent(PrepareEventName(AnalyticsEvents.pl_level_started.ToString()), paramProgr);
            }
            else if (currentEvent.EventKey == AnalyticsEvents.pl_new_score.ToString())
            {
                var param = new DTDCustomEventParameters();
                param.Add("score", currentEvent.pr_level_score);
                param.Add("level_info", $"{PrepareEventName(currentEvent.pr_level_type)}:{currentEvent.pr_level}");
                DTDAnalytics.CustomEvent(preparedEventKey, param);
            }
            else if (currentEvent.EventKey == AnalyticsEvents.pl_achievement.ToString())
            {
                var param = new DTDCustomEventParameters();
                param.Add("content_id", currentEvent.pr_content_id);
                DTDAnalytics.CustomEvent(preparedEventKey, param);
            }
            else if (currentEvent.EventKey == AnalyticsEvents.pl_purchase_checkout.ToString())
            {
                var param = new DTDCustomEventParameters();
                param.Add("content_id", currentEvent.pr_content_id);
                param.Add("placement", currentEvent.pr_placement);
                DTDAnalytics.CustomEvent(preparedEventKey, param);
            }
            else if (currentEvent.EventKey == AnalyticsEvents.pl_purchase_success.ToString())
            {
                PurchaseSuccess(currentEvent.pr_currency, currentEvent.pr_revenue,
                    currentEvent.pr_content_id, currentEvent.pr_placement, currentEvent.pr_receipt);
            }
            else if (currentEvent.EventKey == AnalyticsEvents.pl_purchase_error.ToString())
            {
                var param = new DTDCustomEventParameters();
                param.Add("content_id", currentEvent.pr_content_id);
                param.Add("reason", currentEvent.pr_reason);
                DTDAnalytics.CustomEvent(preparedEventKey, param);
            }
            else if (currentEvent.EventKey == AnalyticsEvents.pl_purchase_canceled.ToString())
            {
                var param = new DTDCustomEventParameters();
                param.Add("content_id", currentEvent.pr_content_id);
                param.Add("placement", currentEvent.pr_placement);
                DTDAnalytics.CustomEvent(preparedEventKey, param);
            }
            else if (currentEvent.EventKey == AnalyticsEvents.pl_resources_add.ToString())
            {
                DTDAnalytics.CurrencyAccrual(currentEvent.pr_currency, amount,
                    currentEvent.pr_content_id ?? "unknown", DTDAccrualType.Earned);
            }
            else if (currentEvent.EventKey == AnalyticsEvents.pl_resources_remove.ToString())
            {
                DTDAnalytics.VirtualCurrencyPayment(
                    currentEvent.pr_content_id ?? "purchase",
                    currentEvent.pr_content_type ?? "item",
                    1, amount, currentEvent.pr_currency);
            }
            else if (currentEvent.EventKey == AnalyticsEvents.pl_share_checkout.ToString() ||
                     currentEvent.EventKey == AnalyticsEvents.pl_share_success.ToString() ||
                     currentEvent.EventKey == AnalyticsEvents.pl_request_checkout.ToString() ||
                     currentEvent.EventKey == AnalyticsEvents.pl_request_success.ToString())
            {
                var param = new DTDCustomEventParameters();
                param.Add("content_id", currentEvent.pr_content_id);
                param.Add("placement", currentEvent.pr_placement);
                DTDAnalytics.CustomEvent(preparedEventKey, param);
            }
            else if (currentEvent.EventKey == AnalyticsEvents.pl_ads_rewarded_button_click.ToString())
            {
                var param = new DTDCustomEventParameters();
                param.Add("placement", currentEvent.pr_placement);
                param.Add("ad_network", PrepareAdsPlatformName(currentEvent.pr_ad_network));
                DTDAnalytics.CustomEvent(preparedEventKey, param);
            }
            else if (currentEvent.EventKey == AnalyticsEvents.pl_ads_rewarded_showed.ToString())
            {
                var param = new DTDCustomEventParameters();
                param.Add("placement", currentEvent.pr_placement);
                param.Add("ad_network", PrepareAdsPlatformName(currentEvent.pr_ad_network));
                DTDAnalytics.CustomEvent(preparedEventKey, param);
            }
            else if (currentEvent.EventKey == AnalyticsEvents.pl_ads_rewarded_complete.ToString())
            {
                var param = new DTDCustomEventParameters();
                param.Add("placement", currentEvent.pr_placement);
                param.Add("ad_network", PrepareAdsPlatformName(currentEvent.pr_ad_network));
                DTDAnalytics.CustomEvent(preparedEventKey, param);
            }
            else if (currentEvent.EventKey == AnalyticsEvents.pl_ads_rewarded_canceled.ToString())
            {
                var param = new DTDCustomEventParameters();
                param.Add("placement", currentEvent.pr_placement);
                param.Add("ad_network", PrepareAdsPlatformName(currentEvent.pr_ad_network));
                DTDAnalytics.CustomEvent(preparedEventKey, param);
            }
            else if (currentEvent.EventKey == AnalyticsEvents.pl_ads_insterstitial_showed.ToString())
            {
                var param = new DTDCustomEventParameters();
                param.Add("placement", currentEvent.pr_placement);
                param.Add("ad_network", PrepareAdsPlatformName(currentEvent.pr_ad_network));
                DTDAnalytics.CustomEvent(preparedEventKey, param);
            }
            else if (currentEvent.EventKey == AnalyticsEvents.pl_ads_insterstitial_closed.ToString())
            {
                var param = new DTDCustomEventParameters();
                param.Add("placement", currentEvent.pr_placement);
                param.Add("ad_network", PrepareAdsPlatformName(currentEvent.pr_ad_network));
                DTDAnalytics.CustomEvent(preparedEventKey, param);
            }
            else if (currentEvent.EventKey == AnalyticsEvents.pl_ads_banner_showed.ToString())
            {
                var param = new DTDCustomEventParameters();
                param.Add("placement", currentEvent.pr_placement);
                param.Add("ad_network", PrepareAdsPlatformName(currentEvent.pr_ad_network));
                DTDAnalytics.CustomEvent(preparedEventKey, param);
            }
            else if (currentEvent.EventKey == AnalyticsEvents.pl_loading_start.ToString())
            {
                var paramProgr = new DTDStartProgressionEventParameters();
                DTDAnalytics.StartProgressionEvent("Loading", paramProgr);
            }
            else if (currentEvent.EventKey == AnalyticsEvents.pl_loading_step.ToString())
            {
                var param = new DTDCustomEventParameters();
                param.Add("step", currentEvent.pr_amount);
                DTDAnalytics.CustomEvent(preparedEventKey, param);
            }
            else if (currentEvent.EventKey == AnalyticsEvents.pl_loading_end.ToString())
            {
                var paramProgr = new DTDFinishProgressionEventParameters();
                paramProgr.SuccessfulCompletion = true;
                DTDAnalytics.FinishProgressionEvent("Loading", paramProgr);
            }
            else if (currentEvent.EventKey == AnalyticsEvents.pl_notification_permission_show.ToString())
            {
                DTDAnalytics.CustomEvent("NotificationPermissionShow");
            }
            else if (currentEvent.EventKey == AnalyticsEvents.pl_notification_permission_denied.ToString())
            {
                DTDAnalytics.CustomEvent("NotificationPermissionDenied");
            }
            else if (currentEvent.EventKey == AnalyticsEvents.pl_notification_permission_granted.ToString())
            {
                DTDAnalytics.CustomEvent("NotificationPermissionGranted");
            }
            else
            // CUSTOM EVENTS
            {
                var param = new DTDCustomEventParameters();
                if (amount > 0)
                {
                    param.Add("amount", amount);
                }
                DTDAnalytics.CustomEvent(preparedEventKey, param);
            }
        }

        private void PurchaseSuccess(string currency, string price, string itemID, string placement, string receipt)
        {
            // Convert price to double
            double priceDouble = 0;
            if (!string.IsNullOrEmpty(price))
            {
                double.TryParse(price, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out priceDouble);
            }

            // Extract transaction ID from receipt
            string transactionId = itemID;
            try
            {
                if (!string.IsNullOrEmpty(receipt))
                {
#if !UNITY_WSA
                    JSONNode receiptParsed = JSON.Parse(receipt);
                    if (receiptParsed != null && receiptParsed["TransactionID"] != null)
                    {
                        transactionId = receiptParsed["TransactionID"];
                    }
#endif
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"D2DAnalyticsPlatform: Failed to parse receipt: {e.Message}");
            }

            // Track purchase
            DTDAnalytics.RealCurrencyPayment(transactionId, priceDouble, itemID, currency);

            // Also track as custom event
            var param = new DTDCustomEventParameters();
            param.Add("content_id", itemID);
            param.Add("placement", placement);
            param.Add("revenue", price);
            DTDAnalytics.CustomEvent("pl_purchase_success", param);
        }

#elif PL_SDK_D2D_ON && UNITY_EDITOR
        public override void TryToInit()
        {
            Debug.Log("D2DAnalyticsPlatform: Skipped in Editor (sqlite3 not available on Windows)");
            InitComplete();
        }

        public override void TrackEvent(AnalyticsEvent currentEvent) { }
#else
        public override void TryToInit()
        {
            Debug.LogError("D2DAnalyticsPlatform: Insert PL_SDK_D2D_ON scriptable symbol in project settings", gameObject);
        }

        public override void TrackEvent(AnalyticsEvent currentEvent) { }

        [HelpBox("Insert PL_SDK_D2D_ON scriptable symbol in project settings", HelpBoxMessageType.Warning)]
#endif
        [SerializeField] private bool _iSeeThisAlert;
    }
}
