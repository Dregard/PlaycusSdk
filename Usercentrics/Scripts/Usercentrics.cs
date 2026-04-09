using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using System.ComponentModel;

namespace Unity.Usercentrics
{
    /// <summary>
    /// Usercentrics Consent Management Platform (c) 2024.
    /// It gives access to the singleton (Instance) and the whole API.
    /// </summary>
    public class Usercentrics : Singleton<Usercentrics>
    {
        #region INSPECTOR FIELDS
        [Header("Enter either Settings ID or Ruleset ID")]
        [SerializeField] public string SettingsID = "";
        [SerializeField] public string RulesetID = "";
        [SerializeField] public UsercentricsOptions Options = new UsercentricsOptions();
#if UNITY_EDITOR
        [InspectorLink] [SerializeField] internal string ConfigurationDashboard = "https://account.usercentrics.eu";
        [InspectorLink] [SerializeField] internal string Documentation = "https://usercentrics.com/docs/games/intro/";
#endif
        #endregion

        private readonly IUsercentricsPlatform UsercentricsPlatform =
#if UNITY_IOS
            new UsercentricsIOS();
#elif UNITY_ANDROID
            new UsercentricsAndroid();
#else
            null;
#endif

        private UnityAction<UsercentricsConsentUserResponse> onDismissCallback;
        private UnityAction<UsercentricsReadyStatus> initializeCallback;
        private UnityAction<string> initializeErrorCallback;
        private UnityAction<TCFData> tcfDataCallback;
        private List<UnityAction<UsercentricsUpdatedConsentEvent>> onConsentUpdatedCallbacks = new List<UnityAction<UsercentricsUpdatedConsentEvent>>();
        private List<UnityAction<UsercentricsMediationEvent>> onConsentMediationCallbacks = new List<UnityAction<UsercentricsMediationEvent>>();

        private UnityAction<UsercentricsReadyStatus> restoreSessionSuccessCallback;
        private UnityAction<string> restoreSessionErrorCallback;
        
        private UnityAction<UsercentricsReadyStatus> clearSessionSuccessCallback;
        private UnityAction<string> clearSessionErrorCallback;

        #region USERCENTRICS API
        /// <summary>
        /// Get if Usercentrics was initialized or not.
        /// </summary>
        /// <returns>true if it was initialized, false otherwise.</returns>
        public bool IsInitialized
        { get; private set; }

        /// <summary>
        /// Initialize Usercentrics obtaining latest online configuration and
        /// local storage to enable consents management.
        /// </summary>
        /// <param name="initializeCallback">
        /// Callback block that is invoked when the initialize process finishes.
        /// It returns UsercentricsReadyStatus.
        /// </param>
        /// <param name="initializeErrorCallback">
        /// Callback block that is invoked when the initialize process finishes
        /// with an error.
        /// It returns a non-localized string with information about the error.
        /// </param>
        public void Initialize(
            UnityAction<UsercentricsReadyStatus> initializeCallback,
            UnityAction<string> initializeErrorCallback
        ){
            ensureSupportedPlatform();
            logDebug("Initialize invoked");

            this.initializeCallback = initializeCallback;
            this.initializeErrorCallback = initializeErrorCallback;

            var optionsInternal = UsercentricsOptionsInternal.CreateFrom(Options, SettingsID, RulesetID);

            string optionsJson = JsonUtility.ToJson(optionsInternal);

            this.IsInitialized = false;
            UsercentricsPlatform?.Initialize(optionsJson);
        }
        
