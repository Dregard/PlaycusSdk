using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
#if PL_SDK_GA_ON
using GameAnalyticsSDK;
#endif

namespace Playcus
{
    public interface IService
    {
        ServiceState State { get; }
        public UniTask LoadAsync();
    }

    /// <summary>
    /// Base class for singletone monobehavior with loading process by Loader and bind-resolve services pattern
    /// Documentation https://docs.google.com/document/d/1A6Ce52Xh3iLBiVgYKnjTHNCm4g21k5jln3PdMnZE0To/edit#
    /// </summary>
    // todo: add IsInitialized flag to explicitly reflect when service was loaded with an error
    public abstract class Service : MonoBehaviour, IService
    {
        public ServiceState State { get; protected set; } = ServiceState.NotStarted;
        // private ServiceState _State  = ServiceState.NotStarted;
        //
        // public ServiceState State
        // {
        //     get => _State;
        //     set
        //     {
        //         _State = value;
        //         Debug.LogError($"Service {this.name} state changed to {value}");
        //     }
        // }
        
        /// <summary>
        /// Is service complete loaded
        /// </summary>
        // public bool IsLoaded { get; private set; }

        /// <summary>
        /// Main mode of loading for separation back compability with service self-bind-resolve-loading without Loader in OnEnable and Start methods.
        /// </summary>
        public bool IsLoadProcessByLoader { get; private set; }

        // private bool _isLoading;
        private UniTask _loadingTask;
        
        /// <summary>
        /// Timeout in seconds. Unlimited if -1.
        /// </summary>
        [SerializeField]
        protected int _initializationTimeout = -1;

        /// <summary>
        /// We need MarkAsLoadedByLoader for back compability with service self-bind-resolve-loading without Loader in OnEnable and Start methods.
        /// </summary>
        public virtual void MarkAsLoadedByLoader()
        {
            IsLoadProcessByLoader = true;
        }

        public async UniTask LoadAsync()
        {
            // If already loaded, return a completed task
            if (State is ServiceState.Ready or ServiceState.Failed)
            {
                return;
            }

            if (State == ServiceState.Initializing)
            {
                await _loadingTask;
            }
            else
            {
                var timeout = _initializationTimeout > 0 ? _initializationTimeout * 1000 : Timeout.Infinite;
                using var cts = new CancellationTokenSource(timeout);
                
                State = ServiceState.Initializing;

                try
                {
#if DEBUG
                    var stalledServices = new System.Collections.Generic.List<string>(PlayerPrefs.GetString("stalledServices", "").Split(';'));
                    if (stalledServices.Contains(name))
                    {
                        Debug.LogWarning($"{name}: imitating timeout.");
                        await UniTask.Delay(TimeSpan.FromSeconds(60));
                    }
#endif
                    _loadingTask = LoadAsyncInternal(cts.Token);
                    await _loadingTask;
                }
                catch (OperationCanceledException e)
                {
                    Debug.LogError($"{GetType().Name} loading cancelled with exception: {e}",this.gameObject);
                    
                    TrackServiceInitializationFailure($"{GetType().Name}", "Initialization timeout");

                    State = ServiceState.Failed;
                }
                catch (Exception e)
                {
                    Debug.LogError($"{GetType().Name} loading failed with exception: {e}");
                    
                    TrackServiceInitializationFailure($"{GetType().Name}", $" loading failed with exception: {e}");

                    State = ServiceState.Failed;
                }
            }
        }

        protected void TrackServiceInitializationFailure(string serviceName, string errorMessage)
        {
            var message = $"[Service Init Failure] {serviceName} – {errorMessage}";
#if PL_SDK_FIREBASE_ON && !PL_CRASHLYTICS_OFF
            if (FirebaseSDK.FirebaseDependencies.Status == Firebase.DependencyStatus.Available)
            {
                var exception = new Exception(message);
                Firebase.Crashlytics.Crashlytics.Log(message);
                Firebase.Crashlytics.Crashlytics.LogException(exception);
            }
#endif
#if PL_SDK_GA_ON
            GameAnalytics.NewErrorEvent(GAErrorSeverity.Critical, message);
#endif
        }

        protected abstract UniTask LoadAsyncInternal(CancellationToken cancellationToken);
        
        
        
        /// <summary>
        /// Loading process of service just completed
        /// </summary>
        protected void ServiceLoadingComplete()
        {
            State = ServiceState.Ready;
            Debug.Log($"{GetType().Name}: Loading complete", gameObject);
        }

        /// <summary>
        /// Loading without loader - first all services need to bind in OnEnable phase.
        /// </summary>
        protected virtual void OnEnable()
        {
            if (IsLoadProcessByLoader == false)
            {
                ServiceLocator.BindServicesFromObject(this.gameObject);
            }
        }
    }
    
    
    /// <summary>
    /// Represents the current initialization state of a service.
    /// </summary>
    public enum ServiceState
    {
        /// <summary>
        /// The service has not started initialization yet.
        /// </summary>
        NotStarted = 0,

        /// <summary>
        /// The service is currently initializing.
        /// </summary>
        Initializing = 1,

        /// <summary>
        /// Initialization completed successfully, the service is ready to use.
        /// </summary>
        Ready = 2,

        /// <summary>
        /// Initialization failed.
        /// </summary>
        Failed = 3,
    }
}