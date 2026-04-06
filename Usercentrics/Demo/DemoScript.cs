using Unity.Usercentrics;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Demo example.
/// This class contains a complete example of the Usercentrics integration.
/// This is:
/// - Initialization
/// - Update Services
/// - Show First and Second Layer
///
/// It also contains an example of AppTrackingTransparency usage.
///
/// Note that in a real integration the initialization should occur in some
/// initial stage of your game, for example the splash screen.
/// </summary>
namespace Unity.Usercentrics
{
    public class DemoScript : MonoBehaviour
    {
        [SerializeField] private Button ShowFirstLayerButton = null;
        [SerializeField] private Button ShowSecondLayerButton = null;
        [SerializeField] private Button ShowAttButton = null;
        [SerializeField] private Button GetAttStatusButton = null;
        [SerializeField] private Button ShowCustomizedFirstLayerButton = null;

        [SerializeField] private Canvas FirstLayerUI = null;
        [SerializeField] private Button ShowUnityUIButton = null;

        void Awake()
        {
            InitAndShowConsentManagerIfNeeded();
        }

        private void Start()
        {
            ShowFirstLayerButton.onClick.AddListener(() => { ShowFirstLayer(); });
            ShowSecondLayerButton.onClick.AddListener(() => { ShowSecondLayer(); });
            ShowAttButton.onClick.AddListener(() => { ShowAtt(); });
            GetAttStatusButton.onClick.AddListener(() => { GetAttStatus(); });
            ShowCustomizedFirstLayerButton.onClick.AddListener(() => { ShowCustomizedFirstLayer(); });

            ShowUnityUIButton.onClick.AddListener(() => { ShowCustomFirstLayer(); });
            FirstLayerUI.GetComponent<FirstLayerUIScript>().OnFinishCallback = () => { HideCustomFirstLayer(); };
        }

        private void InitAndShowConsentManagerIfNeeded()
        {
            Usercentrics.Instance.Initialize((status) =>
            {
                if (status.shouldCollectConsent)
                {
                    ShowFirstLayer();
                }
                else
                {
                    UpdateServices(status.consents);
                }
            }, (errorMessage) =>
            {
                // Log and collect the error...
            });
        }

        private void ShowAtt()
        {
            AppTrackingTransparency.Instance.PromptForAppTrackingTransparency((status) =>
            {
                switch (status)
                {
                    case AuthorizationStatus.AUTHORIZED:
                        break;
                    case AuthorizationStatus.DENIED:
                        break;
                    case AuthorizationStatus.NOT_DETERMINED:
                        break;
                    case AuthorizationStatus.RESTRICTED:
                        break;
                }
            });
        }

        private void GetAttStatus()
        {
            var attStatus = AuthorizationStatus.NOT_DETERMINED;
            AppTrackingTransparency.Instance.GetAuthorizationStatus((status) =>
            {
                switch (status)
                {
                    case AuthorizationStatus.AUTHORIZED:
                        attStatus = AuthorizationStatus.AUTHORIZED;
                        break;
                    case AuthorizationStatus.DENIED:
                        attStatus = AuthorizationStatus.DENIED;
                        break;
                    case AuthorizationStatus.NOT_DETERMINED:
                        attStatus = AuthorizationStatus.NOT_DETERMINED;
                        break;
                    case AuthorizationStatus.RESTRICTED:
                        attStatus = AuthorizationStatus.RESTRICTED;
                        break;
                    default:
                        break;
                }
            });
            Debug.Log("[USERCENTRICS][DEBUG] ATT status callback: " + attStatus);
        }

        private void ShowFirstLayer()
        {
            Usercentrics.Instance.ShowFirstLayer((usercentricsConsentUserResponse) =>
            {
                UpdateServices(usercentricsConsentUserResponse.consents);
            });
        }

        private void ShowCustomizedFirstLayer()
        {
            Usercentrics.Instance.ShowFirstLayer(GetBannerSettingsCustom(), (usercentricsConsentUserResponse) =>
            {
                UpdateServices(usercentricsConsentUserResponse.consents);
            });
        }

        private BannerSettings GetBannerSettings()
        {
            return new BannerSettings(generalStyleSettings: GetGeneralStyleSettings(),
                                      firstLayerStyleSettings: GetFirstLayerStyleSettings(),
                                      secondLayerStyleSettings: new SecondLayerStyleSettings(showCloseButton: true),
                                      variantName: "");
        }

        private BannerSettings GetBannerSettingsCustom()
        {
            return new BannerSettings(generalStyleSettings: GetGeneralStyleSettingsCustom(),
                                      firstLayerStyleSettings: GetFirstLayerStyleSettings(),
                                      secondLayerStyleSettings: new SecondLayerStyleSettings(showCloseButton: true),
                                      variantName: "");
        }

        private GeneralStyleSettings GetGeneralStyleSettings()
        {
            return new GeneralStyleSettings(androidDisableSystemBackButton: true,
                                            androidStatusBarColor: "#f51d7e");
        }