        private void Awake()
        {
            // Make instance persistent.
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// Get if current selected platform is supported by Usercentrics.
        /// </summary>
        /// <returns>true if it is supported, false otherwise.</returns>
        public bool IsPlatformSupported()
        {
            return UsercentricsPlatform != null;
        }

        /// <summary>
        /// Show Usercentrics Banner's First Layer.
        /// </summary>
        /// <param name="onDismissCallback">
        /// Callback block that is invoked when the user performs an action on the Banner.
        /// </param>
        public void ShowFirstLayer(UnityAction<UsercentricsConsentUserResponse> onDismissCallback)
        {
            ShowFirstLayer(bannerSettings: null, onDismissCallback: onDismissCallback);
        }

        /// <summary>
        /// Show Usercentrics Banner's First Layer.
        /// </summary>
        /// <param name="bannerSettings"></param>
        /// Sends the customization configuration for the banner
        /// </param>
        /// <param name="onDismissCallback">
        /// Callback block that is invoked when the user performs an action on the Banner.
        /// </param>
        public void ShowFirstLayer(BannerSettings bannerSettings, UnityAction<UsercentricsConsentUserResponse> onDismissCallback)
        {
            ensureSupportedPlatform();
            logDebug("ShowFirstLayer invoked");
            ensureInitialized();

            this.onDismissCallback = onDismissCallback;

            var bannerSettingsWithOptions = BuildBannerSettingsWithOptions(bannerSettings);
            var bannerSettingsJson = JsonUtility.ToJson(bannerSettingsWithOptions);

            UsercentricsPlatform?.ShowFirstLayer(bannerSettingsJson);
        }

        /// <summary>
        /// Show Usercentrics Banner's First Layer.
        /// </summary>
        /// <param name="bannerSettings"></param>
        /// Sends the customization configuration for the banner
        /// </param>
        /// <param name="onDismissCallback">
        /// Callback block that is invoked when the user performs an action on the Banner.
        /// </param>
        public void ShowSecondLayer(BannerSettings bannerSettings, UnityAction<UsercentricsConsentUserResponse> onDismissCallback)
        {
            ensureSupportedPlatform();
            logDebug("ShowSecondLayer invoked");
            ensureInitialized();

            this.onDismissCallback = onDismissCallback;

            var bannerSettingsWithOptions = BuildBannerSettingsWithOptions(bannerSettings);
            var bannerSettingsJson = JsonUtility.ToJson(bannerSettingsWithOptions);

            UsercentricsPlatform?.ShowSecondLayer(bannerSettingsJson);
        }

        /// <summary>
        /// Get the controller ID of the current session.
        /// </summary>
        /// <returns>the controller ID string</returns>
        public string GetControllerId()
        {
            ensureSupportedPlatform();
            logDebug("GetControllerID Invoked");
            ensureInitialized();
            return UsercentricsPlatform?.GetControllerID();
        }

        /// <summary>
        /// Retrieve TCF Data
        /// </summary>
        /// <returns>all data related to TCF, in case the SDK is using a TCF framework configuration</returns>
        public void GetTCFData(UnityAction<TCFData> callback)
        {
            ensureSupportedPlatform();
            logDebug("GetTCFData invoked");
            this.tcfDataCallback = callback;
            UsercentricsPlatform?.GetTCFData();
        }

        /// <summary>
        /// Get the CCPA Data
        /// </summary>
        /// <returns>all data related to CCPA consent management</returns>
        public CCPAData GetUSPData()
        {
            ensureSupportedPlatform();
            logDebug("GetUSPData Invoked");
            ensureInitialized();

            var rawCCPAData = UsercentricsPlatform?.GetUSPData();
            logDebug(rawCCPAData);
            var ccpaData = JsonUtility.FromJson<CCPAData>(rawCCPAData);

            return ccpaData;
        }

        /// <summary>
        /// Get the First Layer Settings Data.
        /// This method only returns data when using TCF Framework.
        /// In case your Settings ID is using other configuration, no data will be returned.
        /// </summary>
        /// <returns>First layer data related to TCF Framework</returns>
        public FirstLayerSettings GetFirstLayerSettings()
        {
            ensureSupportedPlatform();
            logDebug("GetFirstLayerSettings Invoked");
            ensureInitialized();

            var rawFirstLayerSettings = UsercentricsPlatform?.GetFirstLayerSettings();
            logDebug(rawFirstLayerSettings);
            var firstLayerSettings = JsonUtility.FromJson<FirstLayerSettings>(rawFirstLayerSettings);

            return firstLayerSettings;
        }

        /// <summary>
        /// Available only when using TCF Framework.
        /// Accept All button action. Use this method to signal when users presses this button.
        /// Only First Layer is supported.
        /// </summary>
        public void AcceptAll()
        {
            ensureSupportedPlatform();
            logDebug("AcceptAll Invoked");
            ensureInitialized();

            UsercentricsPlatform?.AcceptAll();
        }

        /// <summary>
        /// Available only when using TCF Framework.
        /// Deny All button action. Use this method to signal when users presses this button.
        /// Only First Layer is supported.
        /// </summary>
        public void DenyAll()
        {
            ensureSupportedPlatform();
            logDebug("DenyAll Invoked");
            ensureInitialized();

            UsercentricsPlatform?.DenyAll();
        }
        
        /// <summary>
        /// Available only when using GDPR Framework.
        /// Accept All button action. Use this method to signal when users presses this button.
        /// Only First Layer is supported.
        /// </summary>
        public void AcceptAllForGDPR()
        {
            ensureSupportedPlatform();
            logDebug("AcceptAll For GDPR Invoked");
            ensureInitialized();

            UsercentricsPlatform?.AcceptAllForGDPR();
        }

        /// <summary>
        /// Available only when using GDPR Framework.
        /// Deny All button action. Use this method to signal when users presses this button.
        /// Only First Layer is supported.
        /// </summary>
        public void DenyAllForGDPR()
        {
            ensureSupportedPlatform();
            logDebug("DenyAll For GDPR Invoked");
            ensureInitialized();

            UsercentricsPlatform?.DenyAllForGDPR();
        }


        /// <summary>
        /// Restore Consents given by a user using its Controller ID.
        /// </summary>
        /// <param name="controllerId">
        /// User's Controller Id string.
        /// </param>
        /// <param name="successCallback">
        /// Callback block that is invoked when the restoration completes successfully.
        /// It returns UsercentricsReadyStatus.
        /// </param>
        /// <param name="initializeErrorCallback">
        /// Callback block that is invoked when the initialize process finishes
        /// with an error.
        /// It returns a non-localized string with information about the error.
        /// </param>
        public void RestoreUserSession(
            string controllerId,
            UnityAction<UsercentricsReadyStatus> successCallback,
            UnityAction<string> errorCallback
        ){
            ensureSupportedPlatform();
            logDebug("RestoreUserSession invoked");

            this.restoreSessionSuccessCallback = successCallback;
            this.restoreSessionErrorCallback = errorCallback;

            UsercentricsPlatform?.RestoreUserSession(controllerId);
        }

        /// <summary>
        /// Subscribe to any consent updated event that happens within Usercentrics SDK
        /// </summary>
        public void SubscribeOnConsentUpdated(UnityAction<UsercentricsUpdatedConsentEvent> callback)
        {
            ensureSupportedPlatform();
            logDebug("SubscribeOnConsentUpdated invoked");
            if (onConsentUpdatedCallbacks.Count == 0)
            {
                UsercentricsPlatform?.SubscribeOnConsentUpdated();
            }
            this.onConsentUpdatedCallbacks.Add(callback);
        }

        /// <summary>
        /// Dispose all callbacks related to OnConsentUpdated event
        /// </summary>
        public void DisposeOnConsentUpdatedSubscription()
        {
            ensureSupportedPlatform();
            logDebug("DisposeOnConsentUpdatedSubscription invoked");
            this.onConsentUpdatedCallbacks.Clear();
            UsercentricsPlatform?.DisposeOnConsentUpdatedSubscription();
        }

        /// <summary>
        /// Subscribe to any mediation event that happens within Usercentrics SDK
        /// </summary>
        public void SubscribeOnConsentMediation(UnityAction<UsercentricsMediationEvent> callback)
        {
            ensureSupportedPlatform();
            logDebug("SubscribeOnConsentMediation invoked");
            if (onConsentMediationCallbacks.Count == 0)
            {
                UsercentricsPlatform?.SubscribeOnConsentMediation();
            }
            this.onConsentMediationCallbacks.Add(callback);
        }

        /// <summary>
        /// Dispose all callbacks related to OnConsentMediation event
        /// </summary>
        public void DisposeOnConsentMediationSubscription()
        {
            ensureSupportedPlatform();
            logDebug("DisposeOnConsentMediationSubscription invoked");
            this.onConsentMediationCallbacks.Clear();
            UsercentricsPlatform?.DisposeOnConsentMediationSubscription();
        }

        /// <summary>
        /// Track events using our Interaction Analytics API, so they can be displayed in Usercentrics analytics dashboard
        /// </summary>
        public void Track(UsercentricsAnalyticsEventType eventType)
        {
            ensureSupportedPlatform();
            logDebug("Track invoked");

            UsercentricsPlatform?.Track((int) eventType);
        }

        /// <summary>
        /// When building your own TCF 2.0 CMP, it is required to have your CMP UI design certified by
        /// the [IAB](https://iabeurope.eu/tcf-2-0/). Once certified, you will need to provide your CMP Id using this API
        /// </summary>
        public void SetCmpId(int cmpId)
        {
            ensureSupportedPlatform();
            logDebug("Set CmpId invoked");

            UsercentricsPlatform?.SetCmpId(cmpId);
        }

        /// <summary>
        /// Get the CMP Data
        /// </summary>
        /// <returns>CMP useful data</returns>
        public CmpData GetCmpData()
        {
            ensureSupportedPlatform();
            logDebug("GetCmpData Invoked");
            ensureInitialized();

            var rawCmpData = UsercentricsPlatform?.GetCmpData();
            logDebug(rawCmpData);

            var cmpData = JsonUtility.FromJson<CmpData>(rawCmpData);
            return cmpData;
        }

        /// <summary>
        /// Sets the variant for AB testing
        /// </summary>
        public void SetABTestingVariant(string variant)
        {
            ensureSupportedPlatform();
            logDebug("Set AB Testing Variant invoked");
            ensureInitialized();

            UsercentricsPlatform?.SetABTestingVariant(variant);
        }

        /// <summary>
        /// Gets the variant for AB testing
        /// </summary>
        /// <returns>AB Testing Variant</returns>
        public string GetABTestingVariant()
        {
            ensureSupportedPlatform();
            logDebug("GetABTestingVariant Invoked");
            ensureInitialized();

            return UsercentricsPlatform?.GetABTestingVariant();
        }
        
        /// <summary>
        /// Get the Additional Consent Mode Data with the AC string and selected Ad Tech Providers
        /// </summary>
        /// <returns>CMP useful data</returns>
        public AdditionalConsentModeData GetAdditionalConsentModeData()
        {
            ensureSupportedPlatform();
            logDebug("GetAdditionalConsentModeData Invoked");
            ensureInitialized();

            var rawAcmData = UsercentricsPlatform?.GetAdditionalConsentModeData();
            logDebug(rawAcmData);

            var acmData = JsonUtility.FromJson<AdditionalConsentModeData>(rawAcmData);
            return acmData;
        }
        
        /// <summary>
        /// Get Consents
        /// </summary>
        /// <returns>Consents</returns>
        public List<UsercentricsServiceConsent> GetConsents()
        {
            ensureSupportedPlatform();
            logDebug("GetConsents Invoked");
            ensureInitialized();

            var rawConsents = UsercentricsPlatform?.GetConsents();
            logDebug(rawConsents);

            var usercentricsConsents = JsonUtility.FromJson<UsercentricsConsentsHolder>(rawConsents).consents;
            
            return usercentricsConsents;
        }
        #endregion
        
        /// <summary>
        /// Clears user's session avoiding the sdk initialization.
        /// </param>
        /// <param name="successCallback">
        /// Callback block that is invoked when the restoration completes successfully.
        /// It returns UsercentricsReadyStatus.
        /// </param>
        /// <param name="initializeErrorCallback">
        /// Callback block that is invoked when the initialize process finishes
        /// with an error.
        /// It returns a non-localized string with information about the error.
        /// </param>
        public void ClearUserSession(
            UnityAction<UsercentricsReadyStatus> successCallback,
            UnityAction<string> errorCallback
        ){
            ensureSupportedPlatform();
            logDebug("ClearUserSession invoked");
            ensureInitialized();

            this.clearSessionSuccessCallback = successCallback;
            this.clearSessionErrorCallback = errorCallback;

            UsercentricsPlatform?.ClearUserSession();
        }
        
        /// <summary>
        /// Available only when using GDPR Framework.
        /// Save Decisions button action. Use this method to signal when users presses this button.
        /// Only First Layer is supported.
        /// </summary>
        public List<UsercentricsServiceConsent> saveDecisionsForGDPR(List<UsercentricsUserDecision> decisions)
        {
            ensureSupportedPlatform();
            logDebug("saveDecisionsGDPR For GDPR Invoked");
            ensureInitialized();
            
            var wrapper = new UsercentricsUserDecisionListWrapper { decisions = decisions };
            var decisionsJson = JsonUtility.ToJson(wrapper, true);
            logDebug(decisionsJson);
            
            var rawServiceConsent = UsercentricsPlatform?.SaveDecisionsForGDPR(decisionsJson);
            logDebug(rawServiceConsent);
            
            var data = JsonUtility.FromJson<UsercentricsServiceConsentWrapper>(rawServiceConsent);
            return data.consents;
            
        }

        #region UTILS

        private void ensureInitialized()
        {
            if (!IsInitialized)
            {
                throw new NotInitializedException();
            }
        }

        private void ensureSupportedPlatform()
        {
            ShowEditorNotSupportedDialogIfNeeded();

            if (!IsPlatformSupported())
            {
                throw new PlatformSupportException();
            }
        }

        private void ShowEditorNotSupportedDialogIfNeeded()
        {
            #if UNITY_EDITOR
            bool openRequirements = EditorUtility.DisplayDialog("Unity Editor not supported", UCConstants.UNITY_EDITOR_MESSAGE, "See Documentation", "Close");
            if (openRequirements)
            {
                Help.BrowseURL(UCConstants.REQUIREMENTS_URL);
            }
            #endif
        }

        private void logDebug(string message)
        {
            if (Options.DebugMode)
            {
                Debug.Log("[USERCENTRICS][DEBUG] " + message);
            }
        }

        private BannerSettings BuildBannerSettingsWithOptions(BannerSettings bannerSettings)
        {
            if (bannerSettings == null)
            {
                return new BannerSettings(generalStyleSettings: GetGeneralStyleSettingsFromOptions(),
                                          firstLayerStyleSettings: null,
                                          secondLayerStyleSettings: null,
                                          variantName: null);
            }

            if (bannerSettings.generalStyleSettings == null)
            {
                bannerSettings.generalStyleSettings = GetGeneralStyleSettingsFromOptions();
            }
            return bannerSettings;
        }

        private GeneralStyleSettings GetGeneralStyleSettingsFromOptions()
        {
            var isSystemBackButtonDisabled = Options.Android.DisableSystemBackButton;
            var statusBarColor = Options.Android.StatusBarColor;
            var showWindowInFullscreen = Options.Android.ShowWindowInFullscreen;

            return new GeneralStyleSettings(androidDisableSystemBackButton: isSystemBackButtonDisabled,
                                            androidStatusBarColor: statusBarColor,
                                            androidWindowFullscreen: showWindowInFullscreen);
        }
        #endregion

        #region MESSAGES HANDLERS
#pragma warning disable IDE0051 // Remove unused private members
        internal void HandleInitSuccess(string rawUsercentricsReadyStatus)
        {
            logDebug("HandleInitSuccess UsercentricsReadyStatus=" + rawUsercentricsReadyStatus);
            var usercentricsReadyStatus = JsonUtility.FromJson<UsercentricsReadyStatus>(rawUsercentricsReadyStatus);
            this.IsInitialized = true;
            this.initializeCallback?.Invoke(usercentricsReadyStatus);
            this.initializeCallback = null;
        }

        internal void HandleInitError(string errorMessage)
        {
            logDebug("HandleInitError errorMessage=" + errorMessage);
            this.initializeErrorCallback?.Invoke(errorMessage);
            this.initializeErrorCallback = null;
        }

        internal void HandleBannerResponse(string rawUsercentricsConsentUserResponse)
        {
            logDebug("HandleBannerResponse UsercentricsConsentUserResponse=" + rawUsercentricsConsentUserResponse);
            var usercentricsConsentUserResponse = JsonUtility.FromJson<UsercentricsConsentUserResponse>(rawUsercentricsConsentUserResponse);
            this.onDismissCallback?.Invoke(usercentricsConsentUserResponse);
            this.onDismissCallback = null;
        }

        internal void HandleRestoreSuccess(string rawUsercentricsReadyStatus)
        {
            logDebug("HandleRestoreSuccess UsercentricsReadyStatus=" + rawUsercentricsReadyStatus);
            var usercentricsReadyStatus = JsonUtility.FromJson<UsercentricsReadyStatus>(rawUsercentricsReadyStatus);
            this.restoreSessionSuccessCallback?.Invoke(usercentricsReadyStatus);
            this.restoreSessionSuccessCallback = null;
        }

        internal void HandleRestoreError(string errorMessage)
        {
            logDebug("HandleRestoreError errorMessage=" + errorMessage);
            this.restoreSessionErrorCallback?.Invoke(errorMessage);
            this.restoreSessionErrorCallback = null;
        }

        internal void HandleTCFData(string rawTCFData)
        {
            logDebug("HandleTCFData tcfData=" + rawTCFData);
            var tcfData = JsonUtility.FromJson<TCFData>(rawTCFData);
            this.tcfDataCallback?.Invoke(tcfData);
            this.tcfDataCallback = null;
        }

        internal void HandleOnConsentUpdated(string rawUsercentricsUpdatedConsentEvent)
        {
            logDebug("HandleOnConsentUpdated usercentricsUpdatedConsentEvent=" + rawUsercentricsUpdatedConsentEvent);
            var usercentricsUpdatedConsentEvent = JsonUtility.FromJson<UsercentricsUpdatedConsentEvent>(rawUsercentricsUpdatedConsentEvent);
            this.onConsentUpdatedCallbacks?.ForEach(callback => callback.Invoke(usercentricsUpdatedConsentEvent));
        }

        internal void HandleOnConsentMediation(string rawUsercentricsMediationEvent)
        {
            logDebug("HandleOnConsentMediation usercentricsMediationEvent=" + rawUsercentricsMediationEvent);
            var usercentricsMediationEvent = JsonUtility.FromJson<UsercentricsMediationEvent>(rawUsercentricsMediationEvent);
            this.onConsentMediationCallbacks?.ForEach(callback => callback.Invoke(usercentricsMediationEvent));
        }
        
        internal void HandleClearSuccess(string rawUsercentricsReadyStatus)
        {
            logDebug("HandleClearSuccess UsercentricsReadyStatus=" + rawUsercentricsReadyStatus);
            var usercentricsReadyStatus = JsonUtility.FromJson<UsercentricsReadyStatus>(rawUsercentricsReadyStatus);
            this.clearSessionSuccessCallback?.Invoke(usercentricsReadyStatus);
            this.clearSessionSuccessCallback = null;
        }

        internal void HandleClearError(string errorMessage)
        {
            logDebug("HandleClearError errorMessage=" + errorMessage);
            this.clearSessionErrorCallback?.Invoke(errorMessage);
            this.clearSessionErrorCallback = null;
        }
        
#pragma warning restore IDE0051 // Remove unused private members
        #endregion
    }
}
