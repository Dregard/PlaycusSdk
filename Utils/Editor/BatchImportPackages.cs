using UnityEngine;
using UnityEditor;
using System.IO;

public class BatchImportPackages : EditorWindow
{
    // Add a new menu item under "Tools" to open the Batch Import Packages window
    [MenuItem("Tools/Batch Import Packages")]
    static void ShowWindow()
    {
        // Create and display the custom editor window
        EditorWindow.GetWindow(typeof(BatchImportPackages), false, "Batch Import Packages");
    }

    private string folderPath = "";

    void OnGUI()
    {
        // Display a bold label prompting the user to select a folder
        GUILayout.Label("Select a folder with packages to import", EditorStyles.boldLabel);

        // Button to open the folder panel for selecting the package folder
        if (GUILayout.Button("Select Folder"))
        {
            folderPath = EditorUtility.OpenFolderPanel("Select Package Folder", "", "");
        }

        // If a folder has been selected, display its path and the import button
        if (!string.IsNullOrEmpty(folderPath))
        {
            GUILayout.Label("Folder Path: " + folderPath);

            // Button to start importing all packages in the selected folder
            if (GUILayout.Button("Import All Packages"))
            {
                ImportAllPackages(folderPath);
            }
        }
    }

    void ImportAllPackages(string path)
    {
        // Get all .unitypackage files in the selected folder (non-recursive)
        string[] packageFiles = Directory.GetFiles(path, "*.unitypackage", SearchOption.TopDirectoryOnly);

        // If no packages are found, display an alert and exit the method
        if (packageFiles.Length == 0)
        {
            EditorUtility.DisplayDialog("No Packages Found", "There are no .unitypackage files in the selected folder.", "OK");
            return;
        }

        // Loop through each package file and import it
        for (int i = 0; i < packageFiles.Length; i++)
        {
            string packageFile = packageFiles[i];
            // Display a progress bar indicating the import status
            EditorUtility.DisplayProgressBar("Importing Packages", $"Importing package {Path.GetFileName(packageFile)} ({i + 1}/{packageFiles.Length})", (float)i / packageFiles.Length);
            // Import the package without showing the import dialog
            AssetDatabase.ImportPackage(packageFile, false);
        }

        // Clear the progress bar and display a completion message
        EditorUtility.ClearProgressBar();
        EditorUtility.DisplayDialog("Import Complete", "All packages have been successfully imported.", "OK");
    }
}
