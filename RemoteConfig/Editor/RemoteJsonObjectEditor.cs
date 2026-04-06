using UnityEngine;
using System.Collections;
using UnityEditor;

namespace Playcus.RemoteConfig
{
    [CustomEditor(typeof(RemoteJsonObject))]
    public class RemoteJsonObjectEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            RemoteJsonObject myScript = (RemoteJsonObject)target;
            if (GUILayout.Button("Parse target as JSON to console"))
            {
                myScript.ParseObjectToJsonInLog();
            }
        }
    }
}