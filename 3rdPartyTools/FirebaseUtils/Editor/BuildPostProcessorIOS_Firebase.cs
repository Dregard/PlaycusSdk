using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using UnityEngine;

/// <summary>
/// Modification for build result files. Implement automatization of any manual
/// actions with xproject files, needed by any plugins, sdk and etc.
/// </summary>
public class BuildPostProcessorIOS_Firebase
{
    /// <summary>
    /// First method calling by Unity after build process ended
    /// </summary>
    [PostProcessBuildAttribute(1)]
    public static void OnPostProcessBuild(BuildTarget target, string path)
    {
#if PL_SDK_FIREBASE_ON 
        //Only for Ios!
        if (target != BuildTarget.iOS)
            return;

        Debug.Log("BuildPostProcessorIOS_Firebase OnPostProcessBuild");

        //XCode project variables
        PBXProject project = new PBXProject();
        string projectPath = PBXProject.GetPBXProjectPath(path);
        project.ReadFromFile(projectPath);
       
        string targetName = "Unity-iPhone";
        string mainTargetGuid;
        string unityFrameworkTargetGuid;
       
        var unityMainTargetGuidMethod = project.GetType().GetMethod("GetUnityMainTargetGuid");
        var unityFrameworkTargetGuidMethod = project.GetType().GetMethod("GetUnityFrameworkTargetGuid");
               
        if (unityMainTargetGuidMethod != null && unityFrameworkTargetGuidMethod != null)
        {
            // after unity 2019.4.10
            mainTargetGuid = (string)unityMainTargetGuidMethod.Invoke(project, null);
            unityFrameworkTargetGuid = (string)unityFrameworkTargetGuidMethod.Invoke(project, null);
        }
        else
        {
            // before unity 2019.4.10
            mainTargetGuid = project.TargetGuidByName (targetName);
            unityFrameworkTargetGuid = mainTargetGuid;
        }

        // Modify Plist
        string plistPath = path + "/Info.plist";
        PlistDocument plist = new PlistDocument();
        plist.ReadFromString(File.ReadAllText(plistPath));
#if GDPR
        plist.root.SetString("FirebaseMessagingAutoInitEnabled", "NO");
        plist.root.SetString("FIREBASE_ANALYTICS_COLLECTION_ENABLED", "NO");
        plist.root.SetString("FirebaseCrashlyticsCollectionEnabled", "NO");
#endif
        File.WriteAllText(plistPath, plist.WriteToString());

        //Modify XCode project files
        ModifyProject(project, projectPath, targetName, mainTargetGuid);

        //Save XCode project
        File.WriteAllText(projectPath, project.WriteToString());

        Debug.Log("BuildPostProcessorIOS_Firebase OnPostProcessBuild completed");
#endif
    }

    private static void ModifyProject(PBXProject project, string projectPath, string targetName, string guid)
    {
        Debug.Log("BuildPostProcessor ModifyProject started");

        //Properties
        project.AddBuildProperty(guid,
            "OTHER_LDFLAGS",
            "-ObjC");
        project.AddBuildProperty(guid,
            "CLANG_ENABLE_MODULES",
            "YES");

        //Frameworks
        project.AddFrameworkToProject(guid, "UserNotifications.framework", false);

        //Entitlements
        var entitlementsFilePath = "ios.entitlements";
        var entitlements = new ProjectCapabilityManager(projectPath, entitlementsFilePath, targetName);
        entitlements.AddPushNotifications(AutoBuilder.IsDevelopment);
        entitlements.AddBackgroundModes(BackgroundModesOptions.RemoteNotifications);

        /*//Add firebase deeplinks options
        entitlements.AddAssociatedDomains(new[]
        {
            $"applinks:{ProjectName.AppName}.{ApplicationDomain.Production}",
            $"applinks:{ProjectName.AppName}.page.link"
        });
        */

        entitlements.WriteToFile();

        //Capabilities
        //project.AddCapability(guid, PBXCapabilityType.AssociatedDomains, entitlementsFilePath);//Add firebase deeplinks options
        project.AddCapability(guid, PBXCapabilityType.PushNotifications, entitlementsFilePath);
        project.AddCapability(guid, PBXCapabilityType.BackgroundModes, entitlementsFilePath);

        Debug.Log("BuildPostProcessor ModifyProject completed");
    }

}