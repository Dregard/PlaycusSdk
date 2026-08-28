using System;
using System.Collections.Generic;
using Playcus.Analytics;
using UnityEngine;
#if GDPR
    using Playcus.GDPR;
#endif
#if PL_SDK_PLAYCUSDATALAKE_ON
using PlaycusDL;

#endif

#if SDK_DELTADNA
using DeltaDNA;
#endif

#if PL_AMAZON_TAM_ON&&!UNITY_EDITOR
using AmazonAds;
#endif

namespace Playcus.Ads
{
    /// <summary>
    /// For IAdsManager used only! Don't use directly! (Simonenko Alexey)
    /// https://dash.applovin.com/documentation/mediation/unity/getting-started
    /// </summary>
    public class AdsPlatformApplovin : MonoBehaviour, IAdsPlatform
    {
        #region consent

        public static void SetConsent(bool consentGiven)
        {
            MaxSdk.SetHasUserConsent(consentGiven);
        }

        #endregion

        // DEPENDENCIES
        [InjectService]
        private IAnalyticsManager _analyticsManager;

        // EVENTS
        public event Action RectChanged;
        public event Action RewardReady;
        public event Action RewardStarted;
        public event Action RewardCompleted;
        public event Action RewardErrorShowed;
        public event Action RewardCanceled;
        public event Action InterstitialReady;
        public event Action InterstitialShowed;
        public event Action InterstitialClosed;
        public event Action AppOpenReady;
        public event Action AppOpenShowed;
        public event Action AppOpenClosed;
        public event Action MRecAdLoaded;

        public event Action MRecAdLoadFailed;
        //public event Action MRecAdClicked;
        //public event Action MRecAdRevenuePaid;
        //public event Action MRecAdExpanded;
        //public event Action MRecAdCollapsed;

        // EDITOR
        [HelpBox(
            @"Each platform must be configured separately!
Blank unit ID will disable ads type.
SDK Key - one for account."
            , HelpBoxMessageType.Info)]
        [SerializeField]
        private string SDKKey =
            "5AAhiuFzwRBZXL6NRkfMQIFE9TpJ-fX4qinXb1VVTh4_1ANSv1qJJ3TSWLnV_Jaq1LLcMr7rXCqTMC0FDqZXu6";

        [Header("IDs for ads")]
        [SerializeField]
        private string _interstitialUnitID;

        [SerializeField]
        private string _rewardedUnitID;

        [SerializeField]
        private string _bannerUnitID;

        [SerializeField]
        private string _MERCUnitID;

        [SerializeField]
        private string _appOpenUnitID;

        [Header("Other ad settings")]
        [SerializeField]
        private Color _bannerBackgroundColor = Color.black;

        [Header("TAM AMAZON")]
        [SerializeField]
        private string appId;

        [SerializeField]
        private string amazonBannerSlotId; //320x50

        [SerializeField]
        private string amazonInterstitialSlotId;

        [SerializeField]
        private string amazonInterstitialVideoSlotId;

        [SerializeField]
        private string amazonRewardedVideoSlotId;

#if PL_AMAZON_TAM_ON && !UNITY_EDITOR
        private bool _isFirstInterstitialRequest = true;
        private bool _isFirstIRewardedRequest = true;
        private APSBannerAdRequest _bannerAdRequest;
        private APSInterstitialAdRequest _interstitialAdRequest;
        private APSVideoAdRequest _rewardedVideoAdRequest;
#endif

        public string PlatformName => "Applovin";

#if PL_SDK_APPLOVIN_ON //&& (UNITY_IOS || UNITY_ANDROID||UNITY_STANDALONE_OSX||UNITY_) 
        // PRIVATE
        private bool _isInitialized;

        private BANNER_POS _bannerShowedPosition = BANNER_POS.NONE;
        private bool _bannerShowed;


        private Action _delayedAction;

        public Rect BannerScreenRect
        {
            get
            {
                if (string.IsNullOrEmpty(_bannerUnitID))
                    return new Rect(0, Screen.height, Screen.width, 0);

                var density = MaxSdkUtils.GetScreenDensity();
                var rect = MaxSdk.GetBannerLayout(_bannerUnitID);
                rect.height = Mathf.Max(rect.height, MaxSdkUtils.GetAdaptiveBannerHeight());
                rect.y = rect.y > 0f ? rect.y : Screen.height / density - rect.height;
                rect.min *= density;
                rect.max *= density;
                return rect;
            }
        }

        public bool isInitialized()
        {
            return _isInitialized;
        }

