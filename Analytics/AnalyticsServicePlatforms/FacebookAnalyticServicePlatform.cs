using System;
using System.Collections.Generic;
using Playcus.Utils;
using UnityEngine;

#if PL_SDK_FACEBOOK_ON && (STORE_Facebook || STORE_GooglePlay || STORE_Appstore || UNITY_EDITOR)
using Facebook.Unity;
using Facebook.Unity.Settings;

#endif

namespace Playcus.Analytics
{
    /// <summary>
    /// For AnalyticsService used only! Don't use directly! (Simonenko Alexey)
    /// Facebook Analytics https://developers.facebook.com/docs/unity/reference/current/FB.LogAppEvent
    /// All playcus events list https://docs.google.com/spreadsheets/d/1JjSoB1pyAnKDIZ4Ir3PsFESau0e4rwD1WF_-PFT0bTg
    ///
    /// Each event source in Facebook Analytics supports 1000 distinct events, each with up to 25 parameters.
    ///
    /// </summary>
    public partial class FacebookAnalyticServicePlatform : AnalyticsServicePlatform
    {
        public override bool CanTesterBeTracked()
        {
            return false;
        }

        public const string APP_INSTALL = "fb_mobile_first_app_launch";
        public const string APP_LAUNCH = "fb_mobile_activate_app";

        // CONFIG
        [HelpBox(@"If FacebookSettings.AutoLogAppEventsEnabled - install,launch and purchase events start tracking by sdk.

Don't forgive manual setup AutoLogAppEventsEnabled option in android manifest!"
 , HelpBoxMessageType.Warning)]
        [Header("Settings")]
        [SerializeField] private bool TrackAppLaunch = true;
        [SerializeField] private bool TrackInstall = true;

#if PL_SDK_FACEBOOK_ON && (STORE_Facebook || STORE_GooglePlay || STORE_Appstore || UNITY_EDITOR)
        // PRIVATE
        private IAnalyticsManager _analyticsManager;

        public override void TryToInit()
        {
            if (IsInited)
                return;

            _analyticsManager = ServiceLocator.Get<IAnalyticsManager>();

            try
            {
                //Init events
                if (FB.IsInitialized)
                {
                    _analyticsManager.LogDebug("FacebookAnalyticsServicePlatform already inited", true);
                    Inited();
                }
                else
                {
                    FacebookUtils.FacebookInitializedEvent += Inited;
                    if (!FacebookUtils.IsFacebookInitializingStarted)
                    {
                        _analyticsManager.LogDebug("FacebookAnalyticsServicePlatform start FB.Init", true);
                        FB.Init(FacebookUtils.OnFacebookInitialized);
                    }
                    else
                    {
                        _analyticsManager.LogDebug("FacebookAnalyticsServicePlatform initializing already started", true);
                    }

                }
            }
            catch (Exception e)
            {
                Debug.LogError($"FacebookAnalyticsServicePlatform:Init error {e.Message}");
            }

        }

        void Inited()
        {
            _analyticsManager.LogDebug("FacebookAnalyticsServicePlatform Inited ", true);
            // Activate App for sdk best practices

            // log app install and app launch manually
            if (!FacebookSettings.AutoLogAppEventsEnabled)
            {
                // Track install only one time manually
                if (!PlayerPrefs.HasKey(APP_INSTALL) && TrackInstall)
                {
                    PlayerPrefs.SetInt(APP_INSTALL, 1);
                    FB.LogAppEvent(APP_INSTALL);
                }
                // Track any app launch manually
                if (TrackAppLaunch) FB.LogAppEvent(APP_LAUNCH);
            }
            FB.ActivateApp();
            InitComplete();
        }

