using System;
using Cysharp.Threading.Tasks;
using Mistplay;
using Playcus.Saves;
using UnityEngine;

namespace Playcus.Analytics
{
    /// <summary>
    /// Track summary lifetime from install
    /// All playcus events list https://docs.google.com/spreadsheets/d/1JjSoB1pyAnKDIZ4Ir3PsFESau0e4rwD1WF_-PFT0bTg
    /// </summary>
    public class AnalyticsTimerTracker : MonoBehaviour
    {
        // CONFIG
        [Tooltip("Track summary lifetime from install on every minute count point from config.")]
        [SerializeField] private int[] _trackMinutePoints = new int[] { 1, 3, 5, 10, 15, 20, 30, 40, 60, 120, 180 };
        [SerializeField] private int[] _trackEveryMinutePoints = new int[] { 1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31,32,33,34,35,36,37,38,39,40,41,42,43,44,45,46,47,48,49,50 };
        [SerializeField] private bool _trackMistplay = false;

        // STATIC
        private const string PREFS_KEY_LAUNCHTIME = "secondsFromFirstLaunch";
        private const string PREFS_KEY_TRACKEDMINUTES = "trackedMinutes";
        private const string PREFS_KEY_TRACKED_EVERY_MINUTES = "trackedEveryMinutes";
        private const float TRACK_TIME = 1.0F;
        
        // PRIVATE
        private float _trackTimer;
        private float _timeFromFirstLaunch;
        private int _trackedMinutes;
        private int _trackedEveryMinutes;
        private bool _timerEnabled = false;

        private readonly TimerTrackerSaveVO _saveVO = new TimerTrackerSaveVO();
        private ISaveService _saveService;
        private const string SAVE_KEY = "TimerTrackerSave";
        
        private void Start()
        {
            LoadAsync();
        }
        
        private async UniTask LoadAsync()
        {
            do
            {
                _saveService = ServiceLocator.Get<ISaveService>();
                await UniTask.Yield();

            } while (!(_saveService != null && _saveService.State == ServiceState.Ready));
            
            await _saveService.RegisterAndLoadAsync(SAVE_KEY, _saveVO);
 
            if (PlayerPrefs.HasKey(PREFS_KEY_TRACKEDMINUTES))
            {
                _saveVO.TrackedMinutes = PlayerPrefs.GetInt(PREFS_KEY_TRACKEDMINUTES,0);
                PlayerPrefs.DeleteKey(PREFS_KEY_TRACKEDMINUTES);
            }
            
            if (PlayerPrefs.HasKey(PREFS_KEY_TRACKED_EVERY_MINUTES))
            {
                _saveVO.EveryMinutes = PlayerPrefs.GetInt(PREFS_KEY_TRACKED_EVERY_MINUTES,0);
                PlayerPrefs.DeleteKey(PREFS_KEY_TRACKED_EVERY_MINUTES);
            }
            
            if (PlayerPrefs.HasKey(PREFS_KEY_LAUNCHTIME))
            {
                _saveVO.SecondsFromFirstLaunch = PlayerPrefs.GetInt(PREFS_KEY_LAUNCHTIME,0);
                PlayerPrefs.DeleteKey(PREFS_KEY_LAUNCHTIME);
            }
           
            
            _trackedMinutes = _saveVO.TrackedMinutes;
            _trackedEveryMinutes = _saveVO.EveryMinutes;
            _timerEnabled = false;

            for (int i = 0; i < _trackMinutePoints.Length; i++)
            {
                if (_trackMinutePoints[i] > _trackedMinutes)
                {
                    _timerEnabled = true;
                    break;
                }
            }

            if (_timerEnabled)
                _timeFromFirstLaunch = _saveVO.SecondsFromFirstLaunch;
        }

        private void Update()
        {
            if (_timerEnabled)
            {
                _trackTimer += Time.unscaledDeltaTime;
                _timeFromFirstLaunch += Time.unscaledDeltaTime;
                if (_trackTimer > TRACK_TIME)
                {
                    _trackTimer = 0;
                    CheckPoint();
                }
            }
        }

        private void CheckPoint()
        {
            var minutes = Mathf.CeilToInt(_timeFromFirstLaunch / 60);

            for (int i = 0; i < _trackMinutePoints.Length; i++)
            {
                if (_trackMinutePoints[i] > _trackedMinutes &&
                    minutes > _trackMinutePoints[i])
                {
                    TrackNewPoint(_trackMinutePoints[i]);
                }
            }
            
            for (int i = 0; i < _trackEveryMinutePoints.Length; i++)
            {
                if (_trackEveryMinutePoints[i] > _trackedEveryMinutes &&
                    minutes > _trackEveryMinutePoints[i])
                {
                    TrackNewPointMinutely(_trackEveryMinutePoints[i]);
                }
            }
            
            //Mistplay must be tracked every 5 minutes
            if (_trackMistplay && ((minutes % 5) == 0))
            {
                MistplayTimeTrackingAppsFlyer.SendEvent();
            }
        }

        private void TrackNewPoint(int _minutes)
        {
            ServiceLocator.Get<IAnalyticsManager>().CustomEvent(AnalyticsEvents.pl_lifetime_.ToString() + _minutes, _minutes);
            _trackedMinutes = _minutes;
            SaveValues();
        }
        
        private void TrackNewPointMinutely(int _minutes)
        {
            ServiceLocator.Get<IAnalyticsManager>().CustomEvent(AnalyticsEvents.pl_lifetime.ToString(), _minutes);
            _trackedEveryMinutes = _minutes;
            SaveValues();
        }

        private void SaveValues()
        {
            /*PlayerPrefs.SetInt(PREFS_KEY_TRACKEDMINUTES, _trackedMinutes);
            PlayerPrefs.SetInt(PREFS_KEY_TRACKED_EVERY_MINUTES, _trackedEveryMinutes);
            PlayerPrefs.SetFloat(PREFS_KEY_LAUNCHTIME, _timeFromFirstLaunch);*/

            _saveVO.TrackedMinutes = _trackedMinutes;
            _saveVO.EveryMinutes = _trackedEveryMinutes;
            _saveVO.SecondsFromFirstLaunch = _timeFromFirstLaunch;
            _saveService.Save();
        }

        private void OnApplicationPause(bool pause)
        {
            if (pause && _timerEnabled)
                SaveValues();
        }
    }

    [Serializable]
    public class TimerTrackerSaveVO
    {
        public int TrackedMinutes;
        public int EveryMinutes;
        public float SecondsFromFirstLaunch;
    }
}