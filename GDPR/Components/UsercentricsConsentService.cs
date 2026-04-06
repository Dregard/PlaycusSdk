using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using AppsFlyerSDK;
#if UNITY_IOS && PL_SDK_FACEBOOK_ON
using AudienceNetwork;
#endif
using Cysharp.Threading.Tasks;
using GameAnalyticsSDK;
using Playcus.Ads;
using Playcus.Analytics;
// using Playcus.UI;
using UnityEngine;
using UnityEngine.Events;
using Playcus.Saves;
using Unity.Usercentrics;
#if PL_SDK_FACEBOOK_ON
using Facebook.Unity;
#endif
#if PL_SDK_PLAYCUSDATALAKE_ON
using PlaycusDL;
#endif
#if PL_SDK_DELTADNA_ON
using DeltaDNA;
#endif
#if UNITY_IOS
using UnityEngine.iOS;
using Unity.Advertisement.IosSupport;
#endif
#if PL_SDK_APPSFLYER_ON
using AppsFlyerSDK;
#endif
#if PL_SDK_ADJUST_ON
using com.adjust.sdk;

#endif

#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace Playcus.GDPR
{
    /// <summary>
    /// GDPR service will audit version of privacy policy and show POPUP.GDPR
    /// </summary>
    [ServiceBind(typeof(UsercentricsConsentService))]
    public class UsercentricsConsentService : ServiceWithConfig
    {
        private const string FIREBASE_TEMPLATE_ID = "42vRvlulK96R-F";
        private const string APPLOVIN_TEMPLATE_ID = "fHczTMzX8";
        private const string APPS_FLYER_TEMPLATE_ID = "Gx9iMF__f";
        private const string GAME_ANALYTICS_TEMPLATE_ID = "bQTbuxnTb";
        private const string AMAZON_APS_TEMPLATE_ID = "IUyljv4X5";
        private const string META_AUDIENCE_NETWORK_TEMPLATE_ID = "ax0Nljnj2szF_r";
        
        // CONFIG
        [HelpBox(@"Usercentrics service controls 3dParty SDK conscends, ATT for ios & privacy policy", HelpBoxMessageType.Info)]
        [SerializeField]
        private bool _readMe;
        // DEPENDENCIES
        // [InjectService] private IPopupsManager _popupsManager;
        protected override Type ConfigType => typeof(UsercentricsConsentServiceConfig);
        protected UsercentricsConsentServiceConfig Config => (UsercentricsConsentServiceConfig) _serviceConfig;


        // STATIC
        static public event Action UserForgetedEvent;
        static public event Action ConsendAcceptedEvent;
        static public event Action GDPRAcceptedEvent;
        
        private const string VERSION_TOS = "VERSION_TOS";
        private const string PrefKeyIosDataAnalytics = "IosDataChoiseAnalyticsTracked";
        
        private bool _checkingGDPR;
        
        private bool _isSdkInitializing = true;
        private string _initializationError;

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern string InternalGDPRGetVersionTOS();

#else

        private static string InternalGDPRGetVersionTOS()
        {
            return "0";
        }

#endif
        
        protected override async UniTask LoadAsyncInternal(CancellationToken cancellationToken)
        {
            Debug.Log($"UsercentricsConsentService: Loading started", gameObject);
                
            Debug.Log("UsercentricsConsentService: Waiting for remote config...", gameObject);
                
            await LoadServiceConfigAsync(cancellationToken, false);
            
            Debug.Log("UsercentricsConsentService: Config successfully loaded.", gameObject);

#if !UNITY_ANDROID && !UNITY_IOS && !UNITY_WEBGL
        if (Config.manualAuditGDPR)
            CompleteLoading();
        else
            AuditGDPR();
#elif !UNITY_EDITOR && PL_USERCENTRICS_CONSENT_ON
            //Current version of usercentrics works only on real devices
            AuditConsent();

            Debug.Log("UsercentricsConsentService: Awaiting SDK initialization...", gameObject);

            await UniTask.WaitWhile(() => _isSdkInitializing, cancellationToken: cancellationToken);
            
            if (string.IsNullOrEmpty(_initializationError) == false)
            {
                throw new PlaycusInitializationException(gameObject.name, _initializationError);
            }
#else
            CompleteLoading();
#endif
        }
        
        private void AuditConsent()
        {
#if PL_USERCENTRICS_CONSENT_ON
            Debug.Log("UsercentricsConsentService: Starting Usercentrics SDK initialization...", gameObject);
            Usercentrics.Instance.Initialize(OnInitSuccess, OnError);
#endif
        }
        public void AuditGDPR()
        {
            _checkingGDPR = true;
            // if (!Config.manualAuditGDPR)
            // {
            //     CompleteLoading();
            //     return;
            // }

            if (IsGDPRAccepted())
            {
                CompleteLoading();
            }
            // else
            // {
            //     CallGDPRPopup();
            // }

        }
        public bool IsGDPRAccepted()
        {
            if (Config != null)
            {
                var acceptedVersionTos = 0;
                if (PlayerPrefs.HasKey(VERSION_TOS))
                {
                    acceptedVersionTos = PlayerPrefs.GetInt(VERSION_TOS);
                }

#if UNITY_WEBGL && !UNITY_EDITOR
                if (Config.versionTos > acceptedVersionTos)
                {
                    Debug.Log("Trying to ask Version TOS from WebGL environment");
                    var rawEnvTOS = InternalGDPRGetVersionTOS();
                    Debug.Log($"WebGL environment Version TOS is {rawEnvTOS}");
                    acceptedVersionTos = Convert.ToInt32(rawEnvTOS);
                    if (acceptedVersionTos <= 0)
                    {
                        return true;
                    }
                }
#endif

                return Config.versionTos <= acceptedVersionTos;
            }
            else
            {
                return false;
            }
        }

        public void SetVersionTos(int version)
        {
            if (Config == null) {
                Debug.LogWarning($"UsercentricsConsentService.SetVersionTos({version}). Config is null");
                return;
            }
            
            Config.versionTos = version;
        }
        
        // todo: restore for other platforms
        // private void CallGDPRPopup()
        // {
        //     StartCoroutine(WaitPopUpsManagerAndShowGDPR());
        // }
        //
        // IEnumerator WaitPopUpsManagerAndShowGDPR()
        // {
        //     while (_popupsManager == null)
        //     {
        //         yield return new WaitForSeconds(0.5f);
        //     }
        //     _popupsManager.ShowPopup<GDPRPopup>(POPUP.GDPR).Init(GDPRPopupCallback, Config.eighteenToggleActive,
        //         Config.eighteenToggleChecked);
        // }
        //
        // private void GDPRPopupCallback()
        // {
        //     PlayerPrefs.SetInt(VERSION_TOS, Config.versionTos);
        //     
        //     CompleteLoading();
        // }

        private void OnInitSuccess(UsercentricsReadyStatus status)
        {
            Debug.Log("UsercentricsConsentService: Usercentrics SDK initialization succeeded.", gameObject);

            _isSdkInitializing = false;
            Debug.Log($"[USERCENTRICS] OnInitSuccess shouldCollectConsent {status.shouldCollectConsent}");
            if (status.shouldCollectConsent)
            {
                ShowFirstLayer();
            }
            else
            {
                HandleConsents(status.consents);
            }
        }

        private void OnError(String errorMessage)
        {
            Debug.LogError("UsercentricsConsentService: Initialization failed – Usercentrics SDK encountered an error.", gameObject);

            _isSdkInitializing = false;
            _initializationError = errorMessage;
        }

        private void ShowFirstLayer()
        {
#if PL_USERCENTRICS_CONSENT_ON
           // Debug.Log("UsercentricsConsentService ShowFirstLayer");
           Debug.Log("UsercentricsConsentService: Showing first layer consent dialog (Usercentrics SDK).", gameObject);

            Usercentrics.Instance.ShowFirstLayer(UpdateServices);
#endif
        }
        
        private void UpdateServices(UsercentricsConsentUserResponse response)
        {
            // With Usercentrics settings, now user can only ACCEPT ALL or leave app, so no need to pass consent separately for each SDK (will pass on their inits in their own classes)
            Debug.Log("UsercentricsConsentService: Applying user consent preferences.");
            HandleConsents(response.consents);
        }

        private void HandleConsents(List<UsercentricsServiceConsent> consents)
        {
            var consentGiven = false;
            foreach (var serviceConsent in consents)
            {
                consentGiven = consentGiven || serviceConsent.status;
                switch (serviceConsent.templateId)
                {
                    case FIREBASE_TEMPLATE_ID:
                        Debug.Log("UsercentricsConsentService: Passing consent to Firebase");
                        FirebaseAnalyticServicePlatform.SetConsent(serviceConsent.status);
                        break;
                    case APPLOVIN_TEMPLATE_ID:
                        Debug.Log("UsercentricsConsentService: Passing consent to Applovin");
                        AdsPlatformApplovin.SetConsent(serviceConsent.status);
                        break;
                    case APPS_FLYER_TEMPLATE_ID:
                        Debug.Log("UsercentricsConsentService: Passing consent to AppsFlyer");
                        AppsFlyerAnalyticServicePlatform.SetConsent(serviceConsent.status);
                        break;
                    case GAME_ANALYTICS_TEMPLATE_ID:
                        Debug.Log("UsercentricsConsentService: Passing consent to GameAnalytics");
                        GameAnalyticsAnalyticServicePlatform.SetConsent(serviceConsent.status);
                        break;
                    case AMAZON_APS_TEMPLATE_ID:
                        Debug.Log("UsercentricsConsentService: Passing consent to Amazon APS");
                        // todo
                        break;
                    case META_AUDIENCE_NETWORK_TEMPLATE_ID:
                        Debug.Log("UsercentricsConsentService: Passing consent to Facebook Audience Network");
#if PL_SDK_FACEBOOK_ON && (UNITY_IOS || UNITY_ANDROID)
                        if (serviceConsent.status)
                        {
                            AdSettings.SetDataProcessingOptions(new string[] {});
                        }
#endif
                        break;
                    default:
                        break;
                }
            }
            
            // show ATT only if user gave full or partial consent
            if (consentGiven)
            {
                AcceptConsent();
            }
        }

        private void AcceptConsent()
        {
            if (Application.platform != RuntimePlatform.IPhonePlayer)
            {
                CompleteLoading();
            }
            else
            {
                // On ios we need take permission from user about data
                StartCoroutine(CheckIOSDataPermission());
            }
        }
        
        /*private void ShowAtt()
        {
            AppTrackingTransparency.Instance.PromptForAppTrackingTransparency((status) =>
            {
                switch (status)
                {
                    case AuthorizationStatus.AUTHORIZED:
                        break;
                    case AuthorizationStatus.DENIED:
                        break;
                    case AuthorizationStatus.NOT_DETERMINED:
                        break;
                    case AuthorizationStatus.RESTRICTED:
                        break;
                }
            });
        }*/

        IEnumerator CheckIOSDataPermission()
        {
#if UNITY_IOS && !ATTRACKING_CONSENT_SERVICE
            bool needPermission = false;
            try
            {
                needPermission = new Version(UnityEngine.iOS.Device.systemVersion) >= new Version("14.0");
            }
            catch (Exception e)
            {
                Debug.LogError($"Ios get system version error {e.Message}");
            }

            if (needPermission)
            {
                // Ask user permission to identification on ios 14 if we don't ask before
                if (ATTrackingStatusBinding.GetAuthorizationTrackingStatus() ==
                    ATTrackingStatusBinding.AuthorizationTrackingStatus.NOT_DETERMINED)
                {
                    ATTrackingStatusBinding.RequestAuthorizationTracking();
                    yield return new WaitUntil(() =>
                        ATTrackingStatusBinding.GetAuthorizationTrackingStatus() !=
                        ATTrackingStatusBinding.AuthorizationTrackingStatus.NOT_DETERMINED);
                }

                AudienceNetwork.AdSettings.SetAdvertiserTrackingEnabled(
                    ATTrackingStatusBinding.GetAuthorizationTrackingStatus() ==
                    ATTrackingStatusBinding.AuthorizationTrackingStatus.AUTHORIZED);

                //Send analytics
                if (!PlayerPrefs.HasKey(PrefKeyIosDataAnalytics))
                {
                    PlayerPrefs.SetInt(PrefKeyIosDataAnalytics, 1);
                    IAnalyticsManager analytics = ServiceLocator.Get<IAnalyticsManager>();
                    if (analytics != null)
                        analytics.CustomEvent(AnalyticsEvents.pl_gdpr_ios_permission.ToString(),
                            (int) ATTrackingStatusBinding.GetAuthorizationTrackingStatus());
                }
            }
#endif
            // in case with Usercentrics setting Status to Ready after consent is given
            // to ensure that consent-dependent services will not start initialization before the user response
            CompleteLoading();
            yield return null;
        }


        private void CompleteLoading()
        {
            Debug.Log($"UsercentricsConsentService.CompleteLoading() _checkingGDPR: {_checkingGDPR}");

            if (_checkingGDPR)
            {
                GDPRAcceptedEvent?.Invoke();
            }

#if !UNITY_EDITOR && PL_USERCENTRICS_CONSENT_ON
            Debug.Log("ConsendAcceptedEvent");
            ConsendAcceptedEvent?.Invoke();
// #else
            // CompleteLoading();
#endif
            
            ServiceLoadingComplete();
        }

        /// <summary>
        /// All user data will be erased permanently. All SDK, saves and
        /// </summary>
        static public async UniTask ForgetMe()
        {
            //Send analytics
            IAnalyticsManager analytics = ServiceLocator.Get<IAnalyticsManager>();
            if (analytics != null)
                analytics.CustomEvent(AnalyticsEvents.pl_gdpr_forgeted.ToString());

            // Forget me in all sdk
#if PL_SDK_APPLOVIN_ON && (UNITY_IOS || UNITY_ANDROID)
            AdsPlatformApplovin.SetConsent(false);
#endif
#if PL_SDK_PLAYCUSDATALAKE_ON
            PDL.Instance.ForgetMe();
#endif
#if PL_SDK_DELTADNA_ON
            DDNA.Instance.ForgetMe();
#endif
#if PL_SDK_FACEBOOK_ON && (UNITY_IOS || UNITY_ANDROID)
            // Limit Data Usage
            AdSettings.SetDataProcessingOptions(new string[] { "LDU" }, 0, 0);
#endif
#if PL_SDK_FIREBASE_ON
            FirebaseAnalyticServicePlatform.SetConsent(false);
#endif
#if PL_SDK_APPSFLYER_ON
            AppsFlyerAnalyticServicePlatform.SetConsent(false);
#endif
#if PL_SDK_ADJUST_ON
            Adjust.gdprForgetMe();
#endif
#if PL_SDK_GA_ON
            GameAnalyticsAnalyticServicePlatform.SetConsent(false);
#endif

            // Clear prefs
            PlayerPrefs.DeleteAll();

            // Clear save
            ISaveService save = ServiceLocator.Get<ISaveService>();
            if (save != null)
            {
                await save.DeleteSaveData();
            }

            // Other services callback
            UserForgetedEvent?.Invoke();

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
#if !UNITY_WEBGL
            // Exit app
            Application.Quit();
#endif
        }
    }
}