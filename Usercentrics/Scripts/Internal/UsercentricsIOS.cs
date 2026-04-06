#if UNITY_IOS
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Unity.Usercentrics
{
    internal class UsercentricsIOS: IUsercentricsPlatform
    {
        [DllImport("__Internal")]
        private static extern void ucInitCMP(string initArgsJson);
    
        [DllImport("__Internal")]
        private static extern void ucShowFirstLayer(string bannerSettingsJson);

        [DllImport("__Internal")]
        private static extern void ucShowSecondLayer(string bannerSettingsJson);
    
        [DllImport("__Internal")]
        private static extern string ucGetControllerId();

        [DllImport("__Internal")]
        private static extern void ucGetTCFData();

        [DllImport("__Internal")]
        private static extern string ucGetUSPData();

        [DllImport("__Internal")]
        private static extern void ucRestoreUserSession(string controllerId);

        [DllImport("__Internal")]
        private static extern void ucSubscribeOnConsentUpdated();

        [DllImport("__Internal")]
        private static extern void ucDisposeOnConsentUpdatedSubscription();

        [DllImport("__Internal")]
        private static extern void ucSubscribeOnConsentMediation();

        [DllImport("__Internal")]
        private static extern void ucDisposeOnConsentMediationSubscription();

        [DllImport("__Internal")]
        private static extern string ucGetFirstLayerSettings();

        [DllImport("__Internal")]
        private static extern void ucAcceptAll();

        [DllImport("__Internal")]
        private static extern void ucDenyAll();

        [DllImport("__Internal")]
        private static extern void ucTrack(int eventType);

        [DllImport("__Internal")]
        private static extern void ucSetCmpId(int cmpId);

        [DllImport("__Internal")]
        private static extern string ucGetCmpData();
		
        [DllImport("__Internal")]
        private static extern void ucSetABTestingVariant(string variant);

        [DllImport("__Internal")]
        private static extern string ucGetABTestingVariant();
        
        [DllImport("__Internal")]
        private static extern string ucGetAdditionalConsentModeData();

        [DllImport("__Internal")]
        private static extern string ucGetConsents();
        
        [DllImport("__Internal")]
        private static extern void ucClearUserSession();

        public void Initialize(string initArgsJson)
        {
            ucInitCMP(initArgsJson);
        }

        public void ShowFirstLayer(string bannerSettingsJson)
        {
            ucShowFirstLayer(bannerSettingsJson);
        }

        public void ShowSecondLayer(string bannerSettingsJson)
        {
            ucShowSecondLayer(bannerSettingsJson);
        }

        public string GetControllerID()
        {
            return ucGetControllerId();
        }

        public void GetTCFData()
        {
            ucGetTCFData();
        }

        public string GetUSPData()
        {
            return ucGetUSPData();
        }

        public void RestoreUserSession(string controllerId)
        {
            ucRestoreUserSession(controllerId);
        }

        public void SubscribeOnConsentUpdated()
        {
            ucSubscribeOnConsentUpdated();
        }

        public void DisposeOnConsentUpdatedSubscription()
        {
            ucDisposeOnConsentUpdatedSubscription();
        }

        public void SubscribeOnConsentMediation()
        {
            ucSubscribeOnConsentMediation();
        }

        public void DisposeOnConsentMediationSubscription()
        {
            ucDisposeOnConsentMediationSubscription();
        }

        public string GetFirstLayerSettings()
        {
            return ucGetFirstLayerSettings();
        }

        public void AcceptAll()
        {
            ucAcceptAll();
        }

        public void DenyAll()
        {
            ucDenyAll();
        }

        public void Track(int eventType)
        {
            ucTrack(eventType);
        }

        public void SetCmpId(int cmpId)
        {
            ucSetCmpId(cmpId);
        }

        public string GetCmpData()
        {
            return ucGetCmpData();
        }

        public void SetABTestingVariant(string variant)
        {
            ucSetABTestingVariant(variant);
        }

        public string GetABTestingVariant()
        {
            return ucGetABTestingVariant();
        }

        public string GetAdditionalConsentModeData()
        {
            return ucGetAdditionalConsentModeData();
        }

        public string GetConsents()
        {
            return ucGetConsents();
        }

        public void ClearUserSession()
        {
            ucClearUserSession();
        }
    }
}
#endif
