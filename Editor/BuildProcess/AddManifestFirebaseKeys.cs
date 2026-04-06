#if UNITY_ANDROID
using System.IO;
using System.Xml;
using UnityEditor;
using UnityEditor.Android;
using UnityEngine;

public class AddManifestFirebaseKeys : IPostGenerateGradleAndroidProject
{
    public int callbackOrder => 0;

    public void OnPostGenerateGradleAndroidProject(string path)
    {
        Debug.Log("AddManifestFirebaseKeys.OnPostGenerateGradleAndroidProject called.");
        string manifestPath = Path.Combine(path, "src", "main", "AndroidManifest.xml");

        if (!File.Exists(manifestPath))
        {
            Debug.LogError($"[FirebaseMetaData] AndroidManifest.xml not found at {manifestPath}");
            return;
        }

        var doc = new XmlDocument();
        doc.Load(manifestPath);

        var nsManager = new XmlNamespaceManager(doc.NameTable);
        nsManager.AddNamespace("android", "http://schemas.android.com/apk/res/android");

        XmlNode applicationNode = doc.SelectSingleNode("/manifest/application");
        if (applicationNode == null)
        {
            Debug.LogError("[FirebaseMetaData] <application> tag not found in manifest.");
            return;
        }

        AddMetaDataIfMissing(doc, applicationNode, "firebase_messaging_auto_init_enabled", "false");
        AddMetaDataIfMissing(doc, applicationNode, "firebase_analytics_collection_enabled", "false");
        AddMetaDataIfMissing(doc, applicationNode, "firebase_crashlytics_collection_enabled", "false");

        doc.Save(manifestPath);

        Debug.Log("[FirebaseMetaData] Meta-data entries ensured in AndroidManifest.xml");
    }

    private void AddMetaDataIfMissing(XmlDocument doc, XmlNode applicationNode, string name, string value)
    {
        XmlNamespaceManager nsManager = new XmlNamespaceManager(doc.NameTable);
        nsManager.AddNamespace("android", "http://schemas.android.com/apk/res/android");

        XmlNode existing = applicationNode.SelectSingleNode($"meta-data[@android:name='{name}']", nsManager);
        if (existing != null)
        {
            Debug.Log($"[FirebaseMetaData] Meta-data '{name}' already exists. Skipping.");
            return;
        }

        XmlElement metaData = doc.CreateElement("meta-data");
        metaData.SetAttribute("name", "http://schemas.android.com/apk/res/android", name);
        metaData.SetAttribute("value", "http://schemas.android.com/apk/res/android", value);
        applicationNode.AppendChild(metaData);

        Debug.Log($"[FirebaseMetaData] Added meta-data: {name} = {value}");
    }
}
#endif