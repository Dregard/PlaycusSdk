#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace Unity.Usercentrics
{
    internal class InspectorLinkAttribute : PropertyAttribute { }

    [CustomPropertyDrawer(typeof(InspectorLinkAttribute))]
    internal class InspectorLinkDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty prop, GUIContent label)
        {
            var linkUrl = prop.stringValue;
            EditorGUI.LabelField(position, label.text, linkUrl);

            bool linkClicked = Event.current.rawType == EventType.MouseDown &&
                Event.current.button == 0 &&
                position.Contains(Event.current.mousePosition);
            if (linkClicked)
            {
                Help.BrowseURL(linkUrl);
            }
        }
    }
}
#endif