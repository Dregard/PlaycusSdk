using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using Playcus.Utils;
#if PL_SDK_GA_ON
using GameAnalyticsSDK;
using GameAnalyticsSDK.Events;

#endif

namespace Playcus.Analytics
{
    /// <summary>
    /// For AnalyticsService used only! Don't use directly! (Simonenko Alexey)
    /// GameAnalytics https://gameanalytics.com/docs/item/unity-sdk
    /// All playcus events list https://docs.google.com/spreadsheets/d/1JjSoB1pyAnKDIZ4Ir3PsFESau0e4rwD1WF_-PFT0bTg
    /// </summary>
    public class GameAnalyticsAnalyticServicePlatform : AnalyticsServicePlatform
    {
#region consent

        public static void SetConsent(bool consentGiven)
        {     
#if PL_SDK_GA_ON
            GameAnalytics.SetEnabledEventSubmission(consentGiven);
#endif
        }
        
#endregion
        private GameAnalyticsCustomTrackEventBase _gameAnalyticsCustomTrackEvent;
        
        public override bool CanTesterBeTracked()
        {
            return false;
        }

#if PL_SDK_GA_ON

        // PRIVATE
        private Action _initCallbackFunction;
        private IAnalyticsManager _analyticsManager;


        public override void TryToInit()
        {
            if (IsInited)
            {
                return;
            }

            _gameAnalyticsCustomTrackEvent = GetComponent<GameAnalyticsCustomTrackEventBase>();
            
            _analyticsManager = ServiceLocator.Get<IAnalyticsManager>();
            _analyticsManager.LogDebug("AnalyticsServicePlatform init");
            Debug.Log($"{nameof(GameAnalyticsAnalyticServicePlatform)}: TryToInit called.");
            try
            {
                GameAnalytics.Initialize();
                InitComplete();
                _analyticsManager.LogDebug("AnalyticsServicePlatform InitCompleted");
                Application.logMessageReceived+= LoggerMy;
                GameAnalyticsILRD.SubscribeMaxImpressions();
            }
            catch (Exception e)
            {
                Debug.LogError($"GameAnalyticsAnalyticServicePlatform:Init error {e.Message}");
            }
        }
        public void LoggerMy(string text, string stackTrace, LogType type)
        {
            if (type == LogType.Error )
            {
                GameAnalytics.NewErrorEvent(GAErrorSeverity.Error, text);
            }
            if ( type == LogType.Exception)
            {
                GameAnalytics.NewErrorEvent(GAErrorSeverity.Critical, text);
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

        public override void TrackEvent(AnalyticsEvent currentEvent)
        {
            var preparedEventKey = PrepareEventName(currentEvent.EventKey);
            var amount = 0;
            if (currentEvent.Parameters.ContainsKey(AnalyticsProperties.pr_amount.ToString()))
            {
                int.TryParse(currentEvent.Parameters[AnalyticsProperties.pr_amount.ToString()].ToString(), out amount);
            }

            //CUSTOM EVENT SENDING FOR A PROJECT
            if (_gameAnalyticsCustomTrackEvent == null||
                _gameAnalyticsCustomTrackEvent != null &&
                _gameAnalyticsCustomTrackEvent.SendEventIfItCustom(currentEvent))
           
            {
                // PREDEFINED EVENTS
                if (currentEvent.EventKey == AnalyticsEvents.pl_user_level.ToString())
                {
                    GameAnalytics.NewDesignEvent(preparedEventKey, currentEvent.pr_level);
                }
                else if (currentEvent.EventKey == AnalyticsEvents.pl_level_opened.ToString())
                {
                    GameAnalytics.NewDesignEvent(
                        PrepareEventName(preparedEventKey + ":" + currentEvent.pr_level_type + ":" +
                                         currentEvent.pr_level),
                        currentEvent.pr_level);
                }
                else if (currentEvent.EventKey == AnalyticsEvents.pl_level_started.ToString())
                {
                    GameAnalytics.NewProgressionEvent(GAProgressionStatus.Start, currentEvent.pr_level_type,
                        currentEvent.pr_level.ToString());
                }
                else if (currentEvent.EventKey == AnalyticsEvents.pl_level_failed.ToString())
                {
                    GameAnalytics.NewProgressionEvent(GAProgressionStatus.Fail, currentEvent.pr_level_type,
                        currentEvent.pr_level.ToString(), currentEvent.pr_level_score);
                }
                else if (currentEvent.EventKey == AnalyticsEvents.pl_level_completed.ToString())
                {
                    GameAnalytics.NewProgressionEvent(GAProgressionStatus.Complete, currentEvent.pr_level_type,
                        currentEvent.pr_level.ToString(), currentEvent.pr_level_score);
                }
                else if (currentEvent.EventKey == AnalyticsEvents.pl_new_score.ToString())
                {
                    GameAnalytics.NewDesignEvent(
                        PrepareEventName(preparedEventKey + ":" + currentEvent.pr_level_type + ":" +
                                         currentEvent.pr_level), currentEvent.pr_level_score);
                }
                else if (currentEvent.EventKey == AnalyticsEvents.pl_achievement.ToString())
                {
                    GameAnalytics.NewDesignEvent(PrepareEventName(preparedEventKey + ":" + currentEvent.pr_content_id));
                }
                else if (currentEvent.EventKey == AnalyticsEvents.pl_purchase_checkout.ToString())
                {
                    GameAnalytics.NewDesignEvent(PrepareEventName(preparedEventKey + ":" + currentEvent.pr_content_id +
                                                                  ":" + currentEvent.pr_placement));
                }
                else if (currentEvent.EventKey == AnalyticsEvents.pl_purchase_success.ToString())
                {
                    GameAnalytics.NewDesignEvent(PrepareEventName(preparedEventKey + ":" + currentEvent.pr_content_id +
                                                                  ":" + currentEvent.pr_placement));
                    Debug.LogError("pl_purchase_success after design event");
                    PurchaseSuccess(currentEvent.pr_currency, currentEvent.pr_revenue,
                        currentEvent.pr_content_id, currentEvent.pr_placement, currentEvent.pr_receipt);
                }
                else if (currentEvent.EventKey == AnalyticsEvents.pl_purchase_error.ToString())
                {
                    GameAnalytics.NewDesignEvent(PrepareEventName(preparedEventKey + ":" + currentEvent.pr_content_id +
                                                                  ":" + currentEvent.pr_reason));
                }
                else if (currentEvent.EventKey == AnalyticsEvents.pl_purchase_canceled.ToString())
                {
                    GameAnalytics.NewDesignEvent(PrepareEventName(preparedEventKey + ":" + currentEvent.pr_content_id +
                                                                  ":" + currentEvent.pr_placement));
                }
                else if (currentEvent.EventKey == AnalyticsEvents.pl_resources_add.ToString())
                {
                    GameAnalytics.NewResourceEvent(GAResourceFlowType.Source, currentEvent.pr_currency, amount,
                        currentEvent.pr_content_type, currentEvent.pr_content_id);
                }
                else if (currentEvent.EventKey == AnalyticsEvents.pl_resources_remove.ToString())
                {
                    GameAnalytics.NewResourceEvent(GAResourceFlowType.Sink, currentEvent.pr_currency, amount,
                        currentEvent.pr_content_type, currentEvent.pr_content_id);
                }
                else if (currentEvent.EventKey == AnalyticsEvents.pl_share_checkout.ToString())
                {
                    GameAnalytics.NewDesignEvent(PrepareEventName(preparedEventKey + ":" + currentEvent.pr_content_id +
                                                                  ":" + currentEvent.pr_placement));
                }
                else if (currentEvent.EventKey == AnalyticsEvents.pl_share_success.ToString())
                {
                    GameAnalytics.NewDesignEvent(PrepareEventName(preparedEventKey + ":" + currentEvent.pr_content_id +
                                                                  ":" + currentEvent.pr_placement));
                }
                else if (currentEvent.EventKey == AnalyticsEvents.pl_request_checkout.ToString())
                {
                    GameAnalytics.NewDesignEvent(PrepareEventName(preparedEventKey + ":" + currentEvent.pr_content_id +
                                                                  ":" + currentEvent.pr_placement));
                }
                else if (currentEvent.EventKey == AnalyticsEvents.pl_request_success.ToString())
                {
                    GameAnalytics.NewDesignEvent(PrepareEventName(preparedEventKey + ":" + currentEvent.pr_content_id +
                                                                  ":" + currentEvent.pr_placement));
                }
                else if (currentEvent.EventKey == AnalyticsEvents.pl_ads_rewarded_button_click.ToString())
                {
                    GameAnalytics.NewAdEvent(GAAdAction.Clicked, GAAdType.RewardedVideo,
                        PrepareAdsPlatformName(currentEvent.pr_ad_network),
                        PrepareEventName(preparedEventKey + ":" + currentEvent.pr_placement));
                }
                else if (currentEvent.EventKey == AnalyticsEvents.pl_ads_rewarded_showed.ToString())
                {
                    currentRewardedVideoPlacement = (string)currentEvent.Parameters[AnalyticsProperties.pr_placement.ToString()];
                   // Debug.LogError("placement " + currentRewardedVideoPlacement);
                    GameAnalytics.StartTimer(currentRewardedVideoPlacement);
                    GameAnalytics.NewAdEvent(GAAdAction.Show, GAAdType.RewardedVideo,
                        PrepareAdsPlatformName(currentEvent.pr_ad_network),
                        PrepareEventName(preparedEventKey + ":" + currentEvent.pr_placement));
                }
                else if (currentEvent.EventKey == AnalyticsEvents.pl_ads_rewarded_complete.ToString())
                {
                    long elapsedTime = GameAnalytics.StopTimer(currentRewardedVideoPlacement);
                   // Debug.LogError("elapsedTime " + elapsedTime);
                    currentRewardedVideoPlacement = "";
                    GameAnalytics.NewAdEvent(GAAdAction.RewardReceived, GAAdType.RewardedVideo,
                        PrepareAdsPlatformName(currentEvent.pr_ad_network),
                        PrepareEventName(preparedEventKey + ":" + currentEvent.pr_placement),elapsedTime);
                }
                else if (currentEvent.EventKey == AnalyticsEvents.pl_ads_rewarded_canceled.ToString())
                {
                    GameAnalytics.StopTimer(currentRewardedVideoPlacement);
                    currentRewardedVideoPlacement = "";
                    GameAnalytics.NewAdEvent(GAAdAction.FailedShow, GAAdType.RewardedVideo,
                        PrepareAdsPlatformName(currentEvent.pr_ad_network),
                        PrepareEventName(preparedEventKey + ":" + currentEvent.pr_placement), GAAdError.InternalError);
                }
                else if (currentEvent.EventKey == AnalyticsEvents.pl_ads_insterstitial_showed.ToString())
                {
                    string st = AnalyticsProperties.pr_placement.ToString();
                    currentRewardedVideoPlacement = (string)currentEvent.Parameters[st];
                    GameAnalytics.StartTimer(currentRewardedVideoPlacement);
                    GameAnalytics.NewAdEvent(GAAdAction.Show, GAAdType.Interstitial,
                        PrepareAdsPlatformName(currentEvent.pr_ad_network),
                        PrepareEventName(preparedEventKey + ":" + currentEvent.pr_placement));
                }
                else if (currentEvent.EventKey == AnalyticsEvents.pl_ads_insterstitial_closed.ToString())
                {
                    long elapsedTime = GameAnalytics.StopTimer(currentRewardedVideoPlacement);
                    currentRewardedVideoPlacement = "";
                    GameAnalytics.NewAdEvent(GAAdAction.RewardReceived, GAAdType.Interstitial,
                        PrepareAdsPlatformName(currentEvent.pr_ad_network),
                        PrepareEventName(preparedEventKey + ":" + currentEvent.pr_placement),elapsedTime);
                }
                else if (currentEvent.EventKey == AnalyticsEvents.pl_ads_banner_showed.ToString())
                {
                    GameAnalytics.NewAdEvent(GAAdAction.Show, GAAdType.Banner,
                        PrepareAdsPlatformName(currentEvent.pr_ad_network),
                        PrepareEventName(preparedEventKey + ":" + currentEvent.pr_placement));
                }
                else if (currentEvent.EventKey == AnalyticsEvents.pl_loading_start.ToString())
                {
                    GameAnalytics.NewDesignEvent(PrepareEventName(preparedEventKey));
                    GameAnalytics.NewProgressionEvent(GAProgressionStatus.Start,"Loading");
                }
                else if (currentEvent.EventKey == AnalyticsEvents.pl_loading_step.ToString())
                {
                    GameAnalytics.NewDesignEvent(PrepareEventName(preparedEventKey + ":" + currentEvent.pr_amount));
                }
                else if (currentEvent.EventKey == AnalyticsEvents.pl_loading_end.ToString())
                {
                    GameAnalytics.NewDesignEvent(PrepareEventName(preparedEventKey));
                    GameAnalytics.NewProgressionEvent(GAProgressionStatus.Complete,"Loading");
                }
                else if (currentEvent.EventKey == AnalyticsEvents.pl_notification_permission_show.ToString())
                {
                    GameAnalytics.NewProgressionEvent(GAProgressionStatus.Start, "NotificationPermission");
                    GameAnalytics.NewDesignEvent("NotificationPermissionShow");
                }
                else if (currentEvent.EventKey == AnalyticsEvents.pl_notification_permission_denied.ToString())
                {
                    GameAnalytics.NewProgressionEvent(GAProgressionStatus.Complete, "NotificationPermission");
                    GameAnalytics.NewDesignEvent("NotificationPermissionDenied");
                }
                else if (currentEvent.EventKey == AnalyticsEvents.pl_notification_permission_granted.ToString())
                {
                    GameAnalytics.NewProgressionEvent(GAProgressionStatus.Complete, "NotificationPermission");
                    GameAnalytics.NewDesignEvent("NotificationPermissionGranted");
                }
                else
                    // CUSTOM EVENTS
                {
                    GameAnalytics.NewDesignEvent(preparedEventKey, amount);
                }
                // ATTENTION - GameAnalytics does not support additional parameters for events
            }

        }
        private string currentRewardedVideoPlacement = "";
        void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                if (!string.IsNullOrEmpty(currentRewardedVideoPlacement))
                {
                    GameAnalytics.PauseTimer(currentRewardedVideoPlacement);
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(currentRewardedVideoPlacement))
                {
                    GameAnalytics.ResumeTimer(currentRewardedVideoPlacement);
                }
            }
        }

