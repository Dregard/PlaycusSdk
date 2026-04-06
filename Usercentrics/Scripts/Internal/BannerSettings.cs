using System;

namespace Unity.Usercentrics
{

    [Serializable]
    public class BannerSettings
    {
        public GeneralStyleSettings generalStyleSettings;
        public FirstLayerStyleSettings firstLayerStyleSettings;
        public SecondLayerStyleSettings secondLayerStyleSettings = null;
        public string variantName;

        public BannerSettings(GeneralStyleSettings generalStyleSettings = null,
                              FirstLayerStyleSettings firstLayerStyleSettings = null,
                              SecondLayerStyleSettings secondLayerStyleSettings = null,
                              string variantName = "")
        {
            this.generalStyleSettings = generalStyleSettings;
            this.firstLayerStyleSettings = firstLayerStyleSettings ?? DefaultFirstLayerSettings();
            this.secondLayerStyleSettings = secondLayerStyleSettings;
            this.variantName = variantName;
        }
        
        private FirstLayerStyleSettings DefaultFirstLayerSettings()
        {
            return new FirstLayerStyleSettings(
                layout: UsercentricsLayout.Undefined,
                headerImage: HeaderImageSettings.Undefined(),
                title: new TitleSettings(
                    alignment: SectionAlignment.Undefined,
                    textSize: 0.0F,
                    textColor: ""
                ),
                message: new MessageSettings(
                    textSize: 0.0F,
                    alignment: SectionAlignment.Undefined,
                    textColor: "",
                    linkTextColor: "",
                    underlineLink: true
                ),
                buttonLayout: ButtonLayout.Undefined(),
                backgroundColor: "",
                cornerRadius: 0.0F,
                overlayColor: "",
                overlayAlpha: 0.0F
            );
        }
    }

    [Serializable]
    public class GeneralStyleSettings
    {
        public bool androidDisableSystemBackButton;
        public string androidStatusBarColor;
        public bool androidWindowFullscreen;
        public string textColor;
        public string layerBackgroundColor;
        public string layerBackgroundSecondaryColor;
        public string linkColor;
        public string tabColor;
        public string bordersColor;
        public ToggleStyleSettings toggleStyleSettings = null;
        public string logoImageUrl;
        public LegalLinksSettings links;

        private GeneralStyleSettings() {}

        public GeneralStyleSettings(bool androidDisableSystemBackButton = true,
                                    string androidStatusBarColor = "",
                                    bool androidWindowFullscreen = true,
                                    string textColor = "",
                                    string layerBackgroundColor = "",
                                    string layerBackgroundSecondaryColor = "",
                                    string linkColor = "",
                                    string tabColor = "",
                                    string bordersColor = "",
                                    ToggleStyleSettings toggleStyleSettings = null,
                                    string logoImageUrl = "",
                                    LegalLinksSettings links = LegalLinksSettings.Undefined)
        {
            this.androidDisableSystemBackButton = androidDisableSystemBackButton;
            this.androidStatusBarColor = androidStatusBarColor;
            this.androidWindowFullscreen = androidWindowFullscreen;
            this.textColor = textColor;
            this.layerBackgroundColor = layerBackgroundColor;
            this.layerBackgroundSecondaryColor = layerBackgroundSecondaryColor;
            this.linkColor = linkColor;
            this.tabColor = tabColor;
            this.bordersColor = bordersColor;
            this.toggleStyleSettings = toggleStyleSettings;
            this.logoImageUrl = logoImageUrl;
            this.links = links;
        }
    }

    [Serializable]
    public class SecondLayerStyleSettings
    {
        public bool showCloseButton;

        public SecondLayerStyleSettings(bool showCloseButton)
        {
            this.showCloseButton = showCloseButton;
        }
    }

    [Serializable]
    public class ToggleStyleSettings
    {
        public string activeBackgroundColor;
        public string inactiveBackgroundColor;
        public string disabledBackgroundColor;
        public string activeThumbColor;
        public string inactiveThumbColor;
        public string disabledThumbColor;

        public ToggleStyleSettings(string activeBackgroundColor = "",
                                   string inactiveBackgroundColor = "",
                                   string disabledBackgroundColor = "",
                                   string activeThumbColor = "",
                                   string inactiveThumbColor = "",
                                   string disabledThumbColor = "")
        {
            this.activeBackgroundColor = activeBackgroundColor;
            this.inactiveBackgroundColor = inactiveBackgroundColor;
            this.disabledBackgroundColor = disabledBackgroundColor;
            this.activeThumbColor = activeThumbColor;
            this.inactiveThumbColor = inactiveThumbColor;
            this.disabledThumbColor = disabledThumbColor;
        }
    }

    [Serializable]
    public enum LegalLinksSettings
    {
        Undefined,
        FirstLayerOnly,
        SecondLayerOnly,
        Both,
        Hidden
    }
}
