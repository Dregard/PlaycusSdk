using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace PlaycusDL {

    using JSONObject = Dictionary<string, object>;

    internal class PDLImpl : PDLBase {

        private readonly EventStore eventStore = null;
        private readonly ActionStore actionStore = null;
        private readonly ExecutionCountManager executionCountManager = null;

        private bool started = false;
        private bool uploading = false;
        private DateTime lastActive = DateTime.MinValue;

        private GameEventPDL launchNotificationEvent = null;

        private string pushNotificationToken = null;
        private string androidRegistrationId = null;

        private ReadOnlyCollection<string> whitelistDps =
            new ReadOnlyCollection<string>(new List<string>());
        private ReadOnlyCollection<string> whitelistEvents =
            new ReadOnlyCollection<string>(new List<string>());
        private Dictionary<string, ReadOnlyCollection<EventTrigger>> eventTriggers =
            new Dictionary<string, ReadOnlyCollection<EventTrigger>>();
        private ReadOnlyCollection<string> cacheImages =
            new ReadOnlyCollection<string>(new List<string>());

        private bool hasSentDefaultEvents = false;
        private bool newPlayer;
        private int retryAttempts = 0;

        internal PDLImpl(PDL pdl) : base(pdl) {
            string eventStorePath = null;
            if (Settings.UseEventStore) {
                eventStorePath = Settings.EVENT_STORAGE_PATH
                    .Replace("{persistent_path}", Application.persistentDataPath);
                if (!Utils.IsDirectoryWritable(eventStorePath)) {
                    Logger.LogWarning("Event store path unwritable, event caching disabled.");
                    Settings.UseEventStore = false;
                }
            }

            eventStore = new EventStore(eventStorePath);
            if (Settings.UseEventStore && !eventStore.IsInitialised) {
                // failed to access files for some reason
                Logger.LogWarning("Failed to access event store path, event caching disabled.");
                Settings.UseEventStore = false;
                eventStore = new EventStore(eventStorePath);
            }
            actionStore = new ActionStore(Settings.ACTIONS_STORAGE_PATH
                .Replace("{persistent_path}", Application.persistentDataPath));
            ImageMessageStore = new ImageMessageStore(pdl);
            executionCountManager = new ExecutionCountManager();

        }

        #region Unity Lifecycle

        override internal void OnApplicationPause(bool pauseStatus) {
            if (pauseStatus) {
                lastActive = DateTime.UtcNow;
                eventStore.FlushBuffers();
            } else {
                var backgroundSeconds = (DateTime.UtcNow - lastActive).TotalSeconds;
                if (backgroundSeconds > Settings.SessionTimeoutSeconds) {
                    lastActive = DateTime.MinValue;
                    retryAttempts = 0;
                    NewSession();
                }
            }
        }

        override internal void OnDestroy() {
            if (eventStore != null) {
                eventStore.FlushBuffers();
                eventStore.Dispose();
            }
        }

        #endregion
        #region Client Interface

        override internal async void StartSDK(bool newPlayer){
            started = true;
            this.newPlayer = newPlayer;
            if (newPlayer) {
                actionStore.Clear();
                hasSentDefaultEvents = false;
            }

            retryAttempts = 0;
            NewSession();

            // setup automated event uploads
            if (Settings.BackgroundEventUpload)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(Settings.BackgroundEventUploadStartDelaySeconds), ignoreTimeScale: true);
                    
                await Upload();
                
                while (Settings.BackgroundEventUpload)
                {
                    
                    await UniTask.Delay(TimeSpan.FromSeconds(pdl.GetBackgroundEventUploadRepeatRateSeconds()), ignoreTimeScale: true);
                    
                    await Upload();
                }
            }
        }

        override internal void StopSDK() {
            if (started) {
                Logger.LogInfo("Stopping PDL SDK");

                RecordEvent("gameEnded").Run();
                CancelInvoke();
                Upload();

                started = false;
                newPlayer = false;
                hasSentDefaultEvents = false;
                retryAttempts = 0;
            } else {
                Logger.LogDebug("SDK not running");
            }
        }

        override internal EventAction RecordEvent<T>(T gameEvent) {
            if (!started) {
                throw new Exception("You must first start the SDK via the StartSDK method");
            } else if (whitelistEvents.Count != 0 && !whitelistEvents.Contains(gameEvent.Name)) {
                Logger.LogDebug("Event " + gameEvent.Name + " is not whitelisted, ignoring");
                return EventAction.CreateEmpty(gameEvent as GameEventPDL);
            }

            gameEvent.AddParam("platform", Platform);
            gameEvent.AddParam("sdkVersion", Settings.SDK_VERSION);

            var eventSchema = gameEvent.AsDictionary();

            eventSchema = AddGeneralParamsToEventSchema(eventSchema);

            try {
                string json = MiniJSON.Json.Serialize(eventSchema);
                if (!this.eventStore.Push(json)) {
                    Logger.LogWarning("Event store full, dropping '"+gameEvent.Name+"' event.");
                }
            } catch (Exception ex) {
                Logger.LogWarning("Unable to generate JSON for '"+gameEvent.Name+"' event. "+ex.Message);
            }

            return new EventAction(
                gameEvent as GameEventPDL,
                eventTriggers.ContainsKey(gameEvent.Name)
                    ? eventTriggers[gameEvent.Name]
                    : EventAction.EMPTY_TRIGGERS, actionStore, Settings);
        }

        private Dictionary<string, object> AddGeneralParamsToEventSchema(Dictionary<string, object> eventSchema)
        {
            eventSchema["userID"] = this.UserID;
            eventSchema["sessionID"] = this.SessionID;
            eventSchema["eventUUID"] = Guid.NewGuid().ToString();
            eventSchema["eventNumber"] = ++this.EventNumber;

            if (!string.IsNullOrEmpty(this.DeviceID))
            {
                eventSchema["deviceID"] = this.DeviceID;
            }
            if (!string.IsNullOrEmpty(this.GameDeviceID))
            {
                eventSchema["gameDeviceID"] = this.GameDeviceID;
            }
            if (!string.IsNullOrEmpty(this.ExternalID))
            {
                eventSchema["externalID"] = this.ExternalID;
            }
            if (!string.IsNullOrEmpty(this.AdvertisingID))
            {
                eventSchema["advertisingID"] = this.AdvertisingID;
            }
            if (!string.IsNullOrEmpty(this.GameNetwork))
            {
                eventSchema["gameNetwork"] = this.GameNetwork;
            }
            if (!string.IsNullOrEmpty(ClientVersion))
            {
                eventSchema["clientVersion"] = this.ClientVersion;
            }

            string currentTimestamp = GetCurrentTimestamp();
            if (!string.IsNullOrEmpty(currentTimestamp)) {
                eventSchema["eventTimestamp"] = currentTimestamp;
            }

            if (!string.IsNullOrEmpty(ClientInfo.TimezoneOffset))
            {
                eventSchema["timezoneOffset"] = Convert.ToInt32(ClientInfo.TimezoneOffset);
            }

            var firstSession = GetFirstSession();

            eventSchema["timeSinceFirstSession"] = firstSession != null
                ? ((TimeSpan) (DateTime.UtcNow - firstSession)).TotalMilliseconds
                : 0;

            var lastSession = GetLastSession();

            eventSchema["timeSinceLastSession"] = lastSession != null
                ? ((TimeSpan) (DateTime.UtcNow - lastSession)).TotalMilliseconds
                : 0;

            if (!string.IsNullOrEmpty(Settings.SDK_VERSION))
            {
                eventSchema["sdkVersion"] = Settings.SDK_VERSION;
            }

            if (!string.IsNullOrEmpty(ClientInfo.DeviceName))
            {
                eventSchema["deviceName"] = ClientInfo.DeviceName;
            }

            if (!string.IsNullOrEmpty(ClientInfo.DeviceModel))
            {
                eventSchema["hardwareVersion"] = ClientInfo.DeviceModel;
            }

            if (!string.IsNullOrEmpty(ClientInfo.DeviceType))
            {
                eventSchema["deviceType"] = ClientInfo.DeviceType;
            }

            if (!string.IsNullOrEmpty(ClientInfo.OperatingSystemVersion))
            {
                eventSchema["operatingSystemVersion"] = ClientInfo.OperatingSystemVersion;
            }

            if (!string.IsNullOrEmpty(ClientInfo.Manufacturer))
            {
                eventSchema["manufacturer"] = ClientInfo.Manufacturer;
            }

            if (!string.IsNullOrEmpty(Platform))
            {
                eventSchema["platform"] = Platform;
            }

            if (!string.IsNullOrEmpty(ClientInfo.OperatingSystem))
            {
                eventSchema["operatingSystem"] = ClientInfo.OperatingSystem;
            }

            if (!string.IsNullOrEmpty(ClientInfo.CountryCode))
            {
                eventSchema["country"] = ClientInfo.CountryCode;
            }

            if (!string.IsNullOrEmpty(ClientInfo.LanguageCode))
            {
                eventSchema["userLanguage"] = ClientInfo.LanguageCode;
            }

            /*
            if (!string.IsNullOrEmpty(ClientInfo.Locale))
            {
                eventSchema["userLocale"] = ClientInfo.Locale;
            }

            if (!string.IsNullOrEmpty(Locale) && !string.IsNullOrEmpty(ClientInfo.CountryCode))
            {
                eventSchema["locale"] = Locale + "_" + ClientInfo.CountryCode;
            }
            */

            if (!string.IsNullOrEmpty(this.GameLanguage))
            {
                eventSchema["gameLanguage"] = this.GameLanguage;
            }

            if (PlayerPrefs.HasKey(PDL.PF_KEY_FIRST_SESSION))
            {
                var timeString = PlayerPrefs.GetString(PDL.PF_KEY_FIRST_SESSION);
                eventSchema["pr_install_date"] = timeString.Split(' ')[0];
            }

            if (!string.IsNullOrEmpty(Vendor))
            {
                eventSchema["vendor"] = Vendor;
            }

            return eventSchema;
        }

        override internal EventAction RecordEvent(string eventName) {
            return RecordEvent(new GameEventPDL(eventName));
        }

        override internal EventAction RecordEvent(string eventName, Dictionary<string, object> eventParams) {
            var gameEvent = new GameEventPDL(eventName);
            foreach (var key in eventParams.Keys) {
                gameEvent.AddParam(key, eventParams[key]);
            }
            return RecordEvent(gameEvent);
        }

        override internal void RecordPushNotification(Dictionary<string, object> payload) {
            Logger.LogDebug("Received push notification: "+payload);

            var notificationEvent = new GameEventPDL("notificationOpened");
            try {
                if (payload.ContainsKey("_dlId")) {
                    notificationEvent.AddParam("notificationId", Convert.ToInt64(payload["_dlId"]));
                }
                if (payload.ContainsKey("_dlName")) {
                    notificationEvent.AddParam("notificationName", payload["_dlName"]);
                }

                bool insertCommunicationAttrs = false;
                if (payload.ContainsKey("_dlCampaign")) {
                    notificationEvent.AddParam("campaignId", Convert.ToInt64(payload["_dlCampaign"]));
                    insertCommunicationAttrs = true;
                }
                if (payload.ContainsKey("_dlCohort")) {
                    notificationEvent.AddParam("cohortId", Convert.ToInt64(payload["_dlCohort"]));
                    insertCommunicationAttrs = true;
                }
                if (insertCommunicationAttrs && payload.ContainsKey("_dlCommunicationSender")) {
                    // _dlCommunicationSender inserted by respective native notification SDK
                    notificationEvent.AddParam("communicationSender", payload["_dlCommunicationSender"]);
                    notificationEvent.AddParam("communicationState", "OPEN");
                }

                if (payload.ContainsKey("_dlLaunch")) {
                    // _dlLaunch inserted by respective native notification SDK
                    notificationEvent.AddParam("notificationLaunch", Convert.ToBoolean(payload["_dlLaunch"]));
                }
                if (payload.ContainsKey("_dlCampaign")) {
                    notificationEvent.AddParam("campaignId", Convert.ToInt64(payload["_dlCampaign"]));
                }
                if (payload.ContainsKey("_dlCohort")) {
                    notificationEvent.AddParam("cohortId", Convert.ToInt64(payload["_dlCohort"]));
                }
                notificationEvent.AddParam("communicationState", "OPEN");
            } catch (Exception ex) {
                Logger.LogError("Error parsing push notification payload. "+ex.Message);
            }

            if (this.started) {
                RecordEvent(notificationEvent).Run();
            } else {
                this.launchNotificationEvent = notificationEvent;
            }
        }

        private DateTime? GetFirstSession()
        {
            return PlayerPrefs.HasKey(PDL.PF_KEY_FIRST_SESSION)
                ? DateTime.ParseExact(
                    PlayerPrefs.GetString(PDL.PF_KEY_FIRST_SESSION),
                    Settings.EVENT_TIMESTAMP_FORMAT,
                    CultureInfo.InvariantCulture)
                : (DateTime?) null;
        }

        private DateTime? GetLastSession()
        {
            return PlayerPrefs.HasKey(PDL.PF_KEY_LAST_SESSION)
                ? DateTime.ParseExact(
                    PlayerPrefs.GetString(PDL.PF_KEY_LAST_SESSION),
                    Settings.EVENT_TIMESTAMP_FORMAT,
                    CultureInfo.InvariantCulture)
                : (DateTime?) null;
        }

        override internal void RequestSessionConfiguration() {
            Logger.LogDebug("Requesting session configuration");

            pdl.NotifyOnSessionConfigured(false);
            TriggerDefaultEvents(newPlayer);

            Logger.LogDebug("Session configured");
        }

        override internal async UniTask Upload() {

            if (!started) {
                Logger.LogError("You must first start the SDK via the StartSDK method.");
                return;
            }

            if (IsUploading) {
                Logger.LogWarning("Event upload already in progress, try again later.");
                return;
            }

            await UploadTask();
        }
     
        override internal void ClearPersistentData() {
            if (eventStore != null) eventStore.ClearAll();
            if (actionStore != null) actionStore.Clear();
            if (ImageMessageStore != null) ImageMessageStore.Clear();
            if (executionCountManager != null) executionCountManager.Clear();
        }

        internal override void ForgetMe() {
            if (HasStarted) StopSDK();
        }

        internal override void StopTrackingMe()
        {
            if (HasStarted) StopSDK();
        }

        #endregion
        #region Properties

        override internal bool HasStarted { get { return started; }}
        override internal bool IsUploading { get { return uploading; }}

        #endregion
        #region Client Configuration

        override internal string CrossGameUserID {
            get { return PlayerPrefs.GetString(PDL.PF_KEY_CROSS_GAME_USER_ID, null); }

            set {
                if (String.IsNullOrEmpty(value)) {
                    Logger.LogWarning("CrossGameUserID cannot be null or empty");
                } else {
                    PlayerPrefs.SetString(PDL.PF_KEY_CROSS_GAME_USER_ID, value);

                    if (started) {
                        RecordEvent(new GameEventPDL("pdlRegisterCrossGameUserID")
                            .AddParam("pdlCrossGameUserID", value));
                    } // else send with gameStarted event
                }
            }
        }

        override internal string AndroidRegistrationID {
            get { return androidRegistrationId; }
            set {
                if (!String.IsNullOrEmpty(value) && value != androidRegistrationId) {
                    var notificationServicesEvent = new GameEventPDL("notificationServices")
                        .AddParam("androidRegistrationID", value);

                    if (started) {
                        RecordEvent(notificationServicesEvent);
                    } // else send with gameStarted event
                    androidRegistrationId = value;
                }
            }
        }

        override internal string PushNotificationToken {
            get { return pushNotificationToken; }
            set {
                if (!String.IsNullOrEmpty(value) && value != pushNotificationToken) {
                    var notificationServicesEvent = new GameEventPDL("notificationServices")
                        .AddParam("pushNotificationToken", value);

                    if (started) {
                        RecordEvent(notificationServicesEvent);
                    } // else send with gameStarted event
                    pushNotificationToken = value;
                }
            }
        }

        #endregion
        #region Helpers

        private async UniTask UploadTask()
        {
            uploading = true;

            try {
                // Swap over event queue.
                this.eventStore.Swap();

                // Create bulk event message to post.
                List<string> events = eventStore.Read();

                if (events != null && events.Count > 0)
                {
                    Logger.LogDebug("Starting event upload.");

                    Action<bool, int> postCb = (succeeded, statusCode) =>
                    {
                        if (succeeded)
                        {
                            Logger.LogDebug("Event upload successful.");
                            this.eventStore.ClearOut();
                        }
                        else if (statusCode == 400) {
                            Logger.LogDebug("Collect rejected events, possible corruption.");
                            this.eventStore.ClearOut();
                        }
                        else {
                            Logger.LogWarning("Event upload failed - try again later.");
                        }
                    };

                    await PostEvents(events.ToArray(), postCb);
                }
            } finally {
                uploading = false;
            }
        }

        private async UniTask PostEvents(string[] events, Action<bool, int> resultCallback)
        {
            string bulkEvent = "{\"eventList\":[" + String.Join(",", events) + "]}";
            string url;
            if (HashSecret != null) {
                string md5Hash = PDL.GenerateHash(bulkEvent, this.HashSecret);
                url = PDL.FormatURI(Settings.COLLECT_HASH_URL_PATTERN, this.CollectURL, this.EnvironmentKey, md5Hash);
            } else {
                url = PDL.FormatURI(Settings.COLLECT_URL_PATTERN, this.CollectURL, this.EnvironmentKey, null);
            }

            int attempts = 0;
            bool succeeded = false;
            int status = 0;

            Action<int, string, string> completionHandler = (statusCode, data, error) => {
                if (statusCode > 0 && statusCode < 400) {
                    succeeded = true;
                }
                else {
                    Logger.LogDebug("Error posting events: "+error+" "+data);
                }
                status = statusCode;
            };

            HttpRequest request = new HttpRequest(url);
            request.HTTPMethod = HttpRequest.HTTPMethodType.POST;
            request.HTTPBody = bulkEvent;
            request.setHeader("Content-Type", "application/json");

            do {
               await Network.SendRequest(request, completionHandler);

                if (succeeded || ++attempts < Settings.HttpRequestMaxRetries) break;

                await UniTask.Delay(TimeSpan.FromSeconds(Settings.HttpRequestRetryDelaySeconds), ignoreTimeScale: true);
            } while (attempts < Settings.HttpRequestMaxRetries);

            resultCallback(succeeded, status);

/*
            resultCallback(true, 204);
            yield return null;
*/
        }

        private void TriggerDefaultEvents(bool newPlayer)
        {
            if (launchNotificationEvent != null) {
                RecordEvent(launchNotificationEvent).Run();
                launchNotificationEvent = null;
            }
            if (hasSentDefaultEvents) return;
            if (Settings.OnFirstRunSendNewPlayerEvent && newPlayer)
            {
                Logger.LogDebug("Sending 'newPlayer' event");

                var newPlayerEvent = new GameEventPDL("newPlayer");
                if (ClientInfo.CountryCode != null) {
                    newPlayerEvent.AddParam("userCountry", ClientInfo.CountryCode);
                }

                RecordEvent(newPlayerEvent).Run();
            }

            if (Settings.OnInitSendGameStartedEvent)
            {
                Logger.LogDebug("Sending 'gameStarted' event");

                var gameStartedEvent = new GameEventPDL("gameStarted")
                    .AddParam("clientVersion", this.ClientVersion);
                    // .AddParam("userLocale", ClientInfo.Locale);

                if (!string.IsNullOrEmpty(CrossGameUserID)) {
                    gameStartedEvent.AddParam("pdlCrossGameUserID", CrossGameUserID);
                }

                if (!String.IsNullOrEmpty(this.PushNotificationToken)) {
                    gameStartedEvent.AddParam("pushNotificationToken", this.PushNotificationToken);
                }

                if (!String.IsNullOrEmpty(this.AndroidRegistrationID)) {
                    gameStartedEvent.AddParam("androidRegistrationID", this.AndroidRegistrationID);
                }

                RecordEvent(gameStartedEvent).Run();
            }

            if (Settings.OnInitSendClientDeviceEvent)
            {
                Logger.LogDebug("Sending 'clientDevice' event");

                var clientDeviceEvent = new GameEventPDL("clientDevice")
                    .AddParam("deviceName", ClientInfo.DeviceName)
                    .AddParam("deviceType", ClientInfo.DeviceType)
                    .AddParam("hardwareVersion", ClientInfo.DeviceModel)
                    .AddParam("operatingSystem", ClientInfo.OperatingSystem)
                    .AddParam("operatingSystemVersion", ClientInfo.OperatingSystemVersion)
                    .AddParam("timezoneOffset", ClientInfo.TimezoneOffset)
                    .AddParam("userLanguage", ClientInfo.LanguageCode);

                if (ClientInfo.Manufacturer != null) {
                    clientDeviceEvent.AddParam("manufacturer", ClientInfo.Manufacturer);
                }

                RecordEvent(clientDeviceEvent).Run();
            }

            hasSentDefaultEvents = true;
        }

        #endregion
    }
}