        public void Init(bool rewardedEnabled, bool interstitialEnabled, bool bannerEnabled, BANNER_POS bannerPos, bool appOpenEnabled)
        {

            if (_isInitialized)
                return;

            ServiceLocator.ResolveServicesInComponent(this);

// #if UNITY_ANDROID
//             //Setting verbose logging level may prevent rare android crashes related to null pointer reference
//             MaxSdk.SetVerboseLogging(true);
// #endif
            
#if PL_AMAZON_TAM_ON&&!UNITY_EDITOR
            if (!string.IsNullOrEmpty(appId))
            {
                Amazon.Initialize(appId);
#if DEVELOPMENT_BUILD
                Amazon.EnableTesting(true);
                Amazon.EnableLogging(true);
#else
                Amazon.EnableTesting(false);
                Amazon.EnableLogging(false);
#endif
                Amazon.UseGeoLocation(false);
                // Amazon.IsLocationEnabled();
                Amazon.SetMRAIDPolicy(Amazon.MRAIDPolicy.CUSTOM);
                Amazon.SetAdNetworkInfo(new AdNetworkInfo(DTBAdNetwork.MAX));
                Amazon.SetMRAIDSupportedVersions(new string[] { "1.0", "2.0", "3.0" }); 
            }
#endif
            
            // You should start listening to the sdk initialized event before initializing the sdk.
            MaxSdkCallbacks.OnSdkInitializedEvent += (MaxSdkBase.SdkConfiguration sdkConfiguration) =>
            {
                InitAds(rewardedEnabled, interstitialEnabled, bannerEnabled, bannerPos, appOpenEnabled, sdkConfiguration);
            };

            // You should set the SDK key and initialize the MAX SDK as soon as your app launches.
            if (!string.IsNullOrEmpty(SDKKey))
            {
#if GDPR
                // GDPR flow
                var usercentricsService = ServiceLocator.Get<UsercentricsConsentService>();
                if (usercentricsService != null && usercentricsService.IsGDPRAccepted())
                    MaxSdk.SetHasUserConsent(true);
#endif
  
#if USERCENTRICS_CONSENT
                // With Usercentrics settings, now user can only ACCEPT ALL or leave app, so no need to check if it accepted
                MaxSdk.SetHasUserConsent(true);
#endif
                
                // SDK initialization
                MaxSdk.SetSdkKey(SDKKey);

                var userId = "";
#if SDK_DELTADNA
                if (DDNA.Instance != null && !string.IsNullOrEmpty(DDNA.Instance.UserID))
                {
                    userId = DDNA.Instance.UserID;
                }
                else
                {
                    Debug.Log($"ApplovinAdsPlatform - can't use DDNA.Instance.UserID");
                }
#elif PL_SDK_PLAYCUSDATALAKE_ON
                var userInfoService = ServiceLocator.Get<IUserInfoService>();

                if (userInfoService != null)
                {
                    userId = userInfoService.UserID;
                }
                else
                {
                    Debug.Log($"ApplovinAdsPlatform - can't use IUserInfoService.UserID");
                }
#endif
                if (string.IsNullOrEmpty(userId))
                {
                    userId = SystemInfo.deviceUniqueIdentifier;
                }

                SetUserId(userId);

                MaxSdk.InitializeSdk();
                Debug.Log("ApplovinAdsPlatform Initialized");
            }
            else
            {
                Debug.LogError("ApplovinAdsPlatform SDKKey must be setup!");
            }
        }

