//#if UNITY_IOS || UNITY_ANDROID
using ByteBrewSDK;
//#endif
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Playcus.Utils;

namespace Playcus.Analytics
{
    public partial class ByteBrewAnalyticsPlatform : AnalyticsServicePlatform
    {
#if PL_BYTEBREW_ANALYTICS_ON
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
        public override bool CanTesterBeTracked()
        {
            return false;
        }
        public override void TryToInit()
        {
//#if UNITY_IOS || UNITY_ANDROID
            if (IsInited)
                return;
            try
            {
                ByteBrew.InitializeByteBrew();
                InitComplete();
            }
            catch (Exception ex)
            {
                Debug.LogError("InitializeByteBrew exception:" + ex.Message);
            }
//#endif
        }
        public override void TrackEvent(AnalyticsEvent currentEvent)
        {
//#if UNITY_IOS || UNITY_ANDROID


            var preparedEventKey = PrepareEventName(currentEvent.EventKey);
            var amount = 0;
            if (currentEvent.Parameters.ContainsKey(AnalyticsProperties.pr_amount.ToString()))
            {
                int.TryParse(currentEvent.Parameters[AnalyticsProperties.pr_amount.ToString()].ToString(), out amount);
            }

            // PREDEFINED EVENTS
            if (currentEvent.EventKey == AnalyticsEvents.pl_user_level.ToString())
            {
                ByteBrew.NewCustomEvent(preparedEventKey, currentEvent.pr_level);
            }
            else if (currentEvent.EventKey == AnalyticsEvents.pl_level_opened.ToString())
            {
                ByteBrew.NewCustomEvent(
                        PrepareEventName(preparedEventKey + ":" + currentEvent.pr_level_type + ":" +
                                         currentEvent.pr_level),
                        currentEvent.pr_level);
            }
            else if (currentEvent.EventKey == AnalyticsEvents.pl_level_started.ToString()) 
            {
                ByteBrew.NewProgressionEvent(ByteBrewProgressionTypes.Started, currentEvent.pr_level_type,
                        currentEvent.pr_level.ToString());
            }
            else if (currentEvent.EventKey == AnalyticsEvents.pl_level_failed.ToString())
            {
                ByteBrew.NewProgressionEvent(ByteBrewProgressionTypes.Failed, currentEvent.pr_level_type,
                        currentEvent.pr_level.ToString(), currentEvent.pr_level_score);
            }
            else if (currentEvent.EventKey == AnalyticsEvents.pl_level_completed.ToString())
            {
                ByteBrew.NewProgressionEvent(ByteBrewProgressionTypes.Completed, currentEvent.pr_level_type,
                        currentEvent.pr_level.ToString(), currentEvent.pr_level_score);
            }
            else if (currentEvent.EventKey == AnalyticsEvents.pl_new_score.ToString())
            {
                ByteBrew.NewCustomEvent(
                        PrepareEventName(preparedEventKey + ":" + currentEvent.pr_level_type + ":" +
                                         currentEvent.pr_level), currentEvent.pr_level_score);
            }
            else if (currentEvent.EventKey == AnalyticsEvents.pl_achievement.ToString())
            {
                ByteBrew.NewCustomEvent(PrepareEventName(preparedEventKey + ":" + currentEvent.pr_content_id));
            }
            else if (currentEvent.EventKey == AnalyticsEvents.pl_purchase_checkout.ToString())
            {
                ByteBrew.NewCustomEvent(PrepareEventName(preparedEventKey + ":" + currentEvent.pr_content_id +
                                                                  ":" + currentEvent.pr_placement));
            }
            else if (currentEvent.EventKey == AnalyticsEvents.pl_purchase_success.ToString())
            {
                PurchaseSuccess(currentEvent.pr_currency, currentEvent.pr_revenue,
                    currentEvent.pr_content_id, currentEvent.pr_placement, currentEvent.pr_receipt);
            }
            else if (currentEvent.EventKey == AnalyticsEvents.pl_purchase_error.ToString())
            {
                ByteBrew.NewCustomEvent(PrepareEventName(preparedEventKey + ":" + currentEvent.pr_content_id +
                                                                  ":" + currentEvent.pr_reason));
            }
            else if (currentEvent.EventKey == AnalyticsEvents.pl_purchase_canceled.ToString())
            {
                ByteBrew.NewCustomEvent(PrepareEventName(preparedEventKey + ":" + currentEvent.pr_content_id +
                                                                  ":" + currentEvent.pr_placement));
            }
            else if (currentEvent.EventKey == AnalyticsEvents.pl_resources_add.ToString())
            {
                ByteBrew.NewCustomEvent(currentEvent.EventKey + "_" + currentEvent.pr_currency, amount);
            }
            else if (currentEvent.EventKey == AnalyticsEvents.pl_resources_remove.ToString())
            {
                ByteBrew.NewCustomEvent(currentEvent.EventKey + "_" + currentEvent.pr_currency, amount);
            }
            else if (currentEvent.EventKey == AnalyticsEvents.pl_share_checkout.ToString())
            {
                ByteBrew.NewCustomEvent(PrepareEventName(preparedEventKey + ":" + currentEvent.pr_content_id +
                                                                  ":" + currentEvent.pr_placement));
            }
            else if (currentEvent.EventKey == AnalyticsEvents.pl_share_success.ToString())
            {
                ByteBrew.NewCustomEvent(PrepareEventName(preparedEventKey + ":" + currentEvent.pr_content_id +
                                                                  ":" + currentEvent.pr_placement));
            }
            else if (currentEvent.EventKey == AnalyticsEvents.pl_request_checkout.ToString())
            {
                ByteBrew.NewCustomEvent(PrepareEventName(preparedEventKey + ":" + currentEvent.pr_content_id +
                                                                  ":" + currentEvent.pr_placement));
            }
            else if (currentEvent.EventKey == AnalyticsEvents.pl_request_success.ToString())
            {
                ByteBrew.NewCustomEvent(PrepareEventName(preparedEventKey + ":" + currentEvent.pr_content_id +
                                                                  ":" + currentEvent.pr_placement));
            }
            else if (currentEvent.EventKey == AnalyticsEvents.pl_ads_rewarded_button_click.ToString())
            {

            }
            else if (currentEvent.EventKey == AnalyticsEvents.pl_ads_rewarded_showed.ToString())
            {

            }
            else if (currentEvent.EventKey == AnalyticsEvents.pl_ads_rewarded_complete.ToString())
            {
                ByteBrew.TrackAdEvent(ByteBrewAdTypes.Reward,
                    PrepareEventName(preparedEventKey + ":" + currentEvent.pr_placement), "complete",
                    PrepareAdsPlatformName(currentEvent.pr_ad_network));
            }
            else if (currentEvent.EventKey == AnalyticsEvents.pl_ads_rewarded_canceled.ToString())
            {
                ByteBrew.TrackAdEvent(ByteBrewAdTypes.Reward,
                PrepareEventName(preparedEventKey + ":" + currentEvent.pr_placement), "canceled",
                PrepareAdsPlatformName(currentEvent.pr_ad_network));
            }
            else if (currentEvent.EventKey == AnalyticsEvents.pl_ads_insterstitial_showed.ToString())
            {
                ByteBrew.TrackAdEvent(ByteBrewAdTypes.Interstitial,
                PrepareEventName(preparedEventKey + ":" + currentEvent.pr_placement), "showed",
                PrepareAdsPlatformName(currentEvent.pr_ad_network));
            }
            else if (currentEvent.EventKey == AnalyticsEvents.pl_ads_insterstitial_closed.ToString())
            {
                ByteBrew.TrackAdEvent(ByteBrewAdTypes.Interstitial,
             PrepareEventName(preparedEventKey + ":" + currentEvent.pr_placement), "closed",
             PrepareAdsPlatformName(currentEvent.pr_ad_network));
            }
            else if (currentEvent.EventKey == AnalyticsEvents.pl_ads_banner_showed.ToString())
            {
                ByteBrew.TrackAdEvent(ByteBrewAdTypes.Banner,
         PrepareEventName(preparedEventKey + ":" + currentEvent.pr_placement), "showed",
         PrepareAdsPlatformName(currentEvent.pr_ad_network));
            }
            else if (currentEvent.EventKey == AnalyticsEvents.pl_loading_start.ToString())
            {
                ByteBrew.NewProgressionEvent(ByteBrewProgressionTypes.Started, "Player", "Loading");
            }
            else if (currentEvent.EventKey == AnalyticsEvents.pl_loading_step.ToString())
            {
                ByteBrew.NewCustomEvent(PrepareEventName(preparedEventKey), currentEvent.pr_amount);
            }
            else if (currentEvent.EventKey == AnalyticsEvents.pl_loading_end.ToString())
            {
                ByteBrew.NewProgressionEvent(ByteBrewProgressionTypes.Completed, "Player", "Loading_complete");
            }
            else
            // CUSTOM EVENTS
            {
                Func<AnalyticsEvent, bool> handler = null;
                HandleCustomEvent(ref handler);

                if (handler == null || handler.Invoke(currentEvent) == false)
                {
                    // Track custom event
                    ByteBrew.NewCustomEvent(preparedEventKey, amount);
                }
            }

//#endif
        }

