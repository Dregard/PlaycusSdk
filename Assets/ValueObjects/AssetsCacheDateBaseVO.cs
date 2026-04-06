using System;
using System.Collections.Generic;

namespace Playcus.Assets
{
    [Serializable]
    public class AssetsCacheDateBaseVO
    {
        public List<AssetsCacheGroupVO> groups = new List<AssetsCacheGroupVO>();
    }

    [Serializable]
    public class AssetsCacheGroupVO
    {
        public string cacheGroup;
        public List<string> hashes = new List<string>();

        public AssetsCacheGroupVO(string cacheGroup)
        {
            this.cacheGroup = cacheGroup;
        }
    }


}
