using Playcus.Ads;
using Playcus.Analytics;
using Playcus.Analytics.Internal;

namespace Playcus
{
    public class AnalyticsSettingsEntry : SettingsEntry
    {
        public override string Label => "Analytics";
        private AnalyticsServiceConfig _config;

        private AnalyticsPlatformSettings<AppsFlyerAnalyticServicePlatform> _appsFlyerEditor;
        private AnalyticsPlatformSettings<GameAnalyticsAnalyticServicePlatform> _gameAnalyticsEditor;
        private AnalyticsPlatformSettings<ByteBrewAnalyticsPlatform> _bytebrewEditor;
        private AnalyticsPlatformSettings<D2DAnalyticsPlatform> _d2dEditor;

        protected override void OnInitialize()
        {
            _config = PlaycusEditorUtils.LoadOrCreateScriptableObject<AnalyticsServiceConfig>("AnalyticsService", "ServiceConfigs");

            _appsFlyerEditor = new AnalyticsPlatformSettings<AppsFlyerAnalyticServicePlatform>(
                "AppsFlyer settings",
                _config,
                new DefineSymbolSwitch("AppsFlyer Analytics enabled", "PL_SDK_APPSFLYER_ON"),
                "AppsFlyer");

            _gameAnalyticsEditor = new AnalyticsPlatformSettings<GameAnalyticsAnalyticServicePlatform>(
                "GameAnalytics settings",
                _config,
                new DefineSymbolSwitch("GameAnalytics enabled", "PL_SDK_GA_ON"),
                "GameAnalytics");

            _bytebrewEditor = new AnalyticsPlatformSettings<ByteBrewAnalyticsPlatform>(
                "ByteBrew settings",
                _config,
                new DefineSymbolSwitch("ByteBrew Analytics enabled", "PL_BYTEBREW_ANALYTICS_ON"),
                "ByteBrew");

            _d2dEditor = new AnalyticsPlatformSettings<D2DAnalyticsPlatform>(
                "DevToDev settings",
                _config,
                new DefineSymbolSwitch("DevToDev Analytics enabled", "PL_SDK_D2D_ON"),
                "D2D");
        }

        protected override void DrawLayout()
        {
            _appsFlyerEditor.Draw();
            _gameAnalyticsEditor.Draw();
            _bytebrewEditor.Draw();
            _d2dEditor.Draw();
        }
    }
}