        private GeneralStyleSettings GetGeneralStyleSettingsCustom()
        {
            return new GeneralStyleSettings(androidDisableSystemBackButton: true,
                                            androidStatusBarColor: "#af7ac5",
                                            tabColor: "#ffffff",
                                            textColor: "#ffffff",
                                            layerBackgroundColor: "#001d3b",
                                            layerBackgroundSecondaryColor: "#1f5791",
                                            bordersColor: "#ffffff",
                                            linkColor: "#00d0fc");
        }

        private FirstLayerStyleSettings GetFirstLayerStyleSettings()
        {
            var logoImageUrl = "https://drive.google.com/uc?export=download&id=1Cd6o0FBqsGVb3zZW8KRSUOmFAcCAvZ9o";
            var headerImageSettings = HeaderImageSettings.Custom(imageUrl: logoImageUrl,
                                                                 alignment: SectionAlignment.Center,
                                                                 height: 100);

            var buttons = new List<ButtonSettings>
            {
                new ButtonSettings(type: ButtonType.More,
                                   textSize: 10f,
                                   textColor: "#001d3b",
                                   backgroundColor: "#00d0fc",
                                   cornerRadius: 12,
                                   isAllCaps: false),
                new ButtonSettings(type: ButtonType.AcceptAll,
                                   textSize: 10f,
                                   textColor: "#001d3b",
                                   backgroundColor: "#00d0fc",
                                   cornerRadius: 12,
                                   isAllCaps: false)
            };

            var buttonLayout = ButtonLayout.Row(buttons);

            var titleSettings = new TitleSettings(textSize: 24f, alignment: SectionAlignment.Center, textColor: "#FFFFFF");

            var messageSettings = new MessageSettings(textSize: 10f,
                                                      alignment: SectionAlignment.Start,
                                                      textColor: "#e8daef",
                                                      linkTextColor: "#00d0fc",
                                                      underlineLink: true);

            return new FirstLayerStyleSettings(layout: UsercentricsLayout.PopupCenter,
                                               headerImage: headerImageSettings,
                                               title: titleSettings,
                                               message: messageSettings,
                                               buttonLayout: buttonLayout,
                                               backgroundColor: "#001d3b",
                                               cornerRadius: 30f,
                                               overlayColor: "#350aab",
                                               overlayAlpha: 0.5f);
        }

        private ButtonLayout GridButtonLayoutExample()
        {
            var buttons = new List<ButtonSettingsRow>
            {
                new ButtonSettingsRow(
                    new List<ButtonSettings>
                    {
                        new ButtonSettings(type: ButtonType.AcceptAll,
                                           textSize: 13f, textColor: "#ffffff",
                                           backgroundColor: "#350aab",
                                           cornerRadius: 12f,
                                           isAllCaps: true),
                        new ButtonSettings(type: ButtonType.Save,
                                           textSize: 13f,
                                           textColor: "#ffffff",
                                           backgroundColor: "#350aab",
                                           cornerRadius: 12f,
                                           isAllCaps: true)
                    }
                ),
                new ButtonSettingsRow(
                    new List<ButtonSettings>
                    {
                        new ButtonSettings(type: ButtonType.More,
                                           textSize: 13f,
                                           textColor: "#ffffff",
                                           backgroundColor: "#350aab",
                                           cornerRadius: 12f,
                                           isAllCaps: true)
                    }
                )
            };
            return ButtonLayout.Grid(buttons);
        }

        private ButtonLayout ColumnButtonLayoutExample()
        {
            var buttons = new List<ButtonSettings>
            {
                new ButtonSettings(type: ButtonType.AcceptAll,
                                   textSize: 13f,
                                   textColor: "#ffffff",
                                   backgroundColor: "#350aab",
                                   cornerRadius: 12,
                                   isAllCaps: true),
                new ButtonSettings(type: ButtonType.More,
                                   textSize: 13f,
                                   textColor: "#ffffff",
                                   backgroundColor: "#350aab",
                                   cornerRadius: 12,
                                   isAllCaps: true)
            };
            return ButtonLayout.Column(buttons);
        }

        private void ShowSecondLayer()
        {
            Usercentrics.Instance.ShowSecondLayer(GetBannerSettings(), (usercentricsConsentUserResponse) =>
            {
                UpdateServices(usercentricsConsentUserResponse.consents);
            });
        }

        private void UpdateServices(List<UsercentricsServiceConsent> consents)
        {
            foreach (var serviceConsent in consents)
            {
                switch (serviceConsent.templateId)
                {
                    case "XxxXXxXxX":
                        // GoogleAdsFramework.Enabled = service.consent.status;
                        break;
                    case "YYyyYyYYY":
                        // AnalyticsFramework.Enabled = service.consent.status;
                        break;
                    default:
                        break;
                }
            }
        }

        private void ShowCustomFirstLayer()
        {
            Debug.Log("[USERCENTRICS][DEBUG] show custom first layer");
            FirstLayerUI.gameObject.SetActive(true);
        }

        private void HideCustomFirstLayer()
        {
            Debug.Log("[USERCENTRICS][DEBUG] hide custom first layer");
            FirstLayerUI.gameObject.SetActive(false);
        }
    }
}
