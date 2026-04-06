using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using UnityEngine;

/// <summary>
/// Modification for Adcolony mediator of applovin requeriments
/// https://dash.applovin.com/documentation/mediation/unity/mediation-adapters?network=ADCOLONY_NETWORK
/// </summary>
public class BuildPostProcessorIOS_Adcolony
{
    [PostProcessBuildAttribute(1000)]
    public static void OnPostProcessBuild(BuildTarget target, string path)
    {
        //Only for Ios!
        if (target != BuildTarget.iOS)
            return;

        Debug.Log("BuildPostProcessorIOS_Adcolony ModifyPlist");

        // Get plist
        string plistPath = path + "/Info.plist";
        PlistDocument plist = new PlistDocument();
        plist.ReadFromString(File.ReadAllText(plistPath));

        // Add value to Xcode plist
        plist.root.values.Remove("NSAppTransportSecurity");
        PlistElementDict transportSec = plist.root.CreateDict("NSAppTransportSecurity");
        transportSec.values.Add("NSAllowsArbitraryLoads", new PlistElementBoolean(true));
        // remove
        transportSec.values.Remove("NSAllowsArbitraryLoadsInWebContent");
        //transportSec.values.Add("NSAllowsArbitraryLoadsInWebContent", new PlistElementBoolean(false));


        // Write to file
        File.WriteAllText(plistPath, plist.WriteToString());

        Debug.Log("BuildPostProcessorIOS_Adcolony NSAppTransportSecurity added to Info.plist");
    }

}