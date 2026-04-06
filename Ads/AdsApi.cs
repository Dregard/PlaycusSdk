using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Playcus.Iap;
using UnityEngine;
using Playcus.Analytics;
using Playcus;

namespace Playcus.Ads
{
    /// <summary>
    /// Must be used only as IAdsManager. You can find full documentation in IAdsManager.
    /// </summary>   
    [ServiceBind(typeof(IAdsApi))]
    public class AdsApi : ServiceWithConfig, IAdsApi
    {
        // DEPENDENCIES
        [InjectService] private IAnalyticsManager _analyticsManager;
        [InjectService] private IIapManager _iapManager;

        // EVENTS
        public event Action RewardReady;
        public event Action<string> RewardStarted;
        public event Action<string> RewardCompleted;
        public event Action<string> RewardErrorShowed;
        public event Action<string> RewardCanceled;
        public event Action RectChanged;
        public event Action BannerShowed;
        public event Action InterstitialReady;
        public event Action InterstitialShowed;
        public event Action InterstitialClosed;
        public event Action AppOpenReady;
        public event Action AppOpenShowed;
        public event Action AppOpenClosed;

        // CONFIG
        [HelpBox(
            @"SETUP INSTRUCTION 
1. Service must be used only as ServiceLocator.Get<IAdsApi>()."
            , HelpBoxMessageType.Info)]
        [SerializeField]
        private bool _readme;

        protected override Type ConfigType => typeof(AdsApiConfig);
        protected AdsApiConfig Config => (AdsApiConfig) _serviceConfig;

        public string AdsPlatformName => _adsPlatform != null ? _adsPlatform.PlatformName : "undefined";

        // PRIVATE VARIABLES
        private bool _adDisabled;
        private IAdsPlatform _adsPlatform;
        private bool _isAdsShowedNow;
        private bool _isPurchaseInProgressNow;
        private string _currentAdsPlace;
        private Action _currentRewardedOnSuccess;
        private Action _currentRewardedOnFailure;
        private Action _currentInterstitialShowedEvent;
        private Action _currentInterstitialClosedEvent;
        private Action _currentAppOpenShowedEvent;
        private Action _currentAppOpenClosedEvent;
        private bool _bannerShowed;
        private string _currentBannerAdsPlace;
        private int[] _interstitialClickCounters;
        private int[] _interstitialClickCountersFirstSession;
        private bool _appOpenLoaded;
        private DateTime _appOpenLoadedTime = DateTime.Now;

        private enum PREF_KEYS
        {
            ADS_REWARD_DATE_KEY
        }

        protected override async UniTask LoadAsyncInternal(CancellationToken cancellationToken)
        {
            // await base.LoadAsyncInternal(cancellationToken);
            try
            {
                await LoadServiceConfigAsync(cancellationToken);

                if (IsTestEnvironment())
                {
                    ServiceLoadingComplete();
                    return;
                }

                // Try find and init ads network config
                if (Config.Stores.Length > 0)
                {
                    foreach (var store in Config.Stores)
                    {
                        if (store.Store == StoreConstants.GetCurrentStore() && store.AdsPlatformConfigPrefab != null)
                        {
                            GameObject adsPlatformPrefab = Instantiate(store.AdsPlatformConfigPrefab, transform);
                            adsPlatformPrefab.SetActive(true);
                            _adsPlatform = adsPlatformPrefab.GetComponent<IAdsPlatform>();
                        }
                    }
                }

                SubscribeToIapManagerEvents();
                if (_adsPlatform != null)
                {
                    //Manager public events
                    _adsPlatform.RewardReady += OnAnyRewardReady;
                    _adsPlatform.RewardStarted += OnRewardStarted;
                    _adsPlatform.RewardCompleted += OnRewardCompleted;
                    _adsPlatform.RewardErrorShowed += OnRewardErrorShowed;
                    _adsPlatform.RewardCanceled += OnRewardCanceled;

                    _adsPlatform.InterstitialReady += OnAnyInterstitialReady;
                    _adsPlatform.InterstitialShowed += OnInterstitialShowed;
                    _adsPlatform.InterstitialClosed += OnInterstitialClosed;

                    _adsPlatform.AppOpenReady += OnAppOpenReady;
                    _adsPlatform.AppOpenShowed += OnAppOpenShowed;
                    _adsPlatform.AppOpenClosed += OnAppOpenClosed;
                    
                    _adsPlatform.RectChanged += () => { RectChanged?.Invoke(); };

                    //Init platform
                    _adsPlatform.Init(
                        Config.RewardedEnabled,
                        Config.InterstitialEnabled && !_adDisabled, 
                        Config.BannerEnabled && !_adDisabled, 
                        Config.DefaultBannerPosition,
                        Config.AppOpenEnabledOnStart && !_adDisabled);
                }
                else
                {
                    Debug.LogWarning(
                        "AdsService: Can't find IAdsPlatform on object. Please attach valid Ads platform.",
                        gameObject);
                }

                LoadCompleted();
            }
            catch (OperationCanceledException)
            {
                State = ServiceState.Failed;
                return;
            }
            
        }

