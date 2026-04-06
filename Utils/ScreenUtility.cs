using System;
using System.Collections;
using UnityEngine;

namespace Playcus.Utils
{
    public class ScreenUtility : MonoBehaviour
    {
        [SerializeField] private bool BlockPortraitRotatePhone;
        [SerializeField] private bool BlockLandscapeRotatePhone;
        [SerializeField] private bool BlockPortraitRotateTablet;
        [SerializeField] private bool BlockLandscapeRotateTablet;
        
        [Header("Debug")]
        [SerializeField] private bool EditorAlwaysTablet;
        
        private static ScreenUtility Instance;
        private const float TABLET_DIAGONAL = 7.0F;
        private const float TABLET_DIAGONAL_ANDROID = 6.5F;

        private event Action _screenSizeChangedEvent;
        private Vector2 resolution;
        private bool running = true;
        
        private bool screenAutoRotation = true;
        
        private bool autorotateToPortrait;
        private bool autorotateToPortraitUpsideDown;
        private bool autorotateToLandscapeLeft;
        private bool autorotateToLandscapeRight;

        public static bool AllowScreenAutoRotation
        {
            get
            {
                if (Instance == null)
                {
                    Debug.Log("ScreenUtility Instance not installed");
                    return false;
                }
                else
                {
                    return Instance.screenAutoRotation;
                }
            }
            set
            {
                if (Instance == null)
                {
                    Debug.Log("ScreenUtility Instance not installed");
                    return;
                }

                if (Instance.screenAutoRotation == value) return;

                if (value)
                {
                    Instance.screenAutoRotation = true;
                    Instance.RestoreRotation();
                }
                else
                {
                    Instance.screenAutoRotation = false;
                    Instance.LockRotation();
                }
            }
        }

        private void Start()
        {
            Instance = this;
            StartCoroutine(CheckForChange());
            SetupRotation();
            autorotateToPortrait = Screen.autorotateToPortrait;
            autorotateToPortraitUpsideDown = Screen.autorotateToPortraitUpsideDown;
            autorotateToLandscapeLeft = Screen.autorotateToLandscapeLeft;
            autorotateToLandscapeRight = Screen.autorotateToLandscapeRight;
        }

        private void SetupRotation()
        {
            // Rotation settings
            if ((BlockPortraitRotatePhone && !IsTablet()) || (BlockPortraitRotateTablet && IsTablet()))
            {
                Screen.autorotateToPortrait = false;
                Screen.autorotateToPortraitUpsideDown = false;
            }
            if ((BlockLandscapeRotatePhone && !IsTablet()) || (BlockLandscapeRotateTablet && IsTablet()))
            {
                Screen.autorotateToLandscapeLeft = false;
                Screen.autorotateToLandscapeRight = false;
            }
            
        }

        private void LockRotation()
        {
            var isTablet = IsTablet();
            Screen.autorotateToPortrait = false;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = false;
            Screen.autorotateToLandscapeRight = false;
            if (IsHorizontal() && ((BlockLandscapeRotatePhone && !isTablet) || (BlockLandscapeRotateTablet && isTablet)))
            {
                Screen.autorotateToPortrait = true;
                Screen.autorotateToPortraitUpsideDown = true;
            }else if (!IsHorizontal() && ((BlockPortraitRotatePhone && !isTablet) || (BlockPortraitRotateTablet && isTablet)))
            {
                Screen.autorotateToLandscapeLeft = true;
                Screen.autorotateToLandscapeRight = true;
            }
        }

        private void RestoreRotation()
        {
            Screen.autorotateToPortrait = autorotateToPortrait;
            Screen.autorotateToPortraitUpsideDown = autorotateToPortraitUpsideDown;
            Screen.autorotateToLandscapeLeft = autorotateToLandscapeLeft;
            Screen.autorotateToLandscapeRight = autorotateToLandscapeRight;
        }
        
        private IEnumerator CheckForChange()
        {
            resolution = new Vector2(Screen.width, Screen.height);

            while (running)
            {
                bool changed = false;
                if (Math.Abs(resolution.x - Screen.width) > 0f || Math.Abs(resolution.y - Screen.height) > 0f)
                {
                    resolution = new Vector2(Screen.width, Screen.height);
                    changed = true;
                }

                if (changed)
                {
                    yield return new WaitForEndOfFrame();
                    _screenSizeChangedEvent?.Invoke();
                }

                yield return new WaitForSeconds(0.05f);
            }
        }

        public static float DeviceDiagonalSizeInInches()
        {
            float screenWidth = (float)Screen.width / Screen.dpi;
            float screenHeight = (float)Screen.height / Screen.dpi;
            float diagonalInches = Mathf.Sqrt(Mathf.Pow(screenWidth, 2) + Mathf.Pow(screenHeight, 2));
            return diagonalInches;
        }
        
        public static float DeviceAspectRatio()
        {
            var aspectRatio = Mathf.Max(Screen.width, Screen.height) / Mathf.Min(Screen.width, Screen.height);
            return aspectRatio;
        }

        public static bool IsDeviceIPad()
        {
            var identifier = SystemInfo.deviceModel;
            if (identifier.StartsWith("iPhone", StringComparison.Ordinal))
            {
                return false;
            }
            else if (identifier.StartsWith("iPad", StringComparison.Ordinal))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static bool IsTablet()
        {
            if (Application.isEditor)
            {
                if (Instance != null && Instance.EditorAlwaysTablet)
                {
                    return true;
                }
                else
                {
                    return Screen.width > Screen.height
                        ? (float) Screen.width / (float) Screen.height < 1.4f
                        : (float) Screen.height / (float) Screen.width < 1.4f;
                }
            }                
            else
            {
                if (Application.platform == RuntimePlatform.Android)
                {
                    return DeviceDiagonalSizeInInches() > TABLET_DIAGONAL_ANDROID && DeviceAspectRatio() < 2.0f;
                }
                else if (Application.platform == RuntimePlatform.IPhonePlayer)
                {
                    return IsDeviceIPad();
                }
                else
                {
                    return DeviceDiagonalSizeInInches() > TABLET_DIAGONAL;
                }
            }
                
        }
        
        public static bool IsHorizontal()
        {
            return Screen.width > Screen.height;
        }

        public static void SubscribeToScreenChangeEvent(Action delegateAction)
        {
            if (Instance != null)
            {
                Instance._screenSizeChangedEvent += delegateAction;
            }
            else
            {
                Debug.LogError(
                    "ScreenUtility : Can't subscribe to screenSizeChangedEvent because no instance of ScreenUtility in Loader");
            }
        }
        
        public static void UnsubscribeToScreenChangeEvent(Action delegateAction)
        {
            if (Instance != null)
            {
                Instance._screenSizeChangedEvent -= delegateAction;
            }
        }
    }
}