        private async UniTask InitAds(bool rewardedEnabled, bool interstitialEnabled, bool bannerEnabled, BANNER_POS bannerPos,
            bool appOpenEnabled, MaxSdkBase.SdkConfiguration sdkConfiguration)
        {
            // Check for platform configuration
            if (string.IsNullOrEmpty(_interstitialUnitID + _rewardedUnitID + _bannerUnitID))
            {
                Debug.LogWarning(
                    $"ApplovinAdsPlatform No any UnitID setup for this platform - {Application.platform.ToString()}");
            }

#if UNITY_IOS || UNITY_IPHONE || UNITY_EDITOR
            if (MaxSdkUtils.CompareVersions(UnityEngine.iOS.Device.systemVersion, "14.5") !=
                MaxSdkUtils.VersionComparisonResult.Lesser)
            {
                // Note that App transparency tracking authorization can be checked via `sdkConfiguration.AppTrackingStatus` for Unity Editor and iOS targets
                AudienceNetwork.AdSettings.SetAdvertiserTrackingEnabled(
                    sdkConfiguration.AppTrackingStatus.Equals(MaxSdkBase.AppTrackingStatus.Authorized));
            }
#endif
            MaxSdk.SetExtraParameter("return_audio_focus", "true");
            
            // potential fix by https://github.com/AppLovin/AppLovin-MAX-Unity-Plugin/issues/362
            MaxSdk.SetExtraParameter("pisw", "true");

            // AppLovin SDK is initialized, start loading ads

            if (!string.IsNullOrEmpty(_bannerUnitID) && bannerEnabled)
            {
                InitializeBannerAds(bannerPos);
            }
            else
            {
                Debug.LogWarning(
                    $"ApplovinAdsPlatform No bannerUnitID setup for this platform - {Application.platform.ToString()}");
            }

            //if (!string.IsNullOrEmpty(_MERCUnitID))
            //{
            //    InitializeMERCAds();
            //}
            //else
            //{
            //    Debug.LogWarning(
            //        $"ApplovinAdsPlatform No bannerUnitID setup for this platform - {Application.platform.ToString()}");
            //}
            // Attaching the interstitial callback here because AppOpen is actually an interstitial too
            if ((!string.IsNullOrEmpty(_interstitialUnitID) && interstitialEnabled)
                || (!string.IsNullOrEmpty(_appOpenUnitID) && appOpenEnabled))
            {
                AttachInterstitialCallback();
            }
            
            // Attaching the rewarded callback
            if (!string.IsNullOrEmpty(_rewardedUnitID) && rewardedEnabled)
            {
                AttachRewardedCallback();
            }
            
            
            // The initialization is complete, as all necessary callbacks are subscribed. The next step is to load ads.
            _isInitialized = true;

            if (!string.IsNullOrEmpty(_appOpenUnitID) && appOpenEnabled)
            {
                InitializeAppOpenAds();
                
                var cts = new CancellationTokenSource(); 
                cts.CancelAfterSlim(TimeSpan.FromSeconds(10));  
                
                try
                {
                    await UniTask.WaitUntil((() => MaxSdk.IsInterstitialReady(_appOpenUnitID)),
                        cancellationToken: cts.Token); 
                }
                catch (OperationCanceledException ex)
                {
                    if (ex.CancellationToken == cts.Token)
                    {
                        Debug.Log("AppOpen load Timeout");
                    }
                }
            }
            else
            {
                Debug.LogWarning(
                    $"ApplovinAdsPlatform No appOpenUnitID setup for this platform - {Application.platform.ToString()}");
            }

            if (!string.IsNullOrEmpty(_interstitialUnitID) && interstitialEnabled)
            {
                InitializeInterstitialAds();
            }
            else
            {
                Debug.LogWarning(
                    $"ApplovinAdsPlatform No interstitialUnitID setup for this platform - {Application.platform.ToString()}");
            }

            if (!string.IsNullOrEmpty(_rewardedUnitID) && rewardedEnabled)
            {
                InitializeRewardedAds();
            }
            else
            {
                Debug.LogWarning(
                    $"ApplovinAdsPlatform No rewardedUnitID setup for this platform - {Application.platform.ToString()}");
            }

        }

        private void AttachInterstitialCallback()
        {
            Debug.Log("ApplovinAdsPlatform AttachInterstitialCallback");

            MaxSdkCallbacks.Interstitial.OnAdLoadedEvent += OnInterstitialAdLoadedEvent;
            MaxSdkCallbacks.Interstitial.OnAdLoadFailedEvent += OnInterstitialAdLoadFailedEvent;
            MaxSdkCallbacks.Interstitial.OnAdDisplayFailedEvent += OnInterstitialAdDisplayFailedEvent;
            MaxSdkCallbacks.Interstitial.OnAdHiddenEvent += OnInterstitialAdHiddenEvent;
            MaxSdkCallbacks.Interstitial.OnAdRevenuePaidEvent += OnAdRevenuePaidEvent;   
        }

        private void OnInterstitialAdHiddenEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            if (!string.IsNullOrEmpty(_interstitialUnitID) && adUnitId.Equals(_interstitialUnitID))
            {
                Debug.Log("ApplovinAdsPlatform OnInterstitialDismissedEvent", gameObject);
                // Interstitial ad is hidden. Pre-load the next ad
                LoadInterstitial();
                if (InterstitialClosed != null)
                {
                    _delayedAction += InterstitialClosed;
                }
                if (RectChanged != null) _delayedAction += RectChanged;
            }
            else if (!string.IsNullOrEmpty(_appOpenUnitID) && adUnitId.Equals(_appOpenUnitID))
            {
                Debug.Log("ApplovinAdsPlatform OnAppOpenDismissedEvent", gameObject);
                // AppOpen ad is hidden. Pre-load the next ad
                LoadAppOpen();
                if (AppOpenClosed != null)
                {
                    _delayedAction += AppOpenClosed;
                }
                if (RectChanged != null) _delayedAction += RectChanged;
            }
        }

        private void OnInterstitialAdDisplayFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo)
        {
            if (!string.IsNullOrEmpty(_interstitialUnitID) && adUnitId.Equals(_interstitialUnitID))
            {
                Debug.Log("ApplovinAdsPlatform InterstitialFailedToDisplayEvent", gameObject);
                // Interstitial ad failed to display. We recommend loading the next ad
                LoadInterstitial();
            }
            else if (!string.IsNullOrEmpty(_appOpenUnitID) && adUnitId.Equals(_appOpenUnitID))
            {
                Debug.Log("ApplovinAdsPlatform AppOpenFailedToDisplayEvent", gameObject);
                // Interstitial ad failed to display. We recommend loading the next ad
                LoadAppOpen();
            }
        }

        private void OnInterstitialAdLoadFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
        {
            if (!string.IsNullOrEmpty(_interstitialUnitID) && adUnitId.Equals(_interstitialUnitID))
            {
                Debug.Log("ApplovinAdsPlatform OnInterstitialFailedEvent", gameObject);
                // Interstitial ad failed to load. We recommend re-trying in 3 seconds.
                Invoke(nameof(LoadInterstitial), 3);
            }
            else if (!string.IsNullOrEmpty(_appOpenUnitID) && adUnitId.Equals(_appOpenUnitID))
            {
                Debug.Log("ApplovinAdsPlatform OnAppOpenFailedEvent", gameObject);
                // Interstitial ad failed to load. We recommend re-trying in 3 seconds.
                Invoke(nameof(LoadAppOpen), 3);
            }
        }

        public virtual void SetUserId(string userId)
        {
            MaxSdk.SetUserId(userId);
        }

        void Update()
        {
            if (_delayedAction != null && !_invokig)
            {
                StartCoroutine(_delayedInvoke());
            }
        }

        private bool _invokig = false;

        private IEnumerator _delayedInvoke()
        {
            _invokig = true;
            yield return new WaitForSecondsRealtime(.2f);
            _delayedAction.Invoke();
            _delayedAction = null;
            _invokig = false;
        }

        // INTERSTITIAL

        public void Init(bool rewardedEnabled, bool interstitialEnabled, bool bannerEnabled, bool appOpenEnabled)
        {
            throw new NotImplementedException();
        }

        public bool IsInterstitialReady()
        {
            return !string.IsNullOrEmpty(_interstitialUnitID) && MaxSdk.IsInterstitialReady(_interstitialUnitID);
        }

        public void InitializeInterstitialAds()
        {
            Debug.Log("ApplovinAdsPlatform InitializeInterstitialAds");

#if PL_AMAZON_TAM_ON&&!UNITY_EDITOR
     if (_isFirstInterstitialRequest && !string.IsNullOrEmpty(amazonInterstitialSlotId)) 
     {
            _isFirstInterstitialRequest = false;
            _interstitialAdRequest = new APSInterstitialAdRequest(amazonInterstitialSlotId);

            _interstitialAdRequest.onSuccess += (adResponse) =>
            {
                MaxSdk.SetInterstitialLocalExtraParameter(_interstitialUnitID, "amazon_ad_response", adResponse.GetResponse());
                LoadInterstitial(); 
            };
            _interstitialAdRequest.onFailedWithError += (adError) =>
            {
                MaxSdk.SetInterstitialLocalExtraParameter(_interstitialUnitID, "amazon_ad_error", adError.GetAdError());
                LoadInterstitial(); 
            };

            _interstitialAdRequest.LoadAd();
     } else {
         LoadInterstitial(); 
     }       
#else
            // Load the first interstitial
            LoadInterstitial();
#endif
        }

        private void LoadInterstitial()
        {
            Debug.Log("ApplovinAdsPlatform LoadInterstitial");
            if (string.IsNullOrEmpty(_interstitialUnitID)) return;
            MaxSdk.LoadInterstitial(_interstitialUnitID);
        }

        public void ShowInterstitial(PLACE adsPlaceName)
        {
            ShowInterstitial(adsPlaceName.ToString());
        }
        
        public void ShowInterstitial(string adsPlaceName)
        {
            Debug.Log("ApplovinAdsPlatform try ShowInterstetial", gameObject);
            if (MaxSdk.IsInterstitialReady(_interstitialUnitID))
            {
                InterstitialShowed?.Invoke();
                MaxSdk.ShowInterstitial(_interstitialUnitID, adsPlaceName.ToString());
            }
            else
            {
                Debug.LogWarning("ApplovinAdsPlatform Interstitial is not Ready but called ShowInterstetial!",
                    gameObject);
                InterstitialClosed?.Invoke();
            }
        }


        // REWARDED

        private void AttachRewardedCallback()
        {
            Debug.Log("ApplovinAdsPlatform AttachRewardedCallback");
            
            // Attach callback
            MaxSdkCallbacks.Rewarded.OnAdLoadedEvent += OnRewardedAdLoadedEvent;
            MaxSdkCallbacks.Rewarded.OnAdLoadFailedEvent += OnRewardedAdFailedEvent;
            MaxSdkCallbacks.Rewarded.OnAdDisplayFailedEvent += OnRewardedAdFailedToDisplayEvent;
            MaxSdkCallbacks.Rewarded.OnAdDisplayedEvent += OnRewardedAdDisplayedEvent;
            MaxSdkCallbacks.Rewarded.OnAdClickedEvent += OnRewardedAdClickedEvent;
            MaxSdkCallbacks.Rewarded.OnAdHiddenEvent += OnRewardedAdDismissedEvent;
            MaxSdkCallbacks.Rewarded.OnAdReceivedRewardEvent += OnRewardedAdReceivedRewardEvent;
            MaxSdkCallbacks.Rewarded.OnAdRevenuePaidEvent += OnAdRevenuePaidEvent;
        }

        public void InitializeRewardedAds()
        {
            Debug.Log("ApplovinAdsPlatform InitializeRewardedAds", gameObject);
#if PL_AMAZON_TAM_ON && !UNITY_EDITOR
            if (!string.IsNullOrEmpty(amazonRewardedVideoSlotId))
            {
                if(_isFirstIRewardedRequest){
                    // APS LoadAd only needs to be called once.
                    _isFirstIRewardedRequest = false;
                    _rewardedVideoAdRequest = new APSVideoAdRequest(320, 480, amazonRewardedVideoSlotId);
                    _rewardedVideoAdRequest.onSuccess += (adResponse) => {
                        MaxSdk.SetRewardedAdLocalExtraParameter(amazonRewardedVideoSlotId, "amazon_ad_response", adResponse.GetResponse());
                        LoadRewardedAd();
                    };
                    _rewardedVideoAdRequest.onFailedWithError += (adError) => {
                        MaxSdk.SetRewardedAdLocalExtraParameter(amazonRewardedVideoSlotId, "amazon_ad_error", adError.GetAdError());
                        LoadRewardedAd();
                    };
                    _rewardedVideoAdRequest.LoadAd();
                } else {
                    LoadRewardedAd();
                }
            }
            else
            {
                LoadRewardedAd();
            }
           
#else
            // Load the first RewardedAd
            LoadRewardedAd();
#endif
        }

        private void LoadRewardedAd()
        {
            Debug.Log("ApplovinAdsPlatform LoadRewardedAd", gameObject);
            if (string.IsNullOrEmpty(_rewardedUnitID)) return;
            MaxSdk.LoadRewardedAd(_rewardedUnitID);
        }

        private void OnRewardedAdLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            Debug.Log("ApplovinAdsPlatform OnRewardedAdLoadedEvent", gameObject);
            // Rewarded ad is ready to be shown. MaxSdk.IsRewardedAdReady(rewardedAdUnitId) will now return 'true'
            if (RewardReady != null) _delayedAction += RewardReady;
        }

        private void OnRewardedAdFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
        {
            Debug.Log("ApplovinAdsPlatform OnRewardedAdFailedEvent", gameObject);
            // Rewarded ad failed to load. We recommend re-trying in 3 seconds.
            Invoke(nameof(LoadRewardedAd), 3);
        }

        private void OnRewardedAdFailedToDisplayEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo arg3)
        {
            Debug.Log("ApplovinAdsPlatform OnRewardedAdFailedToDisplayEvent", gameObject);
            // Rewarded ad failed to display. We recommend loading the next ad
            LoadRewardedAd();
            if (RewardErrorShowed != null) _delayedAction += RewardErrorShowed;
        }

        private void OnRewardedAdDisplayedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
        }

        private void OnRewardedAdClickedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
        }

        private void OnRewardedAdDismissedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            Debug.Log("ApplovinAdsPlatform OnRewardedAdDismissedEvent", gameObject);
            // Rewarded ad is hidden. Pre-load the next ad
            LoadRewardedAd();
            if (RewardCanceled != null) _delayedAction += RewardCanceled;
            if (RectChanged != null) _delayedAction += RectChanged;
        }

        private void OnRewardedAdReceivedRewardEvent(string adUnitId, MaxSdk.Reward reward, MaxSdkBase.AdInfo arg3)
        {
            Debug.Log("ApplovinAdsPlatform OnRewardedAdReceivedRewardEvent", gameObject);
            // Rewarded ad was displayed and user should receive the reward
            if (RewardCompleted != null) _delayedAction += RewardCompleted;
            if (RectChanged != null) _delayedAction += RectChanged;
        }

        public bool IsRewardedReady()
        {
            return !string.IsNullOrEmpty(_rewardedUnitID) && MaxSdk.IsRewardedAdReady(_rewardedUnitID);
        }


        public void ShowRewarded(PLACE adsPlaceName)
        {
            ShowRewarded(adsPlaceName.ToString());
        }
        
        public void ShowRewarded(string adsPlaceName)
        {
            Debug.Log("ApplovinAdsPlatform ShowRewardedAd", gameObject);
            if (string.IsNullOrEmpty(_rewardedUnitID)) return;
            if (MaxSdk.IsRewardedAdReady(_rewardedUnitID))
            {
                RewardStarted?.Invoke();
                MaxSdk.ShowRewardedAd(_rewardedUnitID, adsPlaceName.ToString());
            }
        }


        // BANNERS

        public bool IsBannerReady()
        {
            return true;
        }
        public void InitMREC(MaxSdkBase.AdViewPosition pos)
        {
            if (string.IsNullOrEmpty(_MERCUnitID))
            {
                Debug.LogError("AdsPlatformApplovin.InitMREC _MERCUnitID IsNullOrEmpty");
                return;
            }
                MaxSdk.CreateMRec(_MERCUnitID, pos);
            MaxSdkCallbacks.MRec.OnAdLoadedEvent += OnMRecAdLoadedEvent;
            MaxSdkCallbacks.MRec.OnAdLoadFailedEvent += OnMRecAdLoadFailedEvent;
            MaxSdkCallbacks.MRec.OnAdClickedEvent += OnMRecAdClickedEvent;
            MaxSdkCallbacks.MRec.OnAdRevenuePaidEvent += OnMRecAdRevenuePaidEvent;
            MaxSdkCallbacks.MRec.OnAdExpandedEvent += OnMRecAdExpandedEvent;
            MaxSdkCallbacks.MRec.OnAdCollapsedEvent += OnMRecAdCollapsedEvent;
        }
        public void OnMRecAdLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo) 
        {
            Debug.Log("ApplovinAdsPlatform OnMRecAdLoadedEvent", gameObject);
           if(MRecAdLoaded!=null) MRecAdLoaded.Invoke();
        }
        public void OnMRecAdLoadFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo error) 
        {
            Debug.Log("ApplovinAdsPlatform OnMRecAdLoadFailedEvent", gameObject);
            if (MRecAdLoadFailed != null) MRecAdLoadFailed.Invoke();
        }
        public void OnMRecAdClickedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo) 
        {
            Debug.Log("ApplovinAdsPlatform OnMRecAdClickedEvent", gameObject);
        }
        public void OnMRecAdRevenuePaidEvent(string adUnitId, MaxSdkBase.AdInfo adInfo) 
        {
            Debug.Log("ApplovinAdsPlatform OnMRecAdRevenuePaidEvent", gameObject);
        }
        public void OnMRecAdExpandedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo) 
        {
            Debug.Log("ApplovinAdsPlatform OnMRecAdExpandedEvent", gameObject);
        }
        public void OnMRecAdCollapsedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo) 
        {
            Debug.Log("ApplovinAdsPlatform OnMRecAdCollapsedEvent", gameObject);
        }
        public void ShowMREC()
        {
            MaxSdk.ShowMRec(_MERCUnitID);
        }
        public void HideMREC()
        {
            MaxSdk.HideMRec(_MERCUnitID);
        }
        public void InitializeBannerAds(BANNER_POS bannerPos)
        {
#if PL_AMAZON_TAM_ON&&!UNITY_EDITOR
            Debug.LogError("Amazon initialized " + Amazon.IsInitialized());
            if (!string.IsNullOrEmpty(amazonBannerSlotId))
            {
                if (_bannerAdRequest != null) _bannerAdRequest.DestroyFetchManager();
                _bannerAdRequest = new APSBannerAdRequest(320, 50, amazonBannerSlotId);
                //impossible to make 2 sizes of banner with one slotId MaxSdkUtils.IsTablet()?728:320, MaxSdkUtils.IsTablet()?90:50, amazonBannerSlotId);
                _bannerAdRequest.onFailedWithError += async (adError) => {
                    await UniTask.SwitchToMainThread();
                    Debug.LogError($"Amazon InitializeBanner onFailedWithError: {adError.GetCode()} - {adError.GetMessage()}");

                   MaxSdk.SetBannerLocalExtraParameter(amazonBannerSlotId, "amazon_ad_error", adError.GetAdError());
                    CreateMaxBannerAd(bannerPos);
                };
                _bannerAdRequest.onSuccess += async (adResponse) => {
                    await UniTask.SwitchToMainThread();
                    Debug.LogError("Amazon InitializeBanner onSuccess");
                    MaxSdk.SetBannerLocalExtraParameter(amazonBannerSlotId, "amazon_ad_response", adResponse.GetResponse());
                    CreateMaxBannerAd(bannerPos);
                };
     
                _bannerAdRequest.LoadAd();  
            }
            else
            {
                CreateMaxBannerAd(bannerPos);
            }
#else
            CreateMaxBannerAd(bannerPos);
#endif
        }

        private void CreateMaxBannerAd(BANNER_POS bannerPos)
        {
            // Banners are automatically sized to 320x50 on phones and 728x90 on tablets
            // You may use the utility method `MaxSdkUtils.isTablet()` to help with view sizing adjustments
            var pos = bannerPos == BANNER_POS.BOTTOM
                ? MaxSdkBase.BannerPosition.BottomCenter
                : MaxSdkBase.BannerPosition.TopCenter;
            MaxSdk.CreateBanner(_bannerUnitID, pos);

            MaxSdkCallbacks.Banner.OnAdLoadFailedEvent += OnBannerAdLoadFailed;
            MaxSdkCallbacks.Banner.OnAdLoadedEvent += OnBannerAdLoaded;
            MaxSdkCallbacks.Banner.OnAdRevenuePaidEvent += OnAdRevenuePaidEvent;

            // Set background or background color for banners to be fully functional
            MaxSdk.SetBannerBackgroundColor(_bannerUnitID, _bannerBackgroundColor);
            Debug.Log("ApplovinAdsPlatform InitializeBannerAds success", gameObject);

            MaxSdk.LoadBanner(_bannerUnitID);
        }
        
        public void ShowBanner(BANNER_TYPE bannerType, BANNER_POS bannerPosition, PLACE adsPlaceName)
        {
            ShowBanner(bannerType, bannerPosition, adsPlaceName.ToString());
        }

        public void ShowBanner(BANNER_TYPE bannerType, BANNER_POS bannerPosition, string adsPlaceName)
        {
            Debug.Log("ApplovinAdsPlatform ShowBanner ", gameObject);
            if (string.IsNullOrEmpty(_bannerUnitID))
            {
                Debug.LogWarning("ApplovinAdsPlatform ShowBanner _bannerUnitID is null");
                return;
            }

            Debug.Log(
                $"ApplovinAdsPlatform MaxSdk.SetBannerPlacement - unitId:{_bannerUnitID}, adsPlaceName:{adsPlaceName.ToString()}");
            MaxSdk.SetBannerPlacement(_bannerUnitID, adsPlaceName.ToString());
            MaxSdk.ShowBanner(_bannerUnitID);
            _bannerShowedPosition = bannerPosition;
            _bannerShowed = true;
            Debug.Log("ApplovinAdsPlatform _bannerShowed true");
            if (RectChanged != null) _delayedAction += RectChanged;
        }

        private void OnBannerAdLoaded(string obj, MaxSdkBase.AdInfo adInfo)
        {
            //throw new NotImplementedException();
            Debug.LogWarning($"OnBannerAdLoaded: {obj}");
        }

        private void OnBannerAdLoadFailed(string obj, MaxSdkBase.ErrorInfo errorInfo)
        {
            Debug.LogWarning($"OnBannerAdLoadFailed: {obj} {errorInfo.AdLoadFailureInfo}");
        }

        public void HideBanners()
        {
            Debug.Log("ApplovinAdsPlatform HideBanners ", gameObject);
            if (string.IsNullOrEmpty(_bannerUnitID)) return;
            MaxSdk.HideBanner(_bannerUnitID);
            _bannerShowedPosition = BANNER_POS.NONE;
            if (RectChanged != null) _delayedAction += RectChanged;
        }

        public Dictionary<BANNER_POS, int> GetBannerOffset()
        {
            // Banners are automatically sized to 320x50 on phones and 728x90 on tablets
            // You may use the utility method `MaxSdkUtils.IsTablet()` to help with view sizing adjustments
            // int bannerSize = Application.isEditor ? 65 : MaxSdkUtils.IsTablet() ? 100 : 65;
            int bannerSize = (int)MaxSdkUtils.GetAdaptiveBannerHeight();
//
// #if UNITY_EDITOR
//             bannerSize = bannerSize * 5;
// #endif

            int bannerHeight = Mathf.FloorToInt((Screen.dpi / 160f) * (float) (bannerSize + 5));
            var dictResponse = new Dictionary<BANNER_POS, int>()
            {
                {BANNER_POS.TOP, _bannerShowedPosition == BANNER_POS.TOP && _bannerShowed ? bannerHeight : 0},
                {BANNER_POS.BOTTOM, _bannerShowedPosition == BANNER_POS.BOTTOM && _bannerShowed ? bannerHeight : 0}
            };
            return dictResponse;
        }


        // GENERIC

        public void InitializeAppOpenAds()
        {
            Debug.Log("ApplovinAdsPlatform InitializeAppOpenAds");

            // Load the first appOpen
            LoadAppOpen();
        }

        private void OnInterstitialAdLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            if (!string.IsNullOrEmpty(_interstitialUnitID) && adUnitId.Equals(_interstitialUnitID))
            {
                Debug.Log("ApplovinAdsPlatform OnInterstitialLoadedEvent", gameObject);
                // Interstitial ad is ready to be shown. MaxSdk.IsInterstitialReady(interstitialAdUnitId) will now return 'true'
                if (InterstitialReady != null) _delayedAction += InterstitialReady;
            }
            else if (!string.IsNullOrEmpty(_appOpenUnitID) && adUnitId.Equals(_appOpenUnitID))
            {
                Debug.Log("ApplovinAdsPlatform OnAppOpenLoadedEvent", gameObject);
                // AppOpen ad is ready to be shown. MaxSdk.IsInterstitialReady(interstitialAdUnitId) will now return 'true'
                if (AppOpenReady != null) _delayedAction += AppOpenReady;
            }
        }

        public bool IsSupportAppOpen()
        {
            return true;
        }

        public bool IsAppOpenAdAvailable()
        {
            return !string.IsNullOrEmpty(_appOpenUnitID) && MaxSdk.IsInterstitialReady(_appOpenUnitID);
        }

        private void OnAdRevenuePaidEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            //Note: The value of Revenue may be -1 in the case of an error.
            double revenue = adInfo.Revenue;
            Debug.Log(
                $"Applovin: OnAdRevenuePaidEvent : revenue:{revenue} NetworkName:{adInfo.NetworkName} AdFormat:{adInfo.AdFormat} Placement:{adInfo.Placement}");
            if (revenue > 0)
            {
                _analyticsManager.AdRevenue("USD", revenue, adInfo.NetworkName, adInfo.AdFormat, adInfo.Placement);
            }
        }

        public void ShowAppOpen(PLACE adsPlaceName)
        {
            ShowAppOpen(adsPlaceName.ToString());
        }
        
        public void ShowAppOpen(string adsPlaceName)
        {
            Debug.Log("ApplovinAdsPlatform try ShowAppOpen", gameObject);
            if (MaxSdk.IsInterstitialReady(_appOpenUnitID))
            {
                AppOpenShowed?.Invoke();
                MaxSdk.ShowInterstitial(_appOpenUnitID, adsPlaceName.ToString());
            }
            else
            {
                Debug.LogWarning("ApplovinAdsPlatform AppOpen is not Ready but called v!",
                    gameObject);
                AppOpenClosed?.Invoke();
            }
        }
        
        public void LoadAppOpen()
        {
            Debug.Log("ApplovinAdsPlatform LoadAppOpen(Interstitial, actually)");
            if (string.IsNullOrEmpty(_appOpenUnitID)) return;
            MaxSdk.LoadInterstitial(_appOpenUnitID);
        }

