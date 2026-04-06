using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Playcus
{
    /// <summary>
    /// Store platform identifiers for build configuration
    /// </summary>
    public enum STORE
    {
        GooglePlay,
        Amazon,
        Appstore,
        Facebook,
        WindowsStore,
        MacStore
    }

    /// <summary>
    /// Utility class for getting current store from scripting define symbols
    /// </summary>
    public static class StoreConstants
    {
        /// <summary>
        /// Get current store based on scripting define symbols (STORE_GooglePlay, STORE_Amazon, etc.)
        /// </summary>
        public static STORE GetCurrentStore()
        {
#if UNITY_EDITOR
            // In Editor, check scripting define symbols
            var buildTarget = EditorUserBuildSettings.activeBuildTarget;
            var buildTargetGroup = BuildPipeline.GetBuildTargetGroup(buildTarget);
            var namedBuildTarget = UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(buildTargetGroup);
            var defines = PlayerSettings.GetScriptingDefineSymbols(namedBuildTarget);
#else
            // In runtime, check preprocessor directives
            var defines = "";
#if STORE_GOOGLEPLAY
            defines += "STORE_GooglePlay;";
#endif
#if STORE_AMAZON
            defines += "STORE_Amazon;";
#endif
#if STORE_APPSTORE
            defines += "STORE_Appstore;";
#endif
#if STORE_FACEBOOK
            defines += "STORE_Facebook;";
#endif
#if STORE_WINDOWSSTORE
            defines += "STORE_WindowsStore;";
#endif
#if STORE_MACSTORE
            defines += "STORE_MacStore;";
#endif
#endif

            // Check for each store define
            if (defines.Contains("STORE_GooglePlay"))
                return STORE.GooglePlay;
            if (defines.Contains("STORE_Amazon"))
                return STORE.Amazon;
            if (defines.Contains("STORE_Appstore"))
                return STORE.Appstore;
            if (defines.Contains("STORE_Facebook"))
                return STORE.Facebook;
            if (defines.Contains("STORE_WindowsStore"))
                return STORE.WindowsStore;
            if (defines.Contains("STORE_MacStore"))
                return STORE.MacStore;

            // Default fallback
            return STORE.GooglePlay;
        }
    }
}
