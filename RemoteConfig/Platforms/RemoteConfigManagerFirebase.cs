using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Playcus.Analytics;
using UnityEngine;
#if PL_SDK_FIREBASE_ON
using Firebase;
using Firebase.RemoteConfig;
#endif

namespace Playcus.FirebaseSDK
{
    /// <summary>
    /// https://firebase.google.com/docs/remote-config/
    /// Load config from firebase and return variables from config by public methods
    /// </summary>
    [ServiceBind(typeof(IRemoteConfigManager))]
    public class RemoteConfigManagerFirebase : ServiceWithConfig, IRemoteConfigManager
    {
        [Header("Development Only")] [SerializeField]
        private DevelopmentRemoteConfigManager _developmentRemoteConfig;

        /// CONFIG
#if !PL_SDK_FIREBASE_ON
        [UnityEngine.Header("Add SDK_FIREBASE to script defined symbols")] 
        [SerializeField] private bool defineError;
#endif

        public event Action ConfigUpdated;
        
        protected override Type ConfigType => typeof(RemoteConfigManagerFirebaseConfig);
        protected RemoteConfigManagerFirebaseConfig Config => (RemoteConfigManagerFirebaseConfig) _serviceConfig;

        private RemoteConfigsStages _currentStage = RemoteConfigsStages.AwaitingDependencyFixes;
        private Reasons _currentReason = Reasons.Loading;
        private int _waitFixDependencyCount = 0;
        private int _tryFetchCount = 0;
        private int _tryActivationCount = 0;
        
#if PL_FIREBASE_REMOTE_CONFIGS_ON

        protected override async UniTask LoadAsyncInternal(CancellationToken cancellationToken)
        {
            await LoadServiceConfigAsync(cancellationToken, false);
            
            if (Debug.isDebugBuild && _developmentRemoteConfig != null)
            {
                _developmentRemoteConfig.LoadAllDevConfigs();
            }
            
            await UpdateConfigsAndCompleteLoading(cancellationToken);
        }

        private async UniTask UpdateConfigsAndCompleteLoading(CancellationToken cancellationToken)
        {
            await UpdateRemoteConfigAsync(Reasons.Loading, cancellationToken);
            
            LoadComplete();
        }
        
        private void LoadComplete()
        {
            // isFetched = true;
            if (_developmentRemoteConfig != null && Debug.isDebugBuild &&
                _developmentRemoteConfig.Status == DevRemoteConfigLoadStatus.Loading)
            {
                return;
            }
            ServiceLoadingComplete();
            Debug.Log("FirebaseRemoteConfigManager LoadComplete");
            
            Firebase.Installations.FirebaseInstallations.DefaultInstance.GetTokenAsync(false).ContinueWith(
                task => {
                    if (!(task.IsCanceled || task.IsFaulted) && task.IsCompleted) {
                        Debug.Log($"Firebase Installations token {task.Result}");
                    }
                });
        }
        
        private async UniTask SetTimer(float configReUpdateInterval, CancellationToken cancellationToken)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(configReUpdateInterval), cancellationToken: cancellationToken);

