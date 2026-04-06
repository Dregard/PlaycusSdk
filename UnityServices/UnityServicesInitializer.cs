using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.Services.Core;
using Unity.Services.Core.Environments;
using UnityEngine;

namespace Playcus.Services.Unity
{
    [ServiceBind(typeof(UnityServicesInitializer))]
    public class UnityServicesInitializer : Service
    {
        private string _environment = "production";

        protected override async UniTask LoadAsyncInternal(CancellationToken cancellationToken)
        {
            var options = new InitializationOptions()
                .SetEnvironmentName(_environment);

            // wait & initialize services
            Debug.Log("UnityServicesInitializer: Initializing UnityServices now...", gameObject);
          
            await UnityServices.InitializeAsync(options);
               
            ServiceLoadingComplete();
        }
    }
}
