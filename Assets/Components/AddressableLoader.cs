using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Playcus.Assets
{
    public class AddressableLoader : MonoBehaviour
    {
        [SerializeField] private string _address;
        
        private void Start()
        {
            Addressables.InstantiateAsync(_address, Vector3.zero,Quaternion.identity,gameObject.transform ).Completed +=
 OnLoadDone;
        }

        private void OnLoadDone(UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<GameObject> obj)
        {
            // In a production environment, you should add exception handling to catch scenarios such as a null result.
            //myGameObject = obj.Result;
        }
    }
}