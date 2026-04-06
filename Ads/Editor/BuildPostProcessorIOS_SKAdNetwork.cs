using System;
using System.Collections;
using System.IO;
using Playcus.Ads;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Modification plist for ios 14 requirements.
/// Will be add all ads network partners in plist from this google sheet:
/// https://docs.google.com/spreadsheets/d/1rHrx2ZgHQdQiXH4lVAZreaXczF-HlK1VFiJlb31SNIA/edit#gid=660081661
/// </summary>
public class BuildPostProcessorIOS_SKAdNetwork : IPostprocessBuildWithReport
{
    public int callbackOrder => int.MaxValue;
    
    public void OnPostprocessBuild(BuildReport report)
    {
        BuildTarget target = report.summary.platform;
        string path = report.summary.outputPath;
        
        //Only for Ios!
        if (target != BuildTarget.iOS)
            return;

#if PL_SDK_ADMOB_ON
        // TODO 
        /*
        Debug.Log("BuildPostProcessorIOS_SKAdNetwork");
        
        // Get plist
        string plistPath = path + "/Info.plist";
        PlistDocument plist = new PlistDocument();
        plist.ReadFromString(File.ReadAllText(plistPath));

        // Add value to Xcode plist
        PlistElementArray arrayPlist = plist.root.CreateArray("SKAdNetworkItems");
        

        // Write to file
        File.WriteAllText(plistPath, plist.WriteToString());
        */
#endif
    }
}
