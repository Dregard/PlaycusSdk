using System;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Build versions management with support of external version setters from system variables
/// </summary>
public static class VersioningHelperUtility
{
    public static string BuildNumber => _buildNumber;
    
    // Contain actual global version value
    private static string _versionNumber;
    private static string _buildNumber;
    
    /// <summary>
    /// Get actual global version value
    /// </summary>
    public static string VersionNumber
    {
        get
        {
            if (string.IsNullOrEmpty(_versionNumber))
            {
                _buildNumber = Environment.GetEnvironmentVariable("BUILD_NUMBER");

                if (!string.IsNullOrEmpty(_buildNumber))
                {
                    var number = Convert.ToInt32(_buildNumber);
                    var major = number / 1000000;
                    var minor = (number - major * 1000000) / 1000;
                    var build = number - major * 1000000 - minor * 1000;

                    _versionNumber = new Version(major, minor, build).ToString();
                }
                else
                {
                    _versionNumber = PlayerSettings.bundleVersion;
                }
            }
            return _versionNumber;
        }
    }

    /// <summary>
    /// Refresh all version values
    /// </summary>
    private static bool IsCI => !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("BUILD_NUMBER"));

    [MenuItem("Build/Versioning/RefreshVersion")]
    public static void RefreshVersion()
    {
        var versionString = VersionNumber;
        if (Version.TryParse(versionString, out var version))
        {
            int build = Math.Max(0, version.Build);
            if (!IsCI) build++;
            SetVersion(new Version(version.Major, version.Minor, build));
        }
        else
        {
            var parts = versionString.Split('.');
            int major = 0, minor = 0;
            if (parts.Length >= 1) int.TryParse(new string(Array.FindAll(parts[0].ToCharArray(), char.IsDigit)), out major);
            if (parts.Length >= 2) int.TryParse(new string(Array.FindAll(parts[1].ToCharArray(), char.IsDigit)), out minor);
            int build = IsCI ? 0 : 1;
            Debug.LogWarning($"VersioningHelper: Version '{versionString}' has non-standard format, using {major}.{minor}.{build}");
            SetVersion(new Version(major, minor, build));
        }
    }

    /// <summary>
    /// Set version values
    /// </summary>
    private static void SetVersion(Version version)
    {
        PlayerSettings.bundleVersion = version.ToString(); //this is string version code like 1.0.0

        var buildNumber = version.Major * 1000000 + version.Minor * 1000 + version.Build;

        // Update cached values so subsequent reads return the correct version
        _versionNumber = version.ToString();
        _buildNumber = buildNumber.ToString();

        Debug.LogWarning($"BUILD_NUMBER: {buildNumber}");
        Debug.LogWarning($"VERSION: {PlayerSettings.bundleVersion}");

        PlayerSettings.Android.bundleVersionCode = buildNumber;//TruncateBundleVersion(buildNumber); //this is int bundle version (try w/o truncate for now)
        PlayerSettings.iOS.buildNumber = buildNumber.ToString();
        PlayerSettings.macOS.buildNumber = buildNumber.ToString();
    }

    /// <summary>
    /// Truncate bundle version to buypass GP limit
    /// </summary>
    private static int TruncateBundleVersion(int buildNumber)
    {
        //Keep this number under 100000 if Split APKs by target architecture is enabled. Each APK must have a unique version code so Unity adds 100000 to the number for ARMv7, and 200000 for ARM64.
        var maxDigit = 5; //reserve 1 digit for future
        string buildString = buildNumber.ToString();
        if (buildString.Length <= maxDigit)
        {
            return buildNumber;
        }

        string truncatedString = buildString.Substring(0, maxDigit);
        return int.Parse(truncatedString);
    }

    /// <summary>
    /// Utility function for NOT ci builds
    /// </summary>
    [MenuItem("Build/Versioning/Increase Major Version")]
    public static void IncreaseMajor()
    {
        var version = new Version(VersionNumber);
        var newVersion = new Version(version.Major + 1, 0, 0);
        SetVersion(newVersion);
    }

    /// <summary>
    /// Utility function for NOT ci builds
    /// </summary>
    [MenuItem("Build/Versioning/Increase Minor Version")]
    public static void IncreaseMinor()
    {
        var version = new Version(VersionNumber);
        if (version.Minor < 999)
        {
            SetVersion(new Version(version.Major, version.Minor + 1, 0));
        }
        else
        {
            IncreaseMajor();
        }
    }

    /// <summary>
    /// Utility function for NOT ci builds
    /// </summary>
    [MenuItem("Build/Versioning/Increase Build Version")]
    public static void IncreaseBuild()
    {
        var version = new Version(VersionNumber);
        if (version.Build < 999)
        {
            SetVersion(new Version(version.Major, version.Minor, version.Build + 1));
        }
        else
        {
            IncreaseMinor();
        }
    }
}