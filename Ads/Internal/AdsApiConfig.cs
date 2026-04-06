using System;
using UnityEngine;
using Playcus;

namespace Playcus.Ads
{
    public class AdsApiConfig : ServiceConfig
    {
        [field: SerializeField] public bool RewardedEnabled { get; private set; } = true;
        [field: SerializeField] public bool InterstitialEnabled { get; private set; } = true;
        [field: SerializeField] public bool BannerEnabled { get; private set; } = true;
        [field: SerializeField] public BANNER_POS DefaultBannerPosition { get; private set; } = BANNER_POS.BOTTOM;
        [field: SerializeField] public bool AppOpenEnabledOnStart { get; private set; } = true;

        [Header("Configs by store")] public AdsServiceStoreConfig[] Stores;

        [Serializable]
        public class AdsServiceStoreConfig
        {
            public STORE Store;
            public GameObject AdsPlatformConfigPrefab;
        }
    }
}