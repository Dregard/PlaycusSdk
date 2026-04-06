using System.Collections;
using System.Collections.Generic;
using UnityEditor;

namespace Unity.Editor
{
    public static class EditorCoroutinesUtility
    {
        private class Coroutine {
            public IEnumerator Enumerator;
            public System.Action<bool> OnUpdate;
            public readonly List<IEnumerator> History = new List<IEnumerator> ();
        }
 
        private static readonly List<Coroutine> Coroutines = new List<Coroutine> ();

        public static void StartWaitingCoroutine()
        {
            StartCoroutine(WaitingCoroutine());
        }

        public static void StopWaitingCoroutine()
        {
            StopAll();
        }
        
        private static IEnumerator WaitingCoroutine()
        {
            while (true)
            {
                yield return null;
            }
        }
 
        private static void StartCoroutine(IEnumerator enumerator, System.Action<bool> onUpdate = null) {
            if (Coroutines.Count == 0) {
                EditorApplication.update += Update;
            }
            var coroutine = new Coroutine { Enumerator = enumerator, OnUpdate = onUpdate };
            Coroutines.Add(coroutine);
        }
 
        private static void Update() {
            for (int i = 0; i < Coroutines.Count; i++) {
                var coroutine = Coroutines[i];
                bool done = !coroutine.Enumerator.MoveNext();
                if (done) {
                    if (coroutine.History.Count == 0) {
                        Coroutines.RemoveAt (i);
                        i--;
                    } else {
                        done = false;
                        coroutine.Enumerator = coroutine.History[coroutine.History.Count - 1];
                        coroutine.History.RemoveAt(coroutine.History.Count - 1);
                    }
                } else {
                    if (coroutine.Enumerator.Current is IEnumerator) {
                        coroutine.History.Add(coroutine.Enumerator);
                        coroutine.Enumerator = (IEnumerator)coroutine.Enumerator.Current;
                    }
                }
                if (coroutine.OnUpdate != null) coroutine.OnUpdate(done);
            }
            if (Coroutines.Count == 0) EditorApplication.update -= Update;
        }
 
        private static void StopAll() {
            Coroutines.Clear();
            EditorApplication.update -= Update;
        }
    }
}
