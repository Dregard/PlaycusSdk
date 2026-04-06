using System;
using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

namespace Unity.Usercentrics
{
    public class AppTrackingTransparency : Singleton<AppTrackingTransparency>
    {
        #region INSPECTOR FIELDS

        public string EnglishDefaultMessage = "";
        public static string m_EnglishDefaultMessage = "";

        public List<ATTLocalizationMessage> LocalizationMessages = new List<ATTLocalizationMessage>();
        public static List<ATTLocalizationMessage> m_LocalizationMessages = new List<ATTLocalizationMessage>();

        #endregion

        private void OnValidate()
        {
            m_EnglishDefaultMessage = EnglishDefaultMessage;
            m_LocalizationMessages = LocalizationMessages;
        }

        /// <summary>
        /// Show Apple Tracking Transparency Popup if needed,
        /// if the user has already interacted with, it will not display anything,
        /// just return the previous input.
        /// </summary>
        /// <param name="attCallback">
        /// Callback block that is invoked when the user interacts with the popup.
        /// </param>
        public void PromptForAppTrackingTransparency(UnityAction<AuthorizationStatus> attCallback)
        {
            #if UNITY_IOS
                AppTrackingTransparencyManager.AttCallback = attCallback;
                AppTrackingTransparencyManager.RequestForAppTrackingTransparency();
            #else
                Debug.Log("AppTrackingTransparency Not supported for this platform.");
            #endif
        }

        /// <summary>
        /// Get Apple Tracking Transparency user answer.
        /// </summary>
        /// <param name="attCallback">
        /// Callback block that is invoked with the user previous answer of the popup.
        /// </param>
        public void GetAuthorizationStatus(UnityAction<AuthorizationStatus> attStatusCallback)
        {
            #if UNITY_IOS
                AppTrackingTransparencyManager.AttStatusCallback = attStatusCallback;
                AppTrackingTransparencyManager.GetTrackingAuthorizationStatus();
            #else
                Debug.Log("AppTrackingTransparency Not supported for this platform.");
            #endif
        }
    }
}
