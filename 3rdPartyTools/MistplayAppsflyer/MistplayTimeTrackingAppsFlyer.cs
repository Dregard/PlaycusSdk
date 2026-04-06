using System.Collections.Generic;
using UnityEngine;
#if PL_SDK_APPSFLYER_ON
using AppsFlyerSDK;
#endif

namespace Mistplay
{
    public class MistplayTimeTrackingAppsFlyer : MonoBehaviour
    {
        //  The event name we use to track your user's playtime. Please keep it as Playtime
        const string eventKey = "Playtime";

        static int eventCount;
        static long sessionId;
        static bool shouldTrack;
        
        public static void SendEvent()
        {
            if (shouldTrack)
            {
                sessionId = UnityEngine.Analytics.AnalyticsSessionInfo.sessionId;

#if PL_SDK_APPSFLYER_ON
                //// Sending event to track the playtime
                var afEvent = new Dictionary<string, string> ();
                afEvent.Add("ID", eventCount.ToString());
                afEvent.Add("Session", sessionId.ToString());
                AppsFlyer.sendEvent(eventKey, afEvent);
#endif

                //// Incrementing the event count
                ++eventCount;
            }
        }

        public static void OnDeepLinkValueReceived(string deepLinkValue)
        {
            Debug.Log($"MistplayTimeTrackingAppsFlyer: OnDeepLinkValueReceived = {deepLinkValue}");
            // Do |= instead of = because we will check the saved value at start
            shouldTrack |= deepLinkValue != null && deepLinkValue.ToLower() == "mistplay_install";
        }
    }
}