using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if PL_SDK_FIREBASE_ON
using Playcus.FirebaseSDK;
using Firebase.Analytics;
#endif
#if PL_SDK_FIREBASE_ON && !PL_CRASHLYTICS_OFF
using Firebase.Crashlytics;

#endif

namespace Playcus.Analytics
{
    /// <summary>
    /// For AnalyticsService used only! Don't use directly! (Simonenko Alexey)
    /// Firebase Analytics https://firebase.google.com/docs/analytics/
    /// All playcus events list https://docs.google.com/spreadsheets/d/1JjSoB1pyAnKDIZ4Ir3PsFESau0e4rwD1WF_-PFT0bTg
    /// </summary>
    public partial class FirebaseAnalyticServicePlatform : AnalyticsServicePlatform
    {
#region consent

        public static void SetConsent(bool consentGiven)
        {
#if PL_SDK_FIREBASE_ON
            if (FirebaseSDK.FirebaseDependencies.Status != Firebase.DependencyStatus.Available)
                return;
            //Configuration
            FirebaseAnalytics.SetAnalyticsCollectionEnabled(consentGiven);
#if !PL_CRASHLYTICS_OFF
            Crashlytics.IsCrashlyticsCollectionEnabled = consentGiven;
#endif
#endif
        }
        
#endregion

        public override bool CanTesterBeTracked()
        {
            return false;
        }

        // CONFIG
        [Header("Settings")] [SerializeField] private int _sessionTimeoutDurationMS = 180000;

#if PL_SDK_FIREBASE_ON

        // PRIVATE
        private Action _initCallbackFunction;
        private IAnalyticsManager _analyticsManager;


        public override void TryToInit()
        {
            StartCoroutine(Init());
        }


        IEnumerator Init()
        {
            yield return new WaitUntil(() =>
                FirebaseDependencies.Status == Firebase.DependencyStatus.Available);

          
            _analyticsManager = ServiceLocator.Get<IAnalyticsManager>();

            _analyticsManager.LogDebug("FirebaseAnalyticServicePlatform init", true);
            try
            {
                TimeSpan sessionTimeout = new TimeSpan(_sessionTimeoutDurationMS * TimeSpan.TicksPerMillisecond);
                FirebaseAnalytics.SetSessionTimeoutDuration(sessionTimeout);

                //User properties
                //FirebaseAnalytics.SetUserId(SystemInfo.deviceUniqueIdentifier);
                //FirebaseAnalytics.SetUserProperty(FirebaseAnalytics.UserPropertySignUpMethod, "Google");

                InitComplete();

                //Init events
                FirebaseAnalytics.LogEvent(FirebaseAnalytics.EventAppOpen);
                FirebaseAnalytics.LogEvent(FirebaseAnalytics.EventLogin);

                _analyticsManager.LogDebug("FirebaseAnalyticServicePlatform InitComplete", true);
            }
            catch (Exception e)
            {
                Debug.LogError($"FirebaseAnalyticServicePlatform:Init error {e.Message}");
            }

            yield return null;
        }


        private void OnApplicationPause(bool pause)
        {
            //SDK event
            if (!pause && IsInited)
                FirebaseAnalytics.LogEvent(FirebaseAnalytics.EventAppOpen);
        }