        partial void HandleCustomEvent(ref Func<AnalyticsEvent, bool> handler);


        private void PurchaseSuccess(string currency, string price, string itemID,
            string placement, string receipt)
        {
            // Convert price to int cents format
            // (work only with predefined dollar prices // int priceCents = Mathf.CeilToInt(float.Parse(price, System.Globalization.CultureInfo.InvariantCulture) * 100f);
            int priceCents = currency.Equals("USD") ? Mathf.CeilToInt(float.Parse(price, System.Globalization.CultureInfo.InvariantCulture) * 100f) : 0;
            //SDK event
#if UNITY_IOS
             ByteBrew.TrackiOSInAppPurchaseEvent("AppStore",currency, priceCents,  itemID, placement, receipt);
#elif UNITY_ANDROID
            // Deserelize android receipt
            JSONNode receiptParsed = JSON.Parse(receipt);
            JSONNode payload = JSON.Parse(receiptParsed["Payload"]);
            ByteBrew.TrackGoogleInAppPurchaseEvent("Google Play",currency, priceCents, itemID, placement, receipt,
                payload["signature"]);
#endif
        }
#else
        public override void TryToInit()
        {
            
        }

        public override bool CanTesterBeTracked()
        {
            return false;
        }

        public override void TrackEvent(AnalyticsEvent currentEvent)
        {
        }
#endif
    }
}
