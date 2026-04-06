using UnityEditor;
using UnityEngine;

namespace Unity.Usercentrics
{

    [CustomPropertyDrawer(typeof(ATTLocalizationMessage))]
    public class ATTLocalizationMessageDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            Rect languageRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            Rect messageRect = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing, position.width, EditorGUIUtility.singleLineHeight);

            SerializedProperty languageProperty = property.FindPropertyRelative("Language");
            SerializedProperty messageProperty = property.FindPropertyRelative("PopupMessage");
            SerializedProperty manualIsoCodeProperty = property.FindPropertyRelative("ManualIsoCode");

            EditorGUI.PropertyField(languageRect, languageProperty, GUIContent.none);

            Language selectedLanguage = (Language) languageProperty.enumValueIndex;

            if (isOtherLanguageSelected(selectedLanguage))
            {
                Rect manualIsoCodeRect = new Rect(position.x, position.y + (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing) * 2, position.width, EditorGUIUtility.singleLineHeight);
                EditorGUI.PropertyField(manualIsoCodeRect, manualIsoCodeProperty, new GUIContent("Manual ISO Code"));
            }

            EditorGUI.PropertyField(messageRect, messageProperty);

            EditorGUI.EndProperty();

            Rect removeButtonRect = new Rect(position.x, position.y + (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing) * 3, position.width, EditorGUIUtility.singleLineHeight);
            if (GUI.Button(removeButtonRect, "Remove"))
            {
                RemoveLocalizationMessage(property);
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            int numRectangles = 3; // Language, ManualIsoCode and Remove buttons

            SerializedProperty languageProperty = property.FindPropertyRelative("Language");
            Language selectedLanguage = (Language)languageProperty.enumValueIndex;

            if (isOtherLanguageSelected(selectedLanguage))
            {
                numRectangles++;
            }

            float lineHeight = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            return (numRectangles + 1) * lineHeight;
        }

        private bool isOtherLanguageSelected(Language selectedLanguage)
        {
            return selectedLanguage == Language.Other;
        }

        private void RemoveLocalizationMessage(SerializedProperty property)
        {
            SerializedObject serializedObject = property.serializedObject;
            SerializedProperty localizationMessagesProperty = serializedObject.FindProperty("LocalizationMessages");

            int arrayIndex = GetArrayIndexFromPropertyPath(property);
            if (arrayIndex >= 0 && arrayIndex < localizationMessagesProperty.arraySize)
            {
                localizationMessagesProperty.DeleteArrayElementAtIndex(arrayIndex);
                serializedObject.ApplyModifiedProperties();
            }
        }

        private int GetArrayIndexFromPropertyPath(SerializedProperty property)
        {
            string propertyPath = property.propertyPath;
            int startIndex = propertyPath.IndexOf('[') + 1;
            int endIndex = propertyPath.IndexOf(']');
            string indexStr = propertyPath.Substring(startIndex, endIndex - startIndex);

            int index;
            if (int.TryParse(indexStr, out index))
            {
                return index;
            }
            return -1;
        }
    }

}
