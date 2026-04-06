using System;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using UnityEngine;


/// <summary>
/// Validator step by step go by checklist of Playcus best practices and return warnings or exceptions in Unity
/// </summary>
[InitializeOnLoad]
public static class Validator
{
    /// <summary>
    /// Run critical cases on editor load or build started
    /// </summary>
    static Validator()
    {
        ValidateCriticalCases();
    }

    /// <summary>
    /// Validate can be called from Unity menu
    /// </summary>
    [MenuItem("Tools/Playcus Validator",false,128)]
    public static void Validate()
    {
        Debug.Log("Validator.Validate");
        ValidateCriticalCases();
        ValidateWarningCases();
    }

    private static void ValidateCriticalCases()
    {
        Debug.Log("Validator.ValidateCriticalCases");

        BuildTarget activeBuildTarget = EditorUserBuildSettings.activeBuildTarget;
        BuildTargetGroup activeBuildTargetGroup = BuildPipeline.GetBuildTargetGroup(activeBuildTarget);
        string message = "";

        message = "Default icon in BuildSettings must not be blank.";
        if (PlayerSettings.GetIconsForTargetGroup(BuildTargetGroup.Unknown)[0] == null)
            CaseFailed(message);
        else
            CaseValidated(message);
    }

    private static void ValidateWarningCases()
    {
        Debug.Log("Validator.ValidateWarningCases");
    }

    private static void CaseFailed(string message)
    {
        Debug.LogError($"Validator: {message} FAILED ");
    }

    private static void CaseWarning(string message)
    {
        Debug.LogWarning($"Validator: {message} WARNING");
    }

    private static void CaseValidated(string message)
    {
        Debug.Log($"Validator: {message} OK");
    }
}