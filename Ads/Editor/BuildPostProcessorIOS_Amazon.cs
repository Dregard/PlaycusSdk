using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using UnityEngine;

/// <summary>
/// Modification for Adcolony mediator of applovin requirements
/// https://dash.applovin.com/documentation/mediation/unity/mediation-adapters?network=ADCOLONY_NETWORK
/// </summary>
public class BuildPostProcessorIOS_Amazon
{
    [PostProcessBuildAttribute(1000)]
    public static void OnPostProcessBuild(BuildTarget target, string path)
    {
        //Only for Ios!
        if (target != BuildTarget.iOS)
            return;

        Debug.Log("IosPlistUsageDescription OnPostProcessBuild");

        // Get plist
        string plistPath = path + "/Info.plist";
        PlistDocument plist = new PlistDocument();
        plist.ReadFromString(File.ReadAllText(plistPath));

        // Add value to Xcode plist
        plist.root.SetString("NSCalendarsUsageDescription", "Used to deliver better advertising experience.");

        // Write to file
        File.WriteAllText(plistPath, plist.WriteToString());
        Debug.Log("IosPlistUsageDescription OnPostProcessBuild completed");
    }

}