using UnityEngine;

namespace Playcus.GDPR
{
    public class UsercentricsConsentServiceConfig : ServiceConfig
    {
        [Tooltip("Is toggle is service enabled")]
        public int isUsercentricsConsentEnabled;
        [Tooltip("0 - will be disabled. >=1 will be checked and showed")]
        [Space(10)]
        public int versionTos;

        [Tooltip("Is toggle i'm older 18 will be showed")]
        public int eighteenToggleActive;

        [Tooltip("Is toggle i'm older 18 will be cheked by default")]
        public int eighteenToggleChecked;

        public bool manualAuditGDPR;
    }
}