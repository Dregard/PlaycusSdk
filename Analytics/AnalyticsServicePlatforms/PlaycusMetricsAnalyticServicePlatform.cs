using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Playcus.Network;
using UnityEngine;
using Playcus.Utils;
using UnityEngine.Networking;
using Playcus;
#if UNITY_EDITOR
using UnityEditor;

#endif

namespace Playcus.Analytics
{
    /// <summary>
    /// For AnalyticsService used only! Don't use directly! (Simonenko Alexey)
    /// </summary>
    public class PlaycusMetricsAnalyticServicePlatform : AnalyticsServicePlatform
    {
        public override bool CanTesterBeTracked()
        {
            return false;
        }
        
        // CONSTANTS
        private const string domain = "https://metrics.playcus.com";
        
        // PROPERTIES
        private string _appId;
        
        public override void TryToInit()
        {
            if (IsInited)
                return;
            try
            {
                _appId = $"{Application.productName} {StoreConstants.GetCurrentStore().ToString()}";
                InitComplete();
            }
            catch (Exception e)
            {
                Debug.LogError("PlaycusMetricsAnalyticServicePlatform: init error {e.Message}", gameObject);
            }
        }

        public override void TrackEvent(AnalyticsEvent currentEvent)
        {
            // TODO: It is necessary to add sending events when the Internet appears.
            var internetAvailable = ServiceLocator.Get<INetworkManager>(true)?
                .CheckNetworkConnection(false);
            if (internetAvailable.HasValue && internetAvailable.Value == false)
            {
                return;
            }
            
            try
            {
                StartCoroutine(SendEventRequest(currentEvent));
            }
            catch (Exception e)
            {
                Debug.Log("PlaycusMetricsAnalyticServicePlatform: Exception", gameObject);
                Debug.Log(e.ToString(), gameObject);
                throw;
            }
        }
        
        IEnumerator SendEventRequest(AnalyticsEvent currentEvent)
        {
            UnityWebRequest www = UnityWebRequest.Get($"{domain}/input?appid={_appId}&eventid={currentEvent.EventKey}");
            www.downloadHandler = new DownloadHandlerBuffer();
            if (Application.isEditor)
            {
                Debug.Log($"PlaycusMetricsAnalyticServicePlatform {www.url}");
            }

            yield return www.SendWebRequest();

            if (www.isNetworkError)
            {
                Debug.LogError($"PlaycusMetricsAnalyticServicePlatform {www.error}");
            }
            else
            {
               // Debug.LogWarning($"PlaycusMetricsAnalyticServicePlatform {www.downloadHandler.text}");
            }

            yield break;
        }

    }
}