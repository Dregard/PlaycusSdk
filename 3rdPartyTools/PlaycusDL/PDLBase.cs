using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Cysharp.Threading.Tasks;
using Playcus;
using UnityEngine;

namespace PlaycusDL {

    internal abstract class PDLBase {

        protected static Func<DateTime?> TimestampFunc = new Func<DateTime?>(DefaultTimestampFunc);

        protected readonly PDL pdl;
        protected readonly GameObject gameObject;

        internal PDLBase(PDL pdl) {
            this.pdl = pdl;

            gameObject = pdl.gameObject;
        }

        #if UNITY_EDITOR
        internal PDLBase() {
            pdl = null;
            gameObject = null;
        }
        #endif

        #region Unity Lifecycle

        internal abstract void OnApplicationPause(bool pauseStatus);
        internal abstract void OnDestroy();

        #endregion
        #region Client Interface

        internal abstract void StartSDK(bool newPlayer);
        internal abstract void StopSDK();

        internal abstract EventAction RecordEvent<T>(T gameEvent) where T : GameEvent<T>;
        internal abstract EventAction RecordEvent(string eventName);
        internal abstract EventAction RecordEvent(string eventName, Dictionary<string, object> eventParams);


        internal abstract void RecordPushNotification(Dictionary<string, object> payload);

        internal abstract void RequestSessionConfiguration();

        internal abstract UniTask Upload();
        internal abstract void ClearPersistentData();
        internal abstract void ForgetMe();
        internal abstract void StopTrackingMe();
        internal ImageMessageStore ImageMessageStore { get; set; }

        #endregion
        #region Properties

        protected string EnvironmentKey { get { return pdl.EnvironmentKey; }}
        protected string CollectURL { get { return pdl.CollectURL; }}
        protected string Platform { get { return pdl.Platform; }}
        protected string HashSecret { get {return pdl.HashSecret; }}
        protected string ClientVersion { get { return pdl.ClientVersion; }}
        protected Settings Settings { get { return pdl.Settings; }}

        protected string GameDeviceID { get { return pdl.GameDeviceID; }}
        protected string DeviceID { get { return pdl.DeviceID; }}
        protected string ExternalID { get { return pdl.ExternalID; }}
        protected string AdvertisingID { get { return pdl.AdvertisingID; }}
        protected string GameNetwork { get { return pdl.GameNetwork; }}
        protected string GameLanguage { get { return pdl.GameLanguage; }}
        protected string UserID
        {
            get
            {
                if (string.IsNullOrEmpty(pdl.UserID))
                {
                    pdl.UserID = ServiceLocator.Get<IUserInfoService>().UserID;
                }
                return pdl.UserID;
            }
        }

        protected string SessionID { get { return pdl.SessionID; }}
        protected string Vendor { get { return pdl.Vendor; }}
        protected int UserLevel { get { return pdl.UserLevel; }}

        protected int EventNumber {
            get { return pdl.EventNumber; }
            set { pdl.EventNumber = value; }
        }

        internal abstract bool HasStarted { get; }
        internal abstract bool IsUploading { get; }

        #endregion
        #region Client Configuration

        internal abstract string CrossGameUserID { get; set; }
        internal abstract string AndroidRegistrationID { get; set; }
        internal abstract string PushNotificationToken { get; set; }

        #endregion
        #region Implementation

        protected Coroutine StartCoroutine(IEnumerator routine) {
            return pdl.StartCoroutine(routine);
        }

        protected void InvokeRepeating(string methodName, float time, float repeatRate) {
            pdl.InvokeRepeating(methodName, time, repeatRate);
        }

        protected bool IsInvoking(string methodName) {
            return pdl.IsInvoking(methodName);
        }

        protected void CancelInvoke() {
            pdl.CancelInvoke();
        }

        protected void NewSession() {
          pdl.NewSession();
          pdl.EventNumber = 0;
        }

        internal void UseCollectTimestamp(bool useCollect) {
            if (!useCollect) {
                SetTimestampFunc(DefaultTimestampFunc);
            } else {
                SetTimestampFunc(() => { return null; });
            }
        }

        internal void SetTimestampFunc(Func<DateTime?> TimestampFunc) {
            PDLBase.TimestampFunc = TimestampFunc;
        }

        public static string GetCurrentTimestamp() {
            DateTime? dt = TimestampFunc();
            if (dt.HasValue) {
                String ts = dt.Value.ToString(Settings.EVENT_TIMESTAMP_FORMAT, CultureInfo.InvariantCulture);
                // fix for millisecond timestamp format bug seen on Android.
                if (ts.EndsWith(".1000")) {
                    ts = ts.Replace(".1000", ".999");
                }
                return ts;
            }

            return null; // Collect will insert a timestamp for us.
        }

        private static DateTime? DefaultTimestampFunc() {
            return DateTime.UtcNow;
        }

        #endregion
    }
}
