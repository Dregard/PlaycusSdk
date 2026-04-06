using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.Usercentrics
{
    internal class UsercentricsAndroid : IUsercentricsPlatform
    {
        private const string UC_UNITY_JAVA_NAME = "com.usercentrics.sdk.unity.UsercentricsUnity";
        private readonly Lazy<AndroidJavaClass> _usercentricsUnityClass = new Lazy<AndroidJavaClass>(() => new AndroidJavaClass(UC_UNITY_JAVA_NAME));

        private const string UC_BANNER_JAVA_NAME = "com.usercentrics.sdk.unity.UsercentricsUnityBanner";
        private readonly Lazy<AndroidJavaClass> _usercentricsBannerClass = new Lazy<AndroidJavaClass>(() => new AndroidJavaClass(UC_BANNER_JAVA_NAME));

        private const string UNITY_PLAYER_NAME = "com.unity3d.player.UnityPlayer";
        private readonly Lazy<AndroidJavaObject> _currentActivity = new Lazy<AndroidJavaObject>(() =>
        {
            var androidJavaUnityPlayer = new AndroidJavaClass(UNITY_PLAYER_NAME);
            return androidJavaUnityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
        });

        public void Initialize(string initArgsJson)
        {
            _usercentricsUnityClass.Value.CallStatic("init", _currentActivity.Value, initArgsJson);
        }

        public void ShowFirstLayer(string bannerSettingsJson)
        {
            AndroidJavaObject usercentricsBanner = new AndroidJavaObject(UC_BANNER_JAVA_NAME, _currentActivity.Value);
            usercentricsBanner.Call("showFirstLayer", bannerSettingsJson);
        }

        public void ShowSecondLayer(string bannerSettingsJson)
        {
            AndroidJavaObject usercentricsBanner = new AndroidJavaObject(UC_BANNER_JAVA_NAME, _currentActivity.Value);
            usercentricsBanner.Call("showSecondLayer", bannerSettingsJson);
        }

        public string GetControllerID()
        {
            return _usercentricsUnityClass.Value.CallStatic<string>("getControllerId");
        }

        public void GetTCFData()
        {
            _usercentricsUnityClass.Value.CallStatic("getTCFData");
        }

        public string GetUSPData()
        {
            return _usercentricsUnityClass.Value.CallStatic<string>("getUSPData");
        }

        public void RestoreUserSession(string controllerId)
        {
            _usercentricsUnityClass.Value.CallStatic("restoreUserSession", controllerId);
        }
        
        public void SubscribeOnConsentUpdated()
        {
            _usercentricsUnityClass.Value.CallStatic("subscribeOnConsentUpdated");
        }

        public void DisposeOnConsentUpdatedSubscription()
        {
            _usercentricsUnityClass.Value.CallStatic("disposeOnConsentUpdatedSubscription");
        }

        public void SubscribeOnConsentMediation()
        {
            _usercentricsUnityClass.Value.CallStatic("subscribeOnConsentMediation");
        }

        public void DisposeOnConsentMediationSubscription()
        {
            _usercentricsUnityClass.Value.CallStatic("disposeOnConsentMediationSubscription");
        }

        public string GetFirstLayerSettings()
        {
            return _usercentricsUnityClass.Value.CallStatic<string>("getFirstLayerSettings");
        }

        public void AcceptAll()
        {
            _usercentricsUnityClass.Value.CallStatic("acceptAllFirstLayerForTCF");
        }

        public void DenyAll()
        {
            _usercentricsUnityClass.Value.CallStatic("denyAllFirstLayerForTCF");
        }

        public void Track(int eventType)
        {
            _usercentricsUnityClass.Value.CallStatic("track", eventType);
        }

        public void SetCmpId(int cmpId)
        {
            _usercentricsUnityClass.Value.CallStatic("setCmpId", cmpId);
        }

        public string GetCmpData()
        {
            return _usercentricsUnityClass.Value.CallStatic<string>("getCmpData");
        }

        public void SetABTestingVariant(string variant)
        {
            _usercentricsUnityClass.Value.CallStatic("setABTestingVariant", variant);
        }

        public string GetABTestingVariant()
        {
            return _usercentricsUnityClass.Value.CallStatic<string>("getABTestingVariant");
        }

        public string GetAdditionalConsentModeData()
        {
            return _usercentricsUnityClass.Value.CallStatic<string>("getAdditionalConsentModeData");
        }

        public string GetConsents()
        {
            return _usercentricsUnityClass.Value.CallStatic<string>("getConsents");
        }
        
        public void ClearUserSession()
        {
            _usercentricsUnityClass.Value.CallStatic("clearUserSession");
        }
    }
}
