using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace PlaycusDL {

    using JSONObject = Dictionary<string, object>;

    internal class PDLNonTracking : PDLBase {

        private bool started;
        private bool uploading;

        internal PDLNonTracking(PDL pdl) : base(pdl) {
        }

        #region Unity Lifecycle

        override internal void OnApplicationPause(bool pauseStatus) {}
        override internal void OnDestroy() {}

        #endregion
        #region Client Interface

        override internal void StartSDK(bool newPlayer) {
            started = true;
            NewSession();

            if (PlayerPrefs.HasKey(PDL.PF_KEY_FORGET_ME)
                && !PlayerPrefs.HasKey(PDL.PF_KEY_FORGOTTEN)) {
                ForgetMe();
            }
        }

        override internal void StopSDK() {
            started = false;
        }

        override internal EventAction RecordEvent<T>(T gameEvent) {
            return EventAction.CreateEmpty(gameEvent as GameEventPDL);
        }

        override internal EventAction RecordEvent(string eventName) {
            return RecordEvent(new GameEventPDL(eventName));
        }

        override internal EventAction RecordEvent(string eventName, Dictionary<string, object> eventParams) {
            return RecordEvent(new GameEventPDL(eventName));
        }

        override internal void RecordPushNotification(Dictionary<string, object> payload) {}

        override internal void RequestSessionConfiguration() {
            pdl.NotifyOnSessionConfigured(false);
        }

        override internal UniTask Upload()
        {
            return default;
        }

        override internal void ClearPersistentData() {}

        internal override void ForgetMe() {
            if (PlayerPrefs.HasKey(PDL.PF_KEY_FORGOTTEN)) {
                Logger.LogDebug("Already forgotten user " + UserID);
                return;
            }

            Logger.LogDebug("Forgetting user " + UserID);
            PlayerPrefs.SetInt(PDL.PF_KEY_FORGET_ME, 1);

            if (IsUploading) return;

            var advertisingId = PlayerPrefs.GetString(PDL.PF_KEY_ADVERTISING_ID);
            var dictionary = new Dictionary<string, object>() {
                    { "eventName", "pdlForgetMe" },
                    { "eventTimestamp", GetCurrentTimestamp() },
                    { "eventUUID", Guid.NewGuid().ToString() },
                    { "sessionID", SessionID },
                    { "userID", UserID },
                    { "eventParams", new Dictionary<string, object>() {
                        { "platform", Platform },
                        { "sdkVersion", Settings.SDK_VERSION },
                        { "pdlAdvertisingId", advertisingId }
                    }}};
            if (string.IsNullOrEmpty(advertisingId)) {
                (dictionary["eventParams"] as Dictionary<string, object>)
                    .Remove("pdlAdvertisingId");
            }

            string json;
            try {
                json = MiniJSON.Json.Serialize(dictionary);
            } catch (Exception e) {
                Logger.LogWarning("Unable to generate JSON for 'pdlForgetMe' event. " + e.Message);
                return;
            }

            var url = (HashSecret != null)
                ? PDL.FormatURI(
                    Settings.COLLECT_HASH_URL_PATTERN.Replace("/bulk", ""),
                    CollectURL,
                    EnvironmentKey,
                    PDL.GenerateHash(json, HashSecret))
                : PDL.FormatURI(
                    Settings.COLLECT_URL_PATTERN.Replace("/bulk", ""),
                    CollectURL,
                    EnvironmentKey,
                    null);

            HttpRequest request = new HttpRequest(url) {
                HTTPMethod = HttpRequest.HTTPMethodType.POST,
                HTTPBody = json
            };
            request.setHeader("Content-Type", "application/json");

            StartCoroutine(Send(
                request,
                () => {
                    Logger.LogDebug("Forgot user " + UserID);
                    PlayerPrefs.SetInt(PDL.PF_KEY_FORGOTTEN, 1);
                }));
        }

        internal override void StopTrackingMe()
        {
            if (PlayerPrefs.HasKey(PDL.PF_KEY_FORGOTTEN)) {
                Logger.LogDebug("Already forgotten user " + UserID);
                return;
            }
            if (PlayerPrefs.HasKey(PDL.PF_KEY_STOP_TRACKING_ME)) {
                Logger.LogDebug("Already stopped tracking user " + UserID);
                return;
            }

            Logger.LogDebug(" Stopped Tracking " + UserID);
            PlayerPrefs.SetInt(PDL.PF_KEY_STOP_TRACKING_ME, 1);
        }

        #endregion
        #region Properties

        override internal bool HasStarted { get { return started; }}
        override internal bool IsUploading { get { return uploading; }}

        #endregion
        #region Client Configuration

        override internal string CrossGameUserID { get; set; }
        override internal string PushNotificationToken { get; set; }
        override internal string AndroidRegistrationID { get; set; }

        #endregion
        #region Implementation

        private System.Collections.IEnumerator Send(HttpRequest request, Action onSuccess) {
            int attempts = 0;
            bool succeeded = false;

            Action<int, string, string> onCompletion = (statusCode, data, error) => {
                if (statusCode > 0 && statusCode < 400) {
                    succeeded = true;
                    onSuccess();
                } else {
                    Logger.LogDebug("Error posting events: " + error + " " + data);
                }
            };

            do {
                uploading = true;
                yield return StartCoroutine(Network.SendRequest(request, onCompletion));

                if (succeeded || ++attempts < Settings.HttpRequestMaxRetries) {
                    uploading = false;
                    break;
                }

                yield return new WaitForSeconds(Settings.HttpRequestRetryDelaySeconds);
            } while (attempts < Settings.HttpRequestMaxRetries);
        }

        #endregion
    }
}
