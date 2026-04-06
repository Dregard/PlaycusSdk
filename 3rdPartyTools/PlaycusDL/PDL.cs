using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Cysharp.Threading.Tasks;
using Playcus;
using UnityEngine;

#if SDK_DELTADNA
using DeltaDNA;
#endif


namespace PlaycusDL {

    /// <summary>
    /// PDL is the PlacusDataLake SDK.
    /// </summary>
    public class PDL : Singleton<PDL> {

        internal const string PF_KEY_FIRST_SESSION = "PDLSDK_FIRST_SESSION";
        internal const string PF_KEY_LAST_SESSION = "PDLSDK_LAST_SESSION";
        internal const string PF_KEY_CROSS_GAME_USER_ID = "PDLSDK_CROSS_GAME_USER_ID";
        internal const string PF_KEY_ADVERTISING_ID = "PDLSDK_ADVERTISING_ID";
        internal const string PF_KEY_FORGET_ME = "PDLSDK_FORGET_ME";
        internal const string PF_KEY_STOP_TRACKING_ME = "PDLSKD_STOP_TRACKING_ME";
        internal const string PF_KEY_FORGOTTEN = "PDLSK_FORGOTTEN";
        internal const string PF_KEY_ACTIONS_SALT = "PDLSDK_ACTIONS_SALT";

        private const string PREFS_KEY_SESSIONS_COUNT = "sessionsCount";
        
        public const string PATH_TO_PDL_PARENT_FOLDER = "Assets/submodule-core/Analytics/Libraries";

        private static object _lock = new object();

        public event Action OnNewSession;
        /// <summary>
        /// Will be called when the session configuration will be successfully
        /// retrieved. The type parameter specifies whether the response was
        /// cached or not.
        /// </summary>
        public event Action<bool> OnSessionConfigured;
        /// <summary>
        /// Will be called when the session configuration request fails.
        /// </summary>
        public event Action OnSessionConfigurationFailed;
        /// <summary>
        /// Will be called when the image cache will be successfully populated.
        /// </summary>
        public event Action OnImageCachePopulated;
        /// <summary>
        /// Will be called when the image cache will fail to be populated. The
        /// reason for the failure will be passed as the type argument.
        /// </summary>
        public event Action<string> OnImageCachingFailed;

        private PDLBase delegated;
        private string collectURL;
       
        private int _lifetime = 0;

        protected PDL() {
            Settings = new Settings(); // default configuration
        }

        void OnEnable() {
            #if UNITY_5_OR_NEWER
            Application.logMessageReceived += Logger.HandleLog;
            #endif
        }

        void OnDisable() {
            #if UNITY_5_OR_NEWER
            Application.logMessageReceived -= Logger.HandleLog;
            #endif
        }

        internal void Awake()
        {
            LifeTimeTimer();
            
            lock (_lock) {
                if (PlayerPrefs.HasKey(PF_KEY_FORGET_ME)
                    || PlayerPrefs.HasKey(PF_KEY_FORGOTTEN)
                    || PlayerPrefs.HasKey(PF_KEY_STOP_TRACKING_ME)
                    ) {
                    delegated = new PDLNonTracking(this);
                } else {
                    delegated = new PDLImpl(this);
                }
            }
        }

        private async UniTask LifeTimeTimer()
        {
            _lifetime = PlayerPrefs.GetInt("PDL_LIFETIME", 0);
            
            while (_lifetime < Settings.TimeSendEventsEverySecondInSeconds)
            {
                _lifetime += 1;

                if (_lifetime % 60 == 0)
                {
                    PlayerPrefs.SetInt("PDL_LIFETIME", _lifetime);
                }
                
                await UniTask.Delay(TimeSpan.FromSeconds(1), ignoreTimeScale: true);
            }
        }

        #region Client Interface

        /// <summary>
        /// Starts the SDK.  Call before sending event  The SDK will
        /// generate a new user id if this is the first run.
        /// </summary>
        public void StartSDK() {
            StartSDK((string) null);
        }

        /// <summary>
        /// Starts the SDK.  Call before sending events.
        /// </summary>
        /// <param name="userID">The user id for the player, if set to null we create one for you.</param>
        public void StartSDK(string userID) {
            var configResource = Resources.Load("pdl_configuration", typeof(TextAsset));
            ConfigurationPDL config;
            if (configResource != null) {
                using (var stringReader = new StringReader((configResource as TextAsset).text)) {
                    using (var xmlReader = XmlReader.Create(stringReader)) {
                        config = (new XmlSerializer(
                            typeof(ConfigurationPDL), new XmlRootAttribute("configuration")))
                            .Deserialize(xmlReader) as ConfigurationPDL;
                    }
                }
            } else {
                Logger.LogWarning("Failed to find PDL SDK configuration");
                config = new ConfigurationPDL();
            }

            StartSDK(config, userID);
        }

