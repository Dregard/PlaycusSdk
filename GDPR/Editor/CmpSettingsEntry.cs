using UnityEditor;

namespace Playcus.GDPR.Editor
{
    public class CmpSettingsEntry : SettingsEntry
    {
        protected override void OnInitialize()
        {
            _config = PlaycusEditorUtils.LoadOrCreateScriptableObject<UsercentricsConsentServiceConfig>("UsercentricsConsentService", "ServiceConfigs");
        }

        public override string Label => "Consent Management";
        private bool _isEnabled;
        private const string ENABLE_DEFINE = "PL_USERCENTRICS_CONSENT_ON";
        private DefineSymbolSwitch _enableSwitch = new DefineSymbolSwitch("Consent Management enabled", ENABLE_DEFINE);
        private UsercentricsConsentServiceConfig _config;

        protected override void DrawLayout()
        {
            _enableSwitch.DrawToggle();
        }
    }
}