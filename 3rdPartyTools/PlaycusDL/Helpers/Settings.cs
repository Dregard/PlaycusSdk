namespace PlaycusDL
{
    public class Settings
    {
        internal static readonly string SDK_VERSION = "DDUSDK v4.13.1|PDL 0.1";

        internal static readonly string EVENT_STORAGE_PATH = "{persistent_path}/pdlsdk/events/";
        internal static readonly string ACTIONS_STORAGE_PATH = "{persistent_path}/pdlsdk/actions/";
        internal static readonly string LEGACY_SETTINGS_STORAGE_PATH = "{persistent_path}/GASettings.ini";
        internal static readonly string EVENT_TIMESTAMP_FORMAT = "yyyy-MM-dd HH:mm:ss.fff";
        internal static readonly string USERID_URL_PATTERN = "{host}/uuid";
        internal static readonly string COLLECT_URL_PATTERN = "{host}?app_name={env_key}&bulk";
        internal static readonly string COLLECT_HASH_URL_PATTERN = "{host}/{env_key}/bulk/hash/{hash}";

        private bool _debugMode = false;

        internal Settings()
        {
            // defines default behaviour of the SDK

            DebugMode = false;

            OnFirstRunSendNewPlayerEvent = true;
            OnInitSendClientDeviceEvent = true;
            OnInitSendGameStartedEvent = true;

            HttpRequestRetryDelaySeconds = 2;
            HttpRequestMaxRetries = 5;              // Rely on BackgroundEventUploadRepeatRateSeconds to retry
            #if UNITY_5_6_OR_NEWER
            HttpRequestCollectTimeoutSeconds = 55;
            #else
            HttpRequestCollectTimeoutSeconds = 30;  // 30 seconds is max old Unity allows
            #endif

            BackgroundEventUpload = true;   // send events automatically by default
            BackgroundEventUploadStartDelaySeconds = 0;
            TimeSendEventsEverySecondInSeconds = 3600;
            BackgroundEventUploadRepeatRateSeconds = 60; // Starts to be used after the first session has ended and "TimeSendEventsEverySecondInSeconds" has passed
            HttpRequestConfigurationTimeoutSeconds = 30;
            HttpRequestConfigurationMaxRetries = 5;
            HttpRequestConfigurationRetryBackoffFactorSeconds = 5;

            #if UNITY_WEBPLAYER || UNITY_WEBGL
            UseEventStore = false;
            #else
            UseEventStore = true;
            #endif

            SessionTimeoutSeconds = 10 * 60;
            ImageCacheLimitMB = 50;
            MaxConcurrentImageCacheFetches = 3;
            MultipleActionsForEventTriggerEnabled = false;
            MultipleActionsForImageMessagesEnabled = false;
        }

        /// <summary>
        /// Controls whether a 'newPlayer' event is sent the first time the game is played.
        /// </summary>
        public bool OnFirstRunSendNewPlayerEvent { get; set; }

        /// <summary>
        /// Controls whether a 'clientDevice' event is sent after the Init call.
        /// </summary>
        public bool OnInitSendClientDeviceEvent { get; set; }

        /// <summary>
        /// Controls whether a 'gameStarted' event is sent after the Init call.
        /// </summary>
        public bool OnInitSendGameStartedEvent { get; set; }

        /// <summary>
        /// Controls if additional debug is output to the console.
        /// </summary>
        public bool DebugMode
        {
            get
            {
                return _debugMode;
            }
            set
            {
                Logger.SetLogLevel(value ? Logger.Level.DEBUG : Logger.Level.WARNING);
                _debugMode = value;
            }
        }

        /// <summary>
        /// Controls the time in seconds between retrying a failed Http request.
        /// </summary>
        public float HttpRequestRetryDelaySeconds { get; set; }

        /// <summary>
        /// Controls the number of times we retry an Http request before giving up.
        /// </summary>
        public int HttpRequestMaxRetries { get; set; }

        /// <summary>
        /// Controls the default timeout for uploading events to Collect.
        /// </summary>
        public int HttpRequestCollectTimeoutSeconds { get; set; }

        /// <summary>
        /// Controls if events are uploaded automatically in the background.
        /// </summary>
        public bool BackgroundEventUpload { get; set; }

        /// <summary>
        /// Controls how long after the <see cref="Init"/> call we wait before
        /// sending the first event upload.
        /// </summary>
        public int BackgroundEventUploadStartDelaySeconds { get; set; }

        /// <summary>
        /// Controls how fequently events are uploaded automatically.
        /// </summary>
        public int BackgroundEventUploadRepeatRateSeconds { get; set; }
        
        /// <summary>
        /// Controls how long events should be sent every second.
        /// </summary>
        public int TimeSendEventsEverySecondInSeconds { get; set; }

        /// <summary>
        /// Controls if the event store should be used or not.  The default
        /// is for TRUE unless UNITY_WEBPLAYER or UNITY_WEBGL is defined.
        /// </summary>
        public bool UseEventStore { get; set; }

        /// <summary>
        /// Controls the amount of time the game can be backgrounded before we
        /// consider a new session to have started.  A value of 0 disables
        /// automatically generating new sessions.
        /// </summary>
        /// <value>The session timeout seconds.</value>
        public int SessionTimeoutSeconds { get; set; }

        /// <summary>
        /// Specifies the size, in MB, of the Image Message Cache.
        /// This is not an exact limit, but once this limit has been exceeded,
        /// no more caching will be attempted.
        /// </summary>
        public int ImageCacheLimitMB { get; set; }

        /// <summary>
        /// Specifies the maximum number of concurrent images to fetch when populating the cache.
        /// High values of this, combined with a lot of large image messages on the environment might lead to instability.
        /// Low values of this might lead to delays in showing image messages very early on in the game.
        /// </summary>
        public int MaxConcurrentImageCacheFetches { get; set; }

        /// <summary>
        /// Controls user consent for advertiser tracking.
        ///
        /// Changes to this value will be applied on the next session.
        /// </summary>
        public bool AdvertiserGdprUserConsent { get; set; }

        /// <summary>
        /// Controls whether the current user should be age restricted (under 16).
        ///
        /// Changes to this value will be applied on the next session.
        /// </summary>
        public bool AdvertiserGdprAgeRestrictedUser { get; set; }

        /// <summary>
	    ///Controls whether multiple Event-Triggers can call the callback sequentially.
	    ///
        /// </summary>
        public bool MultipleActionsForEventTriggerEnabled { get; set; }

        /// <summary>
        /// Controls whether multiple actions for Image Message is enabled.
        /// </summary>
        public bool MultipleActionsForImageMessagesEnabled { get; set; }

        public GameParametersHandler DefaultGameParameterHandler { get; set; }

        public int HttpRequestConfigurationTimeoutSeconds { get; set; }
        public int HttpRequestConfigurationMaxRetries { get; set; }
        public int HttpRequestConfigurationRetryBackoffFactorSeconds { get; set; }
    }
}
