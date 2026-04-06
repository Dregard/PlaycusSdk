using UnityEngine;
using UnityEditor;

namespace Playcus
{
    [CustomEditor(typeof(ServiceWithConfig), true)]
    public class ServiceWithConfigEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
    
            ServiceWithConfig myScript = (ServiceWithConfig)target;
            if (GUILayout.Button("Select config file"))
            {
                myScript.SelectConfigAsset();
            }
        }
    }
}