        /// <summary>
        /// Starts the SDK.  Call before sending events.  The SDK will
        /// generate a new user id if this is the first run. This method can be used if the
        /// game configuration needs to be provided in the code as opposed to using the
        /// configuration UI.
        /// </summary>
        /// <param name="config">The game configuration for the SDK.</param>
        public void StartSDK(ConfigurationPDL config) {
            StartSDK(config, null);
        }

        /// <summary>
        /// Starts the SDK.  Call before sending events.  This method
        /// can be used if the game configuration needs to be provided in the code as opposed
        /// to using the configuration UI.
        /// </summary>
        /// <param name="config">The game configuration for the SDK.</param>
        /// <param name="userID">The user id for the player, if set to null we create one for you.</param>
        public void StartSDK(ConfigurationPDL config, string userID) {
            lock (_lock) {
                var userInfoService = ServiceLocator.Get<IUserInfoService>();

                bool newPlayer = userInfoService.IsNewPlayer;
                var actualUserId = userInfoService.UserID;
                
                //PlayerPrefs.DeleteKey(PF_KEY_USER_ID);
//                 if (String.IsNullOrEmpty(UserID)) {         // first time!
//                     newPlayer = true;
//                     if (String.IsNullOrEmpty(userID)) {     // generate a user id
// #if SDK_DELTADNA
//                         userID = DDNA.Instance.UserID;
// #else
//                         userID = GenerateUserID();
// #endif
//                     }
                // } else
                //  if (!String.IsNullOrEmpty(userID)) { // use offered user id
                //     if (actualUserId != userID) {
                //         newPlayer = true;
                //     }
                // }

                // UserID = userID;

                if (newPlayer) {
                    Logger.LogInfo("Starting PDL SDK with new user " + actualUserId);
                } else {
                    Logger.LogInfo("Starting PDL SDK with existing user " + actualUserId);
                }

                EnvironmentKey = (config.environmentKey == 0)
                    ? config.environmentKeyDev
                    : config.environmentKeyLive;
                CollectURL = config.collectUrl;
                if (Platform == null) {
                    Platform = ClientInfo.Platform;
                }

                if (!string.IsNullOrEmpty(config.hashSecret)) {
                    HashSecret = config.hashSecret;
                }
                if (config.useApplicationVersion) {
                    ClientVersion = Application.version;
                } else if (!string.IsNullOrEmpty(config.clientVersion)) {
                    ClientVersion = config.clientVersion;
                }

                if (newPlayer) {
                    PlayerPrefs.DeleteKey(PF_KEY_FIRST_SESSION);
                    PlayerPrefs.DeleteKey(PF_KEY_LAST_SESSION);
                    PlayerPrefs.DeleteKey(PF_KEY_CROSS_GAME_USER_ID);

                    if (delegated is PDLNonTracking) {
                        PlayerPrefs.DeleteKey(PF_KEY_FORGET_ME);
                        PlayerPrefs.DeleteKey(PF_KEY_FORGOTTEN);
                        PlayerPrefs.DeleteKey(PF_KEY_STOP_TRACKING_ME);

                        delegated = new PDLImpl(this);
                    }
                }

                delegated.StartSDK(newPlayer);
            }
        }

        /// <summary>
        /// Starts the SDK.  Call before sending events.
        /// </summary>
        /// <param name="envKey">The unique environment key for this game environment.</param>
        /// <param name="collectURL">The Collect URL for this game.</param>
        /// <param name="userID">The user id for the player, if set to null we create one for you.</param>
        [Obsolete("Deprecated as of version 4.8, please use the Editor configuration UI and StartSDK(userID) instead")]
        public void StartSDK(string envKey, string collectURL, string userID) {
            StartSDK(userID);
        }

