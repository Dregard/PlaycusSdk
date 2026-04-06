using UnityEngine;

#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace Playcus.Utils
{
    public static class UrlUtils
    {

#if UNITY_WEBGL && !UNITY_EDITOR

        [DllImport("__Internal")]
        private static extern void InternalUtilsWebGLProxyOpenURL(string url, string target);

#else

        private static void InternalUtilsWebGLProxyOpenURL(string url, string target)
        {
            Application.OpenURL(url);
        }

#endif

        public static void OpenURL(string url, string target = "_blank")
        {
            InternalUtilsWebGLProxyOpenURL(url, target);
        }

    }
}