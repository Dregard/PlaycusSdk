using System;
using System.ComponentModel;
using System.Reflection;
using UnityEngine;
using Playcus.Analytics;
using System.Collections.Generic;
using System.Collections;

namespace Playcus.RemoteConfig
{
    /// <summary>
    /// Load var from firebase config manager and track value in IAnalyticsManager
    /// </summary>
    public class RemoteVarAnalyticsTracker : MonoBehaviour
    {
        // DEPENDENCIES
        private IRemoteConfigManager _remoteConfigManager;
        private IAnalyticsManager _analyticsManager;

        // CONFIG
        [HelpBox(@"Load var from firebase config manager and track value in IAnalyticsManager", HelpBoxMessageType.Info)]
        [Tooltip("Var that value will be loaded from firebase")]
        [SerializeField] private string RemoteVarName;
        [Tooltip("Analytics event name that will be tracked in IAnalyticsManager")]
        [SerializeField] private string AnalyticsEvent;

        private int _tryCount = 0;
        
        private void Start()
        {
            CheckForServices();
        }

        private void CheckForServices()
        {
            _remoteConfigManager = ServiceLocator.Get<IRemoteConfigManager>();
            _analyticsManager = ServiceLocator.Get<IAnalyticsManager>(true);
            if (_remoteConfigManager != null && _analyticsManager != null && _remoteConfigManager.State==ServiceState.Ready)
            {
                SetVars();
                _remoteConfigManager.ConfigUpdated += OnConfigUpdate;
            }
            else if (_tryCount < 10)
            {
                Invoke("CheckForServices", 1f);
                _tryCount++;
            }
            else
            {
                if (_remoteConfigManager != null)
                {
                    _remoteConfigManager.ConfigUpdated -= OnConfigUpdate;
                }
                OnConfigUpdate();
            }
        }

        public void OnConfigUpdate()
        {
            SetVars();
        }

        private void SetVars()
        {
            string remoteValue = _remoteConfigManager.GetValue(RemoteVarName);
            remoteValue = string.IsNullOrEmpty(remoteValue) ? "-1" : remoteValue;
            
            if (_analyticsManager != null)
            {
                _analyticsManager.CustomEvent(AnalyticsEvent, 0,
                                            new Dictionary<string, object>()
                                            {
                                                { AnalyticsProperties.pr_content_id.ToString(),remoteValue  }
                                            }
                                            );
                
            }
        }
    }
}