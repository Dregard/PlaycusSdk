using System.Collections.Generic;
using UnityEngine;
using System;

namespace Playcus.Assets
{
    public class WebFilesLoaderConfig : ServiceConfig
    {
        [Tooltip("Url with path to CDN where stored project files (Cloudflare, Cloudfront etc)")]
        public string cdnUrlPath;
        public int RepeatFailLoadMaxCount = 2;
        public float RepeatFailLoadWaitSeconds = 3;
        public bool CacheEnabled = true;
        public List<AssetsCacheGroupConfig> cacheGroups;
    }

    [Serializable]
    public class AssetsCacheGroupConfig
    {
        public string cacheGroup = "default";
        public int itemsLimit = 100;
    }
}