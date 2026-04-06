using System;
using System.Collections;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using Playcus.Analytics;

namespace Playcus.Network
{
    /// <summary>
    /// Check internet connection and invoke unity event if no network.
    /// </summary>
    [ServiceBind(typeof(INetworkManager))]
    public class NetworkManager : Service, INetworkManager
    {
        //EVENTS
        [HelpBox(
            @"SETUP INSTRUCTION 
1. Check internet connection manual by method CheckNetworkConnection 
2. Unity event will invoked if method return no network.
3. You can attach any no network reaction to prefab."
            , HelpBoxMessageType.Info)]
        [SerializeField]
        private bool _blockLoadingWithoutInternet = false;

        public UnityEvent NoNetworkOnCheck;

        private bool _connectionLost;

        protected override async UniTask LoadAsyncInternal(CancellationToken cancellationToken)
        {
            try
            {
                Debug.Log("NetworkManager: Load", gameObject);
                if (_blockLoadingWithoutInternet && Application.internetReachability == NetworkReachability.NotReachable)
                {
                    NoNetworkOnCheck?.Invoke();

                    ///todo: need test
                    await WaitRestoreConnection(cancellationToken);
                }
                else
                {
                    ServiceLoadingComplete();
                }
            }
            catch (OperationCanceledException)
            {
                State = ServiceState.Failed;
                
                Debug.LogError($"{gameObject.name} Initializing ERROR");
            }
        }


        private async UniTask WaitRestoreConnection(CancellationToken cancellationToken)
        {
            try
            {
                ///todo: need test
                await UniTask.WaitUntil(()=>Application.internetReachability == NetworkReachability.NotReachable, cancellationToken: cancellationToken);
           
                ServiceLoadingComplete();
            }
            catch (OperationCanceledException)
            {
                State = ServiceState.Failed;
                
                Debug.LogError($"{gameObject.name} Initializing ERROR");
            }
        }

        public bool CheckNetworkConnection(bool noNetworkActions = true)
        {
            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                _connectionLost = true;
                Debug.Log("NetworkManager CheckNetworkConnection: NotReachable", gameObject);
                if (noNetworkActions)
                {
                    NoNetworkOnCheck.Invoke();
                }

                return false;
            }
            else
            {
                if (_connectionLost)
                {
                    _connectionLost = false;
                    ServiceLocator.Get<IAnalyticsManager>().CustomEvent("pl_network_reconnect");
                }

                return true;
            }
        }
    }
}