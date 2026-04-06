using System;
using System.Collections.Generic;
using UnityEngine;
using Playcus;

namespace Playcus.Analytics.Internal
{
    public class AnalyticsServiceConfig : ServiceConfig
    {
        
        [Header("Configs by store")] public AnalyticsServiceStoreConfig[] Stores;

        [Serializable]
        public class AnalyticsServiceStoreConfig
        {
            public STORE Store;
            [HelpBox(@"Analytics order for uid setup is PDL > Appsflyer", HelpBoxMessageType.Warning)]
            public List<GameObject> AnalyticsPlatformConfigPrefabs;
        }

    }
}