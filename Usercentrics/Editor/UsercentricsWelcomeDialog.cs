using System.Diagnostics;
using Unity.Usercentrics;
using UnityEditor;
using UnityEngine;

namespace Unity.Usercentrics
{
    public class UsercentricsWelcomeDialog : AssetPostprocessor
    {
        private static readonly string ASSETS_NAME = "Usercentrics";

        private static readonly int UNITY_MIN_MAJOR_VERSION = 2018;
        private static readonly int UNITY_MIN_MINOR_VERSION = 4;
        private static readonly string UNITY_MIN_VERSION = UNITY_MIN_MAJOR_VERSION + "." + UNITY_MIN_MINOR_VERSION;

        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            foreach (string importedAsset in importedAssets)
            {
                if (importedAsset.Equals("Assets/" + ASSETS_NAME))
                {
                    string unityVersion = Application.unityVersion;
                    if (!ValidateUnityVersion(unityVersion))
                    {
                        DisplayInvalidUnityVersionDialog(unityVersion);
                    }

                    return;
                }
            }
        }

        private static void DisplayInvalidUnityVersionDialog(string unityVersion)
        {
            string message = "The Unity version is not supported.\n - Current: " + unityVersion + "\n - Minimum: " + UNITY_MIN_VERSION;
            bool openRequirements = EditorUtility.DisplayDialog("Usercentrics - Unity", message, "Requirements", "Close");
            if (openRequirements)
            {
                Help.BrowseURL(UCConstants.REQUIREMENTS_URL);
            }
        }

        private static bool ValidateUnityVersion(string unityVersion)
        {
            string[] splitUnityVersion = unityVersion.Split('.');
            return splitUnityVersion.Length >= 2 && ValidateUnityVersion(int.Parse(splitUnityVersion[0]), int.Parse(splitUnityVersion[1]));
        }

        private static bool ValidateUnityVersion(int majorVersion, int minorVersion)
        {
            if (majorVersion > UNITY_MIN_MAJOR_VERSION)
            {
                return true;
            }
            else if (majorVersion == UNITY_MIN_MAJOR_VERSION)
            {
                return minorVersion >= UNITY_MIN_MINOR_VERSION;
            }
            else
            {
                return false;
            }
        }
    }
}
