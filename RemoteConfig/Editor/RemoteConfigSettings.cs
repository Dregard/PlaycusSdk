using Playcus.FirebaseSDK;

namespace Playcus
{
    [HideInSettingsWindow]
    public class RemoteConfigSettings : SettingsEntry
    {
        private RemoteConfigManagerFirebaseConfig _config;
        public override string Label => "Remote Configs";
        protected override void OnInitialize()
        {
            _config = PlaycusEditorUtils.LoadOrCreateScriptableObject<RemoteConfigManagerFirebaseConfig>(
                    "RemoteConfigManagerFirebase",
                    "ServiceConfigs");
        }

        protected override void DrawLayout() { }
    }
}