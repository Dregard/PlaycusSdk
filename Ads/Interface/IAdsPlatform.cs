using System;
using System.Collections.Generic;
using UnityEngine;

namespace Playcus.Ads
{
    /// <summary>
    /// Ads platform is implementation of bridge to target ads sdk. Used by IAdsManager.
    /// </summary>
    public interface IAdsPlatform
    {
        /// <summary>
        /// Screen rect affected by banner was changed. Project may need update ui. 
        /// (Already listened trought IAdsManager by ScreenSafeAreaContainer component)
        /// </summary>
        event Action RectChanged;

        /// <summary>
        /// Rewarded video ads can be showed now
        /// </summary>
        event Action RewardReady;

        /// <summary>
        /// Rewarded video ads started now. 
        /// </summary>
        event Action RewardStarted;

        /// <summary>
        /// Rewarded video was succesfull watched
        /// </summary>
        event Action RewardCompleted;

        /// <summary>
        /// Rewarded video invoke error when showed
        /// </summary>
        event Action RewardErrorShowed;

        /// <summary>
        /// Rewarded video was closed by user
        /// </summary>
        event Action RewardCanceled;

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
        /// AppOpen fullscreen ads started now
        /// </summary>
        event Action AppOpenClosed;

        event Action MRecAdLoaded;

        event Action MRecAdLoadFailed;

        /// <summary>
        /// Init ads platform
        /// </summary>
        /// <param name="rewardedEnabled"> Is reward ads need be initialised on start.</param>
        /// <param name="interstitialEnabled"> Is interstitial ads need be initialised on start.</param>
        /// <param name="bannerEnabled"> Is banner ads need be initialised on start.</param>
        void Init(bool rewardedEnabled, bool interstitialEnabled, bool bannerEnabled, BANNER_POS bannerPos, bool appOpenEnabled);

        /// <summary>
        /// Is Interstitial ad was loading and ready.
        /// </summary>
        bool IsInterstitialReady();

        /// <summary>
        /// Show Interstitial ad that can be skiped.
        /// </summary>
        void ShowInterstitial(PLACE adsPlaceName);
        void ShowInterstitial(string placement);

        /// <summary>
        /// Is Rewarded ad was loading and ready.
        /// </summary>
        bool IsRewardedReady();

        /// <summary>
        /// Show Rewarded ad that can't be skiped.
        /// </summary>
        void ShowRewarded(PLACE adsPlaceName);
        void ShowRewarded(string adsPlaceName);

        /// <summary>
        /// Is Banner ad was loading and ready.
        /// </summary>
        bool IsBannerReady();

        /// <summary>
        /// Show banner.
        /// </summary>
        /// <param name="bannerType">What type of banner need be showed.</param>
        /// <param name="bannerPosition">What position of banner.</param>
        /// <param name="adsPlaceName">What placement from banner was be showed.</param>
        void ShowBanner(BANNER_TYPE bannerType, BANNER_POS bannerPosition, PLACE adsPlaceName);
        
        void ShowBanner(BANNER_TYPE bannerType, BANNER_POS bannerPosition, string placement);

        /// <summary>
        /// Hide all banners on screen.
        /// </summary>
        void HideBanners();
        /// <summary>
        /// Init MERC.
        /// </summary>
        void InitMREC(MaxSdkBase.AdViewPosition pos);
        /// <summary>
        /// Show MERC.
        /// </summary>
        void ShowMREC();
        /// <summary>
        /// Hide MERC.
        /// </summary>
        void HideMREC();

        /// <summary>
        /// Get array of offsets for UI from screen edges
        /// </summary>
        /// <returns>Dicionary of all offsets from screen edges by banner position types.</returns>
        Dictionary<BANNER_POS, int> GetBannerOffset();
        
        /// <summary>
        /// Get array of offsets for UI from screen edges
        /// </summary>
        /// <returns>Dicionary of all offsets from screen edges by banner position types.</returns>
        bool isInitialized();
        
        bool IsSupportAppOpen();
        bool IsAppOpenAdAvailable();
        void ShowAppOpen(string placement);
        void ShowAppOpen(PLACE adsPlaceName);
        void LoadAppOpen();

        string PlatformName { get; }
        
        Rect BannerScreenRect { get; }
    }
}