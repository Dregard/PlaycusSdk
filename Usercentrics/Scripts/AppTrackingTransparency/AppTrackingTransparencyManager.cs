#if UNITY_IOS
using System.Runtime.InteropServices;
using AOT;
using UnityEngine.Events;

namespace Unity.Usercentrics
{
    public class AppTrackingTransparencyManager
    {

        [DllImport("__Internal")]
        private static extern void ucRequestForAppTrackingTransparency(TrackingTransparencyDelegate callback);

        [DllImport("__Internal")]
        private static extern int ucInterfaceGetTrackingAuthorizationStatus();

        public static UnityAction<AuthorizationStatus> AttCallback;
        public static UnityAction<AuthorizationStatus> AttStatusCallback;
        private delegate void TrackingTransparencyDelegate(int status);

        [MonoPInvokeCallback(typeof(TrackingTransparencyDelegate))]
        private static void DelegateMessageReceived(int number)
        {   
            DelegateStatusReceived(number, AttCallback);
        }
        
        private static void DelegateStatusReceived(int value, UnityAction<AuthorizationStatus> callback)
        {   
            switch (value)
            {
                case 0:
                    callback?.Invoke(AuthorizationStatus.NOT_DETERMINED);
                    break;
                case 1:
                    callback?.Invoke(AuthorizationStatus.RESTRICTED);
                    break;
                case 2:
                    callback?.Invoke(AuthorizationStatus.DENIED);
                    break;
                case 3:
                    callback?.Invoke(AuthorizationStatus.AUTHORIZED);
                    break;
                default:
                    break;
            }
        }

        public static void RequestForAppTrackingTransparency()
        {
            ucRequestForAppTrackingTransparency(DelegateMessageReceived);
        }

        public static void GetTrackingAuthorizationStatus()
        {
            DelegateStatusReceived(ucInterfaceGetTrackingAuthorizationStatus(), AttStatusCallback);
        }
    }
}
#endif
