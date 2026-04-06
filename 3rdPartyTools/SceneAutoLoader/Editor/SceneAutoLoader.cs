/*

SceneAutoLoader
Author: User:Yoyo

Contents [hide]
1 Description
2 Usage
3 How It Works
4 Revisions
5 C# - SceneAutoLoader.cs
Description
This editor script lets you work in one scene but load another one when you press play in the Unity Editor. This functionality is useful when you are working on a scene for a level or section of the world, but you need to load a different "master scene" in order to play your game. By automatically switching to and from the master scene when you press play in the editor, the scene auto-loader speeds up your workflow.

This script is a complete reimplementation of an idea discussed on this forum thread.


Usage
Save the script below as Assets/Editor/SceneAutoLoader.cs. The script will be automatically activated when you open your project, creating a "Scene Autoload" sub-menu on the Unity Editor File menu.

The menu options are:

Select Master Scene... - choose the scene that should be loaded when you press play
Load Master On Play - select this to auto-load your master scene (if greyed out then this is the active option)
Don't Load Master On Play - select this to disable auto-load of your master scene (if greyed out then this is the active option)

This script is tested, but as with all community scripts, usage is at your own risk.


How It Works
The script uses InitializeOnLoad to auto-run its static constructor, which attaches a callback to EditorApplication.playmodeStateChanged. This callback will load the designated master scene when the user presses play, and reload the previous scene when they press stop.

Menu items are created to select the master scene, using EditorUtility.OpenFilePanel, and to enabled and disable auto-loading of the master scene.

Before auto-loading the master scene, EditorApplication.SaveCurrentSceneIfUserWantsTo is used to make sure edits in the sub-scene aren't thrown away without the user knowing. If the user chooses to cancel the save scene operation then the editor play operation is also cancelled.

EditorPrefs are used to remember the user's auto-loading preferences.

This forum thread discusses changes for the latest Unity API.

Revisions
March 7, 2013: Initial revision.
October 15, 2017: Updated to latest API.
April 14, 2018: Tiny API tweak.
C# - SceneAutoLoader.cs*/
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

/// <summary>
/// Scene auto loader.
/// </summary>
/// <description>
/// This class adds a File > Scene Autoload menu containing options to select
/// a "master scene" enable it to be auto-loaded when the user presses play
/// in the editor. When enabled, the selected scene will be loaded on play,
/// then the original scene will be reloaded on stop.
///
/// Based on an idea on this thread:
/// http://forum.unity3d.com/threads/157502-Executing-first-scene-in-build-settings-when-pressing-play-button-in-editor
/// </description>
[InitializeOnLoad]
static class SceneAutoLoader
{
	// Static constructor binds a playmode-changed callback.
	// [InitializeOnLoad] above makes sure this gets executed.
	static SceneAutoLoader()
	{
		EditorApplication.playModeStateChanged += OnPlayModeChanged;
	}

	// Menu items to select the "master" scene and control whether or not to load it.
	[MenuItem("Tools/Scene Autoload/Select Master Scene...")]
	private static void SelectMasterScene()
	{
		string masterScene = EditorUtility.OpenFilePanel("Select Master Scene", Application.dataPath, "unity");
		masterScene = masterScene.Replace(Application.dataPath, "Assets");	//project relative instead of absolute path
		if (!string.IsNullOrEmpty(masterScene))
		{
			MasterScene = masterScene;
			LoadMasterOnPlay = true;
		}
	}

	[MenuItem("Tools/Scene Autoload/Load Master On Play", true)]
	private static bool ShowLoadMasterOnPlay()
	{
		return !LoadMasterOnPlay;
	}
	[MenuItem("Tools/Scene Autoload/Load Master On Play")]
	private static void EnableLoadMasterOnPlay()
	{
		LoadMasterOnPlay = true;
	}

	[MenuItem("Tools/Scene Autoload/Don't Load Master On Play", true)]
	private static bool ShowDontLoadMasterOnPlay()
	{
		return LoadMasterOnPlay;
	}
	[MenuItem("Tools/Scene Autoload/Don't Load Master On Play")]
	private static void DisableLoadMasterOnPlay()
	{
		LoadMasterOnPlay = false;
	}

	// Play mode change callback handles the scene load/reload.
	private static void OnPlayModeChanged(PlayModeStateChange state)
	{
		if (!LoadMasterOnPlay)
		{
			return;
		}

		if (!EditorApplication.isPlaying && EditorApplication.isPlayingOrWillChangePlaymode)
		{
			// User pressed play -- autoload master scene.
			PreviousScene = SceneManager.GetActiveScene().path;
			if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
			{
				try
				{
					EditorSceneManager.OpenScene(MasterScene);
				}
				catch
				{
					Debug.LogError(string.Format("error: scene not found: {0}", MasterScene));
					EditorApplication.isPlaying = false;

				}
			}
			else
			{
				// User cancelled the save operation -- cancel play as well.
				EditorApplication.isPlaying = false;
			}
		}

		// isPlaying check required because cannot OpenScene while playing
		if (!EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode)
		{
			// User pressed stop -- reload previous scene.
			try
			{
				EditorSceneManager.OpenScene(PreviousScene);
			}
			catch
			{
				Debug.LogError(string.Format("error: scene not found: {0}", PreviousScene));
			}
		}
	}

	// Properties are remembered as editor preferences.
	private const string cEditorPrefLoadMasterOnPlay = "SceneAutoLoader.LoadMasterOnPlay";
	private const string cEditorPrefMasterScene = "SceneAutoLoader.MasterScene";
	private const string cEditorPrefPreviousScene = "SceneAutoLoader.PreviousScene";

	private static bool LoadMasterOnPlay
	{
		get { return EditorPrefs.GetBool(cEditorPrefLoadMasterOnPlay, false); }
		set { EditorPrefs.SetBool(cEditorPrefLoadMasterOnPlay, value); }
	}

	private static string MasterScene
	{
		get { return EditorPrefs.GetString(cEditorPrefMasterScene, "Master.unity"); }
		set { EditorPrefs.SetString(cEditorPrefMasterScene, value); }
	}

	private static string PreviousScene
	{
		get { return EditorPrefs.GetString(cEditorPrefPreviousScene, EditorSceneManager.GetActiveScene().path); }
		set { EditorPrefs.SetString(cEditorPrefPreviousScene, value); }
	}
}
