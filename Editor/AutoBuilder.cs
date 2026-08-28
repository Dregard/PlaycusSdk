using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using GooglePlayServices;
using Playcus;
using Unity.Editor;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Purchasing;
using UnityEngine;
using UnityEngine.Purchasing;

/// <summary>
/// Methods for automatic builds with external calling from Ci systems or Editor
/// </summary>
public static class AutoBuilder
{
    /// <summary>
    /// Checks if build is running in CI/CD environment (Jenkins, etc.)
    /// </summary>
    private static bool IsCIBuild()
    {
        // Check if Unity is running in batch mode (CI/CD)
        if (Application.isBatchMode)
            return true;

        // Check for CI/CD environment variables
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("JENKINS_URL")))
            return true;

        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CI")))
            return true;

        // Check for explicit CI flag
        var ciFlag = Environment.GetEnvironmentVariable("UNITY_CI_BUILD");
        if (!string.IsNullOrEmpty(ciFlag) && Convert.ToBoolean(ciFlag))
            return true;

        return false;
    }

    /// <summary>
    /// Validates build result and exits with error code if build failed
    /// </summary>
    private static void ValidateBuildResult(BuildReport buildReport, string buildName)
    {
        if (buildReport.summary.result == BuildResult.Succeeded)
        {
            var outputPath = buildReport.summary.outputPath;
            if (!string.IsNullOrEmpty(outputPath))
            {
                // Convert to absolute path if relative
                var fullPath = Path.IsPathRooted(outputPath) ? outputPath : Path.GetFullPath(Path.Combine(Application.dataPath, "..", outputPath));
                Debug.Log($"✅ {buildName} build completed successfully\n   Path: {fullPath}");
            }
            else
            {
                Debug.Log($"✅ {buildName} build completed successfully");
            }
        }
        else
        {
            var errorMessage = $"❌ {buildName} build FAILED: {buildReport.summary.result}";
            if (buildReport.summary.totalErrors > 0)
            {
                errorMessage += $"\nTotal errors: {buildReport.summary.totalErrors}";
                errorMessage += $"\nTotal warnings: {buildReport.summary.totalWarnings}";
            }

            Debug.LogError(errorMessage);

#if UNITY_EDITOR
            // Only exit Unity if running in CI/CD environment
            if (IsCIBuild())
            {
                EditorApplication.Exit(1);
            }
            else
            {
                Debug.LogWarning("Build failed, but Unity will remain open (local build)");
            }
#else
            throw new Exception($"Build failed: {buildReport.summary.result}");
#endif
        }
    }

    /// <summary>
    /// Is build in debug mode
    /// </summary>
    public static bool IsDevelopment
    {
        get
        {
            var devEnv = Environment.GetEnvironmentVariable("DEVELOPMENT");
            if (!string.IsNullOrEmpty(devEnv))
                return Convert.ToBoolean(devEnv);

            // Fallback to EditorPrefs for Editor builds
#if UNITY_EDITOR
            return EditorPrefs.GetBool("AutoBuilder.Development", true);
#else
            return false;
#endif
        }
    }

    /// <summary>
    /// Is build was uploaded to production
    /// </summary>
    public static bool IsRelease
    {
        get
        {
            var destination = Environment.GetEnvironmentVariable("DESTINATION");
            if (!string.IsNullOrEmpty(destination))
                return destination == "RELEASE";

            // Fallback to EditorPrefs for Editor builds
#if UNITY_EDITOR
            return EditorPrefs.GetBool("AutoBuilder.Release", false);
#else
            return false;
#endif
        }
    }

    public static string TargetStoreSymbol { get; private set; }

    //Android manifest constants
    private static readonly string AndroidManifestFilePath =
        Path.Combine(Application.dataPath, "Plugins", "Android", "AndroidManifest.xml");

    private const string AndroidNamespace = "http://schemas.android.com/apk/res/android";
    private const string SymbolFirebase = "SDK_FIREBASE";
    private const string SymbolApplovin = "SDK_APPLOVIN";
    private const string SymbolIronsource = "SDK_IRONSOURCE";
    private const string SymbolAdmob = "SDK_ADMOB";
    private const string SymbolDeltaDNA = "SDK_DELTADNA";
    private const string SymbolDeltaDNAIosPushNotificationsRemoved = "DDNA_IOS_PUSH_NOTIFICATIONS_REMOVED";
    private const string SymbolAppsflyer = "SDK_APPSFLYER";
    private const string SymbolVungleAds = "SDK_VUNGLE_ADS";
    private const string SymbolPlaycusDataLake = "SDK_PLAYCUSDATALAKE";

    private const string AndroidBuildPath = "Build/Android";

    /// <summary>
    /// Return array of all active scenes in project for building
    /// </summary>
    private static string[] GetScenePaths()
    {
        return EditorBuildSettings.scenes.Where(entry => entry.enabled).Select(entry => entry.path).ToArray();
    }

    /// <summary>
    /// Setup from environment variables actual for all platforms
    /// </summary>
    private static void SetupGeneralVariables()
    {
        // Cache server
        var cacheServerMode = Environment.GetEnvironmentVariable("UNITY_CACHE_SERVER_MODE");
        if (!string.IsNullOrEmpty(cacheServerMode))
            EditorPrefs.SetInt("CacheServerMode", Int32.Parse(cacheServerMode));
    }

    /// <summary>
    /// Set SuppressUwpPostprocess True Env Var
    /// </summary>
    private const string SuppressUwpPostprocessTrueMenuName = "Build/EnvVars/SUPPRESS_UWP_POSTPROCESS_TRUE";

    [MenuItem(SuppressUwpPostprocessTrueMenuName)]
    public static void SuppressUwpPostprocessTrue()
    {
        EditorApplication.delayCall += () =>
        {
            var envVar = "SUPPRESS_UWP_POSTPROCESS";
            var enabled = Convert.ToBoolean(Environment.GetEnvironmentVariable(envVar));
            enabled = !enabled;
            Environment.SetEnvironmentVariable(envVar, enabled.ToString());
            EditorPrefs.SetBool(SuppressUwpPostprocessTrueMenuName, enabled);
            Menu.SetChecked(SuppressUwpPostprocessTrueMenuName, enabled);
            Debug.Log($"Environment variable settled: {envVar}={Environment.GetEnvironmentVariable(envVar)}");
        };
    }

    /// <summary>
    /// Set Development True Env Var
    /// </summary>
    private const string DevelopmentTrueMenuName = "Build/EnvVars/DEVELOPMENT_TRUE";

    [MenuItem(DevelopmentTrueMenuName)]
    public static void DevelopmentTrue()
    {
        EditorApplication.delayCall += () =>
        {
            var envVar = "DEVELOPMENT";
            var enabled = Convert.ToBoolean(Environment.GetEnvironmentVariable(envVar));
            enabled = !enabled;
            Environment.SetEnvironmentVariable(envVar, enabled.ToString());
            EditorPrefs.SetBool(DevelopmentTrueMenuName, enabled);
            Menu.SetChecked(DevelopmentTrueMenuName, enabled);
            Debug.Log($"Environment variable settled: {envVar}={Environment.GetEnvironmentVariable(envVar)}");
        };
    }

    /// <summary>
    /// Set Destination Release Env Var
    /// </summary>
    private const string DestinationReleaseMenuName = "Build/EnvVars/DESTINATION_RELEASE";

    [MenuItem(DestinationReleaseMenuName)]
    public static void DestinationRelease()
    {
        EditorApplication.delayCall += () =>
        {
            var envVar = "DESTINATION";
            var enabled = Environment.GetEnvironmentVariable(envVar) == "RELEASE";
            enabled = !enabled;
            Environment.SetEnvironmentVariable(envVar, enabled ? "RELEASE" : "DEVELOP");
            EditorPrefs.SetBool(DestinationReleaseMenuName, enabled);
            Menu.SetChecked(DestinationReleaseMenuName, enabled);
            Debug.Log($"Environment variable settled: {envVar}={Environment.GetEnvironmentVariable(envVar)}");
        };
    }

    /// <summary>
    /// Force android V7 export
    /// </summary>
    private const string AndroidForceV7MenuName = "Build/EnvVars/UNITY_ANDROID_FORCE_V7_TRUE";

    [MenuItem(AndroidForceV7MenuName)]
    public static void AndroidForceV7()
    {
        EditorApplication.delayCall += () =>
        {
            var envVar = "UNITY_ANDROID_FORCE_V7";
            var enabled = Convert.ToBoolean(Environment.GetEnvironmentVariable(envVar));
            enabled = !enabled;
            Environment.SetEnvironmentVariable(envVar, enabled.ToString());
            EditorPrefs.SetBool(AndroidForceV7MenuName, enabled);
            Menu.SetChecked(AndroidForceV7MenuName, enabled);
            Debug.Log($"Environment variables settled: {envVar}={Environment.GetEnvironmentVariable(envVar)}");
        };
    }

    /// <summary>
    /// Set PatchWebglLoaderForMacOs11 True Env Var
    /// </summary>
    private const string PatchWebglLoaderForMacOs11MenuName = "Build/EnvVars/PATCH_WEBGL_LOADER_FOR_MACOS_11_TRUE";

    [MenuItem(PatchWebglLoaderForMacOs11MenuName)]
    public static void PatchWebglLoaderForMacOs11True()
    {
        EditorApplication.delayCall += () =>
        {
            var envVar = "PATCH_WEBGL_LOADER_FOR_MACOS_11";
            var enabled = Convert.ToBoolean(Environment.GetEnvironmentVariable(envVar));
            enabled = !enabled;
            Environment.SetEnvironmentVariable(envVar, enabled.ToString());
            EditorPrefs.SetBool(PatchWebglLoaderForMacOs11MenuName, enabled);
            Menu.SetChecked(PatchWebglLoaderForMacOs11MenuName, enabled);
            Debug.Log($"Environment variable settled: {envVar}={Environment.GetEnvironmentVariable(envVar)}");
        };
    }

    /// <summary>
    /// Set SuppressUwpPostprocess True Env Var
    /// </summary>
    private const string ForceAutoconnectProfilerMenuName = "Build/EnvVars/FORCE_AUTOCONNECT_PROFILER";

    [MenuItem(ForceAutoconnectProfilerMenuName)]
    public static void ForceAutoconnectProfiler()
    {
        EditorApplication.delayCall += () =>
        {
            var envVar = "FORCE_AUTOCONNECT_PROFILER";
            var enabled = Convert.ToBoolean(Environment.GetEnvironmentVariable(envVar));
            enabled = !enabled;
            Environment.SetEnvironmentVariable(envVar, enabled.ToString());
            EditorPrefs.SetBool(ForceAutoconnectProfilerMenuName, enabled);
            Menu.SetChecked(ForceAutoconnectProfilerMenuName, enabled);
            Debug.Log($"Environment variable settled: {envVar}={Environment.GetEnvironmentVariable(envVar)}");
        };
    }

    /// <summary>
    /// Patch android libraries for STORE_Amazon
    /// </summary>
    private const string UnityStoreAmazonMenuName = "Build/EnvVars/UNITY_STORE_AMAZON";

    [MenuItem(UnityStoreAmazonMenuName)]
    public static void AndroidPatchAmazonLibraries()
    {
        EditorApplication.delayCall += () =>
        {
            var envVar = "UNITY_STORE";
            var enabled = Convert.ToBoolean(Environment.GetEnvironmentVariable(envVar));
            enabled = !enabled;
            if (enabled)
            {
                Environment.SetEnvironmentVariable(envVar, STORE.Amazon.ToString());
            }
            else
            {
                Environment.SetEnvironmentVariable(envVar, "");
            }

            EditorPrefs.SetBool(UnityStoreAmazonMenuName, enabled);
            Menu.SetChecked(UnityStoreAmazonMenuName, enabled);
            Debug.Log($"Environment variables settled: {envVar}={Environment.GetEnvironmentVariable(envVar)}");
        };
    }

    /// <summary>
    /// Run Android build with Gradle (Export project only) - Development
    /// </summary>
    public static void AndroidGradleExportDev()
    {
        EditorPrefs.SetBool("AutoBuilder.Development", true);
        EditorPrefs.SetBool("AutoBuilder.Release", false);
        Environment.SetEnvironmentVariable("DEVELOPMENT", "true");
        Environment.SetEnvironmentVariable("DESTINATION", "appcenter");
        AndroidGradleExport();
    }

    /// <summary>
    /// Run Android build with Gradle (Export project only) - Release
    /// </summary>
    public static void AndroidGradleExportRelease()
    {
        EditorPrefs.SetBool("AutoBuilder.Development", false);
        EditorPrefs.SetBool("AutoBuilder.Release", true);
        Environment.SetEnvironmentVariable("DEVELOPMENT", "false");
        Environment.SetEnvironmentVariable("DESTINATION", "RELEASE");
        AndroidGradleExport();
    }

    /// <summary>
    /// Run Android build with Gradle (Export project only)
    /// </summary>
    public static void AndroidGradleExport()
    {
        //Switch market to Google Play
        UnityPurchasingEditor.TargetAndroidStore(AppStore.GooglePlay);

        // Set store directive
        TargetStoreSymbol = "STORE_" + STORE.GooglePlay.ToString();
        AddScriptDefineSymbol(BuildTargetGroup.Android, TargetStoreSymbol);

        //Change script defined symbols
        RemoveScriptDefineSymbol(BuildTargetGroup.Android, SymbolVungleAds);
        FoldersRemover(TargetStoreSymbol);
        //Remove AppsFlyer Tag
        RemoveAppsFlyerChannelTag();

        //Export gradle project
        ExportGradleProject();
    }

    /// <summary>
    /// Save keystore passwords to EditorPrefs for editor Release builds.
    /// Unity does not persist passwords between sessions, so this stores them locally per-machine.
    /// </summary>
    [MenuItem("Build/Android/Setup Keystore Passwords", false, 20)]
    public static void SetupKeystorePasswords()
    {
        var keystorePass = EditorPrefs.GetString("AutoBuilder.KeystorePass", "");
        var keyaliasPass = EditorPrefs.GetString("AutoBuilder.KeyaliasPass", "");

        // Simple sequential prompts since Unity doesn't have a multi-field dialog
        var newKeystorePass = KeystorePasswordPrompt.Show("Keystore Password",
            "Enter keystore password (stored locally in EditorPrefs, not in project):",
            keystorePass);

        if (newKeystorePass == null) return; // cancelled

        var newKeyaliasPass = KeystorePasswordPrompt.Show("Key Alias Password",
            "Enter key alias password:",
            keyaliasPass);

        if (newKeyaliasPass == null) return; // cancelled

        EditorPrefs.SetString("AutoBuilder.KeystorePass", newKeystorePass);
        EditorPrefs.SetString("AutoBuilder.KeyaliasPass", newKeyaliasPass);

        // Also apply to current session
        PlayerSettings.Android.keystorePass = newKeystorePass;
        PlayerSettings.Android.keyaliasPass = newKeyaliasPass;

        Debug.Log("Android Build: Keystore passwords saved to EditorPrefs");
    }

    /// <summary>
    /// Build Android APK directly - Development
    /// </summary>
    [MenuItem("Build/Android/Build APK (Development)", false, 35)]
    public static void AndroidBuildApkDev()
    {
        EditorPrefs.SetBool("AutoBuilder.Development", true);
        EditorPrefs.SetBool("AutoBuilder.Release", false);
        Environment.SetEnvironmentVariable("DEVELOPMENT", "true");
        Environment.SetEnvironmentVariable("DESTINATION", "appcenter");
        AndroidGradleBuildApk();
    }

    /// <summary>
    /// Build Android APK directly - Release
    /// </summary>
    public static void AndroidBuildApkRelease()
    {
        EditorPrefs.SetBool("AutoBuilder.Development", false);
        EditorPrefs.SetBool("AutoBuilder.Release", true);
        Environment.SetEnvironmentVariable("DEVELOPMENT", "false");
        Environment.SetEnvironmentVariable("DESTINATION", "RELEASE");
        AndroidGradleBuildApk();
    }

    /// <summary>
    /// Build Android APK directly without exporting Gradle project
    /// </summary>
    public static void AndroidGradleBuildApk()
    {
        //Switch market to Google Play
        UnityPurchasingEditor.TargetAndroidStore(AppStore.GooglePlay);

        // Set store directive
        TargetStoreSymbol = "STORE_" + STORE.GooglePlay.ToString();
        AddScriptDefineSymbol(BuildTargetGroup.Android, TargetStoreSymbol);

        //Change script defined symbols
        RemoveScriptDefineSymbol(BuildTargetGroup.Android, SymbolVungleAds);
        FoldersRemover(TargetStoreSymbol);
        //Remove AppsFlyer Tag
        RemoveAppsFlyerChannelTag();

        //Build APK directly
        BuildAndroidApk();
    }

    public static void PrepareUDPExport()
    {
        var store = Environment.GetEnvironmentVariable("UNITY_STORE");
        if (!string.IsNullOrEmpty(store))
            Debug.LogError("UNITY_STORE global variable is not set!");

        // Set store directive
        TargetStoreSymbol = "STORE_" + store;
        AddScriptDefineSymbol(BuildTargetGroup.Android, TargetStoreSymbol);

        //Set Build version
        VersioningHelperUtility.RefreshVersion();

        //Update directives
        RemoveScriptDefineSymbol(BuildTargetGroup.Android, SymbolVungleAds);
        RemoveScriptDefineSymbol(BuildTargetGroup.Android, SymbolFirebase);
        RemoveScriptDefineSymbol(BuildTargetGroup.Android, SymbolIronsource);
        RemoveScriptDefineSymbol(BuildTargetGroup.Android, SymbolAdmob);

        //Insert AppsFlyer Tag
        InsertAppsFlyerChannelTag(store);

        //Remove not supported firebase activity
        RemoveFirebaseFromManifest();

        //Switch market to UDP App Store
        UnityPurchasingEditor.TargetAndroidStore(AppStore.NotSpecified);

        AssetDatabase.SaveAssets();

        EditorCoroutinesUtility.StartWaitingCoroutine();
        PlayServicesResolver.Resolve(EditorCoroutinesUtility.StopWaitingCoroutine, true);
    }

    public static void PrepareAmazonForExportGradleProject()
    {
        // Set store directive
        TargetStoreSymbol = "STORE_" + STORE.Amazon.ToString();
        AddScriptDefineSymbol(BuildTargetGroup.Android, TargetStoreSymbol);
        FoldersRemover(TargetStoreSymbol);
        //Set Build version
        VersioningHelperUtility.RefreshVersion();

        //Change script defined symbols
        RemoveScriptDefineSymbol(BuildTargetGroup.Android, SymbolFirebase);
        //RemoveScriptDefineSymbol(BuildTargetGroup.Android, SymbolApplovin);//Applovin now support amazon
        RemoveScriptDefineSymbol(BuildTargetGroup.Android, SymbolIronsource);
        RemoveScriptDefineSymbol(BuildTargetGroup.Android, SymbolAdmob);

        //Insert Appsflyer Tag
        InsertAppsFlyerChannelTag("Amazon");

        //Remove not supported firebase activity
        RemoveFirebaseFromManifest();

        //Switch market to Amazon App Store
        UnityPurchasingEditor.TargetAndroidStore(AppStore.NotSpecified);

        AssetDatabase.SaveAssets();

        EditorCoroutinesUtility.StartWaitingCoroutine();
        PlayServicesResolver.Resolve(EditorCoroutinesUtility.StopWaitingCoroutine, true);
    }

    /// <summary>
    /// Export Amazon Gradle project (same pattern as AndroidGradleExport)
    /// This exports Gradle project instead of building APK directly, allowing Jenkins to control Gradle version
    /// </summary>
    public static void AmazonGradleBuildApk()
    {
        Debug.Log("[AmazonGradleBuildApk] ========== START ==========");

        // Set store directive
        Debug.Log("[AmazonGradleBuildApk] Step 1: Setting store directive...");
        TargetStoreSymbol = "STORE_" + STORE.Amazon.ToString();
        Debug.Log($"[AmazonGradleBuildApk] TargetStoreSymbol = '{TargetStoreSymbol}'");
        AddScriptDefineSymbol(BuildTargetGroup.Android, TargetStoreSymbol);
        Debug.Log("[AmazonGradleBuildApk] Step 1: Store directive added");

        Debug.Log("[AmazonGradleBuildApk] Step 2: Running FoldersRemover...");
        FoldersRemover(TargetStoreSymbol);
        Debug.Log("[AmazonGradleBuildApk] Step 2: FoldersRemover completed");

        // Change script defined symbols
        // Note: Firebase files and SDK_FIREBASE define are removed in prebuild.sh before Unity starts
        Debug.Log("[AmazonGradleBuildApk] Step 3: Removing unsupported SDK symbols...");
        Debug.Log($"[AmazonGradleBuildApk] Removing SymbolFirebase = '{SymbolFirebase}'");
        RemoveScriptDefineSymbol(BuildTargetGroup.Android, SymbolFirebase);
        Debug.Log($"[AmazonGradleBuildApk] Removing SymbolIronsource = '{SymbolIronsource}'");
        RemoveScriptDefineSymbol(BuildTargetGroup.Android, SymbolIronsource);
        Debug.Log($"[AmazonGradleBuildApk] Removing SymbolAdmob = '{SymbolAdmob}'");
        RemoveScriptDefineSymbol(BuildTargetGroup.Android, SymbolAdmob);
        Debug.Log("[AmazonGradleBuildApk] Step 3: SDK symbols removed");

        // Insert Appsflyer Tag
        Debug.Log("[AmazonGradleBuildApk] Step 4: Inserting AppsFlyer channel tag 'Amazon'...");
        InsertAppsFlyerChannelTag("Amazon");
        Debug.Log("[AmazonGradleBuildApk] Step 4: AppsFlyer channel tag inserted");

        // Remove not supported firebase activity
        Debug.Log("[AmazonGradleBuildApk] Step 5: Removing Firebase from AndroidManifest...");
        RemoveFirebaseFromManifest();
        Debug.Log("[AmazonGradleBuildApk] Step 5: Firebase removed from manifest");

        // Switch market to Amazon App Store
        Debug.Log("[AmazonGradleBuildApk] Step 6: Switching Unity Purchasing to Amazon App Store...");
        UnityPurchasingEditor.TargetAndroidStore(AppStore.NotSpecified);
        Debug.Log("[AmazonGradleBuildApk] Step 6: Unity Purchasing target set to Amazon");

        // Export Gradle project (same as AndroidGradleExport) instead of building APK directly
        // Jenkins pipeline will call Gradle separately with correct version
        Debug.Log("[AmazonGradleBuildApk] Step 7: Exporting Gradle project...");
        ExportGradleProject();
        Debug.Log("[AmazonGradleBuildApk] Step 7: Gradle project exported");

        Debug.Log("[AmazonGradleBuildApk] ========== END ==========");
    }

    /// <summary>
    /// Universal method for building Android from CI/CD
    /// Can be called with: Unity -executeMethod AutoBuilder.BuildAndroid
    /// </summary>
    public static void BuildAndroid()
    {
        var store = Environment.GetEnvironmentVariable("UNITY_STORE");

        if (string.IsNullOrEmpty(store) || store == "GooglePlay")
        {
            Debug.Log("Building for Google Play Store");
            AndroidGradleBuildApk();
        }
        else if (store == "Amazon")
        {
            Debug.Log("Building for Amazon App Store");
            AmazonGradleBuildApk();
        }
        else
        {
            Debug.LogWarning($"Unknown store: {store}. Building for Google Play as default.");
            AndroidGradleBuildApk();
        }
    }

    /// <summary>
    /// Setup Android keystore settings.
    /// CI/CD: uses environment variables. Editor: uses existing PlayerSettings (configured in Project Settings).
    /// </summary>
    private static void SetupAndroidKeystore()
    {
        if (IsDevelopment)
        {
            PlayerSettings.Android.useCustomKeystore = false;
            PlayerSettings.Android.keystoreName = "";
            PlayerSettings.Android.keystorePass = "";
            PlayerSettings.Android.keyaliasName = "";
            PlayerSettings.Android.keyaliasPass = "";
            Debug.Log("Android Build: Using debug keystore for Development build");
            return;
        }

        // Release build
        PlayerSettings.Android.useCustomKeystore = true;

        var envKeystoreName = Environment.GetEnvironmentVariable("UNITY_ANDROID_KEYSTORE_NAME");
        var envKeystorePass = Environment.GetEnvironmentVariable("UNITY_ANDROID_KEYSTORE_PASS");
        var envKeyaliasName = Environment.GetEnvironmentVariable("UNITY_ANDROID_KEYALIAS_NAME");
        var envKeyaliasPass = Environment.GetEnvironmentVariable("UNITY_ANDROID_KEYALIAS_PASS");

        var hasEnvVars = !string.IsNullOrEmpty(envKeystoreName) && !string.IsNullOrEmpty(envKeystorePass) &&
                         !string.IsNullOrEmpty(envKeyaliasName) && !string.IsNullOrEmpty(envKeyaliasPass);

        if (hasEnvVars)
        {
            // CI/CD mode: apply environment variables
            PlayerSettings.Android.keystoreName = envKeystoreName;
            PlayerSettings.Android.keystorePass = envKeystorePass;
            PlayerSettings.Android.keyaliasName = envKeyaliasName;
            PlayerSettings.Android.keyaliasPass = envKeyaliasPass;
            Debug.Log("Android Build: Using keystore from environment variables (CI/CD)");
        }
        else
        {
            // Editor mode: Unity does not persist keystore passwords between sessions,
            // so we store them in EditorPrefs (per-machine, not in version control).
            // Keystore path and alias name are persisted by Unity, only passwords need EditorPrefs.
            var keystoreName = PlayerSettings.Android.keystoreName;
            var keyaliasName = PlayerSettings.Android.keyaliasName;
            var keystorePass = PlayerSettings.Android.keystorePass;
            var keyaliasPass = PlayerSettings.Android.keyaliasPass;

            // If passwords are empty (typical after editor restart), restore from EditorPrefs
            if (string.IsNullOrEmpty(keystorePass))
                keystorePass = EditorPrefs.GetString("AutoBuilder.KeystorePass", "");
            if (string.IsNullOrEmpty(keyaliasPass))
                keyaliasPass = EditorPrefs.GetString("AutoBuilder.KeyaliasPass", "");

            PlayerSettings.Android.keystorePass = keystorePass;
            PlayerSettings.Android.keyaliasPass = keyaliasPass;

            var valid = !string.IsNullOrEmpty(keystoreName) && !string.IsNullOrEmpty(keystorePass) &&
                        !string.IsNullOrEmpty(keyaliasName) && !string.IsNullOrEmpty(keyaliasPass);

            if (valid)
            {
                Debug.Log("Android Build: Using keystore from PlayerSettings/EditorPrefs (Editor)");
            }
            else
            {
                Debug.LogWarning("Android Build: Release build requires keystore settings. " +
                                 "Use Build > Android > Setup Keystore Passwords to configure, " +
                                 "or set UNITY_ANDROID_KEYSTORE_* environment variables for CI/CD. " +
                                 $"keystoreName: {!string.IsNullOrEmpty(keystoreName)}, " +
                                 $"keystorePass: {!string.IsNullOrEmpty(keystorePass)}, " +
                                 $"keyaliasName: {!string.IsNullOrEmpty(keyaliasName)}, " +
                                 $"keyaliasPass: {!string.IsNullOrEmpty(keyaliasPass)}");
            }
        }
    }

    /// <summary>
    /// Build Android APK directly (without gradle project export)
    /// </summary>
    public static void BuildAndroidApk()
    {
        //Get general environment variables
        SetupGeneralVariables();

#if !UNITY_2019_3_OR_NEWER
        //Get platform custom environment variables
        string sdkPath = Environment.GetEnvironmentVariable("ANDROID_SDK_PATH");
        if (!string.IsNullOrEmpty(sdkPath))
            EditorPrefs.SetString("AndroidSdkRoot", sdkPath);

        string ndkPath = Environment.GetEnvironmentVariable("ANDROID_NDK_PATH");
        if (!string.IsNullOrEmpty(ndkPath))
        {
            EditorPrefs.SetString("AndroidNdkRoot", ndkPath);
            EditorPrefs.SetString("AndroidNdkRootR16b", ndkPath);
        }
#endif

        // Keystore settings - Use debug key for Development, production keystore for Release
        SetupAndroidKeystore();

        var splitApk = Environment.GetEnvironmentVariable("UNITY_ANDROID_SPLIT_APK");
        if (!string.IsNullOrEmpty(splitApk))
            PlayerSettings.Android.buildApkPerCpuArchitecture = Convert.ToBoolean(splitApk);

#if UNITY_2020_3_OR_NEWER
        PlayerSettings.Android.minifyRelease = false;
        PlayerSettings.Android.minifyDebug = false;
#else
        EditorUserBuildSettings.androidReleaseMinification = AndroidMinification.None;
        EditorUserBuildSettings.androidDebugMinification = AndroidMinification.None;
#endif

        //Switch platform
        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);

        //Build settings - KEY DIFFERENCE: exportAsGoogleAndroidProject = false
        EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;
        EditorUserBuildSettings.exportAsGoogleAndroidProject = false; // Build APK/AAB directly

        if (!IsRelease)
        {
            // Development - For best building speed
            if (Convert.ToBoolean(Environment.GetEnvironmentVariable("UNITY_ANDROID_FORCE_V7")))
            {
                PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARMv7;
            }
            else
            {
                PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            }
        }
        else
        {
            // Release - For best compability
            var X86_64_enabled = Environment.GetEnvironmentVariable("ANDROID_PC_X86_64_ENABLED");
            if (!string.IsNullOrEmpty(X86_64_enabled) && Convert.ToBoolean(X86_64_enabled))
            {
                PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7 | AndroidArchitecture.X86_64;
            }
            else
            {
                PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7;
            }
        }

        // Symbols for crash debugging
        EditorUserBuildSettings.androidCreateSymbols = AndroidCreateSymbols.Debugging;

        BuildOptions buildOptions;
        if (IsDevelopment)
        {
            if (Convert.ToBoolean(Environment.GetEnvironmentVariable("FORCE_AUTOCONNECT_PROFILER")))
            {
                buildOptions = BuildOptions.Development |
                               BuildOptions.CompressWithLz4 | BuildOptions.ConnectWithProfiler;
            }
            else
            {
                buildOptions = BuildOptions.Development |
                               BuildOptions.CompressWithLz4;
            }
        }
        else
        {
            if (Convert.ToBoolean(Environment.GetEnvironmentVariable("FORCE_AUTOCONNECT_PROFILER")))
            {
                buildOptions = BuildOptions.CompressWithLz4HC | BuildOptions.ConnectWithProfiler;
            }
            else
            {
                buildOptions = BuildOptions.CompressWithLz4HC;
            }
        }

        //Set Build version
        VersioningHelperUtility.RefreshVersion();
        AssetDatabase.SaveAssets();

        // Build addressable database & bundles
        BuildAddressable();

        // Determine output path (APK or AAB based on buildAppBundle setting)
        var extension = EditorUserBuildSettings.buildAppBundle ? "aab" : "apk";
        var buildFileName = $"build_{VersioningHelperUtility.VersionNumber}_{VersioningHelperUtility.BuildNumber}.{extension}";
        var buildPath = Path.Combine("Build", buildFileName);

        Debug.Log($"Building Android {extension.ToUpper()} to: {buildPath}");

        //Build APK/AAB directly
        // Note: Gradle wrapper and dependencies are updated automatically via UpdateGradleWrapper.OnPostGenerateGradleAndroidProject
        // after Unity generates the Gradle project, so we don't need to update them here
        var buildReport = BuildPipeline.BuildPlayer(GetScenePaths(), buildPath, BuildTarget.Android, buildOptions);

        ValidateBuildResult(buildReport, $"Android {extension.ToUpper()} ({buildPath})");
    }

    /// <summary>
    /// Build Android AAB directly - Development
    /// </summary>
    public static void AndroidBuildAabDev()
    {
        EditorPrefs.SetBool("AutoBuilder.Development", true);
        EditorPrefs.SetBool("AutoBuilder.Release", false);
        Environment.SetEnvironmentVariable("DEVELOPMENT", "true");
        Environment.SetEnvironmentVariable("DESTINATION", "appcenter");
        AndroidGradleBuildAab();
    }

    /// <summary>
    /// Build Android AAB directly - Release
    /// </summary>
    [MenuItem("Build/Android/Build AAB (Release)", false, 39)]
    public static void AndroidBuildAabRelease()
    {
        EditorPrefs.SetBool("AutoBuilder.Development", false);
        EditorPrefs.SetBool("AutoBuilder.Release", true);
        Environment.SetEnvironmentVariable("DEVELOPMENT", "false");
        Environment.SetEnvironmentVariable("DESTINATION", "RELEASE");
        AndroidGradleBuildAab();
    }

    /// <summary>
    /// Build Android AAB directly without exporting Gradle project
    /// </summary>
    public static void AndroidGradleBuildAab()
    {
        //Switch market to Google Play
        UnityPurchasingEditor.TargetAndroidStore(AppStore.GooglePlay);

        // Set store directive
        TargetStoreSymbol = "STORE_" + STORE.GooglePlay.ToString();
        AddScriptDefineSymbol(BuildTargetGroup.Android, TargetStoreSymbol);

        //Change script defined symbols
        RemoveScriptDefineSymbol(BuildTargetGroup.Android, SymbolVungleAds);
        FoldersRemover(TargetStoreSymbol);
        //Remove AppsFlyer Tag
        RemoveAppsFlyerChannelTag();

        //Build AAB directly
        EditorUserBuildSettings.buildAppBundle = true;
        BuildAndroidApk();
        EditorUserBuildSettings.buildAppBundle = false; // Reset after build
    }

    /// <summary>
    /// Export android gradle project
    /// </summary>
    public static void ExportGradleProject()
    {
        //Get general environment variables
        SetupGeneralVariables();

#if !UNITY_2019_3_OR_NEWER
        //Get platform custom environment variables
        string sdkPath = Environment.GetEnvironmentVariable("ANDROID_SDK_PATH");
        if (!string.IsNullOrEmpty(sdkPath))
            EditorPrefs.SetString("AndroidSdkRoot", sdkPath);

        string ndkPath = Environment.GetEnvironmentVariable("ANDROID_NDK_PATH");
        if (!string.IsNullOrEmpty(ndkPath))
        {
            EditorPrefs.SetString("AndroidNdkRoot", ndkPath);
            EditorPrefs.SetString("AndroidNdkRootR16b", ndkPath);
        }
#endif

        // Keystore settings - Use debug key for Development, production keystore for Release
        SetupAndroidKeystore();

        var splitApk = Environment.GetEnvironmentVariable("UNITY_ANDROID_SPLIT_APK");
        if (!string.IsNullOrEmpty(splitApk))
            PlayerSettings.Android.buildApkPerCpuArchitecture = Convert.ToBoolean(splitApk);

#if UNITY_2020_3_OR_NEWER
        PlayerSettings.Android.minifyRelease = false;
        PlayerSettings.Android.minifyDebug = false;
#else
        EditorUserBuildSettings.androidReleaseMinification = AndroidMinification.None;
        EditorUserBuildSettings.androidDebugMinification = AndroidMinification.None;
#endif

        //Switch platform
        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);

        //Build settings
        EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;
        EditorUserBuildSettings.exportAsGoogleAndroidProject = true;

        if (!IsRelease)
        {
            // Development - For best building speed
            if (Convert.ToBoolean(Environment.GetEnvironmentVariable("UNITY_ANDROID_FORCE_V7")))
            {
                PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARMv7;
            }
            else
            {
                PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            }
        }
        else
        {
            // Release - For best compability
            var X86_64_enabled = Environment.GetEnvironmentVariable("ANDROID_PC_X86_64_ENABLED");
            if (!string.IsNullOrEmpty(X86_64_enabled) && Convert.ToBoolean(X86_64_enabled))
            {
                PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7 | AndroidArchitecture.X86_64;
            }
            else
            {
                PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7;
            }
        }

        // Symbols for crash debugging
        EditorUserBuildSettings.androidCreateSymbols = AndroidCreateSymbols.Debugging;

        BuildOptions buildOptions;
        if (IsDevelopment)
        {
            if (Convert.ToBoolean(Environment.GetEnvironmentVariable("FORCE_AUTOCONNECT_PROFILER")))
            {
                buildOptions = BuildOptions.Development |
                               BuildOptions.CompressWithLz4 | BuildOptions.ConnectWithProfiler;
            }
            else
            {
                buildOptions = BuildOptions.Development |
                               BuildOptions.CompressWithLz4;
            }
        }
        else
        {
            if (Convert.ToBoolean(Environment.GetEnvironmentVariable("FORCE_AUTOCONNECT_PROFILER")))
            {
                buildOptions = BuildOptions.CompressWithLz4HC | BuildOptions.ConnectWithProfiler;
            }
            else
            {
                buildOptions = BuildOptions.CompressWithLz4HC;
            }
        }

        //Set Build version
        VersioningHelperUtility.RefreshVersion();
        AssetDatabase.SaveAssets();

        // Build addressable database & bundles
        BuildAddressable();

        // Clean existing Gradle project to avoid template version mismatch errors
        // Unity expects a clean folder or matching template version
        var fullAndroidBuildPath = Path.Combine(Application.dataPath, "..", AndroidBuildPath);
        if (Directory.Exists(fullAndroidBuildPath))
        {
            try
            {
                Debug.Log($"🧹 Cleaning existing Gradle project at: {fullAndroidBuildPath}");
                Directory.Delete(fullAndroidBuildPath, true);
                Debug.Log("✅ Successfully cleaned Gradle project folder");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"⚠️ Failed to clean Gradle project folder: {ex.Message}. Continuing anyway...");
            }
        }

        //Build
        // Note: Gradle wrapper and dependencies are updated automatically via UpdateGradleWrapper.OnPostGenerateGradleAndroidProject
        // after Unity generates the Gradle project, so we don't need to update them here
        var buildReport = BuildPipeline.BuildPlayer(GetScenePaths(), AndroidBuildPath, BuildTarget.Android, buildOptions);

        ValidateBuildResult(buildReport, $"Android Gradle Export ({AndroidBuildPath})");
    }

    /// <summary>
    /// Updates Gradle wrapper to version 8.0+ to fix compatibility with Android Gradle Plugin
    /// </summary>
    private static void UpdateGradleWrapperVersion()
    {
        UpdateGradleWrapperVersionInternal(null);
    }

    /// <summary>
    /// Internal method to update Gradle wrapper version
    /// Can be called with a specific path (from IPostGenerateGradleAndroidProject) or null to search all paths
    /// </summary>
    public static void UpdateGradleWrapperVersionInternal(string gradleProjectPath)
    {
        try
        {
            // Possible locations for gradle-wrapper.properties
            var possiblePaths = new List<string>();

            // If specific path provided (from callback), use it first
            if (!string.IsNullOrEmpty(gradleProjectPath))
            {
                possiblePaths.Add(Path.Combine(gradleProjectPath, "..", "gradle", "wrapper", "gradle-wrapper.properties"));
                possiblePaths.Add(Path.Combine(gradleProjectPath, "gradle", "wrapper", "gradle-wrapper.properties"));
            }

            // Add default search paths
            possiblePaths.Add(Path.Combine(Application.dataPath, "..", "Library", "Bee", "Android", "Prj", "IL2CPP", "Gradle", "gradle", "wrapper", "gradle-wrapper.properties"));
            possiblePaths.Add(Path.Combine(Application.dataPath, "..", AndroidBuildPath, "gradle", "wrapper", "gradle-wrapper.properties"));
            possiblePaths.Add(Path.Combine(Application.dataPath, "..", "Library", "Bee", "artifacts", "Android", "Gradle", "gradle", "wrapper", "gradle-wrapper.properties"));

            const string targetGradleVersion = "8.0";
            var updated = false;

            foreach (var wrapperPath in possiblePaths)
            {
                try
                {
                    var normalizedPath = Path.GetFullPath(wrapperPath);
                    if (!File.Exists(normalizedPath))
                        continue;

                    var content = File.ReadAllText(normalizedPath);
                    var originalContent = content;

                    // Update distributionUrl to Gradle 8.0+
                    // Pattern: distributionUrl=https\://services.gradle.org/distributions/gradle-7.5.1-all.zip or -bin.zip
                    // Support both -all.zip and -bin.zip variants
                    var regex = new System.Text.RegularExpressions.Regex(
                        @"distributionUrl=https\\://services\.gradle\.org/distributions/gradle-(\d+\.\d+(?:\.\d+)?)-(all|bin)\.zip"
                    );

                    if (regex.IsMatch(content))
                    {
                        var match = regex.Match(content);
                        var currentVersion = match.Groups[1].Value;
                        var zipType = match.Groups[2].Value; // "all" or "bin"

                        // Compare versions - only update if current version is less than 8.0
                        if (CompareVersion(currentVersion, targetGradleVersion) < 0)
                        {
                            // Preserve the zip type (all or bin) when updating
                            content = regex.Replace(content, $"distributionUrl=https\\://services.gradle.org/distributions/gradle-{targetGradleVersion}-{zipType}.zip");

                            if (content != originalContent)
                            {
                                File.WriteAllText(normalizedPath, content);
                                Debug.Log($"✅ Updated Gradle wrapper from {currentVersion} to {targetGradleVersion} in: {normalizedPath}");
                                updated = true;
                            }
                        }
                        else
                        {
                            Debug.Log($"ℹ️ Gradle wrapper already at version {currentVersion} (>= {targetGradleVersion}) in: {normalizedPath}");
                            updated = true; // Consider it updated if already at correct version
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"⚠️ Could not find distributionUrl pattern in: {normalizedPath}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"❌ Failed to update Gradle wrapper in {wrapperPath}: {ex.Message}");
                    Debug.LogException(ex);
                }
            }

            if (!updated)
            {
                Debug.LogWarning("⚠️ Gradle wrapper properties file not found. Build may fail if Gradle version < 8.0 is required.");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"❌ Critical error in UpdateGradleWrapperVersionInternal: {ex.Message}");
            Debug.LogException(ex);
        }
    }

    /// <summary>
    /// Configures Gradle to use JDK 17 by setting org.gradle.java.home in gradle.properties
    /// This is needed because Unity sets JAVA_HOME to JDK 11 (from Unity Preferences),
    /// but Android Gradle Plugin 8.0+ requires JDK 17
    /// </summary>
    public static void ConfigureGradleJavaHome(string gradleProjectPath)
    {
        try
        {
            // JDK 17 path (for Gradle)
            // First, check environment variables (may be set by Jenkins or system)
            var jdk17Path = Environment.GetEnvironmentVariable("JDK_17_PATH")
                            ?? Environment.GetEnvironmentVariable("JAVA_17_HOME")
                            ?? Environment.GetEnvironmentVariable("JAVA_HOME");

            // If JAVA_HOME is not set or points to JDK 11, try to find JDK 17
            if (string.IsNullOrEmpty(jdk17Path) || jdk17Path.Contains("jdk-11") || jdk17Path.Contains("OpenJDK"))
            {
                // Try common JDK 17 locations
                // Order: Jenkins agent paths -> System paths -> User paths
                var possibleJdk17Paths = new List<string>();

                // Jenkins agent paths (most common for CI/CD)
                possibleJdk17Paths.Add(@"C:\jenkins-agent-new\tools\jdk-17");
                possibleJdk17Paths.Add(@"C:\jenkins-agent-new\jdk-17");
                possibleJdk17Paths.Add(@"C:\tools\jdk-17");
                possibleJdk17Paths.Add(@"C:\Program Files\Java\jdk-17");
                possibleJdk17Paths.Add(@"C:\Program Files (x86)\Java\jdk-17");

                // Eclipse Adoptium system paths
                possibleJdk17Paths.Add(@"C:\Program Files\Eclipse Adoptium\jdk-17.0.17+10");
                possibleJdk17Paths.Add(@"C:\Program Files\Eclipse Adoptium\jdk-17");

                // Try to find any jdk-17* folder in common locations
                var commonBasePaths = new[]
                {
                    @"C:\Program Files\Eclipse Adoptium",
                    @"C:\Program Files\Java",
                    @"C:\Program Files (x86)\Java",
                    @"C:\tools",
                    @"C:\jenkins-agent-new\tools",
                    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)
                };

                foreach (var basePath in commonBasePaths)
                {
                    if (Directory.Exists(basePath))
                    {
                        try
                        {
                            var jdkFolders = Directory.GetDirectories(basePath, "jdk-17*", SearchOption.TopDirectoryOnly);
                            foreach (var jdkFolder in jdkFolders)
                            {
                                if (!possibleJdk17Paths.Contains(jdkFolder))
                                {
                                    possibleJdk17Paths.Add(jdkFolder);
                                }
                            }
                        }
                        catch
                        {
                            // Ignore errors when searching directories
                        }
                    }
                }

                // User-specific paths (for local development)
                var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                possibleJdk17Paths.Add(Path.Combine(localAppData, "Eclipse Adoptium", "jdk-17.0.17+10"));
                possibleJdk17Paths.Add(Path.Combine(localAppData, "Eclipse Adoptium", "jdk-17"));

                // Try to find jdk-17* in user's LocalAppData
                var adoptiumPath = Path.Combine(localAppData, "Eclipse Adoptium");
                if (Directory.Exists(adoptiumPath))
                {
                    try
                    {
                        var userJdkFolders = Directory.GetDirectories(adoptiumPath, "jdk-17*", SearchOption.TopDirectoryOnly);
                        foreach (var jdkFolder in userJdkFolders)
                        {
                            if (!possibleJdk17Paths.Contains(jdkFolder))
                            {
                                possibleJdk17Paths.Add(jdkFolder);
                            }
                        }
                    }
                    catch
                    {
                        // Ignore errors
                    }
                }

                // Search through all possible paths
                foreach (var path in possibleJdk17Paths)
                {
                    if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
                    {
                        var javaExe = Path.Combine(path, "bin", "java.exe");
                        if (File.Exists(javaExe))
                        {
                            // Verify it's actually JDK 17 by checking version
                            try
                            {
                                var psi = new System.Diagnostics.ProcessStartInfo
                                {
                                    FileName = javaExe,
                                    Arguments = "-version",
                                    UseShellExecute = false,
                                    RedirectStandardOutput = true,
                                    RedirectStandardError = true,
                                    CreateNoWindow = true
                                };

                                using (var process = System.Diagnostics.Process.Start(psi))
                                {
                                    process.WaitForExit(3000);
                                    var output = process.StandardError.ReadToEnd();
                                    if (output.Contains("version \"17") || output.Contains("17.0"))
                                    {
                                        jdk17Path = path;
                                        Debug.Log($"[ConfigureGradleJavaHome] Found JDK 17 at: {jdk17Path}");
                                        break;
                                    }
                                }
                            }
                            catch
                            {
                                // If version check fails, still use the path if java.exe exists
                                jdk17Path = path;
                                Debug.Log($"[ConfigureGradleJavaHome] Found JDK at: {jdk17Path} (version check skipped)");
                                break;
                            }
                        }
                    }
                }
            }

            if (string.IsNullOrEmpty(jdk17Path) || !Directory.Exists(jdk17Path))
            {
                Debug.LogWarning("⚠️ JDK 17 not found. Gradle may fail with JDK 11. Please install JDK 17 and set JAVA_HOME or JDK_17_PATH.");
                Debug.LogWarning("   Searched in: Jenkins agent paths, Program Files, Eclipse Adoptium, user directories");
                Debug.LogWarning($"   Current JAVA_HOME: {Environment.GetEnvironmentVariable("JAVA_HOME") ?? "not set"}");
                return;
            }

            Debug.Log($"[ConfigureGradleJavaHome] Using JDK 17: {jdk17Path}");

            // Normalize path (use forward slashes for Gradle on Windows)
            var normalizedJdkPath = jdk17Path.Replace('\\', '/');

            // Possible locations for gradle.properties
            var possibleGradlePropertiesPaths = new List<string>();

            // If specific path provided (from callback), use it first
            if (!string.IsNullOrEmpty(gradleProjectPath))
            {
                possibleGradlePropertiesPaths.Add(Path.Combine(gradleProjectPath, "gradle.properties"));
                possibleGradlePropertiesPaths.Add(Path.Combine(gradleProjectPath, "..", "gradle.properties"));
            }

            // Add default search paths
            possibleGradlePropertiesPaths.Add(Path.Combine(Application.dataPath, "..", "Library", "Bee", "Android", "Prj", "IL2CPP", "Gradle", "gradle.properties"));
            possibleGradlePropertiesPaths.Add(Path.Combine(Application.dataPath, "..", AndroidBuildPath, "gradle.properties"));
            possibleGradlePropertiesPaths.Add(Path.Combine(Application.dataPath, "..", "Library", "Bee", "artifacts", "Android", "Gradle", "gradle.properties"));

            var configured = false;

            foreach (var gradlePropertiesPath in possibleGradlePropertiesPaths)
            {
                try
                {
                    var normalizedPath = Path.GetFullPath(gradlePropertiesPath);
                    var gradlePropertiesDir = Path.GetDirectoryName(normalizedPath);

                    // Create directory if it doesn't exist
                    if (!Directory.Exists(gradlePropertiesDir))
                    {
                        Directory.CreateDirectory(gradlePropertiesDir);
                    }

                    // Read or create gradle.properties
                    var content = "";
                    if (File.Exists(normalizedPath))
                    {
                        content = File.ReadAllText(normalizedPath);
                    }

                    // Check if org.gradle.java.home is already set
                    if (content.Contains("org.gradle.java.home"))
                    {
                        // Update existing entry
                        var regex = new System.Text.RegularExpressions.Regex(
                            @"org\.gradle\.java\.home\s*=\s*.*"
                        );

                        if (regex.IsMatch(content))
                        {
                            content = regex.Replace(content, $"org.gradle.java.home={normalizedJdkPath}");
                        }
                        else
                        {
                            // Add on new line if pattern exists but format is different
                            content += $"\norg.gradle.java.home={normalizedJdkPath}";
                        }
                    }
                    else
                    {
                        // Add new entry
                        if (!string.IsNullOrEmpty(content) && !content.EndsWith("\n"))
                        {
                            content += "\n";
                        }

                        content += $"org.gradle.java.home={normalizedJdkPath}\n";
                    }

                    File.WriteAllText(normalizedPath, content);
                    configured = true;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[ConfigureGradleJavaHome] Failed to configure gradle.properties at {gradlePropertiesPath}: {ex.Message}");
                }
            }

            if (!configured)
            {
                Debug.LogWarning("⚠️ Could not find or create gradle.properties file. Gradle may use JDK 11 instead of JDK 17.");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"❌ Critical error in ConfigureGradleJavaHome: {ex.Message}");
            Debug.LogException(ex);
        }
    }

    /// <summary>
    /// Stops Gradle daemon to ensure it uses new JDK settings from gradle.properties
    /// This is important because daemon might be running with old JDK 11
    /// </summary>
    public static void StopGradleDaemon(string gradleProjectPath)
    {
        try
        {
            // Find gradlew or gradle executable
            var possibleGradlePaths = new List<string>();

            if (!string.IsNullOrEmpty(gradleProjectPath))
            {
                possibleGradlePaths.Add(Path.Combine(gradleProjectPath, "gradlew.bat"));
                possibleGradlePaths.Add(Path.Combine(gradleProjectPath, "gradlew"));
            }

            // Add default search paths
            possibleGradlePaths.Add(Path.Combine(Application.dataPath, "..", "Library", "Bee", "Android", "Prj", "IL2CPP", "Gradle", "gradlew.bat"));
            possibleGradlePaths.Add(Path.Combine(Application.dataPath, "..", AndroidBuildPath, "gradlew.bat"));

            foreach (var gradlePath in possibleGradlePaths)
            {
                try
                {
                    var normalizedPath = Path.GetFullPath(gradlePath);
                    if (File.Exists(normalizedPath))
                    {
                        // Try to stop daemon (non-blocking, ignore errors)
                        var psi = new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = normalizedPath,
                            Arguments = "--stop",
                            UseShellExecute = false,
                            CreateNoWindow = true,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true
                        };

                        using (var process = System.Diagnostics.Process.Start(psi))
                        {
                            process.WaitForExit(5000); // Wait max 5 seconds
                        }

                        break;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[StopGradleDaemon] Failed to stop Gradle daemon using {gradlePath}: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[StopGradleDaemon] Error stopping Gradle daemon: {ex.Message}");
            // Non-critical, continue
        }
    }

    /// <summary>
    /// Disables AppLovin SafeDK plugin for development builds to avoid MaxAdView errors
    /// SafeDK is only needed for release builds, and AAR file already contains all necessary classes
    /// </summary>
    public static void AddAppLovinDependencies(string gradleProjectPath)
    {
        try
        {
            // For dev builds, disable SafeDK plugin to avoid MaxAdView errors
            // AAR file already contains all necessary classes, so Maven dependency is not needed
            var isDevelopment = IsDevelopment;

            if (isDevelopment)
            {
                DisableAppLovinSafeDK(gradleProjectPath);
            }
            else
            {
                // For release builds, ensure AppLovin repository is available (if needed)
                AddAppLovinRepository(gradleProjectPath);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"❌ Critical error in AddAppLovinDependencies: {ex.Message}");
            Debug.LogException(ex);
        }
    }

    /// <summary>
    /// Disables AppLovin SafeDK plugin in launcher/build.gradle for development builds
    /// </summary>
    private static void DisableAppLovinSafeDK(string gradleProjectPath)
    {
        try
        {
            // Find launcher build.gradle
            var possibleBuildGradlePaths = new List<string>();

            if (!string.IsNullOrEmpty(gradleProjectPath))
            {
                possibleBuildGradlePaths.Add(Path.Combine(gradleProjectPath, "launcher", "build.gradle"));
            }

            // Add default search paths
            possibleBuildGradlePaths.Add(Path.Combine(Application.dataPath, "..", "Library", "Bee", "Android", "Prj", "IL2CPP", "Gradle", "launcher", "build.gradle"));
            possibleBuildGradlePaths.Add(Path.Combine(Application.dataPath, "..", AndroidBuildPath, "launcher", "build.gradle"));

            foreach (var buildGradlePath in possibleBuildGradlePaths)
            {
                try
                {
                    var normalizedPath = Path.GetFullPath(buildGradlePath);
                    if (!File.Exists(normalizedPath))
                        continue;

                    var content = File.ReadAllText(normalizedPath);
                    var originalContent = content;

                    // Remove AppLovin SafeDK plugin for dev builds
                    // Replace "apply plugin: 'applovin-quality-service'" with commented version
                    if (content.Contains("apply plugin: 'applovin-quality-service'"))
                    {
                        content = System.Text.RegularExpressions.Regex.Replace(
                            content,
                            @"apply plugin:\s*['""]applovin-quality-service['""]",
                            "// apply plugin: 'applovin-quality-service' // Disabled for dev builds"
                        );
                    }

                    // Remove applovin { } block for dev builds
                    if (content.Contains("applovin {"))
                    {
                        var applovinStart = content.IndexOf("applovin {");
                        var applovinEnd = content.IndexOf("}", applovinStart);
                        if (applovinEnd != -1)
                        {
                            var applovinBlock = content.Substring(applovinStart, applovinEnd - applovinStart + 1);
                            content = content.Replace(applovinBlock, "// applovin { ... } // Disabled for dev builds\n");
                        }
                    }

                    // Remove Maven dependency if it exists (AAR file is sufficient)
                    if (content.Contains("com.applovin.mediation:max-sdk"))
                    {
                        content = System.Text.RegularExpressions.Regex.Replace(
                            content,
                            @"\s*implementation\s+['""]com\.applovin\.mediation:max-sdk:.*?['""]\s*\n",
                            "    // implementation 'com.applovin.mediation:max-sdk:...' // Not needed, using AAR file\n"
                        );
                    }

                    if (content != originalContent)
                    {
                        File.WriteAllText(normalizedPath, content);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[DisableAppLovinSafeDK] Failed to modify build.gradle at {buildGradlePath}: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"❌ Critical error in DisableAppLovinSafeDK: {ex.Message}");
            Debug.LogException(ex);
        }
    }

    /// <summary>
    /// Adds AppLovin Maven repository to settings.gradle dependencyResolutionManagement section
    /// Required for Gradle to resolve AppLovin MAX SDK dependencies
    /// </summary>
    private static void AddAppLovinRepository(string gradleProjectPath)
    {
        try
        {
            // Find settings.gradle
            var possibleSettingsGradlePaths = new List<string>();

            if (!string.IsNullOrEmpty(gradleProjectPath))
            {
                possibleSettingsGradlePaths.Add(Path.Combine(gradleProjectPath, "settings.gradle"));
            }

            // Add default search paths
            possibleSettingsGradlePaths.Add(Path.Combine(Application.dataPath, "..", "Library", "Bee", "Android", "Prj", "IL2CPP", "Gradle", "settings.gradle"));
            possibleSettingsGradlePaths.Add(Path.Combine(Application.dataPath, "..", AndroidBuildPath, "settings.gradle"));

            foreach (var settingsGradlePath in possibleSettingsGradlePaths)
            {
                try
                {
                    var normalizedPath = Path.GetFullPath(settingsGradlePath);
                    if (!File.Exists(normalizedPath))
                        continue;

                    var content = File.ReadAllText(normalizedPath);
                    var originalContent = content;

                    // Check if AppLovin repository already exists in dependencyResolutionManagement
                    if (content.Contains("artifacts.applovin.com/android") && content.Contains("dependencyResolutionManagement"))
                    {
                        continue;
                    }

                    // Find dependencyResolutionManagement.repositories section
                    var drmIndex = content.IndexOf("dependencyResolutionManagement");
                    if (drmIndex == -1)
                    {
                        continue;
                    }

                    // Find repositories { block inside dependencyResolutionManagement
                    var repositoriesIndex = content.IndexOf("repositories {", drmIndex);
                    if (repositoriesIndex == -1)
                    {
                        continue;
                    }

                    // Find the closing brace of repositories block
                    var braceCount = 0;
                    var startIndex = repositoriesIndex;
                    var insertIndex = -1;

                    for (var i = startIndex; i < content.Length; i++)
                    {
                        if (content[i] == '{')
                            braceCount++;
                        else if (content[i] == '}')
                        {
                            braceCount--;
                            if (braceCount == 0)
                            {
                                insertIndex = i;
                                break;
                            }
                        }
                    }

                    if (insertIndex == -1)
                    {
                        continue;
                    }

                    // Insert AppLovin Maven repository before closing brace
                    // Use content filter to only include AppLovin packages (for performance)
                    var repositoryToAdd = "        maven { url 'https://artifacts.applovin.com/android'; content { includeGroupByRegex 'com.applovin.*' } }\n";
                    content = content.Insert(insertIndex, repositoryToAdd);

                    if (content != originalContent)
                    {
                        File.WriteAllText(normalizedPath, content);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[AddAppLovinRepository] Failed to modify settings.gradle at {settingsGradlePath}: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"❌ Critical error in AddAppLovinRepository: {ex.Message}");
            Debug.LogException(ex);
        }
    }

    /// <summary>
    /// Adds missing AndroidX dependencies to unityLibrary build.gradle
    /// Required for Firebase and other libraries that use AndroidX
    /// </summary>
    public static void AddAndroidXDependencies(string gradleProjectPath)
    {
        try
        {
            // Find unityLibrary build.gradle
            var possibleBuildGradlePaths = new List<string>();

            if (!string.IsNullOrEmpty(gradleProjectPath))
            {
                possibleBuildGradlePaths.Add(Path.Combine(gradleProjectPath, "unityLibrary", "build.gradle"));
            }

            // Add default search paths
            possibleBuildGradlePaths.Add(Path.Combine(Application.dataPath, "..", "Library", "Bee", "Android", "Prj", "IL2CPP", "Gradle", "unityLibrary", "build.gradle"));
            possibleBuildGradlePaths.Add(Path.Combine(Application.dataPath, "..", AndroidBuildPath, "unityLibrary", "build.gradle"));

            foreach (var buildGradlePath in possibleBuildGradlePaths)
            {
                try
                {
                    var normalizedPath = Path.GetFullPath(buildGradlePath);
                    if (!File.Exists(normalizedPath))
                        continue;

                    var content = File.ReadAllText(normalizedPath);
                    var originalContent = content;

                    // Check if androidx.core dependency already exists
                    if (content.Contains("androidx.core:core"))
                    {
                        continue;
                    }

                    // Find dependencies block and add androidx.core:core
                    // Look for "dependencies {" block
                    var dependenciesIndex = content.IndexOf("dependencies {");
                    if (dependenciesIndex == -1)
                    {
                        Debug.LogWarning($"[AddAndroidXDependencies] Could not find 'dependencies {{' block in: {normalizedPath}");
                        continue;
                    }

                    // Find the closing brace of dependencies block
                    var braceCount = 0;
                    var startIndex = dependenciesIndex;
                    var insertIndex = -1;

                    for (var i = startIndex; i < content.Length; i++)
                    {
                        if (content[i] == '{')
                            braceCount++;
                        else if (content[i] == '}')
                        {
                            braceCount--;
                            if (braceCount == 0)
                            {
                                // Found closing brace, insert before it
                                insertIndex = i;
                                break;
                            }
                        }
                    }

                    if (insertIndex == -1)
                    {
                        Debug.LogWarning($"[AddAndroidXDependencies] Could not find closing brace of dependencies block in: {normalizedPath}");
                        continue;
                    }

                    // Insert androidx.core:core dependency before closing brace
                    var dependencyToAdd = "    implementation 'androidx.core:core:1.12.0'\n";
                    content = content.Insert(insertIndex, dependencyToAdd);

                    if (content != originalContent)
                    {
                        File.WriteAllText(normalizedPath, content);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[AddAndroidXDependencies] Failed to modify build.gradle at {buildGradlePath}: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"❌ Critical error in AddAndroidXDependencies: {ex.Message}");
            Debug.LogException(ex);
        }
    }

    /// <summary>
    /// Compares two version strings (e.g., "7.5.1" vs "8.0")
    /// Returns: -1 if v1 < v2, 0 if v1 == v2, 1 if v1 > v2
    /// </summary>
    private static int CompareVersion(string v1, string v2)
    {
        var parts1 = v1.Split('.');
        var parts2 = v2.Split('.');

        var maxLength = Math.Max(parts1.Length, parts2.Length);

        for (var i = 0; i < maxLength; i++)
        {
            var part1 = i < parts1.Length ? int.Parse(parts1[i]) : 0;
            var part2 = i < parts2.Length ? int.Parse(parts2[i]) : 0;

            if (part1 < part2) return -1;
            if (part1 > part2) return 1;
        }

        return 0;
    }

    public static void OnPostProcessBuildAndroid(BuildTarget target, string path)
    {
        //Only for Android!
        if (target != BuildTarget.Android)
            return;

        // Update Gradle wrapper to fix compatibility issues (backup in case OnPostGenerateGradleAndroidProject wasn't called)
        UpdateGradleWrapperVersion();

        var oldAndroidBuildPath = AndroidBuildPath + "/" + Application.productName;
        var replacedAndroidBuildPath = AndroidBuildPath + "/build";

        if (Directory.Exists(oldAndroidBuildPath))
        {
            FileUtil.MoveFileOrDirectory(oldAndroidBuildPath, replacedAndroidBuildPath);
        }
    }

    /// <summary>
    /// Run iOS project export - Development
    /// </summary>
    [MenuItem("Build/iOS/Export (Development)", false, 40)]
    public static void IOSExportDev()
    {
        EditorPrefs.SetBool("AutoBuilder.Development", true);
        EditorPrefs.SetBool("AutoBuilder.Release", false);
        Environment.SetEnvironmentVariable("DEVELOPMENT", "true");
        Environment.SetEnvironmentVariable("DESTINATION", "appcenter");
        IOSExport();
    }

    /// <summary>
    /// Run iOS project export - Release
    /// </summary>
    [MenuItem("Build/iOS/Export (Release)", false, 41)]
    public static void IOSExportRelease()
    {
        EditorPrefs.SetBool("AutoBuilder.Development", false);
        EditorPrefs.SetBool("AutoBuilder.Release", true);
        Environment.SetEnvironmentVariable("DEVELOPMENT", "false");
        Environment.SetEnvironmentVariable("DESTINATION", "RELEASE");
        IOSExport();
    }

    /// <summary>
    /// Run Ios project export
    /// </summary>
    [MenuItem("Build/iOS/Export", false, 42)]
    public static void IOSExport()
    {
        //Get general environment variables
        SetupGeneralVariables();
        TargetStoreSymbol = "STORE_" + STORE.Appstore.ToString();
        FoldersRemover(TargetStoreSymbol);
        //Get platform custom environment variables

        //Switch platform
        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.iOS, BuildTarget.iOS);

        // Set store directive

        AddScriptDefineSymbol(BuildTargetGroup.iOS, TargetStoreSymbol);

        //Set Build version
        VersioningHelperUtility.RefreshVersion();

        // Build addressable database & bundles
        BuildAddressable();

        //Build settings
        BuildOptions buildOptions;
        if (IsDevelopment)
        {
            if (Convert.ToBoolean(Environment.GetEnvironmentVariable("FORCE_AUTOCONNECT_PROFILER")))
            {
                buildOptions = BuildOptions.Development |
                               BuildOptions.CompressWithLz4 | BuildOptions.ConnectWithProfiler;
            }
            else
            {
                buildOptions = BuildOptions.Development |
                               BuildOptions.CompressWithLz4;
            }
        }
        else
        {
            if (Convert.ToBoolean(Environment.GetEnvironmentVariable("FORCE_AUTOCONNECT_PROFILER")))
            {
                buildOptions = BuildOptions.CompressWithLz4HC | BuildOptions.ConnectWithProfiler;
            }
            else
            {
                buildOptions = BuildOptions.CompressWithLz4HC;
            }
        }

        //Build
        var buildReport = BuildPipeline.BuildPlayer(GetScenePaths(), "Build/iOS", BuildTarget.iOS, buildOptions);
        ValidateBuildResult(buildReport, "iOS Export");

        // Only exit Unity if running in CI/CD environment
        if (IsCIBuild())
        {
            EditorApplication.Exit(0);
        }
        else
        {
            Debug.Log("iOS Export completed successfully. Unity will remain open (local build)");
        }
    }

    /// <summary>
    /// Run WebGL project export
    /// </summary>
    [MenuItem("Build/WebGL", false, 32)]
    public static void WebGLExport()
    {
        //Get general environment variables
        SetupGeneralVariables();

        // Set store directive
        var store = Environment.GetEnvironmentVariable("UNITY_STORE");
        if (!string.IsNullOrEmpty(store))
        {
            TargetStoreSymbol = "STORE_" + store;
        }
        else
        {
            TargetStoreSymbol = "STORE_" + STORE.Facebook.ToString();
        }

        Debug.Log($"TargetStoreSymbol is {TargetStoreSymbol}");
        AddScriptDefineSymbol(BuildTargetGroup.WebGL, TargetStoreSymbol);

        //Get platform custom environment variables
        var webGLCompressionFormat = Environment.GetEnvironmentVariable("UNITY_WEBGL_COMPRESSION_FORMAT");
        var webGLLinkerTarget = Environment.GetEnvironmentVariable("UNITY_WEBGL_LINKER_TARGET");
        var webGLHeapMax = Convert.ToInt32(Environment.GetEnvironmentVariable("UNITY_WEBGL_HEAP_MAX"));
        if (webGLHeapMax <= 0)
            webGLHeapMax = 1024;

        //Switch platform
        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);

        //Set Build version
        VersioningHelperUtility.RefreshVersion();

        // Build addressable database & bundles
        BuildAddressable();

        //Build settings
        PlayerSettings.WebGL.linkerTarget =
            "asm".Equals(webGLLinkerTarget)
                ? WebGLLinkerTarget.Asm
                : WebGLLinkerTarget.Wasm; //TODO : WebGLLinkerTarget.Both;

        PlayerSettings.WebGL.compressionFormat = "gzip".Equals(webGLCompressionFormat) ? WebGLCompressionFormat.Gzip :
            "brotli".Equals(webGLCompressionFormat) ? WebGLCompressionFormat.Brotli : WebGLCompressionFormat.Disabled;

        PlayerSettings.WebGL.dataCaching = true;

        PlayerSettings.WebGL.emscriptenArgs = "-s WASM_MEM_MAX=" + webGLHeapMax + "MB";

        var exceptionsEnabled = Environment.GetEnvironmentVariable("UNITY_WEBGL_EXCEPTIONS");
        if (!string.IsNullOrEmpty(exceptionsEnabled))
        {
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.None;
        }
        else
        {
            // Default exceptionSupport
            if (IsDevelopment)
            {
                PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.FullWithStacktrace;
            }
            else
            {
                PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
            }
        }

        BuildOptions buildOptions;
        if (IsDevelopment)
        {
            buildOptions = BuildOptions.Development |
                           BuildOptions.CompressWithLz4 | BuildOptions.ConnectWithProfiler;
        }
        else
        {
            if (Convert.ToBoolean(Environment.GetEnvironmentVariable("FORCE_AUTOCONNECT_PROFILER")))
            {
                buildOptions = BuildOptions.CompressWithLz4HC | BuildOptions.ConnectWithProfiler;
            }
            else
            {
                buildOptions = BuildOptions.CompressWithLz4HC;
            }
        }

        //Build
        var buildReport = BuildPipeline.BuildPlayer(GetScenePaths(), $"Build/WebGL/{VersioningHelperUtility.VersionNumber}",
            BuildTarget.WebGL,
            buildOptions);
        ValidateBuildResult(buildReport, $"WebGL ({VersioningHelperUtility.VersionNumber})");
    }

    /// <summary>
    /// Export UWP project
    /// </summary>
    [MenuItem("Build/UWP Export Solution", false, 32)]
    public static void UWPExport()
    {
        //Get general environment variables
        SetupGeneralVariables();
        if (Convert.ToBoolean(Environment.GetEnvironmentVariable("USE_DESCRIPTION_AS_NAME"))) PlayerSettings.productName = PlayerSettings.WSA.applicationDescription;
        //Get platform custom environment variables

        //Switch platform
        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WSA, BuildTarget.WSAPlayer);

        // Set store directive
        TargetStoreSymbol = "STORE_" + STORE.WindowsStore.ToString();
        AddScriptDefineSymbol(BuildTargetGroup.WSA, TargetStoreSymbol);
        FoldersRemover(TargetStoreSymbol);
        //Set Build version
        VersioningHelperUtility.RefreshVersion();

        // Build addressable database & bundles
        BuildAddressable();

        //Build settings
        BuildOptions buildOptions;
        if (IsDevelopment)
        {
            buildOptions = BuildOptions.Development |
                           BuildOptions.CompressWithLz4 | BuildOptions.ConnectWithProfiler;

            //FYI: platformName and names of settings you can find in Library/EditorUserBuildSettings.asset
            EditorUserBuildSettings.SetPlatformSettings("WindowsStoreApps", "BuildConfiguration", "Release");
        }
        else
        {
            if (Convert.ToBoolean(Environment.GetEnvironmentVariable("FORCE_AUTOCONNECT_PROFILER")))
            {
                buildOptions = BuildOptions.CompressWithLz4HC | BuildOptions.ConnectWithProfiler;
            }
            else
            {
                buildOptions = BuildOptions.CompressWithLz4HC;
            }

            //FYI: platformName and names of settings you can find in Library/EditorUserBuildSettings.asset
            EditorUserBuildSettings.SetPlatformSettings("WindowsStoreApps", "BuildConfiguration", "Master");
        }

        EditorUserBuildSettings.development = IsDevelopment;
        EditorUserBuildSettings.wsaSubtarget = WSASubtarget.PC;
        EditorUserBuildSettings.wsaArchitecture = "x64";
        EditorUserBuildSettings.wsaUWPSDK = WSASDK.UWP.ToString();
#if !UNITY_2019_1_OR_NEWER
        EditorUserBuildSettings.wsaSDK = WSASDK.UWP;
#endif
        EditorUserBuildSettings.wsaUWPBuildType = WSAUWPBuildType.XAML;
        EditorUserBuildSettings.wsaBuildAndRunDeployTarget = WSABuildAndRunDeployTarget.LocalMachine;

        if (Convert.ToBoolean(Environment.GetEnvironmentVariable("FORCE_AUTOCONNECT_PROFILER")))
        {
            EditorUserBuildSettings.connectProfiler = true;
        }
        else
        {
            EditorUserBuildSettings.connectProfiler = !IsRelease;
        }

        //Build
        var buildReport = BuildPipeline.BuildPlayer(GetScenePaths(), "Build/UWP",
            BuildTarget.WSAPlayer, buildOptions);
        ValidateBuildResult(buildReport, "UWP Export");
    }

    /// <summary>
    /// Export Mac app
    /// </summary>
    private const string defaultMacOsProjectName = "OSXApp";

    [MenuItem("Build/Mac app", false, 32)]
    public static void MacExport()
    {
        //Get general environment variables
        SetupGeneralVariables();

        var projectName = defaultMacOsProjectName;

        //Switch platform
        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneOSX);

        // Set store directive
        TargetStoreSymbol = "STORE_" + STORE.MacStore.ToString();
        AddScriptDefineSymbol(BuildTargetGroup.Standalone, TargetStoreSymbol);

        //Set Build version
        VersioningHelperUtility.RefreshVersion();

        // Build addressable database & bundles
        BuildAddressable();

        //mac build options
        PlayerSettings.useMacAppStoreValidation = !IsDevelopment;

        //Build settings
        BuildOptions buildOptions;
        if (IsDevelopment)
        {
            buildOptions = BuildOptions.Development |
                           BuildOptions.CompressWithLz4;
        }
        else
        {
            buildOptions = BuildOptions.CompressWithLz4HC;
        }

        EditorUserBuildSettings.development = IsDevelopment;
        EditorUserBuildSettings.connectProfiler = false;

        //Build
        var buildReport = BuildPipeline.BuildPlayer(GetScenePaths(), $"Build/{projectName}", BuildTarget.StandaloneOSX, buildOptions);
        ValidateBuildResult(buildReport, $"Mac app ({projectName})");
    }

    /// <summary>
    /// Export Windows Standalone build
    /// </summary>
    [MenuItem("Build/Standalone Windows", false, 32)]
    public static void StandaloneWindowsBuild()
    {
        //Get general environment variables
        SetupGeneralVariables();

        //Get platform custom environment variables
        var projectName = Environment.GetEnvironmentVariable("PROJECT_NAME");
        if (string.IsNullOrEmpty(projectName))
        {
            projectName = Application.productName;
        }

        // Set store directive
        TargetStoreSymbol = "STORE_" + STORE.WindowsStore.ToString();
        AddScriptDefineSymbol(BuildTargetGroup.Standalone, TargetStoreSymbol);

        //Switch platform
        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64);

        //Set Build version
        VersioningHelperUtility.RefreshVersion();

        // Build addressable database & bundles
        BuildAddressable();

        //Build settings
        BuildOptions buildOptions;
        if (IsDevelopment)
        {
            buildOptions = BuildOptions.Development |
                           BuildOptions.CompressWithLz4 | BuildOptions.ConnectWithProfiler;
        }
        else
        {
            if (Convert.ToBoolean(Environment.GetEnvironmentVariable("FORCE_AUTOCONNECT_PROFILER")))
            {
                buildOptions = BuildOptions.CompressWithLz4HC | BuildOptions.ConnectWithProfiler;
            }
            else
            {
                buildOptions = BuildOptions.CompressWithLz4HC;
            }
        }

        EditorUserBuildSettings.development = IsDevelopment;

        if (Convert.ToBoolean(Environment.GetEnvironmentVariable("FORCE_AUTOCONNECT_PROFILER")))
        {
            EditorUserBuildSettings.connectProfiler = true;
        }
        else
        {
            EditorUserBuildSettings.connectProfiler = !IsRelease;
        }

        //Build
        var buildReport = BuildPipeline.BuildPlayer(GetScenePaths(), $"Build/{projectName}", BuildTarget.StandaloneWindows64,
            buildOptions);
        ValidateBuildResult(buildReport, $"Standalone Windows ({projectName})");
    }

    private static BuildPlayerOptions GetBuildPlayerOptions(
        bool askForLocation = false,
        BuildPlayerOptions defaultOptions = new BuildPlayerOptions())
    {
        // Get static internal "GetBuildPlayerOptionsInternal" method
        var method = typeof(BuildPlayerWindow.DefaultBuildMethods).GetMethod(
            "GetBuildPlayerOptionsInternal",
            BindingFlags.NonPublic | BindingFlags.Static);

        // invoke internal method
        return (BuildPlayerOptions)method.Invoke(
            null,
            new object[] { askForLocation, defaultOptions });
    }

    /// <summary>
    /// Insert <meta-data android:name="CHANNEL" android:value="Amazon" /> into manifest
    /// </summary>
    private static void InsertAppsFlyerChannelTag(string channel)
    {
        if (File.Exists(AndroidManifestFilePath))
        {
            var doc = new XmlDocument();
            doc.Load(AndroidManifestFilePath);

            var applicationNode = doc.SelectSingleNode("manifest/application");
            var namespaceManager = new XmlNamespaceManager(doc.NameTable);

            namespaceManager.AddNamespace("android", AndroidNamespace);
            var ns = namespaceManager.LookupNamespace("android");

            if (applicationNode != null)
            {
                var nodes = applicationNode.SelectNodes("meta-data[@android:name='CHANNEL']", namespaceManager);
                if (nodes?.Count == 0)
                {
                    var channelNode = doc.CreateElement("meta-data");

                    var name = doc.CreateAttribute("android", "name", ns);
                    name.Value = "CHANNEL";

                    var value = doc.CreateAttribute("android", "value", ns);
                    value.Value = channel;

                    channelNode.Attributes.Append(name);
                    channelNode.Attributes.Append(value);

                    applicationNode.AppendChild(channelNode);
                }
            }

            doc.Save(AndroidManifestFilePath);
        }
    }

    /// <summary>
    /// Remove <meta-data android:name="CHANNEL" android:value="Amazon" /> from manifest
    /// </summary>
    private static void RemoveAppsFlyerChannelTag()
    {
        if (File.Exists(AndroidManifestFilePath))
        {
            var doc = new XmlDocument();
            doc.Load(AndroidManifestFilePath);

            var applicationNode = doc.SelectSingleNode("manifest/application");
            var namespaceManager = new XmlNamespaceManager(doc.NameTable);
            namespaceManager.AddNamespace("android", AndroidNamespace);

            var nodes = applicationNode?.SelectNodes("meta-data[@android:name='CHANNEL']", namespaceManager);
            if (nodes != null)
            {
                for (var i = nodes.Count - 1; i >= 0; i--)
                {
                    applicationNode.RemoveChild(nodes[i]);
                }
            }

            doc.Save(AndroidManifestFilePath);
        }
    }

    /// <summary>
    /// Remove com.google.firebase.MessagingUnityPlayerActivity /> from manifest
    /// </summary>
    private static void RemoveFirebaseFromManifest()
    {
        if (File.Exists(AndroidManifestFilePath))
        {
            var doc = new XmlDocument();
            doc.Load(AndroidManifestFilePath);

            var applicationNode = doc.SelectSingleNode("manifest/application");
            var namespaceManager = new XmlNamespaceManager(doc.NameTable);
            namespaceManager.AddNamespace("android", AndroidNamespace);

            var nodes = applicationNode?.SelectNodes(
                "service[@android:name='com.google.firebase.messaging.MessageForwardingService']", namespaceManager);
            if (nodes != null)
            {
                for (var i = nodes.Count - 1; i >= 0; i--)
                {
                    applicationNode.RemoveChild(nodes[i]);
                }
            }

            var nodesUPA = applicationNode?.SelectNodes(
                "activity[@android:name='com.unity3d.player.UnityPlayerActivity']", namespaceManager);

            var nodesMUPA = applicationNode?.SelectNodes(
                "activity[@android:name='com.google.firebase.MessagingUnityPlayerActivity']", namespaceManager);

            if (nodesUPA != null && nodesUPA.Count > 0)
            {
                if (nodesMUPA != null)
                {
                    for (var i = nodesMUPA.Count - 1; i >= 0; i--)
                    {
                        applicationNode.RemoveChild(nodes[i]);
                    }
                }
            }
            else
            {
                if (nodesMUPA != null)
                {
                    for (var i = nodesMUPA.Count - 1; i >= 0; i--)
                    {
                        var attributes = nodesMUPA[i].Attributes;
                        for (var j = attributes.Count - 1; j >= 0; j--)
                        {
                            if (attributes[j].Name == "android:exported")
                            {
                                attributes.RemoveAt(j);
                                continue;
                            }

                            if (attributes[j].Name == "android:configChanges")
                            {
                                attributes.RemoveAt(j);
                                continue;
                            }

                            if (attributes[j].Name == "android:theme")
                            {
                                attributes.RemoveAt(j);
                                continue;
                            }

                            if (attributes[j].Name == "android:name")
                            {
                                attributes[j].Value = "com.unity3d.player.UnityPlayerActivity";
                            }
                        }

                        var themeAttribute = doc.CreateAttribute("android", "theme",
                            "http://schemas.android.com/apk/res/android");
                        themeAttribute.Value = "@style/UnityThemeSelector";
                        attributes.Append(themeAttribute);

                        var exportedAttribute = doc.CreateAttribute("android", "exported",
                            "http://schemas.android.com/apk/res/android");
                        exportedAttribute.Value = "true";
                        attributes.Append(exportedAttribute);
                    }
                }
            }

            doc.Save(AndroidManifestFilePath);
            Debug.Log("RemoveFirebaseTag success");
        }
    }

    /// TODO Remove this method
    /// <summary>
    /// Add script defined symbol to control compilation directives by platform
    /// Uses Unity 2022+ API (NamedBuildTarget)
    /// </summary>
    private static void AddScriptDefineSymbol(BuildTargetGroup buildTargetGroup, string symbol)
    {
        Debug.Log($"[AddScriptDefineSymbol] START - BuildTargetGroup: {buildTargetGroup}, Symbol to add: '{symbol}'");

        var namedBuildTarget = NamedBuildTarget.FromBuildTargetGroup(buildTargetGroup);
        var currentSymbols = PlayerSettings.GetScriptingDefineSymbols(namedBuildTarget);
        Debug.Log($"[AddScriptDefineSymbol] Current symbols: '{currentSymbols}'");

        var directives = currentSymbols
            .Split(';')
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();

        if (!directives.Contains(symbol))
        {
            Debug.Log($"[AddScriptDefineSymbol] Symbol '{symbol}' NOT FOUND - adding...");
            directives.Add(symbol);
        }
        else
        {
            Debug.Log($"[AddScriptDefineSymbol] Symbol '{symbol}' already exists");
        }

        PlayerSettings.SetScriptingDefineSymbols(namedBuildTarget, directives.ToArray());
        Debug.Log($"[AddScriptDefineSymbol] END - Symbols updated for {buildTargetGroup}");
    }

    /// <summary>
    /// Remove script defined symbol to control compilation directives by platform
    /// Uses Unity 2022+ API (NamedBuildTarget)
    /// </summary>
    private static void RemoveScriptDefineSymbol(BuildTargetGroup buildTargetGroup, string symbol)
    {
        Debug.Log($"[RemoveScriptDefineSymbol] START - BuildTargetGroup: {buildTargetGroup}, Symbol to remove: '{symbol}'");

        var namedBuildTarget = NamedBuildTarget.FromBuildTargetGroup(buildTargetGroup);
        var currentSymbols = PlayerSettings.GetScriptingDefineSymbols(namedBuildTarget);
        Debug.Log($"[RemoveScriptDefineSymbol] Current symbols: '{currentSymbols}'");

        var directives = currentSymbols
            .Split(';')
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();
        Debug.Log($"[RemoveScriptDefineSymbol] Parsed directives count: {directives.Count}");

        var removed = directives.Remove(symbol);
        Debug.Log(removed
            ? $"[RemoveScriptDefineSymbol] Symbol '{symbol}' FOUND - removed"
            : $"[RemoveScriptDefineSymbol] Symbol '{symbol}' NOT FOUND in directives");

        PlayerSettings.SetScriptingDefineSymbols(namedBuildTarget, directives.ToArray());
        Debug.Log($"[RemoveScriptDefineSymbol] END - Symbols updated for {buildTargetGroup}");
    }

    /// <summary>
    /// Build addressable assets before we can build for platform
    /// </summary>
    private static void BuildAddressable()
    {
        //Choice profile
        var addressableAssetSettings = AddressableAssetSettingsDefaultObject.Settings;
        if (addressableAssetSettings != null)
        {
            var id = addressableAssetSettings.profileSettings.GetProfileId("Default");
            if (IsRelease)
            {
                id = addressableAssetSettings.profileSettings.GetProfileId("Release");
            }
            else
            {
                id = addressableAssetSettings.profileSettings.GetProfileId("Development");
            }

            foreach (var group in addressableAssetSettings.groups)
            {
                var schema = group.GetSchema<BundledAssetGroupSchema>();
                if (schema != null)
                {
                    schema.Compression = BundledAssetGroupSchema.BundleCompressionMode.LZ4;
                }
            }

            addressableAssetSettings.activeProfileId = id;
            EditorUtility.SetDirty(AddressableAssetSettingsDefaultObject.Settings);
            Debug.Log("changing profileID to " + addressableAssetSettings.activeProfileId);
        }
        else
        {
            // Windows standalone is null
            Debug.LogWarning("Error: addressableAssetSettings is null");
        }

        Debug.Log("Addressables Build Started");
        //Build assets
        AddressableAssetSettings.BuildPlayerContent();

        Debug.Log("Addressables Build Complete");
    }

    /// <summary>
    /// Removes folders based on the Remove.data file content for the given store symbol.
    /// Format of entries in the file: store:define:path1,path2,..,pathN;
    /// </summary>
    /// <param name="storeSymbol">The target store symbol to match (e.g. STORE_GooglePlay)</param>
    public static void FoldersRemover(string storeSymbol)
    {
        var dataFilePath = Path.Combine(Application.dataPath, "submodule-core-publishing", "CI", "Remove.data");

        if (!File.Exists(dataFilePath))
        {
            Debug.LogWarning("Remove.data file not found at path: " + dataFilePath);
            return;
        }

        var fileContent = File.ReadAllText(dataFilePath);

        // Split by semicolon to get individual entries
        var entries = fileContent.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var entry in entries)
        {
            var parts = entry.Trim().Split(':');

            if (parts.Length >= 3)
            {
                var store = parts[0].Trim();
                var define = parts[1].Trim();
                var pathsString = parts[2].Trim();
                Debug.Log("Removing folders for store: " + store + " define: " + define + " paths: " + pathsString);
                // Check if the store matches the target store symbol
                if (store == storeSymbol)
                {
                    Debug.Log("Start removing folders for store");
                    // Check if the define is not in the current platform's defines
                    var currentDefines = PlayerSettings.GetScriptingDefineSymbolsForGroup(
                        EditorUserBuildSettings.selectedBuildTargetGroup);

                    var definesList = new List<string>(currentDefines.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries));

                    // If the define is not in the defines list for current platform, remove the paths
                    if (!definesList.Contains(define))
                    {
                        Debug.Log("Removing folders for store: !definesList.Contains(" + define + ")");
                        var paths = pathsString.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                        foreach (var path in paths)
                        {
                            var fullPath = Application.dataPath + path;
                            Debug.Log("Removing folders for store: check path " + fullPath);
                            if (Directory.Exists(fullPath))
                            {
                                Debug.Log($"Removing folder: {fullPath}");
                                Directory.Delete(fullPath, true); // true for recursive delete
                            }
                            else if (File.Exists(fullPath))
                            {
                                Debug.Log($"Removing file: {fullPath}");
                                File.Delete(fullPath);
                            }
                            else
                            {
                                Debug.LogWarning($"Path not found: {fullPath}");
                            }
                        }
                    }
                }
            }
            else
            {
                Debug.LogWarning($"Invalid entry format in Remove.data: {entry}");
            }
        }
    }
}
