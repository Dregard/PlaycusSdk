#if UNITY_IOS
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.iOS.Xcode;
using System.IO;
using UnityEngine;

public class AddPlistFirebaseKeys : IPostprocessBuildWithReport
{
    public int callbackOrder => 0;
    public void OnPostprocessBuild(BuildReport report)
    {
        if (report.summary.platform != BuildTarget.iOS)
            return;
        
        Debug.Log("AddPlistFirebaseKeys.OnPostProcessBuild called.");
        string plistPath = Path.Combine(report.summary.outputPath, "Info.plist");

        PlistDocument plist = new PlistDocument();
        plist.ReadFromFile(plistPath);

        PlistElementDict rootDict = plist.root;

        AddBoolKeyIfMissing(rootDict, "FirebaseMessagingAutoInitEnabled", false);
        AddBoolKeyIfMissing(rootDict, "FIREBASE_ANALYTICS_COLLECTION_ENABLED", false);
        AddBoolKeyIfMissing(rootDict, "FirebaseCrashlyticsCollectionEnabled", false);

        plist.WriteToFile(plistPath);

        UnityEngine.Debug.Log("[FirebasePlist] Firebase keys added to Info.plist");
    }

    private void AddBoolKeyIfMissing(PlistElementDict dict, string key, bool value)
    {
        if (!dict.values.ContainsKey(key))
        {
            dict.SetBoolean(key, value);
            UnityEngine.Debug.Log($"[FirebasePlist] Added {key} = {value}");
        }
        else
        {
            UnityEngine.Debug.Log($"[FirebasePlist] {key} already exists. Skipping.");
        }
    }
}
#endif