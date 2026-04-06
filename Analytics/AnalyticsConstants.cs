using System;
using System.Collections.Generic;
using UnityEngine;

namespace Playcus.Analytics
{
    /// <summary>
    /// Don't change this analytics constants! (Alexey Simonenko)
    /// All playcus events list https://docs.google.com/spreadsheets/d/1JjSoB1pyAnKDIZ4Ir3PsFESau0e4rwD1WF_-PFT0bTg
    /// </summary>
    public enum AnalyticsEvents
    {
        //App loading
        pl_loading_start = 0,
        pl_loading_end = 1,
        pl_loading_step = 2,
        pl_store_ = 3,
        //Purchases
        pl_purchase_checkout = 4,
        pl_purchase_success = 5,
        pl_purchase_error = 6,
        pl_purchase_canceled = 7,
        pl_purchase_first_ = 8,
        pl_purchase_not_found = 9,
        pl_shop_show = 10,
        pl_shop_click = 11,
        pl_subscription_trial = 12,
        pl_subscription_charged = 13,
        //Ads
        pl_ads_rewarded_button_showed = 14,
        pl_ads_rewarded_button_click = 15,
        pl_ads_rewarded_showed = 16,
        pl_ads_rewarded_complete = 17,
        pl_ads_rewarded_canceled = 18,
        pl_ads_rewarded_error = 19,
        pl_ads_insterstitial_showed = 20,
        pl_ads_insterstitial_closed = 21,
        pl_ads_appopen_showed = 71,
        pl_ads_appopen_closed = 72,
        pl_ads_banner_showed = 22,
        pl_ads_revenue = 23,
        //Keypoints
        pl_tutorial_started = 24,
        pl_tutorial_step = 25,
        pl_tutorial_completed = 26,
        pl_user_level = 27,
        pl_first_click = 28,
        //UI
        pl_btn_click = 29,
        pl_scene_changed = 30,
        pl_popup_opened = 31,
        pl_popup_closed = 32,
        pl_screen_opened = 33,
        pl_ui_showed = 70,
        // Promo
        pl_promo_show = 34,
        pl_promo_showed = 35,
        pl_promo_completed = 36,
        pl_promo_closed = 37,
        //Game Levels
        pl_level_opened = 38,
        pl_level_started = 39,
        pl_level_failed = 40,
        pl_level_completed = 41,
        pl_level_ = 42,
        pl_quest_start = 43,
        pl_quest_complete = 44,
        //Metagame
        pl_new_score = 45,
        pl_achievement = 46,
        //Game Resources
        pl_resources_add = 47,
        pl_resources_remove = 48,
        pl_daily_bonus_collect = 49,
        pl_resources_balance = 69,
        //Social & Viral
        pl_social_signup = 50,
        pl_request_checkout = 51,
        pl_request_success = 52,
        pl_share_checkout = 53,
        pl_share_success = 54,
        pl_share_failed = 77,
        pl_rateus_show = 55,
        pl_rateus_completed = 56,
        //Support
        pl_support_initiate = 57,
        pl_support_write = 58,
        pl_support_send = 59,
        pl_support_sent = 60,
        //Sessions
        pl_session_start_ = 61,
        //LifeTime Activity
        pl_lifetime = 62,
        pl_lifetime_ = 63,
        //GDPR
        pl_gdpr_show = 64,
        pl_gdpr_accept = 65,
        pl_gdpr_forget_click = 66,
        pl_gdpr_forgeted = 67,
        pl_gdpr_ios_permission = 68,
        //Remote Configs
        pl_loading_configs = 73,
        //Permission 
        pl_notification_permission_show = 74,
        pl_notification_permission_denied = 75,
        pl_notification_permission_granted = 76,

        // current index = 77 !!! PLEASE UPDATE THIS IF YOU HAVE ADDED NEW ITEMS. !!!
    }

    /// <summary>
    /// Don't change this analytics constants! (Alexey Simonenko)
    /// All playcus events list https://docs.google.com/spreadsheets/d/1JjSoB1pyAnKDIZ4Ir3PsFESau0e4rwD1WF_-PFT0bTg
    /// </summary>
    public enum AnalyticsProperties
    {
        ///<summary>Unique user id</summary>
        pr_user_id = 0,

        ///<summary>level in game or user level</summary>
        pr_level = 1,

        ///<summary>What type level was? Example: tournaments, basic, daily, etc</summary>
        pr_level_type = 2,

        ///<summary>Score (points) that user beated on this level</summary>
        pr_level_score = 3,

        ///<summary>Custom event data</summary>
        pr_content = 4,

        ///<summary>Unique content ID. Examples: gold_pack_1</summary>
        pr_content_id = 5,
        
        ///<summary>Content Type. Examples: gold</summary>
        pr_content_type = 6,

        ///<summary>Place where this event was tracked</summary>
        pr_placement = 7,

        ///<summary>Example: USD</summary>
        pr_currency = 8,

        ///<summary>Holiday Event Name</summary>
        pr_event_name = 9,

        ///<summary>Unique item ID. Examples: gold_pack_1</summary>
        pr_item_id = 10,

        ///<summary>Unique item type. Examples: pack</summary>
        pr_item_type = 11,

        ///<summary>Amount</summary>
        pr_amount = 12,

        ///<summary>Balance Amount</summary>
        pr_balance_amount = 13,

        ///<summary>Amount</summary>
        pr_social_type = 13,

        ///<summary>Count of purchases in this session</summary>
        pr_purchases_count = 14,
        
        ///<summary>Any step of some process like tutorial (step = 1) (step = 2)</summary>
        pr_step = 15,

        ///<summary>Reason why event happened. Example: purchase, level_completed, </summary>
        pr_reason = 16,
        
        ///<summary>Receipt of iap purchase</summary>
        pr_receipt = 17,
        
        ///<summary>Revenue amount of iap purchase</summary>
        pr_revenue = 18,
        
        ///<summary>Some time score in seconds</summary>
        pr_seconds = 19,
        
        ///<summary>how much boosters was used</summary>
        pr_booster_use = 20,
        
        ///<summary>Some time score in minutes</summary>
        pr_minutes = 21,
        
        ///<summary>Ads network</summary>
        pr_ad_network = 22,
        
        ///<summary>Ads format</summary>
        pr_ad_format = 23,
       
        ///<summary>Analogue of "pr_amount" (if you need additional)</summary>
        pr_value = 24,
        
        ///<summary>How long did it take to complete the level (in seconds)</summary>
        pr_level_time = 25,
        
        ///<summary>The number of any misses by the player</summary>
        pr_miss = 26,
        
        /// <summary>This is ab_marker from firebase remote config</summary>
        pr_ab_marker = 27,
        
        /// <summary>The place where the marker was taken from. (1 - if from cache, 0 - actual from firebase)</summary>
        pr_ab_marker_from_cache = 28,
    }

    /// <summary>
    /// Don't change this analytics constants! (Alexey Simonenko)
    /// All playcus events list https://docs.google.com/spreadsheets/d/1JjSoB1pyAnKDIZ4Ir3PsFESau0e4rwD1WF_-PFT0bTg
    /// </summary>
    public enum AnalyticsPlatformsNames
    {
        android,
        amazon,
        ios,
        fb,
        editor,
        desktop,
        windows,
        unknown
    }

}