        /// <summary>
        /// Changes the session ID for the current User.
        /// </summary>
        public void NewSession()
        {
            string sessionID;
#if PL_SDK_DELTADNA_ON
            sessionID = DDNA.Instance.SessionID;
#else
            sessionID = GenerateSessionID();
#endif
            Logger.LogInfo("Starting new session "+sessionID);
            SessionID = sessionID;

            RequestSessionConfiguration();
            if (!PlayerPrefs.HasKey(PF_KEY_FIRST_SESSION)) {
                PlayerPrefs.SetString(
                    PF_KEY_FIRST_SESSION,
                    DateTime.UtcNow.ToString(Settings.EVENT_TIMESTAMP_FORMAT));
            }
            PlayerPrefs.SetString(
                PF_KEY_LAST_SESSION,
                DateTime.UtcNow.ToString(Settings.EVENT_TIMESTAMP_FORMAT));

            if (OnNewSession != null) OnNewSession();
        }

        /// <summary>
        /// Sends a 'gameEnded' event to Collect, disables background uploads.
        /// </summary>
        public void StopSDK() {
            lock (_lock) {
                delegated.StopSDK();
            }
        }

        /// <summary>
        /// Records an event using the GameEvent class.
        /// </summary>
        /// <param name="gameEvent">Event to record.</param>
        /// <returns><see cref="EventAction"/> for this event</returns>
        /// <exception cref="System.Exception">Thrown if the SDK has not been started.</exception>
        public EventAction RecordEvent<T>(T gameEvent) where T : GameEvent<T> {
            return delegated.RecordEvent(gameEvent);
        }

        /// <summary>
        /// Records an event with no custom parameters.
        /// </summary>
        /// <param name="eventName">Name of the event.</param>
        /// <returns><see cref="EventAction"/> for this event</returns>
        /// <exception cref="System.Exception">Thrown if the SDK has not been started.</exception>
        public EventAction RecordEvent(string eventName) {
            return delegated.RecordEvent(eventName);
        }

        /// <summary>
        /// Records an event with a name and a dictionary of event parameters.  The eventParams dictionary
        /// should match the 'eventParams' branch of the event schema.
        /// </summary>
        /// <param name="eventName">Event name.</param>
        /// <param name="eventParams">Event parameters.</param>
        /// <returns><see cref="EventAction"/> for this event</returns>
        /// <exception cref="System.Exception">Thrown if the SDK has not been started.</exception>
        public EventAction RecordEvent(string eventName, Dictionary<string, object> eventParams) {
            return delegated.RecordEvent(eventName, eventParams);
        }

        /// <summary>
        /// Records that the game received a push notification.  It is safe to call this method
        /// before calling StartSDK, the 'notificationOpened' event will be sent at that time.
        /// </summary>
        /// <param name="payload">The notification payload.</param>
        public void RecordPushNotification(Dictionary<string, object> payload) {
            delegated.RecordPushNotification(payload);
        }

        /// <summary>
        /// Makes a session configuration request. This method should be called if
        /// a session configuration request has previously failed.
        ///
        /// The result will be notified via <see cref="OnSessionConfigured"/> or
        /// <see cref="OnSessionConfigurationFailed"/> in case of failure.
        /// </summary>
        public void RequestSessionConfiguration() {
            delegated.RequestSessionConfiguration();
        }

        /// <summary>
        /// Uploads waiting events to our Collect service.  By default this is called automatically in the
        /// background.  If you disable auto uploading via <see cref="Settings.BackgroundEventUpload"/> you
        /// will need to call this method yourself periodically.
        /// </summary>
        public void Upload() {
            delegated.Upload();
        }

        /// <summary>
        /// Clears the persistent data, such as user id. The SDK should be stopped
        /// before this method is called.
        ///
        /// Useful for testing purposes.
        /// </summary>
        public void ClearPersistentData() {
            if (HasStarted) {
                Logger.LogWarning("SDK has not been stopped before clearing persistent data");
            }

            // PlayerPrefs.DeleteKey(PF_KEY_USER_ID);
            PlayerPrefs.DeleteKey(PF_KEY_FIRST_SESSION);
            PlayerPrefs.DeleteKey(PF_KEY_LAST_SESSION);
            PlayerPrefs.DeleteKey(PF_KEY_CROSS_GAME_USER_ID);
            PlayerPrefs.DeleteKey(PF_KEY_ADVERTISING_ID);
            PlayerPrefs.DeleteKey(PF_KEY_FORGET_ME);
            PlayerPrefs.DeleteKey(PF_KEY_FORGOTTEN);
            PlayerPrefs.DeleteKey(PF_KEY_STOP_TRACKING_ME);
            PlayerPrefs.DeleteKey(PF_KEY_ACTIONS_SALT);

            delegated.ClearPersistentData();

            lock (_lock) {
                if (delegated is PDLNonTracking) {
                    delegated = new PDLImpl(this);
                }
            }
        }

