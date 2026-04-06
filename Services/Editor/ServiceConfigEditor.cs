using UnityEngine;
using UnityEditor;

namespace Playcus
{
    [CustomEditor(typeof(ServiceConfig), true)]
    public class ServiceConfigEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            ServiceConfig myScript = (ServiceConfig)target;
            if (GUILayout.Button("Parse config as JSON to console and clipboard"))
            {
                myScript.ParseObjectToJsonInLog();
                
                EditorGUIUtility.systemCopyBuffer = JsonUtility.ToJson(myScript,true);
            }

            if (GUILayout.Button("Save Changes"))
            {
                EditorUtility.SetDirty(myScript);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }
    }
}