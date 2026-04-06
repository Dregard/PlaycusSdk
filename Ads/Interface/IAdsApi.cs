using System;
using System.Collections.Generic;

namespace Playcus.Ads
{
    public interface IAdsApi : IService
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
        event Action<string> RewardStarted;

        /// <summary>
        /// Rewarded video was succesfull watched
        /// 1. PLACE where ads was started.
        /// 2. string customReward if it was passed to ShowRewarded.
        /// </summary>
        event Action<string> RewardCompleted;

        /// <summary>
        /// Rewarded video invoke error when showed
        /// 1. PLACE where ads was started.
        /// </summary>
        event Action<string> RewardErrorShowed;

        /// <summary>
        /// Rewarded video was closed by user
        /// 1. PLACE where ads was started.
        /// </summary>
        event Action<string> RewardCanceled;

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
        bool IsInterstitialReady();

        /// <summary>
        /// Show interstitial ad
        /// </summary>
        /// <param name="placement">Ads placement where ads need be showed.</param>
        void ShowInterstitial(string placement, Action onShowed = null, Action onClosed = null);

        // REWARDED
        /// <summary>
        /// Is rewarded ad ready to be showed
        /// </summary>
        bool IsRewardedReady();

        /// <summary>
        /// Show rewarded ad
        /// </summary>
        /// <param name="placement">Ads placement where ads need be showed.</param>
        /// <param name="onSuccess">Callback that was be invoked if ads was succesfully watched.</param>
        /// <param name="onFailure">Callback that was be invoked if ads was failed.</param>
        void ShowRewarded(string placement, Action onSuccess = null, Action onFailure = null);

        // BANNERS

        /// <summary>
        /// IsBannerReady to be showed
        /// </summary>
        bool IsBannerReady();

        /// <summary>
        /// Show banner on screen now
        /// </summary>
        void ShowBanner(string placement, BANNER_TYPE bannerType, BANNER_POS bannerPosition);

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
        string GetCurrentBannerAdsPlace();
        
        /// <summary>
        /// Current ADS platform name
        /// </summary>
        string AdsPlatformName { get; }
    }
}