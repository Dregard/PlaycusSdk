using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Playcus.Utils
{
    public interface IUpdate
    {
        void OnUpdate();
    }

    public interface IPauseFocusChanged
    {
        void OnPaused(bool paused);

        void OnFocus(bool focused);
    }


    public class MonoInstance : MonoBehaviour
    {
        public class DelayCallData
        {
            private Action _delayCallback;
            private float _delayTime;

            private bool _isInvoked;

            public DelayCallData(float delay, Action callback)
            {
                _delayCallback = callback;
                _delayTime = Time.unscaledTime + delay;
            }

            public bool IsInvoked => _isInvoked;

            public void TryCall()
            {
                if (!_isInvoked && Time.unscaledTime > _delayTime)
                {
                    _isInvoked = true;
                    _delayCallback.Invoke();
                }
            }
        }


        #region Timer wrapper

        public class Timer : IDisposable
        {
            public readonly string Id;

            private readonly long _interval;

            private Action _callback;
            private float _ticker = 0;

            public bool AutoDispose;

            public bool UseUnscaledTime;

            public Timer(long interval, Action callback, string id = null)
            {
                Id = id;
                _interval = interval;
                _callback = callback;
            }

            public override string ToString()
            {
                return $"Timer: interval:{_interval:f}";
            }

            public bool IsDisposed()
            {
                return _callback == null;
            }

            public void OnTick()
            {
                _ticker += UseUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                if (_ticker >= _interval)
                {
                    _ticker -= _interval;
                    _callback?.Invoke();

                    if (AutoDispose)
                        Dispose();
                }
            }


            public void Dispose()
            {
                _callback = null;
            }
        }

        #endregion

        private bool _paused;

        private List<DelayCallData> _delayCalls;
        private List<IUpdate> _updateListeners;
        private List<IPauseFocusChanged> _pauseFocusListeners;

        private Timer[] _timers = new Timer[2];

        public void DisposeTimer(string id)
        {
            if (string.IsNullOrEmpty(id)) return;
            foreach (var timer in _timers)
            {
                if (timer?.Id == id)
                {
                    timer?.Dispose();
                    Debug.Log("Timer stopped: " + id);
                    break;
                }
            }
        }

        public void AddPauseFocusListener(IPauseFocusChanged handler)
        {
            _pauseFocusListeners = _pauseFocusListeners ?? new List<IPauseFocusChanged>();
            _pauseFocusListeners.Add(handler);
        }

        public void RemovePauseFocusListeners(IPauseFocusChanged handler)
        {
            _pauseFocusListeners = _pauseFocusListeners ?? new List<IPauseFocusChanged>();
            if (_pauseFocusListeners.Contains(handler))
            {
                _pauseFocusListeners.Remove(handler);
            }
        }

        public void AddToUpdate(IUpdate updateable)
        {
            _updateListeners = _updateListeners ?? new List<IUpdate>();
            _updateListeners.Add(updateable);
        }

        public void AddDelayCall(DelayCallData data)
        {
            _delayCalls = _delayCalls ?? new List<DelayCallData>();
            _delayCalls.Add(data);
        }

        public void RemoveFromUpdate(IUpdate updateable)
        {
            _updateListeners = _updateListeners ?? new List<IUpdate>();
            if (updateable != null && _updateListeners.Contains(updateable))
            {
                _updateListeners.Remove(updateable);
            }

            _updateListeners.RemoveAll(update => update == null);
        }

        public Timer AddTimer(Timer timer)
        {
            var timerSet = false;
            for (int i = 0; i < _timers.Length; i++)
            {
                if (_timers[i] == null)
                {
                    _timers[i] = timer;
                    timerSet = true;
                    break;
                }
            }

            if (timerSet == false)
            {
                Array.Resize(ref _timers, _timers.Length * 2);
                _timers[_timers.Length - 1] = timer;
            }

            Debug.Log("Add Timer: " + timer.ToString());

            return timer;
        }

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        private void OnApplicationQuit()
        {
            Debug.Log("On App QUit");
        }

        private void OnDestroy()
        {
            _updateListeners?.Clear();
            _delayCalls?.Clear();
        }

#if UNITY_ANDROID || UNITY_EDITOR || UNITY_STANDALONE
        void OnApplicationPause(bool pauseStatus)
        {
            _paused = pauseStatus;
            if (_pauseFocusListeners != null)
            {
                foreach (var listener in _pauseFocusListeners)
                {
                    listener.OnPaused(pauseStatus);
                }
            }


#if !UNITY_EDITOR
        Debug.Log("Application pause changed: " + _paused);
#endif
        }

#endif

#if UNITY_IOS || UNITY_EDITOR || UNITY_STANDALONE
        void OnApplicationFocus(bool focused)
        {
            _paused = !focused;
            if (_pauseFocusListeners != null)
            {
                foreach (var listener in _pauseFocusListeners)
                {
                    listener.OnFocus(focused);
                }
            }


#if !UNITY_EDITOR
        Debug.Log("Application focus changed: " + _paused);
#endif
        }
#endif


        private void Update()
        {
            if (_delayCalls != null)
            {
                foreach (var delayData in _delayCalls)
                {
                    delayData.TryCall();
                }

                _delayCalls.RemoveAll(data => data.IsInvoked);
                _delayCalls = _delayCalls.Count == 0 ? null : _delayCalls;
            }

            var len = _timers.Length - 1;
            for (int i = len; i >= 0; i--)
            {
                var timer = _timers[i];
                if (timer != null)
                {
                    timer.OnTick();
                    if (timer.IsDisposed())
                    {
                        _timers[i] = null;
                    }
                }
            }

            if (_updateListeners != null)
            {
                var updLen = _updateListeners.Count - 1;
                for (int i = updLen; i >= 0; i--)
                {
                    if (_updateListeners[i] != null)
                    {
                        _updateListeners[i].OnUpdate();
                    }
                    else
                    {
                        _updateListeners.RemoveAt(i);
                    }
                }
            }
        }

        public IEnumerator RunThrowingIterator(
            IEnumerator enumerator,
            Action<Exception> done
        )
        {
            while (true)
            {
                object current;
                try
                {
                    if (enumerator.MoveNext() == false)
                    {
                        break;
                    }

                    current = enumerator.Current;
                }
                catch (Exception ex)
                {
                    done(ex);
                    yield break;
                }

                yield return current;
            }

            done(null);
        }
    }


    public static class MonoHelper
    {
        private static MonoInstance _instance;

        private static bool _isInitialized;

        public static void Initialize()
        {
            if (!_isInitialized && _instance == null)
            {
                _instance = Object.FindObjectOfType<MonoInstance>();
                _instance = _instance != null
                    ? _instance
                    : new GameObject("MonoGameHelper").AddComponent<MonoInstance>();
                _isInitialized = true;
                Debug.Log("MonoHelper Initialized!");
            }
        }

        public static void DelayCallback(float delay, Action callback)
        {
            if (IsInitialized())
            {
                _instance.AddDelayCall(new MonoInstance.DelayCallData(delay, callback));
            }
        }

        public static void AddOnUpdate(IUpdate update)
        {
            if (IsInitialized())
            {
                _instance.AddToUpdate(update);
            }
        }

        public static void RemoveFromUpdate(IUpdate update)
        {
            if (IsInitialized())
            {
                _instance.RemoveFromUpdate(update);
            }
        }

        public static void AddTimerOnce(TimeSpan interval, Action callback, bool unscaledTime = false)
        {
            if (IsInitialized())
            {
                var timer = new MonoInstance.Timer(
                        (long) interval.TotalSeconds,
                        callback)
                    {AutoDispose = true, UseUnscaledTime = unscaledTime};

                _instance.AddTimer(timer);
            }
        }

        public static IDisposable AddTimer(TimeSpan interval, Action callback, string id = null,
            bool unscaledTime = false)
        {
            if (IsInitialized())
            {
                _instance.DisposeTimer(id);

                var timer = new MonoInstance.Timer(
                    (long) interval.TotalSeconds,
                    callback, id)
                {
                    UseUnscaledTime = unscaledTime
                };


                return _instance.AddTimer(timer);
            }

            return null;
        }

        public static void StopTimer(string id)
        {
            if (IsInitialized())
            {
                _instance.DisposeTimer(id);
            }
        }


        public static void StartSafeCoroutine(IEnumerator coroutine, Action<Exception> callback)
        {
            if (IsInitialized())
            {
                _instance.StartCoroutine(_instance.RunThrowingIterator(coroutine, callback));
            }
        }

        public static void StopAllCoroutines()
        {
            if (IsInitialized())
            {
                _instance.StopAllCoroutines();
            }
        }

        public static void StartCoroutine(IEnumerator coroutine)
        {
            if (IsInitialized())
            {
                _instance.StartCoroutine(coroutine);
            }
        }

        public static void StopCoroutine(string methodName)
        {
            if (IsInitialized())
            {
                _instance.StopCoroutine(methodName);
            }
        }

        public static void StopCoroutine(IEnumerator coroutine)
        {
            if (IsInitialized())
            {
                _instance.StopCoroutine(coroutine);
            }
        }

        public static void StartSafeCoroutineEx(this MonoBehaviour behavior, IEnumerator coroutine,
            Action<Exception> onError)
        {
            if (IsInitialized())
            {
                behavior.StartCoroutine(_instance.RunThrowingIterator(coroutine, onError));
            }
        }

        private static bool IsInitialized()
        {
            if (!_isInitialized || _instance == null)
            {
                Debug.LogWarning("MonoHelper not Initialized!");
                return false;
            }

            return true;
        }
    }
}