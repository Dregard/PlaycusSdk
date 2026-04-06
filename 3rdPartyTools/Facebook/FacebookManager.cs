using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

#if PL_SDK_FACEBOOK_ON
using Facebook.Unity;
#endif

namespace Playcus.Facebook
{
    /// <summary>
    /// Manage all usage of facebook sdk
    /// </summary>
    public class FacebookManager : MonoBehaviour
    {
        // EVENTS
        public UnityEvent InitCompletedActions;
        public event Action InitCompleted;
        public event Action LoginSuccessed;
        public event Action LoginFailed;
        public event Action LogoutSuccessed;

        // PUBLIC VARIABLES
        [HideInInspector] public bool LoginCompleted;
        [HideInInspector] public bool LoginProcessed;

        // PRIVATE CONSTANTS
        private const string KEY_LOGINED = "FB_logined";

        //PRIVATE VARIABLES

        private int _loginStep;
        private string _userID = "";

#if PL_SDK_FACEBOOK_ON
        // WITH FACEBOOK SDK IN PROJECT

        public void Start()
        {

            Debug.Log("FacebookManager: Start", gameObject);
            if (!FB.IsInitialized)
                FB.Init(OnSDKInit, OnHideUnity, null);
            else
                OnSDKInit();
        }

        private void OnHideUnity(bool unityIsHidden)
        {
        }

        private void OnSDKInit()
        {
            Debug.Log("FacebookManager: OnSDKInit", gameObject);
#if SDK_FACEBOOK_CANVAS
            Login();
#else
            //If FB Login previus session then autologin
            if (PlayerPrefs.GetInt(KEY_LOGINED) == 1)
            {
                Debug.Log("FacebookManager: AutoLogin", gameObject);
                if (FB.IsLoggedIn)
                {
                    Debug.Log("FacebookManager: update acesstoken", gameObject);
                    FB.Mobile.RefreshCurrentAccessToken();
                    LoginCompleted = true;
                    InitComplete();
                }
                else
                {
                    Login();
                }
            }
            else
            {
                Debug.Log("FacebookManager: User not loggined yet", gameObject);
                InitComplete();
            }
#endif
        }

        private void InitComplete()
        {
            InitCompleted?.Invoke();
            InitCompletedActions.Invoke();
        }


        //Initial loging with gettng data permissions
        public void Login()
        {
            Debug.Log("FacebookManager: Login", gameObject);

            if (!LoginCompleted)
            {
                Debug.Log("FacebookManager:StartLogin", gameObject);
                FB.LogInWithReadPermissions(new List<string>() { "public_profile", "user_friends" }, LoginCallback);
                LoginProcessed = true;
            }
            else
            {
                Debug.Log("FacebookManager:Already logined!", gameObject);
                InitComplete();
            }
        }

        public void Logout()
        {
            if (FB.IsLoggedIn)
            {
                FB.LogOut();
                PlayerPrefs.SetInt(KEY_LOGINED, 0);
                LogoutSuccessed?.Invoke();
            }
        }

        private void LoginCallback(ILoginResult result)
        {
            Debug.Log(result.RawResult, gameObject);
            if (result.Error == null && !result.Cancelled)
            {
                PlayerPrefs.SetInt(KEY_LOGINED, 1);
                LoginCompleted = true;
                LoginSuccessed?.Invoke();
            }
            else
            {
                LoginFailed?.Invoke();
            }
            InitComplete();
        }

#else
        // WITHOUT FACEBOOK SDK IN PROJECT
        public void Login() { }
        public void Logout() { }

#endif
    }
}