        /// <summary>
        /// Forgets the current user and stops them from being tracked.
        ///
        /// Any subsequent calls on the SDK will succeed, but not send/request anything to/from
        /// the Platform.
        ///
        /// The status can be cleared by starting the SDK with a new use or clearing the persistent
        /// data.
        /// </summary>
        public void ForgetMe() {
            lock (_lock) {
                if (!PlayerPrefs.HasKey(PF_KEY_FORGET_ME)) {
                    var started = HasStarted;
                    delegated.ForgetMe();

                    delegated = new PDLNonTracking(this);
                    if (started) delegated.StartSDK(false);
                    delegated.ForgetMe();
                }
            }
        }

        public void StopTrackingMe() {
            lock (_lock) {
                if (!PlayerPrefs.HasKey(PF_KEY_STOP_TRACKING_ME)) {
                    var started = HasStarted;
                    delegated.StopTrackingMe();

                    delegated = new PDLNonTracking(this);
                    if (started) delegated.StartSDK(false);
                    delegated.StopTrackingMe();
                }
            }
        }

        /// <summary>
        /// Controls if the device is used as the event timestamp source or our Collect server.
        /// Using the device time (the default) ensures the events will have the correct timestamp
        /// while no internet connection is available to upload events.  But the device time relies
        /// on the user having set their system clock correctly.  If you disable the device
        /// timestamp, our Collect server will inject the time it received the event.  If you
        /// require more control over the timestamp, use <see cref="SetTimestampFunc"/>.
        /// </summary>
        /// <param name="useCollect">If set to <c>true</c> use Collect server for event timestamps.</param>
        public void UseCollectTimestamp(bool useCollect) {
            delegated.UseCollectTimestamp(useCollect);
        }

        /// <summary>
        /// If more control is required over the event timestamp source, you can override the default
        /// behaviour with a function that returns a DateTime.
        /// </summary>
        /// <param name="TimestampFunc">Timestamp func.</param>
        public void SetTimestampFunc(Func<DateTime?> TimestampFunc) {
            delegated.SetTimestampFunc(TimestampFunc);
        }

        /// <summary>
        /// Sets the logging level. Choices are ERROR, WARNING, INFO or DEBUG. Default is WARNING.
        /// </summary>
        public void SetLoggingLevel(Logger.Level level) {
            Logger.SetLogLevel(level);
        }

        /// <summary>
        /// Controls default behaviour of the SDK.  Set prior to initialisation.
        /// </summary>
        public Settings Settings { get; set; }

        #endregion
        #region Properties

        /// <summary>
        /// Gets the environment key.
        /// </summary>
        public string EnvironmentKey { get; private set; }

        /// <summary>
        /// Gets the Collect URL.
        /// </summary>
        public string CollectURL {
            get { return collectURL; }
            private set { collectURL = Utils.FixURL(value); }
        }

        /// <summary>
        /// Gets the game device ID.
        /// </summary>
        public string GameDeviceID { get; set; }

        /// <summary>
        /// Gets the device ID.
        /// </summary>
        public string DeviceID { get; set; }

        /// <summary>
        /// Gets the external ID.
        /// </summary>
        public string ExternalID { get; set; }

        /// <summary>
        /// Gets the Locale.
        /// </summary>
        public string GameLanguage { get; set; }

        /// <summary>
        /// Gets the Vendor.
        /// </summary>
        public string Vendor { get; set; }

        /// <summary>
        /// Gets the User Level.
        /// </summary>
        public int UserLevel { get; set; }

        /// <summary>
        /// Gets the reclam ID.
        /// </summary>
        public string AdvertisingID { get; set; }

        /// <summary>
        /// Gets the game network.
        /// </summary>
        public string GameNetwork { get; set; }

        /// <summary>
        /// Gets the session ID.
        /// </summary>
        public string SessionID { get; private set; }


        public string UserID;
        // /// <summary>
        // /// Gets the user ID.
        // /// </summary>
        // public string UserID {
        //     get {
        //         string v = PlayerPrefs.GetString(PF_KEY_USER_ID, null);
        //         if (String.IsNullOrEmpty(v)) {
        //             return null;
        //         }
        //         return v;
        //     }
        //     private set {
        //         if (!String.IsNullOrEmpty(value)) {
        //             PlayerPrefs.SetString(PF_KEY_USER_ID, value);
        //             PlayerPrefs.Save();
        //         }
        //     }
        // }