        void OnApplicationPause(bool pauseStatus)
        {
            // Check the pauseStatus to see if we are in the foreground
            // or background
            if (!pauseStatus)
            {
                //app resume
                if (FB.IsInitialized)
                {
                    if (!FacebookSettings.AutoLogAppEventsEnabled)
                        // Track any app launch manually
                        if (TrackAppLaunch) FB.LogAppEvent(APP_LAUNCH);

                    // Activate App for sdk best practices
                    FB.ActivateApp();
                }
                else
                {
                    FacebookUtils.FacebookInitializedEvent += FB.ActivateApp;
                    FacebookUtils.FacebookInitializedEvent += InitComplete;
                    if (!FacebookUtils.IsFacebookInitializingStarted)
                    {
                        //Handle FB.Init
                        FacebookUtils.IsFacebookInitializingStarted = true;
                        FB.Init(FacebookUtils.OnFacebookInitialized);
                    }
                }
            }
        }

        public override void TrackEvent(AnalyticsEvent currentEvent)
        {
            try
            {
                if (FB.IsInitialized)
                {
                    //Main parameter is value (amount)
                    int amount = 0;
                    if (currentEvent.Parameters.ContainsKey(AnalyticsProperties.pr_amount.ToString()))
                    {
                        Int32.TryParse(currentEvent.Parameters[AnalyticsProperties.pr_amount.ToString()].ToString(), out amount);
                    }

                    // Facebook SDK track purchases auto or from 3rd party
                    if (currentEvent.EventKey == AnalyticsEvents.pl_purchase_success.ToString())
                    {
                        return;
                    }

                    // Track any other custom event
                    FB.LogAppEvent(currentEvent.EventKey, amount, currentEvent.Parameters);

                    // Additional SDK predefined events (marketing markers)
                    if (currentEvent.EventKey == AnalyticsEvents.pl_tutorial_completed.ToString())
                    {
                        FB.LogAppEvent(AppEventName.CompletedTutorial, currentEvent.pr_step);
                    }
                    else if (currentEvent.EventKey == AnalyticsEvents.pl_user_level.ToString())
                    {
                        UserLevelAchieved(currentEvent.pr_level);
                    }
                    else if (currentEvent.EventKey == AnalyticsEvents.pl_achievement.ToString())
                    {
                        AchievementUnlocked(currentEvent.pr_content_id);
                    }
                    else if (currentEvent.EventKey == AnalyticsEvents.pl_purchase_checkout.ToString())
                    {
                        PurchaseInitiatedCheckout(currentEvent.pr_currency, currentEvent.pr_revenue,
                            currentEvent.pr_content_id, currentEvent.pr_placement);
                    }
                    else
                    {
                        Func<AnalyticsEvent, bool> handler = null;
                        HandleCustomEvent(ref handler);
                        handler?.Invoke(currentEvent);
                    }
                }
            }
            catch (Exception e)
            {
                _analyticsManager.LogDebug("FacebookAnalyticsServicePlatform Exception " + e.ToString());
                throw;
            }
        }

        partial void HandleCustomEvent(ref Func<AnalyticsEvent, bool> handler);

        private void UserLevelAchieved(int level)
        {
            FB.LogAppEvent(AppEventName.AchievedLevel, 0, new Dictionary<string, object>() { { AppEventParameterName.Level, level } });
        }

        private void AchievementUnlocked( string achievementID)
        {
            FB.LogAppEvent(AppEventName.UnlockedAchievement, 0,
                new Dictionary<string, object>() {
                    { AppEventParameterName.Description, achievementID }
                });
        }

        private void PurchaseInitiatedCheckout( string currency, string price, string itemID, string placement)
        {
            if (!FacebookSettings.AutoLogAppEventsEnabled)
            {
                FB.LogAppEvent(AppEventName.InitiatedCheckout, (float)Convert.ToDouble(price),
                    new Dictionary<string, object>() {
                    { AppEventParameterName.Currency, currency },
                    { AppEventParameterName.ContentID, itemID }
                    });
            }
        }


#else
        public override void TryToInit()
        {
            Debug.LogError("FacebookAnalyticsServicePlatform: Insert SDK_FACEBOOK scriptable symbol in project settings", gameObject);
        }

        public override void TrackEvent(AnalyticsEvent currentEvent){}

        [HelpBox(@"Insert SDK_FACEBOOK scriptable symbol in project settings", HelpBoxMessageType.Warning)]
#endif
        [SerializeField] private bool _iSeeThisAlert;
    }
}
