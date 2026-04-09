using System;
using System.Collections.Generic;
using UnityEngine.Serialization;

namespace Unity.Usercentrics
{
    #region UNITY LOCAL

    [Serializable]
    public class UsercentricsOptions
    {
        public string DefaultLanguage = "";
        public string Version = "latest";
        public bool DebugMode = true;
        public long TimeoutMillis = 10000L;
        public UsercentricsNetworkMode NetworkMode = UsercentricsNetworkMode.World;
        public bool ConsentMediation = false;
        public long InitTimeoutMillis = 10000L;
        public AndroidOptions Android = new AndroidOptions();
    }

    [Serializable]
    public class AndroidOptions
    {
        public bool DisableSystemBackButton = true;
        public string StatusBarColor = "";
        public bool ShowWindowInFullscreen = true;
    }
    #endregion

    [Serializable]
    public class UsercentricsOptionsInternal
    {
        public string settingsId;
        public string ruleSetId;
        public string defaultLanguage;
        public string version;
        public long timeoutMillis;
        public UsercentricsLoggerLevel loggerLevel;
        public UsercentricsNetworkMode networkMode;
        public bool consentMediation;
        public long initTimeoutMillis;

        public static UsercentricsOptionsInternal CreateFrom(UsercentricsOptions unityOptions, string settingsId, string ruleSetId)
        {
            var userOptions = new UsercentricsOptionsInternal();
            userOptions.settingsId = settingsId;
            userOptions.ruleSetId = ruleSetId;
            userOptions.defaultLanguage = unityOptions.DefaultLanguage;
            userOptions.version = unityOptions.Version;
            userOptions.timeoutMillis = unityOptions.TimeoutMillis;
            userOptions.loggerLevel = unityOptions.DebugMode ? UsercentricsLoggerLevel.Debug : UsercentricsLoggerLevel.None;
            userOptions.networkMode = unityOptions.NetworkMode;
            userOptions.consentMediation = unityOptions.ConsentMediation;
            userOptions.initTimeoutMillis = unityOptions.InitTimeoutMillis;
            return userOptions;
        }
    }

    [Serializable]
    public enum UsercentricsLoggerLevel
    {
        None,
        Error,
        Warning,
        Debug
    }

    [Serializable]
    public enum UsercentricsNetworkMode
    {
        World,
        EU
    }

    [Serializable]
    public class UsercentricsReadyStatus
    {
        public bool shouldCollectConsent;
        public List<UsercentricsServiceConsent> consents;
        public GeolocationRuleset geolocationRuleset;
        public UsercentricsLocation location;
    }

    [Serializable]
    public class UsercentricsServiceConsent
    {
        public string templateId;
        public bool status;
        public List<UsercentricsConsentHistoryEntry> history;
        public string dataProcessor;
        public string version;
        public bool isEssential;
        public string _type;
        public string category;

        public UsercentricsConsentType? type
        {
            get
            {
                var intValue = StringParser.ToInt(_type);
                if (intValue == null)
                {
                    return null;
                }
                return (UsercentricsConsentType) intValue;
            }
        }
    }

    [Serializable]
    public class UsercentricsConsentHistoryEntry
    {
        public bool status;
        public UsercentricsConsentType type;
        public long timestampInMillis;
    }

    [Serializable]
    public enum UsercentricsConsentType
    {
        Explicit,
        Implicit
    }

    [Serializable]
    public class UsercentricsConsentUserResponse
    {
        public UsercentricsUserInteraction userInteraction;
        public List<UsercentricsServiceConsent> consents;
        public string controllerId;
    }

    [Serializable]
    public enum UsercentricsUserInteraction
    {
        AcceptAll,
        DenyAll,
        Granular,
        NoInteraction
    }

    [Serializable]
    public class TCFData
    {
        public List<TCFFeature> features;
        public List<TCFPurpose> purposes;
        public List<TCFSpecialFeature> specialFeatures;
        public List<TCFSpecialPurpose> specialPurposes;
        public List<TCFStack> stacks;
        public List<TCFVendor> vendors;
        public string tcString;
    }

    [Serializable]
    public class TCFFeature
    {
        public string purposeDescription;
        public string descriptionLegal;
        public int id;
        public string name;
    }

    [Serializable]
    public class TCFPurpose
    {
        public string purposeDescription;
        public string descriptionLegal;
        public int id;
        public string name;
        public bool isPartOfASelectedStack;
        public bool showConsentToggle;
        public bool showLegitimateInterestToggle;
        public string _consent;
        public string _legitimateInterestConsent;
        public string _stackId;

        public bool? consent
        {
            get
            {
                return StringParser.ToBool(_consent);
            }
        }

        public bool? legitimateInterestConsent
        {
            get
            {
                return StringParser.ToBool(_legitimateInterestConsent);
            }
        }

        public int? stackId
        {
            get
            {
                return StringParser.ToInt(_stackId);
            }
        }
    }

    [Serializable]
    public class TCFSpecialFeature
    {
        public string purposeDescription;
        public string descriptionLegal;
        public int id;
        public string name;
        public bool isPartOfASelectedStack;
        public bool showConsentToggle;
        public string _consent;
        public string _stackId;

        public bool? consent
        {
            get
            {
                return StringParser.ToBool(_consent);
            }
        }

        public int? stackId
        {
            get
            {
                return StringParser.ToInt(_stackId);
            }
        }
    }

    [Serializable]
    public class TCFSpecialPurpose
    {
        public string purposeDescription;
        public string descriptionLegal;
        public int id;
        public string name;
    }

    [Serializable]
    public class TCFStack
    {
        public string description;
        public int id;
        public string name;
        public List<int> purposeIds;
        public List<int> specialFeatureIds;
    }

