using UnityEngine;

namespace Playcus.Utils
{
    public class TargetFrameRate : MonoBehaviour
    {
        [HelpBox(@"Application.targetFrameRate setter", HelpBoxMessageType.Info)]
        [SerializeField] private int targetFrameRate = 60;
        [SerializeField] private bool inEditor = false;

        // Use this for initialization
        private void OnEnable()
        {
            if (!Application.isEditor || inEditor)
            {
                // Limit the framerate to 60 to keep device from burning through cpu
                Application.targetFrameRate = targetFrameRate;
            }
        }
    }
}