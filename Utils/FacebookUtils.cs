using System;

namespace Playcus.Utils
{
    public static class FacebookUtils
    {
        public static bool IsFacebookInitializingStarted = false;

        public static event Action FacebookInitializedEvent;

        public static void OnFacebookInitialized()
        {
            FacebookInitializedEvent?.Invoke();
        }
    }
}