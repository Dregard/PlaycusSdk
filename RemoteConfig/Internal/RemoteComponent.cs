using System;
using System.ComponentModel;
using System.Reflection;
using UnityEngine;

namespace Playcus.RemoteConfig
{
    /// <summary>
    /// Load var from remote config manager and set values (set is abstract)
    /// </summary>
    public abstract class RemoteComponent : MonoBehaviour
    {
        // DEPENDENCIES
        protected IRemoteConfigManager remoteConfigManager;

        // CONFIG
        [HelpBox(@"Target component will be override by firebase remote config values. This component must be upper that target in components order on gameobject.", HelpBoxMessageType.Warning)]
        public UnityEngine.Component targetComponent;

        private void Awake()
        {
            TryInit();
        }

        private void TryInit()
        {
            remoteConfigManager = ServiceLocator.Get<IRemoteConfigManager>();
            if (remoteConfigManager != null && remoteConfigManager.State==ServiceState.Ready)
            {
                SetVars();
                remoteConfigManager.ConfigUpdated += OnConfigUpdate;
            }
            else
            {
                Invoke(nameof(TryInit), 0.1f);
            }
        }

        public void OnConfigUpdate()
        {
            SetVars();
        }

        abstract protected void SetVars();

    }
}