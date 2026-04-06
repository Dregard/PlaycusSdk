using Playcus.Iap;
using UnityEditor;

namespace Playcus
{
    public class IAPSettingsEntry : SettingsEntry
    {
        public override string Label => "In-App Purchases";
        private const string IAP_ENABLE_DEFINE = "PL_IAP_ON";
        private const string AF_CONNECTOR_ENABLE_DEFINE = "PL_APPSFLYER_PURCHASE_CONNECTOR_ON";
        
        private DefineSymbolSwitch _generalEnableSwitch = new DefineSymbolSwitch("In-Apps enabled", IAP_ENABLE_DEFINE);
        private DefineSymbolSwitch _appsflyerEnableSwitch = new DefineSymbolSwitch("AppsFlyer Purchase Connector enabled", AF_CONNECTOR_ENABLE_DEFINE);
        private IapManagerOfflineConfig _config;
        private SerializedObject _configObject;

        protected override void OnInitialize()
        {
            _config = PlaycusEditorUtils.LoadOrCreateScriptableObject<IapManagerOfflineConfig>("IapManagerOffline", "ServiceConfigs");
            _configObject = new SerializedObject(_config);
        }

        protected override void DrawLayout()
        {
            _generalEnableSwitch.DrawToggle();
            EditorGUI.BeginDisabledGroup(_generalEnableSwitch.IsEnabled == false);
            _appsflyerEnableSwitch.DrawToggle();
            _configObject.Update();   
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(_configObject.FindProperty("productConfigs"));
            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(_config);
            }
            _configObject.ApplyModifiedProperties();
            AssetDatabase.SaveAssetIfDirty(_config);
            EditorGUI.EndDisabledGroup();
        }
    }
}