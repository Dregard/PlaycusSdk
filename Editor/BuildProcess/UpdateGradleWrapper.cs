#if UNITY_ANDROID
using System.IO;
using UnityEditor;
using UnityEditor.Android;
using UnityEngine;

/// <summary>
/// Updates Gradle wrapper version and Android Gradle Plugin version to fix compatibility
/// This callback is invoked after Unity generates Gradle project but before Gradle build starts
/// </summary>
public class UpdateGradleWrapper : IPostGenerateGradleAndroidProject
{
    public int callbackOrder => 100; // Run after other callbacks (like AddManifestFirebaseKeys which has order 0)

    public void OnPostGenerateGradleAndroidProject(string path)
    {
        try
        {
            // Update Gradle wrapper version to 8.0+ (if possible)
            AutoBuilder.UpdateGradleWrapperVersionInternal(path);
            
            // Configure Gradle to use JDK 17 (required for Android Gradle Plugin 8.0+)
            // Unity sets JAVA_HOME to JDK 11 (from Unity Preferences), but Gradle needs JDK 17
            AutoBuilder.ConfigureGradleJavaHome(path);
            
            // Stop Gradle daemon to ensure it uses new JDK settings
            // This is important because daemon might be running with old JDK 11
            AutoBuilder.StopGradleDaemon(path);
            
            // Add missing AndroidX dependencies (required for Firebase and other libraries)
            AutoBuilder.AddAndroidXDependencies(path);
            
            // Add AppLovin mediation SDK dependencies (required for SafeDK to find MaxAdView)
            AutoBuilder.AddAppLovinDependencies(path);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[UpdateGradleWrapper] Failed to update Gradle configuration: {ex.Message}");
            Debug.LogException(ex);
        }
    }
}
#endif
