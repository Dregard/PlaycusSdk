using System;

namespace Playcus.FirebaseSDK
{
    [Serializable]
    public class RemoteConfigManagerFirebaseConfig : ServiceConfig
    {
        public bool UpdateOnPause;
        public float ReUpdateInterval = -1f;
    }
}