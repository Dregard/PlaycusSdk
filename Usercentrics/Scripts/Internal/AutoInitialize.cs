using Unity.Usercentrics;
using UnityEngine;

namespace Unity.Usercentrics
{
    public class AutoInitialize : MonoBehaviour
    {
        [SerializeField] public bool Enabled = true;

        void Awake()
        {
            Debug.Log("[USERCENTRICS] AutoInitialize is " + Enabled);

            if (!Enabled)
            {
                return;
            }

            Usercentrics.Instance.Initialize((status) =>
            {
                if (status.geolocationRuleset.bannerRequiredAtLocation == false)
                {
                    return;
                }

                if (status.shouldCollectConsent)
                {
                    ShowFirstLayer();
                }
            },
            (errorMessage) =>
            {
                Debug.Log("[USERCENTRICS] Error on AutoInitialize " + errorMessage);
            });
        }

        private void ShowFirstLayer()
        {
            Usercentrics.Instance.ShowFirstLayer((usercentricsConsentUserResponse) => {});
        }
    }
}
