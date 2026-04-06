using UnityEngine;

#if PL_SDK_FIREBASE_ON
using Firebase;
#endif

namespace Playcus.FirebaseSDK
{

    /// <summary>
    /// Every part of Firebase SDK need to check dependency status separatly.
    /// This service provide static property Status for this purpose.
    /// </summary>
    public class FirebaseDependencies : MonoBehaviour
    {
#if PL_SDK_FIREBASE_ON
        static public DependencyStatus Status = DependencyStatus.UnavailableUpdating;

        // Use this for initialization
        private void Start()
        {
            // Initialize Firebase
            if (Application.platform == RuntimePlatform.Android)
            {
                Debug.Log("FirebaseDependencies CheckAndFixDependenciesAsync started");
                FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task =>
                    {
                        Status = task.Result;
                        Debug.Log("FirebaseDependencies CheckAndFixDependenciesAsync completed");
                    });
            }
            else
            {
                Status = DependencyStatus.Available;
            }
        }
#endif
    }
}