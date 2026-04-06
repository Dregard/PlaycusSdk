using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Playcus.Analytics
{
    /// <summary>
    /// Track every unity scene changes
    /// By all events list https://docs.google.com/spreadsheets/d/1JjSoB1pyAnKDIZ4Ir3PsFESau0e4rwD1WF_-PFT0bTg
    /// </summary>
    public class SceneTracker : MonoBehaviour
    {
        private void Start()
        {
            SceneManager.activeSceneChanged += OnSceneChanged;
        }

        public void OnSceneChanged(Scene oldScene, Scene newScene)
        {
            ServiceLocator.Get<IAnalyticsManager>().CustomEvent(AnalyticsEvents.pl_scene_changed.ToString(), 0,
                new Dictionary<string, object>()
                {
                    { AnalyticsProperties.pr_content_id.ToString(), SceneManager.GetActiveScene().name }
                }
            );
        }

    }
}