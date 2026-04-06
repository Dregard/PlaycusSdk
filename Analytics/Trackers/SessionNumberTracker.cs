using UnityEngine;

namespace Playcus.Analytics
{
    /// <summary>
    /// Track session number one time events
    /// All playcus events list https://docs.google.com/spreadsheets/d/1JjSoB1pyAnKDIZ4Ir3PsFESau0e4rwD1WF_-PFT0bTg
    /// </summary>
    public class SessionNumberTracker : MonoBehaviour
    {
        // CONFIG
        [Tooltip("Track summary sessions from install tracked on every session count point from config.")]
        [SerializeField] private int[] _trackSessionsPoints = new int[] { 1, 2, 3, 4, 5 };

        //PRIVATE STATIC
        private const string PREFS_KEY_SESSIONS_COUNT = "sessionsCount";

        private void Start()
        {
            int trackedSessions = PlayerPrefs.GetInt(PREFS_KEY_SESSIONS_COUNT);
            for (int i = 0; i < _trackSessionsPoints.Length; i++)
            {
                if (_trackSessionsPoints[i] > trackedSessions)
                {
                    trackedSessions = _trackSessionsPoints[i];
                    ServiceLocator.Get<IAnalyticsManager>().CustomEvent(AnalyticsEvents.pl_session_start_.ToString() + trackedSessions, trackedSessions);
                    PlayerPrefs.SetInt(PREFS_KEY_SESSIONS_COUNT, trackedSessions);
                    break;
                }
            }
        }

    }
}