#else
        public Rect BannerScreenRect => Rect.zero;

        public bool isInitialized()
        {
            DebugMessage();
            return false;
        }

        public void Init(bool rewardedEnabled, bool interstitialEnabled, bool bannerEnabled, BANNER_POS bannerPos, bool appOpenEnabled)
        {
            DebugMessage();
        }

        public bool IsInterstitialReady()
        {
            DebugMessage();
            return true;
        }

        public void ShowInterstitial(PLACE adsPlaceName)
        {
            DebugMessage();
        }

        public void ShowInterstitial(string adsPlaceName)
        {
            DebugMessage();
        }

        public bool IsRewardedReady()
        {
            DebugMessage();
            return true;
        }

        public void ShowRewarded(PLACE adsPlaceName)
        {
            DebugMessage();
            RewardCompleted?.Invoke();
        }

        public void ShowRewarded(string adsPlaceName)
        {
            DebugMessage();
            RewardCompleted?.Invoke();
        }

        public bool IsBannerReady()
        {
            DebugMessage();
            return false;
        }

        public void ShowBanner(BANNER_TYPE bannerType, BANNER_POS bannerPosition, PLACE adsPlaceName)
        {
            DebugMessage();
        }

        public void ShowBanner(BANNER_TYPE bannerType, BANNER_POS bannerPosition, string adsPlaceName)
        {
            DebugMessage();
        }

        public void HideBanners()
        {
            DebugMessage();
        }

        public void InitMREC(MaxSdkBase.AdViewPosition pos)
        {
            DebugMessage();
        }

        public void ShowMREC()
        {
            DebugMessage();
        }

        public void HideMREC()
        {
            DebugMessage();
        }

        public Dictionary<BANNER_POS, int> GetBannerOffset()
        {
            DebugMessage();
            return null;
        }

        public bool IsSupportAppOpen()
        {
            return false;
        }

        public bool IsAppOpenAdAvailable()
        {
            return false;
        }

        public void ShowAppOpen(PLACE adsPlaceName)
        {
            DebugMessage();
        }

        public void ShowAppOpen(string adsPlaceName)
        {
            DebugMessage();
        }

        public void ShowAppOpen()
        {
            DebugMessage();
        }

        public void LoadAppOpen()
        {
            DebugMessage();
        }

        private void DebugMessage()
        {
            Debug.LogWarning(
                "ApplovinAdsPlatform: Missed SDK_APPLOVIN in script defined symbols or not work in editor mode, or not works with this platform",
                gameObject);
        }

#endif
    }
}
