using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

namespace Playcus.Analytics
{
    /// <summary>
    /// Base abstract class for analytics platforms.
    /// Look at IAnalyticsManager for full documentation about events protocol.
    /// </summary>
    [Serializable]
    public abstract class AnalyticsServicePlatform : MonoBehaviour
    {
        // CONFIG
        [Header("Tracking mask events")]
        [HelpBox(@"Mask of tracking events control what predefined events need or not to be tracked by platform.

1. NotTrackThisEvents (blacklist) - all events on all platforms will be tracked except this lists.

2. TrackOnlyThisEvents (whitelist) - all events on all platforms will not be tracked except this lists.

SDK predefined events affected by wrapper events. Example: pl_purchase_sucess mask will disable sdk's purchased event."
            , HelpBoxMessageType.Info)]
        public EventsMaskType EventsMaskMode;

        public List<AnalyticsEvents> EventsMask;
        public List<string> EventsMaskCustom;
        public List<EventTrackingConfig> AdditionalEventSettings;
        protected bool IsDebug;

        public enum EventsMaskType
        {
            NotTrackThisEvents,
            TrackOnlyThisEvents
        }
        
        public enum EventTrackingStatus
        {
            Track = 0,
            NotTrack = 1,
        }

        public enum EventCompareType
        {
            Mask = 0,
            FullMatch = 1,
        }
        
        [Serializable]
        public class EventTrackingConfig
        {
            public EventTrackingStatus TrackingStatus;
            public EventCompareType CompareType = EventCompareType.Mask;
            public string EventMask;

        }
        

        // INTERNAL
        [HideInInspector] public bool IsInited; 
        private Queue<AnalyticsEvent> _eventQueue = new Queue<AnalyticsEvent>();
        private const float TrackingInterval = 1f;

        public abstract void TryToInit();

        /// <summary>
        /// If analytics platform has separated dev and prod environments - tester can be track important events such iaps or currencies 
        /// </summary>
        public abstract bool CanTesterBeTracked();

        public void InitComplete()
        {
            IsDebug = Debug.isDebugBuild;
            IsInited = true;
            StartCoroutine(DequeueEvents());
            Debug.Log($"AnalyticsServicePlatform.InitComplete {gameObject.name}", gameObject);
        }
        

        /// <summary>
        /// Register, check and try to enqueue event
        /// </summary>
        public void RegisterEvent(string eventKey, Dictionary<string, object> parameters)
        {
            try
            {
                var isEventOverwriteFound = false;
                if (AdditionalEventSettings != null && AdditionalEventSettings.Count > 0)
                {
                    if (!string.IsNullOrEmpty(eventKey))
                    {
                        for (int i = 0; i < AdditionalEventSettings.Count; i++)
                        {
                            if (!string.IsNullOrEmpty(AdditionalEventSettings[i].EventMask))
                            {
                                if ((AdditionalEventSettings[i].CompareType == EventCompareType.Mask && eventKey.StartsWith(AdditionalEventSettings[i].EventMask))
                                    || (AdditionalEventSettings[i].CompareType == EventCompareType.FullMatch && eventKey.Equals(AdditionalEventSettings[i].EventMask)))
                                {
                                    if (AdditionalEventSettings[i].TrackingStatus == EventTrackingStatus.Track)
                                    {
                                        Debug.LogWarning($"AnalyticsServicePlatform : found overwrite for key {AdditionalEventSettings[i].EventMask} = Track");
                                        isEventOverwriteFound = true;
                                        break;
                                    }else if (AdditionalEventSettings[i].TrackingStatus == EventTrackingStatus.NotTrack)
                                    {
                                        Debug.LogWarning($"AnalyticsServicePlatform : found overwrite for key {AdditionalEventSettings[i].EventMask} = NotTrack");

                                        return;
                                    }
                                }
                            }
                        } 
                    }
                }

                if (!isEventOverwriteFound)
                {
                    string maskedKeyName;
                    bool founded = false;
                    for (int i = 0; i < EventsMask.Count; i++)
                    {
                        maskedKeyName = EventsMask[i].ToString();
                        if (eventKey.StartsWith(maskedKeyName) && !string.IsNullOrEmpty(maskedKeyName))
                        {
                            // NotTrackThisEvents mask mode
                            if (EventsMaskMode == AnalyticsServicePlatform.EventsMaskType.NotTrackThisEvents)
                            {
                                return;
                            }

                            // Default mode
                            founded = true;
                            break;
                        }
                    }

                    for (int i2 = 0; i2 < EventsMaskCustom.Count; i2++)
                    {
                        maskedKeyName = EventsMaskCustom[i2];
                        if (eventKey.StartsWith(maskedKeyName) && !string.IsNullOrEmpty(maskedKeyName))
                        {
                            // NotTrackThisEvents mask mode
                            if (EventsMaskMode == AnalyticsServicePlatform.EventsMaskType.NotTrackThisEvents)
                            {
                                return;
                            }

                            // Default mode
                            founded = true;
                            break;
                        }
                    }

                    // TrackOnlyThisEvents mask mode
                    if (EventsMaskMode == AnalyticsServicePlatform.EventsMaskType.TrackOnlyThisEvents && !founded)
                    {
                        return;
                    }
                }

                // All conditions right - add this event to queue
                if (parameters == null)
                {
                    parameters = new Dictionary<string, object>();
                }
                // Clean null parameters
                foreach (var item in parameters.ToList())
                {
                    if (item.Value == null)
                    {
                        Debug.LogWarning($"AnalyticsServicePlatform : key {item.Key} has null value! Key was removed.");
                        parameters.Remove(item.Key);
                    }
                }
                _eventQueue.Enqueue(new AnalyticsEvent(eventKey, parameters));
                
                
            }
            catch (Exception e)
            {
                Debug.LogError("AnalyticsServicePlatform RegisterEvent Exception: " + e.ToString());
                //throw;
            }
        }
        
        /// <summary>
        /// Dequeue event only if analytics system init completed.
        /// </summary>
        private IEnumerator DequeueEvents()
        {
            var waiter = new WaitWhile(() => _eventQueue.Count == 0);
            
            while (true)
            {
                
                //Try to send event in analytics
                if (_eventQueue != null && _eventQueue.Count > 0)
                {
                    while (_eventQueue.Count > 0)
                    {
                        AnalyticsEvent nextEvent = _eventQueue.Dequeue();
                        try
                        {
                           
                            if (IsDebug || Application.isEditor)
                            {
                                Debug.Log($"AnalyticsServicePlatform try track event  {nextEvent.EventKey}");
                            }
                            TrackEvent(nextEvent);
                        }
                        catch (Exception e)
                        {
                            Debug.LogError($"AnalyticsServicePlatform {gameObject.name} track event {nextEvent.EventKey} Exception {e.ToString()}");
                            //throw;
                        }
                    }
                }

                if (_eventQueue != null)
                {
                    yield return waiter;
                }
                else
                {
                    yield return new WaitForSeconds(TrackingInterval);
                }
            }
        }

        /// <summary>
        /// Send event data to analytics
        /// </summary>
        public abstract void TrackEvent(AnalyticsEvent currentEvent);

    }
}