        private void OnAppOpenClosed()
        {
            _isAdsShowedNow = false;

            AppOpenClosed?.Invoke();
            _currentAppOpenClosedEvent?.Invoke();

            if (_analyticsManager != null)
                _analyticsManager.CustomEvent(AnalyticsEvents.pl_ads_appopen_closed.ToString(), 0,
                    new Dictionary<string, object>()
                    {
                        {AnalyticsProperties.pr_placement.ToString(), _currentAdsPlace},
                        {AnalyticsProperties.pr_ad_network.ToString(), AdsPlatformName}
                    }
                );

            UnmuteAudio();
        }

        private void OnAppOpenReady()
        {
            _appOpenLoaded = true;
            _appOpenLoadedTime = DateTime.Now;
        }
        
        private void OnAppOpenShowed()
        {
            _isAdsShowedNow = true;
            
            AppOpenShowed?.Invoke();
            _currentAppOpenShowedEvent?.Invoke();

            if (_analyticsManager != null)
                _analyticsManager.CustomEvent(AnalyticsEvents.pl_ads_appopen_showed.ToString(), 0,
                    new Dictionary<string, object>()
                    {
                        {AnalyticsProperties.pr_placement.ToString(), _currentAdsPlace},
                        {AnalyticsProperties.pr_ad_network.ToString(), AdsPlatformName}
                    }
                );

            MuteAudio();
        }
        
        private void LoadCompleted()
        {
            Debug.Log("AdsService : LoadCompleted");
            if (State == ServiceState.Initializing)
            {
                ServiceLoadingComplete();
            }
        }

        public bool IsAdsAvailibleOnThisStore()
        {
            return _adsPlatform != null;
        }

        private void OnInterstitialShowed()
        {
            _isAdsShowedNow = true;
            
            InterstitialShowed?.Invoke();
            _currentInterstitialShowedEvent?.Invoke();

            if (_analyticsManager != null)
                _analyticsManager.CustomEvent(AnalyticsEvents.pl_ads_insterstitial_showed.ToString(), 0,
                    new Dictionary<string, object>()
                    {
                        {AnalyticsProperties.pr_placement.ToString(), _currentAdsPlace},
                        {AnalyticsProperties.pr_ad_network.ToString(), AdsPlatformName}
                    }
                );

            MuteAudio();
        }

        private void OnInterstitialClosed()
        {
            _isAdsShowedNow = false;
            
            InterstitialClosed?.Invoke();
            _currentInterstitialClosedEvent?.Invoke();

            if (_analyticsManager != null)
                _analyticsManager.CustomEvent(AnalyticsEvents.pl_ads_insterstitial_closed.ToString(), 0,
                    new Dictionary<string, object>()
                    {
                        {AnalyticsProperties.pr_placement.ToString(), _currentAdsPlace},
                        {AnalyticsProperties.pr_ad_network.ToString(), AdsPlatformName}
                    }
                );

            UnmuteAudio();
        }

        private void OnRewardCanceled()
        {
            if (_isAdsShowedNow)
            {
                _isAdsShowedNow = false;

                RewardCanceled?.Invoke(_currentAdsPlace);
                _currentRewardedOnFailure?.Invoke();

                if (_analyticsManager != null)
                    _analyticsManager.CustomEvent(AnalyticsEvents.pl_ads_rewarded_canceled.ToString(), 0,
                        new Dictionary<string, object>()
                            {{AnalyticsProperties.pr_placement.ToString(), _currentAdsPlace},
                                {AnalyticsProperties.pr_ad_network.ToString(), AdsPlatformName}});

                UnmuteAudio();
            }
        }

        private void OnRewardErrorShowed()
        {
            if (_isAdsShowedNow)
            {
                _isAdsShowedNow = false;

                RewardErrorShowed?.Invoke(_currentAdsPlace);
                _currentRewardedOnFailure?.Invoke();

                UnmuteAudio();
            }
        }

