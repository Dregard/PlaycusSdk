using UnityEngine;
using System.IO;

namespace Playcus.Assets
{
    /// <summary>
    /// multithreading task for file writing in cache
    /// </summary>
    public class SaveCacheDataBaseTask
    {
        private string cacheDataBasePath;
        private AssetsCacheDateBaseVO cacheVO;

        public SaveCacheDataBaseTask(AssetsCacheDateBaseVO cacheVO, string cacheDataBasePath)
        {
            this.cacheVO = cacheVO;
            this.cacheDataBasePath = cacheDataBasePath;
        }

        public void ThreadProc()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(cacheDataBasePath));
            File.WriteAllText(cacheDataBasePath, JsonUtility.ToJson(cacheVO));
        }
    }


}