            if (_currentStage == RemoteConfigsStages.ConfigUpdateCompleted)
            {
                await UpdateRemoteConfigAsync(Reasons.Timer, cancellationToken);
            }
        }
        
        private void OnApplicationPause(bool pause)
        {
            if (!pause && Config != null && Config.UpdateOnPause)
            {
                if (_currentStage == RemoteConfigsStages.ConfigUpdateCompleted)
                {
                    UpdateRemoteConfigAsync(Reasons.Pause, CancellationToken.None);
                }
            }
        }

        public void UpdateRemoteConfig()
        {
            if (_currentStage == RemoteConfigsStages.ConfigUpdateCompleted)
            {
                UpdateRemoteConfigAsync(Reasons.Forcibly, CancellationToken.None);
            }
        }
       
        private async UniTask UpdateRemoteConfigAsync(Reasons reason, CancellationToken cancellationToken)
        {
            // when loading, do not interrupt config update regardless the state
            if (reason.Equals(Reasons.Loading))
            {
                ServiceLocator.Get<IAnalyticsManager>().SetGeneralParameterToAllEvents(AnalyticsProperties.pr_ab_marker.ToString(),GetValue("ab_marker"));
                ServiceLocator.Get<IAnalyticsManager>().SetGeneralParameterToAllEvents(AnalyticsProperties.pr_ab_marker_from_cache.ToString(),1);
            }
            // otherwise, do not update config unless the service initialized successfully
            else if (State != ServiceState.Ready)
            {
                return;
            }
            
            _currentReason = reason;
            _currentStage = RemoteConfigsStages.AwaitingDependencyFixes;
            
            var isDependencyFixes = false;
            while (!isDependencyFixes)
            {
                if (FirebaseDependencies.Status == Firebase.DependencyStatus.Available)
                {
                    isDependencyFixes = true;
                }
                else
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(1));
                    _waitFixDependencyCount++;
                }
            }
            Debug.Log($"LoadingConfigs: stage: {_currentStage.ToString()} reason: {_currentReason.ToString()} try count: {_waitFixDependencyCount}");
            ServiceLocator.Get<IAnalyticsManager>().LoadingConfigs(_currentStage.ToString(),_currentReason.ToString(),_waitFixDependencyCount);

            _currentStage = RemoteConfigsStages.Fetching;
            var isFetched = false;
            while (!isFetched)
            {
                var taskFetch = FirebaseRemoteConfig.DefaultInstance.FetchAsync(TimeSpan.Zero);
                await taskFetch.AsUniTask();
               
                if (taskFetch.IsCanceled)
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(1));
                    _tryFetchCount++;
                }
                else if (taskFetch.IsFaulted)
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(1));
                    _tryFetchCount++;
                }
                else if (taskFetch.IsCompleted)
                {
                    isFetched = true;
                }
            }
           
            Debug.Log($"LoadingConfigs: stage: {_currentStage.ToString()} reason: {_currentReason.ToString()} try count: {_tryFetchCount}");
            ServiceLocator.Get<IAnalyticsManager>().LoadingConfigs(_currentStage.ToString(),_currentReason.ToString(),_tryFetchCount);

            _currentStage = RemoteConfigsStages.Activating;

            var isActivated = false;

            while (!isActivated)
            {
                var taskActivate = FirebaseRemoteConfig.DefaultInstance.ActivateAsync();
                
                await taskActivate.AsUniTask();

                if (taskActivate.IsCanceled)
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(1));
                    _tryActivationCount++;
                }
                else if (taskActivate.IsFaulted)
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(1));
                    _tryActivationCount++;
                }
                else if (taskActivate.IsCompleted)
                {
                    isActivated = true;
                }
            }
            
            Debug.Log($"LoadingConfigs: stage: {_currentStage.ToString()} reason: {_currentReason.ToString()} try count: {_tryActivationCount}");
            ServiceLocator.Get<IAnalyticsManager>().LoadingConfigs(_currentStage.ToString(),_currentReason.ToString(),_tryActivationCount);

            if (reason.Equals(Reasons.Loading))
            {
                ServiceLocator.Get<IAnalyticsManager>().SetGeneralParameterToAllEvents(AnalyticsProperties.pr_ab_marker.ToString(),GetValue("ab_marker"));
                ServiceLocator.Get<IAnalyticsManager>().SetGeneralParameterToAllEvents(AnalyticsProperties.pr_ab_marker_from_cache.ToString(),0);
            }
            
            // update config
            await LoadServiceConfigAsync(cancellationToken, false);
            
            _currentStage = RemoteConfigsStages.ConfigUpdateCompleted;

            if (Config.ReUpdateInterval > 0)
            {
                await SetTimer(Config.ReUpdateInterval, cancellationToken);
            }
            
            ConfigUpdated?.Invoke();
        }

#else

        protected override async UniTask LoadAsyncInternal(CancellationToken cancellationToken)
        {
            await LoadServiceConfigAsync(cancellationToken,false);
            
            ServiceLoadingComplete();
        }
        
        public void UpdateRemoteConfig()
        {

        }
#endif


        public string GetValue(string key)
        {
            if (Debug.isDebugBuild && _developmentRemoteConfig != null)
            {
                var config = _developmentRemoteConfig.GetValue(key);
                if (config != null)
                {
                    return config;
                }
            }

#if PL_FIREBASE_REMOTE_CONFIGS_ON

            if (FirebaseDependencies.Status == Firebase.DependencyStatus.Available)
            {
                try
                {
                    return FirebaseRemoteConfig.DefaultInstance.GetValue(key).StringValue;
                }
                catch
                {
                    return "";
                }
            }
#endif
            return "";
        }

        

        

        private enum RemoteConfigsStages
        {
            AwaitingDependencyFixes = 0,
            Fetching = 1,
            Activating = 2,
            ConfigUpdateCompleted = 3,
        }

        private enum Reasons
        {
            Loading = 0,
            Timer = 1,
            Pause = 2,
            Forcibly = 3,  
        }
    }
}
