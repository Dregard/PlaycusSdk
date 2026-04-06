using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using UnityEngine;

/// <summary>
/// Modification for ios Plist for skip are your app use encryption step
/// </summary>
public class BuildPostProcessorIOS_Admob
{
    /// <summary>
    /// First method calling by Unity after build process ended
    /// </summary>
    [PostProcessBuildAttribute(1)]
    public static void OnPostProcessBuild(BuildTarget target, string path)
    {
        //Only for Ios!
        if (target != BuildTarget.iOS)
            return;

        Debug.Log("BuildPostProcessorIOS_Admob.OnPostProcessBuild");

        string plistPath = path + "/Info.plist";
        PlistDocument plist = new PlistDocument();
        plist.ReadFromString(File.ReadAllText(plistPath));
        plist.root.SetString("gad_preferred_webview", "wkwebview");
        File.WriteAllText(plistPath, plist.WriteToString());
        
        Debug.Log("BuildPostProcessorIOS_Admob Modify Info.plist was completed");
    }
}