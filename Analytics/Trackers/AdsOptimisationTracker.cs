using System;
using System.Collections.Generic;
using Playcus.Ads;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Playcus.Analytics
{
    /// <summary>
    /// Track moment when user watch target count of revarded ads.
    /// Use predefined SDK event Achievement for Marketing optimisation.
    /// All events list https://docs.google.com/spreadsheets/d/1JjSoB1pyAnKDIZ4Ir3PsFESau0e4rwD1WF_-PFT0bTg
    /// </summary>
    public class AdsOptimisationTracker : MonoBehaviour
    {
        // CONFIG
        [HelpBox(@"Track moment when user watch target count of revarded ads. Use predefined SDK event Achievement for Marketing optimisation.", HelpBoxMessageType.Info)]
        [Tooltip("Count of reward ads watching before event will be tracked.")]
        [SerializeField] private int _adsBeforeTracked = 4;

        // PRIVATE
        private int _adsBeforeTrackedCounter = 0;

        // STATIC
        private const string KEY = "AdsOptimisationTracker";


        private void Start()
        {
            if (PlayerPrefs.HasKey(KEY))
                _adsBeforeTrackedCounter = PlayerPrefs.GetInt(KEY);

            Init();
        }

        private void Init()
        {
            IAdsManager adsManager = ServiceLocator.Get<IAdsManager>(true);
            if (adsManager != null)
            {
                adsManager.RewardCompleted += OnRewardCompleted;
            }
            else
            {
                Invoke("Init", 3f);
            }
        }

        private void OnRewardCompleted(PLACE arg1, string arg2)
        {
            _adsBeforeTrackedCounter++;
            PlayerPrefs.SetInt(KEY, _adsBeforeTracked);
            // Track only one time
            if (_adsBeforeTrackedCounter == _adsBeforeTracked)
            {
                ServiceLocator.Get<IAnalyticsManager>().AchievementUnlocked("ads");
            }
        }

        private void Update()
        {
        }
    }
}