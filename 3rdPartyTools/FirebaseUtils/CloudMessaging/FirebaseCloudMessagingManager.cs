using UnityEngine;
using UnityEngine.Events;
using System;
using Cysharp.Threading.Tasks;
#if PL_SDK_FIREBASE_ON
using Firebase;
#endif
#if PL_SDK_APPSFLYER_ON
using AppsFlyerSDK;
#endif

namespace Playcus.FirebaseSDK
{
    /// <summary>
    /// https://firebase.google.com/docs/cloud-messaging/unity/client
    /// Register user with firebase cloud messaging
    /// </summary>
    public class FirebaseCloudMessagingManager : MonoBehaviour
    {
        /// PUBLIC
        //public bool useTopics = false;

        /// PRIVATE VARIABLES
        private int _tryLoadCounter = 0;

        //public const string NOTIFICATIONS_ENABLED = "NOTIFICATIONS_ENABLED";
        //private const string TOPIC_DEFAULT = "/topics/default";

#if PL_SDK_FIREBASE_ON

        private void Start()
        {
            Debug.Log("FirebaseCloudMessagingManager Start", gameObject);
            Init();
        }

        private void Init()
        {
            if (FirebaseDependencies.Status != Firebase.DependencyStatus.Available)
            {
                // Try again for 1 sec
                if (_tryLoadCounter < 5)
                {
                    _tryLoadCounter++;
                    Invoke("Init", 1f);
                    Debug.Log($"FirebaseCloudMessagingManager Init reinvoked {_tryLoadCounter}");
                    return;
                }
                else
                {
                    Debug.LogWarning("FirebaseCloudMessagingManager Init NOT completed");
                }
            }
            else
            {
                Firebase.Messaging.FirebaseMessaging.TokenReceived += OnTokenReceived;
                Firebase.Messaging.FirebaseMessaging.MessageReceived += OnMessageReceived;
                Debug.Log($"FirebaseCloudMessagingManager Init receive listeners added");
                /*if (isNotificationsEnabled())
                {
                    subscribeTopics();
                }*/
            }

            EnableTokenRegistration();
        }


        private async UniTask EnableTokenRegistration()
        {
            Debug.Log("FirebaseCloudMessagingManager EnableTokenRegistration requested");
            await UniTask.WaitUntil(() => FirebaseDependencies.Status == Firebase.DependencyStatus.Available); 
            
            Firebase.Messaging.FirebaseMessaging.TokenRegistrationOnInitEnabled = true;
            Debug.Log("FirebaseCloudMessagingManager EnableTokenRegistration settled to true");
        }

        /*public bool isNotificationsEnabled()
        {
            var notificationsEnabled = PlayerPrefs.HasKey(NOTIFICATIONS_ENABLED)
                ? PlayerPrefs.GetInt(NOTIFICATIONS_ENABLED)
                : 1;
            return notificationsEnabled == 1;
        }

        public void setNotificationsEnabled(bool value)
        {
            PlayerPrefs.SetInt(NOTIFICATIONS_ENABLED, value ? 1 : 0);
            if (value)
            {
                subscribeTopics();
            }
            else
            {
                unsubscribeTopics();
            }
        }


        private void subscribeTopics()
        {
            if (!useTopics)
            {
                Debug.LogWarning("UseTopics option must be active in prefab to enable/disable notifications!");
                return;
            }
            Firebase.Messaging.FirebaseMessaging.SubscribeAsync(TOPIC_DEFAULT);
        }

        private void unsubscribeTopics()
        {
            if (!useTopics)
            {
                Debug.LogWarning("UseTopics option must be active in prefab to enable/disable notifications!");
                return;
            }
            Firebase.Messaging.FirebaseMessaging.UnsubscribeAsync(TOPIC_DEFAULT);
        }*/

        public void OnTokenReceived(object sender, Firebase.Messaging.TokenReceivedEventArgs token)
        {
            UnityEngine.Debug.Log("Received Registration Token: " + token.Token, gameObject);
#if PL_SDK_APPSFLYER_ON && STORE_GooglePlay && !UNITY_EDITOR
            AppsFlyer.updateServerUninstallToken(token.Token);
#endif
        }

        public void OnMessageReceived(object sender, Firebase.Messaging.MessageReceivedEventArgs e)
        {
            UnityEngine.Debug.Log("Received a new message from: " + e.Message.From, gameObject);

            // Appsflyer uninstall messages https://support.appsflyer.com/hc/en-us/articles/210289286-Uninstall-measurement#android-uninstall
            if(e.Message.Data.ContainsKey("af-uinstall-tracking")){
                return;
            } else {
                // handleNotification(remoteMessage);
            }
        }


#else
        void Start()
        {
            Debug.LogWarning("FirebaseRemoteConfigManager : Add SDK_FIREBASE to script defined symbols");
        }

#endif

    }
}