        /// <summary>
        /// Gets a value indicating whether this instance is initialised.
        /// </summary>
        public bool HasStarted { get { return delegated.HasStarted; }}

        /// <summary>
        /// Gets a value indicating whether an event upload is in progress.
        /// </summary>
        public bool IsUploading { get { return delegated.IsUploading; }}

        #endregion
        #region Client Configuration

        /// <summary>
        /// To enable hashing of your event data, set this value to your
        /// unique hash secret.  You must also enable hashing for the environment.
        /// To disable hashing set it to null, which is the default.  This must be
        /// set before calling <see cref="Start"/>.
        /// </summary>
        public string HashSecret { get; set; }

        /// <summary>
        /// A version string for your game that will be reported to us.  This must
        /// be set before calling <see cref="Start"/>.
        /// </summary>
        public string ClientVersion { get; set; }

        /// <summary>
        /// By default we detect the platform field for your events.  You can override
        /// this value, make sure to set it before calling <see cref="Start"/>.
        /// </summary>
        public string Platform { get; set; }

        public int EventNumber { get; set; } = 0;

        /// <summary>
        /// The cross game user ID to be used for cross promotion. May be <code>null</code>
        /// or empty if not set.
        /// </summary>
        public string CrossGameUserID {
            get { return delegated.CrossGameUserID; }
            set { delegated.CrossGameUserID = value; }
        }

        /// <summary>
        /// The Android registration ID that is associated with this device if it's running
        /// on the Android platform.  This must be set before calling <see cref="Start"/>.
        /// </summary>
        public string AndroidRegistrationID {
            get { return delegated.AndroidRegistrationID; }
            set { delegated.AndroidRegistrationID = value; }
        }

        /// <summary>
        /// The push notification token from Apple that is associated with this device if
        /// it's running on the iOS platform.  This must be set before calling <see cref="Start"/>.
        /// </summary>
        public string PushNotificationToken {
            get { return delegated.PushNotificationToken; }
            set { delegated.PushNotificationToken = value; }
        }

        #endregion
        #region Helpers

        public override void OnDestroy() {
            PlayerPrefs.Save();
            if (delegated != null)
            {
                delegated.OnDestroy();
            }
            base.OnDestroy();
        }

        private void OnApplicationPause(bool pauseStatus) {
            if (pauseStatus) {
                PlayerPrefs.Save();
            }
            delegated.OnApplicationPause(pauseStatus);
        }

        internal virtual ImageMessageStore GetImageMessageStore() {
            return delegated.ImageMessageStore;
        }

        internal void NotifyOnSessionConfigured(bool cached) {
            if (OnSessionConfigured != null) OnSessionConfigured(cached);
        }

        internal void NotifyOnSessionConfigurationFailed() {
            if (OnSessionConfigurationFailed != null) OnSessionConfigurationFailed();
        }

        internal void NotifyOnImageCachePopulated() {
            if (OnImageCachePopulated != null) OnImageCachePopulated();
        }

        internal void NotifyOnImageCachingFailed(string cause) {
            if (OnImageCachingFailed != null) OnImageCachingFailed(cause);
        }

        private string GenerateSessionID() {
            return Guid.NewGuid().ToString();
        }
        
        internal static string GenerateHash(string data, string secret) {
            var inputBytes = Encoding.UTF8.GetBytes(data + secret);
            var hash = Utils.ComputeMD5Hash(inputBytes);

            var sb = new StringBuilder();
            for (int i = 0; i < hash.Length; i++) {
                sb.Append(hash[i].ToString("X2"));
            }

            return sb.ToString();
        }

        internal static string FormatURI(string uriPattern, string apiHost, string envKey, string hash) {
            var uri = uriPattern.Replace("{host}", apiHost);
            uri = uri.Replace("{env_key}", envKey);
            uri = uri.Replace("{hash}", hash);
            return uri;
        }

        #endregion

        public int GetBackgroundEventUploadRepeatRateSeconds()
        {
            if (_lifetime < Settings.TimeSendEventsEverySecondInSeconds)
            {
                return 1;
            }

            if (PlayerPrefs.GetInt(PREFS_KEY_SESSIONS_COUNT, -1) < 2)
            {
                return 1;
            }
            
            return Settings.BackgroundEventUploadRepeatRateSeconds;
        }
    }
}
