using UnityEngine;
using System;
using Playcus;
// using Playcus.Currency;

namespace Playcus.Ads
{
    public class AdsServiceConfig : ServiceConfig
    {
        [Header("Video Reward")] public bool RewardedEnabled = true;
        public AdsPlaceRewardedModel[] _adsPlacesRewarded;

        [Header("Interstitial")] public bool _interstitialEnabled = true;

        [Tooltip("Interval in seconds between interstitial can be showed")]
        public int _interstitialInterval = 0;

        [Tooltip("Interval in seconds after rewarded ad and before interstitial can be showed")]
        public int _interstitialIntervalAfterRewarded = 2;
        [Tooltip("Confirm Interstitial show Popup name")]
        public string _permitInterstitialPopup;

        public AdsPlaceInterstitialModel[] _adsPlacesInterstitial;

        [Header("Banners")] public bool _bannerEnabled = true;

        [Tooltip("Default banner position")]
        public BANNER_POS BannerPosition = BANNER_POS.BOTTOM;
        
        public AdsPlaceBannerModel[] _adsPlacesBanner;

        [Header("AppOpen")] 
        public int _appOpenAfterAnotherAdMinSeconds = 60;

        public bool _appOpenEnabledOnStart = false;

        public int HOURS_BEFORE_AD_EXPIRE = 4;
        [Header("Configs by store")] public AdsServiceStoreConfig[] Stores;

        [Serializable]
        public class AdsServiceStoreConfig
        {
            public STORE Store;
            public GameObject AdsPlatformConfigPrefab;
        }
    }
}