    [Serializable]
    public class TCFVendor
    {
        public List<IdAndName> features;
        public List<IdAndName> flexiblePurposes;
        public int id;
        public List<IdAndName> legitimateInterestPurposes;
        public string name;
        public string policyUrl;
        public List<IdAndName> purposes;
        public List<TCFVendorRestriction> restrictions;
        public List<IdAndName> specialFeatures;
        public List<IdAndName> specialPurposes;
        public bool showConsentToggle;
        public bool showLegitimateInterestToggle;
        public bool usesNonCookieAccess;
        public string deviceStorageDisclosureUrl = null;
        public bool usesCookies = false;
        public bool cookieRefresh;
        public bool dataSharedOutsideEU;
        public DataRetention dataRetention = null;
        public List<IdAndName> dataCategories;
        public List<VendorUrl> vendorUrls;
        public string _consent;
        public string _legitimateInterestConsent;
        public string _cookieMaxAgeSeconds;

        public bool? consent
        {
            get
            {
                return StringParser.ToBool(_consent);
            }
        }

        public bool? legitimateInterestConsent
        {
            get
            {
                return StringParser.ToBool(_legitimateInterestConsent);
            }
        }

        public double? cookieMaxAgeSeconds
        {
            get
            {
                return StringParser.ToDouble(_cookieMaxAgeSeconds);
            }
        }
    }

    [Serializable]
    public class IdAndName
    {
        public int id;
        public string name;
    }

    [Serializable]
    public class TCFVendorRestriction
    {
        public int purposeId;
        public RestrictionType restrictionType;
    }

    [Serializable]
    public enum RestrictionType
    {
        NotAllowed,
        RequireConsent,
        RequireLi
    }

    [Serializable]
    public class DataRetention
    {
        public RetentionPeriod purposes;
        public RetentionPeriod specialPurposes;
        public String _stdRetention;

        public int? stdRetention
        {
            get
            {
                return StringParser.ToInt(_stdRetention);
            }
        }
    }

    [Serializable]
    public class RetentionPeriod
    {
        public Dictionary<int, int> idAndPeriod;
    }

    [Serializable]
    public class VendorUrl
    {
        public string langId = null;
        public string privacy = null;
        public string legIntClaim = null;
    }

    [Serializable]
    public class UsercentricsUpdatedConsentEvent
    {
        public List<UsercentricsServiceConsent> consents;
        public string controllerId;
        public string tcString = null;
        public string uspString = null;
        public string acString = null;
    }

    [Serializable]
    public class CCPAData
    {
        public int version = 1;
        public bool noticeGiven = false;
        public bool optedOut = false;
        public bool lspact = false;
        public string uspString = null;
    }

    [Serializable]
    public class FirstLayerSettings
    {
        public string title;
        public string description;
        public string additionalInfo;
        public string resurfaceNote;
        public string vendorListLinkTitle;
        public string manageSettingsLinkTitle;
        public string purposesLabel;
        public string featuresLabel;
        public string acceptAllButton;
        public string denyAllButton;
        public string saveButton;
    }

    [Serializable]
    public enum UsercentricsAnalyticsEventType
    {
        CmpShown,
        AcceptAllFirstLayer,
        DenyAllFirstLayer,
        SaveFirstLayer,
        AcceptAllSecondLayer,
        DenyAllSecondLayer,
        SaveSecondLayer,
        ImprintLink,
        MoreInformationLink,
        PrivacyPolicyLink,
        CcpaTogglesOn,
        CcpaTogglesOff,
    }

    [Serializable]
    public enum PublishedAppPlatform
    {
        Android,
        Ios
    }

    [Serializable]
    public enum UsercentricsVariant
    {
        Default,
        CCPA,
        TCF
    }

    [Serializable]
    public class PublishedApp
    {
        public string bundleId;
        public PublishedAppPlatform platform;
    }

    [Serializable]
    public class UsercentricsLocation
    {
        public string countryCode;
        public string regionCode;
    }

    [Serializable]
    public class CmpData
    {
        public UsercentricsVariant activeVariant;
        public List<PublishedApp> publishedApps;
        public UsercentricsLocation userLocation;
    }

    [Serializable]
    public class UsercentricsMediationEvent
    {
        public List<ConsentApplied> applied;
    }

    [Serializable]
    public class ConsentApplied
    {
        public string name;
        public string templateId;
        public bool consent = false;
        public bool mediated = false;
    }
    
    [Serializable]
    public class AdditionalConsentModeData
    {
        public string acString;
        public List<AdTechProvider> adTechProviders;
    }

    [Serializable]
    public class AdTechProvider
    {
        public int id;
        public string name;
        public string privacyPolicyUrl;
        public bool consent;
    }

    [Serializable]
    internal class UsercentricsConsentsHolder
    {
        public List<UsercentricsServiceConsent> consents;
    }

    [Serializable]
    public class GeolocationRuleset
    {
        public String activeSettingsId;
        public bool bannerRequiredAtLocation;
    }

    [Serializable]
    internal class UsercentricsIntegerListHolder
    {
        public int[] list;

        internal UsercentricsIntegerListHolder(int[] list)
        {
            this.list = list;
        }
    }

    [Serializable]
    public class UsercentricsUserDecision
    {
        public string serviceId;
        public bool consent;
    }
    
    [Serializable]
    public class UsercentricsUserDecisionListWrapper
    {
        public List<UsercentricsUserDecision> decisions;
    }
    
    [Serializable]
    public class UsercentricsServiceConsentWrapper
    {
        public List<UsercentricsServiceConsent> consents;
    }
}
