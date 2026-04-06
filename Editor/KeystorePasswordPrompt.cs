using UnityEditor;
using UnityEngine;

/// <summary>
/// Simple password input dialog for keystore configuration.
/// Used by AutoBuilder to prompt for passwords that Unity doesn't persist between sessions.
/// </summary>
public class KeystorePasswordPrompt : EditorWindow
{
    private string password = "";
    private string message = "";
    private bool confirmed;
    private bool cancelled;
    private bool focusSet;

    public static string Show(string title, string message, string defaultValue = "")
    {
        var window = CreateInstance<KeystorePasswordPrompt>();
        window.titleContent = new GUIContent(title);
        window.message = message;
        window.password = defaultValue ?? "";
        window.minSize = new Vector2(350, 120);
        window.maxSize = new Vector2(350, 120);
        window.ShowModalUtility();

        return window.cancelled ? null : window.password;
    }

    private void OnGUI()
    {
        GUILayout.Space(10);
        GUILayout.Label(message, EditorStyles.wordWrappedLabel);
        GUILayout.Space(5);

        GUI.SetNextControlName("PasswordField");
        password = EditorGUILayout.PasswordField(password);

        if (!focusSet)
        {
            EditorGUI.FocusTextInControl("PasswordField");
            focusSet = true;
        }

        GUILayout.Space(10);
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();

        if (GUILayout.Button("OK", GUILayout.Width(80)))
        {
            confirmed = true;
            Close();
        }

        if (GUILayout.Button("Cancel", GUILayout.Width(80)))
        {
            cancelled = true;
            Close();
        }

        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return)
        {
            confirmed = true;
            Close();
        }

        if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
        {
            cancelled = true;
            Close();
        }
    }
}