        private void OnRewardCompleted()
        {
            // Status
            _isAdsShowedNow = false;

            // Analytics
            if (_analyticsManager != null)
                _analyticsManager.CustomEvent(AnalyticsEvents.pl_ads_rewarded_complete.ToString(), 0,
                    new Dictionary<string, object>() {{AnalyticsProperties.pr_placement.ToString(), _currentAdsPlace},
                        {AnalyticsProperties.pr_ad_network.ToString(), AdsPlatformName}});

            // Callbacks
            RewardCompleted?.Invoke(_currentAdsPlace);
            _currentRewardedOnSuccess?.Invoke();

            // Audio
            UnmuteAudio();
        }


        private void OnRewardStarted()
        {
            _isAdsShowedNow = true;

            RewardStarted?.Invoke(_currentAdsPlace);

            if (_analyticsManager != null)
                _analyticsManager.CustomEvent(AnalyticsEvents.pl_ads_rewarded_showed.ToString(), 0,
                    new Dictionary<string, object>() {{AnalyticsProperties.pr_placement.ToString(), _currentAdsPlace},
                        {AnalyticsProperties.pr_ad_network.ToString(), AdsPlatformName}});

            MuteAudio();
        }

        private async UniTask SubscribeToIapManagerEvents()
        {
            if (_iapManager == null)
            {
                Debug.LogError("AdsService: IapManager == null",gameObject);
                return;
            }

            await UniTask.WaitUntil(() => _iapManager.IsInitialised);

            _iapManager.PurchaseStarted += OnPurchaseStarted;
            _iapManager.PurchaseSuccess += OnPurchaseSuccess;
            _iapManager.PurchaseFailed += OnPurchaseFailed;
        }

        private void OnPurchaseFailed()
        {
            _isPurchaseInProgressNow = false;
        }

        private void OnPurchaseSuccess(ProductConfig obj)
        {
            _isPurchaseInProgressNow = false;
        }

        private void OnPurchaseStarted()
        {
            _isPurchaseInProgressNow = true;
        }

        public bool IsTestEnvironment()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            using (var actClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                var context = actClass.GetStatic<AndroidJavaObject>("currentActivity");
                var systemGlobal = new AndroidJavaClass("android.provider.Settings$System");
                var testLab =
 systemGlobal.CallStatic<string>("getString", context.Call<AndroidJavaObject>("getContentResolver"), "firebase.test.lab");
                return testLab == "true";
            }
#else
            return false;
#endif
        }

        public void ShowAppOpen(string placement, Action onShowed = null, Action onClosed = null)
        {
            // No ads platform
            if (_adsPlatform == null)
            {
                return;
            }

            Debug.Log("AdsService: ShowAppOpen", gameObject);
            _currentAdsPlace = placement;

            _currentAppOpenShowedEvent = onShowed;
            _currentAppOpenClosedEvent = onClosed;

            if (_adsPlatform.IsAppOpenAdAvailable())
            {
                // _timerAppOpen = Config._appOpenAfterAnotherAdMinSeconds;
                _appOpenLoaded = false;
                _adsPlatform.ShowAppOpen(placement);
                return;
            }

            //If no any ad showed
            onClosed?.Invoke();
            Debug.LogWarning("AdsService: no any appOpen ads ready", gameObject);
        }

        public bool IsInterstitialReady()
        {
            if (State != ServiceState.Ready)
            {
                return false;
            }
            
            // No ads platform
            if (_adsPlatform == null)
            {
                return false;
            }

            return _adsPlatform.IsInterstitialReady();
        }

        private void OnAnyInterstitialReady()
        {
            InterstitialReady?.Invoke();
        }

        public void ShowInterstitial(string adsPlaceName, Action onShowed = null, Action onClosed = null)
        {
            // No ads platform
            if (_adsPlatform == null)
            {
                return;
            }

            Debug.Log("AdsService: ShowInterstitial ", gameObject);
            if (IsTestEnvironment())
            {
                onClosed?.Invoke();
                return;
            }

            if (_isAdsShowedNow)
            {
                Debug.Log("AdsService: ShowInterstitial can't be showed because another ads show now", gameObject);
                return;
            }

            _currentAdsPlace = adsPlaceName;

            _currentInterstitialShowedEvent = onShowed;
            _currentInterstitialClosedEvent = onClosed;

            if (_adsPlatform.IsInterstitialReady())
            {
                _adsPlatform.ShowInterstitial(adsPlaceName);
                return;
            }

            //If no any ad showed
            onClosed?.Invoke();
            Debug.LogWarning("AdsService: no any interstitial ads ready", gameObject);
        }


        public bool IsRewardedReady()
        {
            // InEditor test
            if (Application.isEditor)
            {
                return true;
            }

            if (State != ServiceState.Ready)
            {
                return false;
            }
            
            // No ads platform
            if (_adsPlatform == null)
            {
                return false;
            }

            if (_adsPlatform.IsRewardedReady())
            {
                return true;
            }
            return false;
        }

