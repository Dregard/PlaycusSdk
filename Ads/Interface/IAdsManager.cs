using System;
using System.Collections.Generic;
using UnityEngine;

// using Playcus.Currency;

namespace Playcus.Ads
{
    /// <summary>
    /// Interface of AdsManager (controlling ads behavior)
    /// </summary>
    public interface IAdsManager
    {
        // EVENTS

        /// <summary>
        /// Screen safe area rect was changed by banner
        /// </summary>
        event Action RectChanged;

        /// <summary>
        /// Rewarded video ads can be showed now
        /// </summary>
        event Action RewardReady;

        /// <summary>
        /// Rewarded video ads started now. 
        /// 1. PLACE where ads was started.
        /// </summary>
        event Action<PLACE> RewardStarted;

        /// <summary>
        /// Rewarded video was succesfull watched
        /// 1. PLACE where ads was started.
        /// 2. string customReward if it was passed to ShowRewarded.
        /// </summary>
        event Action<PLACE, string> RewardCompleted;

        /// <summary>
        /// Rewarded video invoke error when showed
        /// 1. PLACE where ads was started.
        /// </summary>
        event Action<PLACE> RewardErrorShowed;

        /// <summary>
        /// Rewarded video was closed by user
        /// 1. PLACE where ads was started.
        /// </summary>
        event Action<PLACE> RewardCanceled;

        /// <summary>
        /// Banner was displayed on screen
        /// </summary>
        event Action BannerShowed;

        /// <summary>
        /// Interstitial fullscreen ads can be showed now
        /// </summary>
        event Action InterstitialReady;

        /// <summary>
        /// Interstitial fullscreen ads started now
        /// </summary>
        event Action InterstitialShowed;

        /// <summary>
        /// Interstitial fullscreen ads started now
        /// </summary>
        event Action InterstitialClosed;
        
        /// <summary>
        /// AppOpen fullscreen ads can be showed now
        /// </summary>
        event Action AppOpenReady;

        /// <summary>
        /// AppOpen fullscreen ads started now
        /// </summary>
        event Action AppOpenShowed;

        /// <summary>
        /// AppOpen was closed
        /// </summary>
        event Action AppOpenClosed;

        // GENERIC
        
        /// <summary>
        /// return false if ads not availiable on this store (platform)
        /// </summary>
        bool IsAdsAvailibleOnThisStore();
        
        /// <summary>
        /// Disable all ads in this session for this user
        /// </summary>
        void DisableAd();

        /// <summary>
        /// Get array of offsets for UI from screen edges
        /// </summary>
        /// <returns>Dicionary of all offsets from screen edges by banner position types.</returns>
        Dictionary<BANNER_POS, int> GetBannerOffsets();

        /// <summary>
        /// Is rewarded ads showed on screen now?
        /// </summary>
        bool IsAdsShowedNow();

        // // APPOPEN
        //
        // /// <summary>
        // /// Is AppOpen ad ready to be showed
        // /// </summary>
        // /// <param name="adsPlaceName">Ads placement where ads need be showed.</param>
        // /// <returns></returns>
        // bool IsAnyAppOpenReady(PLACE adsPlaceName);
        //
        // /// <summary>
        // /// Show AppOpen ad
        // /// </summary>
        // /// <param name="adsPlaceName">Ads placement where ads need be showed.</param>
        // void ShowAppOpen(PLACE adsPlaceName, Action onShowed = null, Action onClosed = null);

        // INTERSTITIAL

        /// <summary>
        /// Is interstitial ad ready to be showed
        /// </summary>
        /// <param name="adsPlaceName">Ads placement where ads need be showed.</param>
        /// <param name="increaseCounter">Increase try to show counter. Need for places with show only after N try count.</param>
        /// <returns></returns>
        bool IsAnyInterstitialReady(PLACE adsPlaceName, bool increaseCounter = false);

        /// <summary>
        /// Show interstitial ad
        /// </summary>
        /// <param name="adsPlaceName">Ads placement where ads need be showed.</param>
        void ShowInterstitial(PLACE adsPlaceName, Action onShowed = null, Action onClosed = null);

        /// <summary>
        /// Return Interstitial ads custom string data
        /// </summary>
        /// <param name="adsPlaceName"></param>
        /// <returns>String</returns>
        string GetInterstitialCustomData(PLACE adsPlaceName);


        /// <summary>
        /// Setup interstitial interval? if IntervalSeconds > 0
        /// </summary>
        /// <param name="adsPlace"></param>
        void ProlongInterstitialIntervalDelayTime(PLACE adsPlace);

        // REWARDED
        /// <summary>
        /// Is rewarded ad ready to be showed
        /// </summary>
        /// <param name="adsPlaceName">Ads placement where ads need be showed.</param>
        bool IsAnyRewardedReady(PLACE adsPlaceName);

        /// <summary>
        /// Show rewarded ad
        /// </summary>
        /// <param name="adsPlaceName">Ads placement where ads need be showed.</param>
        /// <param name="customReward">Custom reward for custom logic without currency manager.</param>
        /// <param name="onSuccess">Callback that was be invoked if ads was succesfully watched.</param>
        /// <param name="onFailure">Callback that was be invoked if ads was failed.</param>
        void ShowRewarded(PLACE adsPlaceName, string customReward = "", Action onSuccess = null, Action onFailure = null);

        /// <summary>
        /// Return time when ads place will be reloaded (only for places with timer)
        /// </summary>
        TimeSpan GetTimeForRewardedReady(PLACE adsPlaceName);

        // /// <summary>
        // /// Return string representation of reward field of place (not currency)
        // /// </summary>
        // string GetAdsPlaceRewardString(PLACE adsPlaceName);

        // /// <summary>
        // /// Return int representation of reward field of place (not currency)
        // /// </summary>
        // int GetAdsPlaceRewardInt(PLACE adsPlaceName);

        /// <summary>
        /// Return currency rewards config of target rewarded place
        /// </summary>
        // CurrencyReward[] GetAdsPlaceRewardCurrency(PLACE adsPlaceName);

        /// <summary>
        /// Return Custom data for Rewarded Videos ads
        /// </summary>
        /// <param name="adsPlaceName"></param>
        /// <returns>String</returns>
        string GetRewardCustomData(PLACE adsPlaceName);

        // BANNERS

        /// <summary>
        /// IsBannerReady to be showed
        /// </summary>
        bool IsBannerReady(PLACE adsPlaceName);

        /// <summary>
        /// Show banner on screen now
        /// </summary>
        void ShowBanner(PLACE adsPlaceName);

        /// <summary>
        /// Hide all banners from screen
        /// </summary>
        void HideBanners();

        /// <summary>
        /// Is banner showing now on screen
        /// </summary>
        bool IsBannerShowed();

        /// <summary>
        /// What ads place where banner showing now
        /// </summary>
        PLACE GetCurrentBannerAdsPlace();
        
        /// <summary>
        /// Current ADS platform name
        /// </summary>
        string AdsPlatformName { get; }

        Rect BannerScreenRect { get; }
    }
}