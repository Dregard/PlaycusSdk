using System;
using Cysharp.Threading.Tasks;
using Playcus.Utils;
using UnityEngine;

namespace Playcus.Saves
{
    public class BaseStorage : MonoBehaviour
    {
        private Action<BaseStorage, bool> _callbackLoadingComplete;

        public bool IsInit { get; private set; }
        public bool IsSynchronize { get; private set; }
        
        public event Action<string> OnDebug;

        public virtual void Synchronize(Action<BaseStorage, bool> callbackComplete)
        {
            _callbackLoadingComplete = callbackComplete;
        }

        protected void SynchronizeComplete(bool success)
        {
            Debug.Log($"{name}: SynchronizeComplete success = {success}");
            if (!IsInit)
            {
                IsInit = true;
                IsSynchronize = success;
                
                StopAllCoroutines();
                
                _callbackLoadingComplete?.Invoke(this, success);
                _callbackLoadingComplete = null;
            }
        }

        public virtual void Save(string json)
        {
            
        }

        public virtual JSONNode Load()
        {
            return null;
        }

        public virtual async UniTask Delete()
        {
            
        }
        
        protected void DebugText(string str, bool isError = false)
        {
            OnDebug?.Invoke(str);
            
            if(isError)
                Debug.LogError(str);
            else
                Debug.Log(str);
        }
    }
}