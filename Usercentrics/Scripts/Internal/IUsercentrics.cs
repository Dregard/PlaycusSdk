using System.Collections.Generic;

namespace Unity.Usercentrics
{
    internal interface IUsercentricsPlatform
    {
        void Initialize(string initArgsJson);
        void ShowFirstLayer(string bannerSettingsJson);
        void ShowSecondLayer(string bannerSettingsJson);
        string GetControllerID();
        void GetTCFData();
        void RestoreUserSession(string controllerId);
        void SubscribeOnConsentUpdated();
        void DisposeOnConsentUpdatedSubscription();
        string GetUSPData();
        string GetFirstLayerSettings();
        void AcceptAll();
        void DenyAll();
        void Track(int eventType);
        void SetCmpId(int cmpId);
        string GetCmpData();
        void SetABTestingVariant(string variant);
        string GetABTestingVariant();
        void SubscribeOnConsentMediation();
        void DisposeOnConsentMediationSubscription();
        string GetAdditionalConsentModeData();
        string GetConsents();
        void ClearUserSession();
    }
}