        public override void TrackEvent(AnalyticsEvent currentEvent)
        {
            try
            {
                //Main parameter is value (amount)
                int amount = 0;
                if (currentEvent.Parameters.ContainsKey(AnalyticsProperties.pr_amount.ToString()))
                {
                    Int32.TryParse(currentEvent.Parameters[AnalyticsProperties.pr_amount.ToString()].ToString(),
                        out amount);
                }

                currentEvent.Parameters.Add(FirebaseAnalytics.ParameterValue, amount);

                //Convert to array firebase parameters
                Parameter[] eventParameters = new Parameter[currentEvent.Parameters.Count];
                int i = 0;
                foreach (KeyValuePair<string, object> kvp in currentEvent.Parameters)
                {
                    eventParameters[i] = new Parameter(kvp.Key, kvp.Value.ToString());
                    i++;
                }

                // Additional SDK predefined events block track of custom events!
                if (currentEvent.EventKey == AnalyticsEvents.pl_ads_revenue.ToString())
                {
                    AdRevenue(currentEvent.pr_currency, currentEvent.pr_revenue, currentEvent.pr_ad_network,
                        currentEvent.pr_ad_format, currentEvent.pr_placement);
                }
                else
                {
                    Func<AnalyticsEvent, bool> handler = null;
                    HandleCustomEvent(ref handler);

                    if (handler == null || handler.Invoke(currentEvent) == false)
                    {
                        // Track custom event
                        FirebaseAnalytics.LogEvent(currentEvent.EventKey, eventParameters);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError("FirebaseAnalyticServicePlatform Exception", gameObject);
                Debug.LogError(e.ToString(), gameObject);
                throw;
            }
        }

        partial void HandleCustomEvent(ref Func<AnalyticsEvent, bool> handler);

        public void AdRevenue(string currency, string revenue, string network, string format, string placement)
        {
            /*
            // Revenue from admob is already in firebase
            if (network.ToLower() == "admob" || network == "AdMob" || network.ToLower() == "google" ||
                network.ToLower() == "google admob")
            {
                return;
            }
            */

            double revenueValue = 0.0;
            double.TryParse(revenue, out revenueValue);
           /* if (revenueValue > 0.0)
            {
                // Fix 0.00001 values for firebase analytics
                revenueValue *= 100.0;
            }*/
            Debug.Log($"FirebaseAnalyticServicePlatform: AdRevenue currency {currency},revenue {revenue},revenueValue {revenueValue},network {network},format {format},placement {placement}");
            FirebaseAnalytics.LogEvent(FirebaseAnalytics.EventAdImpression,
                new Parameter[]
                {
                    new Parameter(FirebaseAnalytics.ParameterAdPlatform, "AppLovin"),
                    new Parameter(FirebaseAnalytics.ParameterAdSource, network),
                    new Parameter(FirebaseAnalytics.ParameterAdUnitName, placement),
                    new Parameter(FirebaseAnalytics.ParameterAdFormat, format),
                    new Parameter(FirebaseAnalytics.ParameterCurrency, currency),
                    new Parameter(FirebaseAnalytics.ParameterValue, revenueValue)
                }
            );
        }

        /*// This predefined SDK events was returned to custom realisation (Simonenko Alexey)

        public override void TutorialStarted(string eventKey)
        {
            FirebaseAnalytics.LogEvent(FirebaseAnalytics.EventTutorialBegin);
        }

        public override void TutorialCompleted(string eventKey, int step)
        {
            FirebaseAnalytics.LogEvent(FirebaseAnalytics.EventTutorialComplete,
                new Parameter[]
                {
                    new Parameter(FirebaseAnalytics.ParameterValue, step)
                }
            );
        }

        public override void UserLevelAchieved(string eventKey, int level)
        {
            //SDK event
            FirebaseAnalytics.LogEvent(FirebaseAnalytics.EventLevelUp, FirebaseAnalytics.ParameterLevel, level);
        }
        

        public override void LevelStarted(string eventKey, int level, string levelType)
        {
            FirebaseAnalytics.LogEvent(FirebaseAnalytics.EventLevelStart,
                new Parameter[]
                {
                    new Parameter(FirebaseAnalytics.ParameterLevelName, level)
                }
            );
        }

        public override void LevelFailed(string eventKey, int level, string levelType, int score)
        {
            FirebaseAnalytics.LogEvent(FirebaseAnalytics.EventLevelEnd,
                new Parameter[]
                {
                    new Parameter(FirebaseAnalytics.ParameterLevelName, level),
                    new Parameter(FirebaseAnalytics.ParameterSuccess, "false")
                }
            );
        }

        public override void LevelCompleted(string eventKey, int level, string levelType, int score,
            Dictionary<string, object> customParameters = null)
        {
            FirebaseAnalytics.LogEvent(FirebaseAnalytics.EventLevelEnd,
                new Parameter[]
                {
                    new Parameter(FirebaseAnalytics.ParameterLevelName, level),
                    new Parameter(FirebaseAnalytics.ParameterSuccess, "true")
                }
            );
        }

        public override void NewScore(string eventKey, int level, string levelType, int score)
        {
            FirebaseAnalytics.LogEvent(FirebaseAnalytics.EventPostScore,
                new Parameter[]
                {
                    new Parameter(AnalyticsProperties.pr_level.ToString(), level),
                    new Parameter(AnalyticsProperties.pr_level_type.ToString(), levelType),
                    new Parameter(AnalyticsProperties.pr_level_score.ToString(), score)
                }
            );
        }

        public override void AchievementUnlocked(string eventKey, string achievementID)
        {
            FirebaseAnalytics.LogEvent(FirebaseAnalytics.EventUnlockAchievement,
                FirebaseAnalytics.ParameterAchievementId, achievementID);
        }

        public override void PurchaseInitiatedCheckout(string eventKey, string currency, string price, string itemID,
            string placement)
        {
            FirebaseAnalytics.LogEvent(FirebaseAnalytics.EventBeginCheckout,
                new Parameter[]
                {
                    new Parameter(FirebaseAnalytics.ParameterCurrency, currency),
                    new Parameter(FirebaseAnalytics.ParameterValue, price),
                    new Parameter(FirebaseAnalytics.ParameterContent, itemID),
                    new Parameter(FirebaseAnalytics.ParameterLocation, placement)
                }
            );
        }
        

        public override void ResourceAdd(string eventKey, string currency, int amount, string itemID, string itemType,
            string placement, int userBalance)
        {
            FirebaseAnalytics.LogEvent(FirebaseAnalytics.EventEarnVirtualCurrency,
                new Parameter[]
                {
                    new Parameter(FirebaseAnalytics.ParameterVirtualCurrencyName, currency),
                    new Parameter(FirebaseAnalytics.ParameterValue, amount),
                    new Parameter(FirebaseAnalytics.ParameterItemId, itemID),
                    new Parameter(FirebaseAnalytics.ParameterItemCategory, itemType),
                }
            );
        }

        public override void ResourceRemove(string eventKey, string currency, int amount, string itemID,
            string itemType, string placement, int userBalance)
        {
            FirebaseAnalytics.LogEvent(FirebaseAnalytics.EventSpendVirtualCurrency,
                new Parameter[]
                {
                    new Parameter(FirebaseAnalytics.ParameterVirtualCurrencyName, currency),
                    new Parameter(FirebaseAnalytics.ParameterValue, amount),
                    new Parameter(FirebaseAnalytics.ParameterItemId, itemID),
                    new Parameter(FirebaseAnalytics.ParameterItemCategory, itemType),
                }
            );
        }

        public override void SocialSignUp(string eventKey)
        {
            FirebaseAnalytics.LogEvent(FirebaseAnalytics.EventSignUp);
        }

        public override void ShareSuccess(string eventKey, string id, string placement)
        {
            FirebaseAnalytics.LogEvent(FirebaseAnalytics.EventShare,
                new Parameter[]
                {
                    new Parameter(FirebaseAnalytics.ParameterContentType, placement),
                    new Parameter(FirebaseAnalytics.ParameterItemId, id)
                }
            );
        }
        */

#else
        public override void TryToInit()
        {
            Debug.LogError("FirebaseAnalyticServicePlatform: Insert SDK_FIREBASE scriptable symbol in project settings", gameObject);
        }
        
        public override void TrackEvent(AnalyticsEvent currentEvent){}

        [HelpBox(@"Insert SDK_FIREBASE scriptable symbol in project settings", HelpBoxMessageType.Warning)]
#endif
        [SerializeField] private bool _iSeeThisAlert;
    }
}