        private void OnAnyRewardReady()
        {
            RewardReady?.Invoke();
        }

        public void ShowRewarded(string adsPlaceName, Action onSuccess = null, Action onFailure = null)
        {
            // No ads platform
            if (_adsPlatform == null)
            {
                return;
            }

            Debug.Log("AdsService: ShowRewarded : _rewardedEnabled " + Config.RewardedEnabled, gameObject);
            if (IsTestEnvironment())
            {
                onFailure?.Invoke();
                return;
            }

            _currentAdsPlace = adsPlaceName;
            _currentRewardedOnSuccess = onSuccess;
            _currentRewardedOnFailure = onFailure;

            if (Application.isEditor || _adsPlatform.IsRewardedReady())
            {
                // Real device
                if (!Application.isEditor)
                {
                    _adsPlatform.ShowRewarded(adsPlaceName);
                }

                // Emulator
                if (Application.isEditor)
                {
                    OnRewardCompleted();
                }

                return;
            }

            //If no any ad showed
            Debug.Log("AdsService: no any reward ads ready", gameObject);
            if (RewardErrorShowed != null)
                RewardErrorShowed(_currentAdsPlace);
        }


        public bool IsBannerReady()
        {
            if (State != ServiceState.Ready)
            {
                return false;
            }
            
            // No ads platform
            if (_adsPlatform == null)
            {
                return false;
            }

            // Global enabled
            if (!Config.BannerEnabled || _adDisabled)
            {
                Debug.Log("AdsService: IsBannerReady _bannerEnabled false", gameObject);
                return false;
            }

            // network banner ready
            return _adsPlatform.IsBannerReady();
        }


        public void ShowBanner(string placement, BANNER_TYPE bannerType, BANNER_POS bannerPosition)
        {
            // No ads platform
            if (State != ServiceState.Ready || _adsPlatform == null)
            {
                return;
            }

            Debug.Log(
                "AdsService: ShowBanner _bannerEnabled " + Config.BannerEnabled + " adsPlaceName " + placement,
                gameObject);
            // Check ready
            if (!IsBannerReady() || IsTestEnvironment()) return;

            // Try to show
            if (_adsPlatform.IsBannerReady())
            {
                _adsPlatform.ShowBanner(bannerType, bannerPosition, placement);
                _bannerShowed = true;
                _currentBannerAdsPlace = placement;
                BannerShowed?.Invoke();
                if (_analyticsManager != null)
                    _analyticsManager.CustomEvent(AnalyticsEvents.pl_ads_banner_showed.ToString(), 0,
                        new Dictionary<string, object>()
                        {
                            {AnalyticsProperties.pr_placement.ToString(), placement},
                            {AnalyticsProperties.pr_ad_network.ToString(), AdsPlatformName}
                        }
                    );
                return;
            }

            //If no any ad showed
            Debug.LogWarning("AdsService: no any banner ads ready", gameObject);
        }


        public void HideBanners()
        {
            Debug.Log("AdsService: HideBanner", gameObject);


            // No ads platform
            if (_adsPlatform == null)
            {
                return;
            }

            if (IsTestEnvironment()) return;

            _adsPlatform.HideBanners();

            _bannerShowed = false;
        }


        public bool IsBannerShowed()
        {
            return _bannerShowed;
        }


        public string GetCurrentBannerAdsPlace()
        {
            return _currentBannerAdsPlace;
        }


        public Dictionary<BANNER_POS, int> GetBannerOffsets()
        {
            // No ads platform
            if (_adsPlatform == null)
            {
                return null;
            }

            if (_adsPlatform.GetBannerOffset() != null)
            {
                return _adsPlatform.GetBannerOffset();
            }

            return null;
        }

        public void DisableAd()
        {
            Debug.Log($"AdsService: Disable ad");
            _adDisabled = true;
            if (Config != null && Config.BannerEnabled == true)
            {
                HideBanners();
            }
            else
            {
                HideBanners(); 
            }
        }


        public bool IsAdsShowedNow()
        {
            return _isAdsShowedNow;
        }

        private void MuteAudio()
        {
            AudioListener.pause = true;
        }

        private void UnmuteAudio()
        {
            AudioListener.pause = false;
        }

        private void OnDestroy()
        {
            if (_iapManager != null)
            {
                _iapManager.PurchaseStarted -= OnPurchaseStarted;
                _iapManager.PurchaseSuccess -= OnPurchaseSuccess;
                _iapManager.PurchaseFailed -= OnPurchaseFailed;
            }
        }
    }
}