        private void PurchaseSuccess( string currency, string price, string itemID,
            string placement, string receipt)
        {
            // Convert price to int cents format
            // (work only with predefined dollar prices // int priceCents = Mathf.CeilToInt(float.Parse(price, System.Globalization.CultureInfo.InvariantCulture) * 100f);
            int priceCents = currency.Equals("USD")? Mathf.CeilToInt(float.Parse(price, System.Globalization.CultureInfo.InvariantCulture) * 100f):0;
            //SDK event
#if UNITY_IOS
            GameAnalytics.NewBusinessEventIOS(currency, priceCents, "iap", itemID, placement, receipt);
#elif UNITY_ANDROID
            // Deserelize android receipt
            JSONNode receiptParsed = JSON.Parse(receipt);
            JSONNode payload = JSON.Parse(receiptParsed["Payload"]);
            GameAnalytics.NewBusinessEventGooglePlay(currency, priceCents, "iap", itemID, placement, receipt,
                payload["signature"]);
#endif
        }


#else
        public override void TryToInit()
        {
            Debug.LogError("AnalyticsServicePlatform: Insert PL_SDK_GA_ON scriptable symbol in project settings", gameObject);
        }
        
        public override void TrackEvent(AnalyticsEvent currentEvent){}


        [HelpBox(@"Insert PL_SDK_GA_ON scriptable symbol in project settings", HelpBoxMessageType.Warning)]
#endif
        [SerializeField] private bool _iSeeThisAlert;
    }
}