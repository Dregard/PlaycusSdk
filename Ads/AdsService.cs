using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Playcus.Iap;
using UnityEngine;
using Playcus.Analytics;
using Playcus.Utils;
using Playcus;
using UnityEngine.Purchasing;

namespace Playcus.Ads
{
    /// <summary>
    /// Must be used only as IAdsManager. You can find full documentation in IAdsManager.
    /// </summary>   
    [ServiceBind(typeof(IAdsManager))]
    public class AdsService : ServiceWithConfig, IAdsManager
    {
        // DEPENDENCIES
        [InjectService] private IAnalyticsManager _analyticsManager;
        [InjectService] private IUserInfoService _userInfoService;
        [InjectService] private IIapManager _iapManager;

        // EVENTS
        public event Action RewardReady;
        public event Action<PLACE> RewardStarted;
        public event Action<PLACE, string> RewardCompleted;
        public event Action<PLACE> RewardErrorShowed;
        public event Action<PLACE> RewardCanceled;
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
1. Add ads places for every ads type.
2. Places it is places where ads can be showed to player.
3. Places names must be the same that setuped in Ads admin panel (Applovin, Ironsource, etc) 
4. Service must be used only as ServiceLocator.Get<IAdsManager>().
5. Places will be automaticaly sended to analytics and ads platforms."
            , HelpBoxMessageType.Info)]
        [SerializeField]
        private bool _readme;

        protected override Type ConfigType => typeof(AdsServiceConfig);
        protected AdsServiceConfig Config => (AdsServiceConfig) _serviceConfig;

        public string AdsPlatformName => _adsPlatform != null ? _adsPlatform.PlatformName : "undefined";

        public Rect BannerScreenRect => _adsPlatform != null ? _adsPlatform.BannerScreenRect : Rect.zero;

        // PRIVATE VARIABLES
        private bool _adDisabled;
        private IAdsPlatform _adsPlatform;
        private bool _isAdsShowedNow;
        private bool _isPurchaseInProgressNow;
        private float _timerInterstitial;
        private float _timerAppOpen;
        private PLACE _currentAdsPlace;
        private string _currentRewardedCustomReward;
        private Action _currentRewardedOnSuccess;
        private Action _currentRewardedOnFailure;
        private Action _currentInterstitialShowedEvent;
        private Action _currentInterstitialClosedEvent;
        private Action _currentAppOpenShowedEvent;
        private Action _currentAppOpenClosedEvent;
        private bool _bannerShowed;
        private PLACE _currentBannerAdsPlace;
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
            await LoadServiceConfigAsync(cancellationToken);
            try
            {
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
                    _adsPlatform.Init(Config.RewardedEnabled, Config._interstitialEnabled && !_adDisabled,
                        Config._bannerEnabled && !_adDisabled, Config.BannerPosition,Config._appOpenEnabledOnStart && !_adDisabled);
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

                Debug.LogError($"{gameObject.name} Initializing ERROR");
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
                        {AnalyticsProperties.pr_placement.ToString(), _currentAdsPlace.ToString()},
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
                        {AnalyticsProperties.pr_placement.ToString(), _currentAdsPlace.ToString()},
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

        private void Update()
        {
            if (_timerInterstitial > 0f)
            {
                _timerInterstitial -= Time.unscaledDeltaTime;
            }

            if (_timerAppOpen > 0f)
            {
                _timerAppOpen -= Time.unscaledDeltaTime;
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
                        {AnalyticsProperties.pr_placement.ToString(), _currentAdsPlace.ToString()},
                        {AnalyticsProperties.pr_ad_network.ToString(), AdsPlatformName}
                    }
                );

            MuteAudio();
        }

        private void OnInterstitialClosed()
        {
            _isAdsShowedNow = false;
            
            StartInterstitialRestTimer(Config._interstitialInterval);

            var interstitialPlacement = GetInterstitialModel(_currentAdsPlace);

            interstitialPlacement?.UpdateNextIntervalDelayTime();

            InterstitialClosed?.Invoke();
            _currentInterstitialClosedEvent?.Invoke();

            if (_analyticsManager != null)
                _analyticsManager.CustomEvent(AnalyticsEvents.pl_ads_insterstitial_closed.ToString(), 0,
                    new Dictionary<string, object>()
                    {
                        {AnalyticsProperties.pr_placement.ToString(), _currentAdsPlace.ToString()},
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
                            {{AnalyticsProperties.pr_placement.ToString(), _currentAdsPlace.ToString()},
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

            // Interstitial rest timer
            StartInterstitialRestTimer(Config._interstitialIntervalAfterRewarded);

            // Analytics
            if (_analyticsManager != null)
                _analyticsManager.CustomEvent(AnalyticsEvents.pl_ads_rewarded_complete.ToString(), 0,
                    new Dictionary<string, object>() {{AnalyticsProperties.pr_placement.ToString(), _currentAdsPlace.ToString()},
                        {AnalyticsProperties.pr_ad_network.ToString(), AdsPlatformName}});

            // Callbacks
            RewardCompleted?.Invoke(_currentAdsPlace, _currentRewardedCustomReward);
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
                    new Dictionary<string, object>() {{AnalyticsProperties.pr_placement.ToString(), _currentAdsPlace.ToString()},
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

        private void OnPurchaseFailed(string purchaseId, PurchaseFailureReason failureReason)
        {
            _isPurchaseInProgressNow = false;
        }

        private void OnPurchaseSuccess(UnityEngine.Purchasing.Product product)
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

        private void ShowAppOpen(PLACE adsPlaceName, Action onShowed = null, Action onClosed = null)
        {
            // No ads platform
            if (_adsPlatform == null)
            {
                return;
            }

            Debug.Log("AdsService: ShowAppOpen", gameObject);

            if (_timerAppOpen > 0)
            {
                Debug.Log("AdsService: ShowAppOpen can't be showed because enough time has not passed since another ad", gameObject);
                return;
            }

            _currentAdsPlace = adsPlaceName;

            _currentAppOpenShowedEvent = onShowed;
            _currentAppOpenClosedEvent = onClosed;

            if (_adsPlatform.IsAppOpenAdAvailable())
            {
                _timerAppOpen = Config._appOpenAfterAnotherAdMinSeconds;
                _appOpenLoaded = false;
                _adsPlatform.ShowAppOpen(adsPlaceName);
                return;
            }

            //If no any ad showed
            onClosed?.Invoke();
            Debug.LogWarning("AdsService: no any appOpen ads ready", gameObject);
        }

        public bool IsAnyInterstitialReady(PLACE adsPlaceName, bool increaseCounter = false)
        {
            // No ads platform
            if (_adsPlatform == null)
            {
                return false;
            }

            if (IsInterstitialGlobalReady(adsPlaceName, increaseCounter))
            {
                var isInterstitialReady = _adsPlatform.IsInterstitialReady();
                Debug.Log($"AdsService: IsAnyInterstitialReady {isInterstitialReady}", gameObject);

                return isInterstitialReady;
            }

            return false;
        }

        private AdsPlaceInterstitialModel GetInterstitialModel(PLACE adPlace)
        {
            for (int i = 0; i < Config._adsPlacesInterstitial.Length; i++)
            {
                AdsPlaceInterstitialModel place = Config._adsPlacesInterstitial[i];
                if (ConstantsConvert.StringToPlace(place.Name) == adPlace)
                {
                    return place;
                }
            }

            return null;
        }

        private bool HasInterstitialPlaceInterval(PLACE adPlace)
        {
            for (int i = 0; i < Config._adsPlacesInterstitial.Length; i++)
            {
                AdsPlaceInterstitialModel place = Config._adsPlacesInterstitial[i];
                if (ConstantsConvert.StringToPlace(place.Name) == adPlace)
                {
                    if (place.IntervalSeconds > 0)
                    {
                        return true;
                    }
                }
            }

            return false;
        }


        private bool IsInterstitialGlobalReady(PLACE adsPlaceName, bool increaseCounter = false)
        {
            // No ads platform
            if (_adsPlatform == null)
            {
                return false;
            }

            // Global enable
            if (!Config._interstitialEnabled || _adDisabled)
            {
                Debug.Log("AdsService: IsInterstitialGlobalReady _interstitialEnabled false", gameObject);
                return false;
            }

            // Timer limits
            if (_timerInterstitial > 0f)
            {
                Debug.Log("AdsService: IsInterstitialGlobalReady _timerInterstitial not ended", gameObject);
                return false;
            }

            // Placement limits
            bool placeFinded = false;
            for (int i = 0; i < Config._adsPlacesInterstitial.Length; i++)
            {
                AdsPlaceInterstitialModel place = Config._adsPlacesInterstitial[i];
                if (ConstantsConvert.StringToPlace(place.Name) == adsPlaceName)
                {
                    placeFinded = true;
                    if (!place.Enabled)
                        return false;
                    // First session limits
                    if (place.ClicksNeedToShowFirstSession > 0)
                    {
                        if (_interstitialClickCountersFirstSession == null)
                            _interstitialClickCountersFirstSession = new int[Config._adsPlacesInterstitial.Length];
                        if (_interstitialClickCountersFirstSession[i] < place.ClicksNeedToShowFirstSession)
                        {
                            _interstitialClickCountersFirstSession[i]++;
                            return false;
                        }
                    }

                    // Count limits
                    if (place.ClicksNeedToShow > 0)
                    {
                        if (_interstitialClickCounters == null)
                            _interstitialClickCounters = new int[Config._adsPlacesInterstitial.Length];
                        if (_interstitialClickCounters[i] < place.ClicksNeedToShow)
                        {
                            _interstitialClickCounters[i]++;
                            return false;
                        }
                    }

                    //Interval limits
                    if (place.IntervalSeconds > 0f)
                    {
                        return place.CheckIntervalDelay();
                    }
                }
            }

            if (!placeFinded)
            {
                Debug.Log("AdsService: Intertstitial Place with name (" + adsPlaceName + ") was not founded",
                    gameObject);
                return false;
            }

            Debug.Log($"AdsService: IsInterstitialGlobalReady _adDisabled {_adDisabled} return true");

            return true;
        }


        private void OnAnyInterstitialReady()
        {
            InterstitialReady?.Invoke();
        }

        private void StartInterstitialRestTimer(int timerValue)
        {
            if (timerValue > _timerInterstitial)
            {
                _timerInterstitial = timerValue;
            }
        }


        public void ShowInterstitial(PLACE adsPlaceName, Action onShowed = null, Action onClosed = null)
        {
            // No ads platform
            if (_adsPlatform == null)
            {
                return;
            }

            Debug.Log("AdsService: ShowInterstitial ", gameObject);
            if (IsTestEnvironment() || !IsInterstitialGlobalReady(adsPlaceName, true))
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
                _timerAppOpen = Config._appOpenAfterAnotherAdMinSeconds;
                _adsPlatform.ShowInterstitial(adsPlaceName);
                return;
            }

            //If no any ad showed
            onClosed?.Invoke();
            Debug.LogWarning("AdsService: no any interstitial ads ready", gameObject);
        }


        public bool IsAnyRewardedReady(PLACE adsPlaceName)
        {
            // InEditor test
            if (Application.isEditor)
                return true;
            // No ads platform
            if (_adsPlatform == null)
                return false;
            // Placement conditions
            if (!IsGlobalRewardedReady(adsPlaceName))
                return false;
            // Real ads ready ?
            if (_adsPlatform.IsRewardedReady())
                return true;
            return false;
        }


        private bool IsGlobalRewardedReady(PLACE adsPlaceName)
        {
            // No ads platform
            if (_adsPlatform == null)
                return false;

            // Global enable
            if (!Config.RewardedEnabled)
            {
                Debug.Log("AdsService: IsInterstitialReady _rewardedEnabled false", gameObject);
                return false;
            }
            // Placement limits

            bool placeFinded = false;
            foreach (AdsPlaceRewardedModel place in Config._adsPlacesRewarded)
            {
                if (ConstantsConvert.StringToPlace(place.Name) == adsPlaceName)
                {
                    placeFinded = true;
                    if (place.MinutesInterval > 0 &&
                        GetTimeForRewardedReady(ConstantsConvert.StringToPlace(place.Name)).TotalMinutes > 0)
                        return false;
                    if (!place.Enabled)
                        return false;
                }
            }

            if (!placeFinded)
            {
                Debug.Log("AdsService: Reward Place with name (" + adsPlaceName + ") was not founded", gameObject);
                return false;
            }

            return true;
        }


        private void OnAnyRewardReady()
        {
            RewardReady?.Invoke();
        }


        public string GetInterstitialCustomData(PLACE adsPlaceName)
        {
            foreach (var place in Config._adsPlacesInterstitial)
            {
                if (ConstantsConvert.StringToPlace(place.Name) == adsPlaceName)
                {
                    return place.CustomData;
                }
            }

            return "";
        }

        public void ProlongInterstitialIntervalDelayTime(PLACE adsPlace)
        {
            foreach (var place in Config._adsPlacesInterstitial)
            {
                if (ConstantsConvert.StringToPlace(place.Name) == adsPlace)
                {
                    place.UpdateNextIntervalDelayTime();
                    break;
                }
            }
        }

        /// <summary>
        /// Get custom data of the rewarded video
        /// </summary>
        /// <param name="adsPlaceName"></param>
        /// <returns></returns>
        public string GetRewardCustomData(PLACE adsPlaceName)
        {
            foreach (var place in Config._adsPlacesRewarded)
            {
                if (ConstantsConvert.StringToPlace(place.Name) == adsPlaceName)
                {
                    return place.CustomData;
                }
            }

            return "";
        }

        public TimeSpan GetTimeForRewardedReady(PLACE adsPlaceName)
        {
            // Ads place has MinutesInterval value?
            int MinutesInterval = GetAdsPlaceMinutesInterval(adsPlaceName);
            var date = DateTime.UtcNow;
            if (MinutesInterval > 0)
            {
                DateTime lastWatched;
                string prefKey = PREF_KEYS.ADS_REWARD_DATE_KEY.ToString() + adsPlaceName;
                // Interval timer calculate
                if (PlayerPrefs.HasKey(prefKey) && DateUtils.TryParse(PlayerPrefs.GetString(prefKey), out lastWatched))
                {
                    return lastWatched.AddMinutes(MinutesInterval).Subtract(DateTime.Now);
                }
            }

            // default
            return TimeSpan.Zero;
        }


        public void ShowRewarded(PLACE adsPlaceName, string customReward = "", Action onSuccess = null,
            Action onFailure = null)
        {
            // No ads platform
            if (_adsPlatform == null)
            {
                return;
            }

            Debug.Log("AdsService: ShowRewarded : _rewardedEnabled " + Config.RewardedEnabled, gameObject);
            if (!IsGlobalRewardedReady(adsPlaceName) || IsTestEnvironment())
            {
                onFailure?.Invoke();
                return;
            }

            _currentAdsPlace = adsPlaceName;
            _currentRewardedCustomReward = customReward;
            _currentRewardedOnSuccess = onSuccess;
            _currentRewardedOnFailure = onFailure;

            if (Application.isEditor || _adsPlatform.IsRewardedReady())
            {
                // Real device
                if (!Application.isEditor)
                {
                    _adsPlatform.ShowRewarded(adsPlaceName);
                }

                // Write last watch date for limited ads place
                if (GetAdsPlaceMinutesInterval(adsPlaceName) > 0)
                    PlayerPrefs.SetString(PREF_KEYS.ADS_REWARD_DATE_KEY.ToString() + adsPlaceName,
                        DateUtils.Now);
                _timerAppOpen = Config._appOpenAfterAnotherAdMinSeconds;

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


        public bool IsBannerReady(PLACE adsPlaceName)
        {
            // No ads platform
            if (_adsPlatform == null)
            {
                return false;
            }

            // Global enabled
            if (!Config._bannerEnabled || _adDisabled)
            {
                Debug.Log("AdsService: IsBannerReady _bannerEnabled false", gameObject);
                return false;
            }

            // Placement limits
            bool placeFinded = false;
            foreach (AdsPlaceBannerModel place in Config._adsPlacesBanner)
            {
                if (ConstantsConvert.StringToPlace(place.Name) == adsPlaceName)
                {
                    placeFinded = true;
                    if (!place.Enabled)
                        return false;
                    // First session limits
                    if (place.DisabledOnFirstSession && _userInfoService.IsFirstLaunch)
                    {
                        return false;
                    }
                }
            }

            if (!placeFinded)
            {
                Debug.Log("AdsService: Banner Place with name (" + adsPlaceName + ") was not founded", gameObject);
                return false;
            }


            // network banner ready
            if (_adsPlatform.IsBannerReady())
                return true;

            return false;
        }


        public void ShowBanner(PLACE adsPlaceName)
        {
            // No ads platform
            if (_adsPlatform == null)
            {
                return;
            }

            Debug.Log(
                "AdsService: ShowBanner _bannerEnabled " + Config._bannerEnabled + " adsPlaceName " + adsPlaceName,
                gameObject);
            // Check ready
            if (!IsBannerReady(adsPlaceName) || IsTestEnvironment()) return;

            // Configure parameters by placement
            BANNER_TYPE bannerType = BANNER_TYPE.BANNER;
            BANNER_POS bannerPosition = Config.BannerPosition;// BANNER_POS.BOTTOM;

            foreach (AdsPlaceBannerModel place in Config._adsPlacesBanner)
            {
                if (ConstantsConvert.StringToPlace(place.Name) == adsPlaceName)
                {
                    if (!place.Enabled)
                    {
                        return;
                    }
                    else
                    {
                        bannerType = place.BannerType;
                        bannerPosition = place.Position;
                    }
                }
            }

            // Try to show
            if (_adsPlatform.IsBannerReady())
            {
                _adsPlatform.ShowBanner(bannerType, bannerPosition, adsPlaceName);
                _bannerShowed = true;
                _currentBannerAdsPlace = adsPlaceName;
                BannerShowed?.Invoke();
                if (_analyticsManager != null)
                    _analyticsManager.CustomEvent(AnalyticsEvents.pl_ads_banner_showed.ToString(), 0,
                        new Dictionary<string, object>()
                        {
                            {AnalyticsProperties.pr_placement.ToString(), adsPlaceName.ToString()},
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


        public PLACE GetCurrentBannerAdsPlace()
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

        public int GetAdsPlaceMinutesInterval(PLACE adsPlaceName)
        {
            int returned = 0;
            foreach (AdsPlaceRewardedModel place in Config._adsPlacesRewarded)
            {
                if (ConstantsConvert.StringToPlace(place.Name) == adsPlaceName)
                    returned = place.MinutesInterval;
            }

            return returned;
        }

        public void DisableAd()
        {
            Debug.Log($"AdsService: Disable ad");
            _adDisabled = true;
            if (Config != null && Config._bannerEnabled == true)
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