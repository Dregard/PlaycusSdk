using System;
// using Playcus.Currency;
using UnityEngine;

namespace Playcus.Ads
{
     /// <summary>
     /// Show options and rewards for one reward ads placement.
     /// </summary>
     [Serializable]
     public class AdsPlaceRewardedModel
     {
         ///<summary>Name of place that was converted to PLACE enum</summary>
         [Tooltip("Name of place that was converted to PLACE enum")]
         public string Name;

         // ///<summary>List of currency rewards that added to user after reward ads watched.</summary>
         // [Tooltip("List of currency rewards that added to user after reward ads watched.")]
         // public CurrencyReward[] RewardCurrency;

         // ///<summary>Custom reward if you not use CurrencyManager</summary>
         // [Tooltip("Custom reward if you not use CurrencyManager")]
         // public string Reward;

         ///<summary>Interval in minutes while reward ads in this place can't be showed again.</summary>
         [Tooltip("Interval in minutes while reward ads in this place can't be showed again.")]
         public int MinutesInterval;

         /// <summary>
         /// custom string data
         /// </summary>
         [Tooltip("custom string data")]
         public string CustomData;

         ///<summary>Is this place enabled now. (For configs usability)</summary>
         [Tooltip("Is this place enabled now. (For configs usability)")]
         public bool Enabled;
     }


     /// <summary>
     /// Show options for one interstitial ads placement.
     /// </summary>
     [Serializable]
     public class AdsPlaceInterstitialModel
     {
         ///<summary>Name of place that was converted to PLACE enum</summary>
         [Tooltip("Name of place that was converted to PLACE enum")]
         public string Name;

         /// <summary>
         /// custom string data
         /// </summary>
         [Tooltip("custom string data")]
         public string CustomData;

         ///<summary>How many clicks (try to show on place) need for ads can be showed.</summary>
         [Tooltip("How many clicks (try to show on place) need for ads can be showed.")]
         public int ClicksNeedToShow;
         
         ///<summary>How many clicks (try to show on place) need for ads can be showed in first session.</summary>
         [Tooltip("How many clicks (try to show on place) need for ads can be showed in first session.")]
         public int ClicksNeedToShowFirstSession;

         ///<summary>Is this place enabled now. (For configs usability)</summary>
         [Tooltip("Is this place enabled now. (For configs usability)")]
         public bool Enabled;

         /// <summary>
         /// interval for showing. when not 0 ignore global interval
         /// </summary>
         [Tooltip("interval for showing. when not 0 ignore global interval")]
         public float IntervalSeconds = 0f;
         
         // /// <summary>
         // /// If true, show confirm popup before Interstitial show
         // /// </summary>
         // [Tooltip("Show confirm popup before Interstitial show")]
         // public bool ShowPermissionPopup;

         private float _intervalEndTime = 0f;

         public void UpdateNextIntervalDelayTime()
         {
             _intervalEndTime = Time.unscaledTime + IntervalSeconds;
         }

         public bool CheckIntervalDelay()
         {
             return Time.unscaledTime >= _intervalEndTime;
         }
     }


     /// <summary>
     /// Show options for one banner ads placement.
     /// </summary>
     [Serializable]
     public class AdsPlaceBannerModel
     {
         ///<summary>Name of place that was converted to PLACE enum</summary>
         public string Name;

         ///<summary>Type of banner for this place.</summary>
         public BANNER_TYPE BannerType;

         ///<summary>Position of banner for this place.</summary>
         [Tooltip("!!!CURRENTLY NOT WORKING!!! Custom banner position in this place")]
         public BANNER_POS Position;

         ///<summary>Is this place enabled now. (For configs usability)</summary>
         public bool Enabled;
         
         ///<summary>Is this place enabled now. (For configs usability)</summary>
         public bool DisabledOnFirstSession;
     }


     /// <summary>
     /// Banner ads position types
     /// </summary>
     public enum BANNER_POS
     {
         ///<summary>Top edge of the screen.</summary>
         TOP,

         ///<summary>Bottom edge of the screen.</summary>
         BOTTOM,

         ///<summary>Banner position not setuped or banner disabled.</summary>
         NONE
     };


     /// <summary>
     /// Banner ads behavior types
     /// </summary>
     public enum BANNER_TYPE
     {
         ///<summary>Banner with fixed size for all devices.</summary>
         BANNER,

         ///<summary>Banner with apaptive size. Will be different on smartphones and tablets.</summary>
         SMART,

         ///<summary>Big quad banner. May be half of screen area size.</summary>
         RECTANGLE,

         ///<summary>Banner type not setuped or banner disabled.</summary>
         NONE
     };
}