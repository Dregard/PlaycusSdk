using System;
using System.Collections;
using System.Collections.Generic;
using Playcus.Utils;
using UnityEngine;
using UnityEngine.Networking;
using System.Linq;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Playcus.Assets
{
    /// <summary>
    /// Must be used only as IFilesLoader. You can find full documentation in IFilesLoader
    /// </summary>
    [ServiceBind(typeof(IFilesLoader))]
    public class FilesLoadService : ServiceWithConfig, IFilesLoader
    {
        // CONFIG
        [HelpBox(@"Can load from local, cdn, web or cache files of any type (audioclips, images, binary etc.
Work with multi and single threads. )", HelpBoxMessageType.Info)]
        [SerializeField]
        private bool _editorLogs;

        protected override Type ConfigType => typeof(WebFilesLoaderConfig);
        protected WebFilesLoaderConfig Config => (WebFilesLoaderConfig) _serviceConfig;

        // PRIVATE
        private string CachePath
        {
            get { return Application.persistentDataPath + "/Cache"; }
        }

        private const string CACHE_VO_PATH = "database";
        private const string CACHE_DEFAULT_GROUP_NAME = "default";
        private const int CACHE_DEFAULT_GROUP_LIMIT = 100;
        private AssetsCacheDateBaseVO cacheVO = new AssetsCacheDateBaseVO();
        private bool cacheNeedBeSaved;
        
        protected override async UniTask LoadAsyncInternal(CancellationToken cancellationToken)
        {
            await base.LoadAsyncInternal(cancellationToken);

            LoadCacheDataBase();

            ServiceLoadingComplete();
        }


        // FILES LOADING
        public async UniTask<AudioClip> GetAudioClipFromLocal(string url, string cacheGroup, AudioType audioType,
            CancellationTokenSource cancellationToken,
            Action<AudioClip> success = null, Action failed = null)
        {
            return await GetAudioClip($"file://{url}", cacheGroup, audioType, cancellationToken, success, failed);
        }

        public async UniTask<AudioClip> GetAudioClipFromUrl(string url, string cacheGroup, AudioType audioType,
            CancellationTokenSource cancellationToken,
            Action<AudioClip> success = null, Action failed = null)
        {
            return await GetAudioClip(url, cacheGroup, audioType, cancellationToken, success, failed);
        }

        public async UniTask<AudioClip> GetAudioClipFromCdn(string assetAddress, string cacheGroup,
            AudioType audioType, CancellationTokenSource cancellationToken, Action<AudioClip> success = null,
            Action failed = null)
        {
            return await GetAudioClip(Config.cdnUrlPath + assetAddress, cacheGroup, audioType, cancellationToken,
                success, failed);
        }

        public async UniTask<Texture> GetTextureFromLocal(string url, string cacheGroup,
            CancellationTokenSource cancellationToken, Action<Texture> success = null,
            Action failed = null, bool nonReadable = false)
        {
            return await GetTexture($"file://{url}", cacheGroup, cancellationToken, success, failed, nonReadable);
        }

        public async UniTask<Texture> GetTextureFromUrl(string url, string cacheGroup,
            CancellationTokenSource cancellationToken, Action<Texture> success = null,
            Action failed = null, bool nonReadable = false)
        {
            return await GetTexture(url, cacheGroup, cancellationToken, success, failed, nonReadable);
        }

        public async UniTask<Texture> GetTextureFromCdn(string assetAddress, string cacheGroup,
            CancellationTokenSource cancellationToken,
            Action<Texture> success = null, Action failed = null, bool nonReadable = false)
        {
            return await GetTexture(Config.cdnUrlPath + assetAddress, cacheGroup, cancellationToken, success,
                failed, nonReadable);
        }

        public async UniTask<byte[]> GetFileFromLocal(string url, string cacheGroup,
            CancellationTokenSource cancellationToken, Action<byte[]> success = null,
            Action failed = null)
        {
            return await GetFile($"file://{url}", cacheGroup, cancellationToken, success, failed);
        }

        public async UniTask<byte[]> GetFileFromUrl(string url, string cacheGroup,
            CancellationTokenSource cancellationToken, Action<byte[]> success = null,
            Action failed = null)
        {
            return await GetFile(url, cacheGroup, cancellationToken, success, failed);
        }

        public async UniTask<byte[]> GetFileFromCdn(string assetAddress, string cacheGroup,
            CancellationTokenSource cancellationToken,
            Action<byte[]> success = null, Action failed = null)
        {
            return await GetFile(Config.cdnUrlPath + assetAddress, cacheGroup, cancellationToken, success,
                failed);
        }

        private AudioClip GetAudioClipWithName(UnityWebRequest webRequest)
        {
            var audioClip = DownloadHandlerAudioClip.GetContent(webRequest);
            audioClip.name = Path.GetFileNameWithoutExtension(webRequest.url) ?? "";
            return audioClip;
        }


        private async UniTask<AudioClip> GetAudioClip(string url, string cacheGroup, AudioType audioType,
            CancellationTokenSource cancellationTokenSource,
            Action<AudioClip> success, Action failed)
        {
            if (Application.isEditor && _editorLogs)
                Debug.Log($"FilesLoader GetAudioClip {url}", gameObject);

            try
            {
                bool cacheEnabled = Config.CacheEnabled && !url.StartsWith("file://") &&
                                    Application.platform != RuntimePlatform.WebGLPlayer;

                // From Cache
                string cachedPath = CachedAssetPath(url);
                if (cacheEnabled && !string.IsNullOrEmpty(cacheGroup) && System.IO.File.Exists(cachedPath))
                {
                    UnityWebRequest localWebRequest = await UnityWebRequestMultimedia
                        .GetAudioClip("file://" + cachedPath, audioType)
                        .SendWebRequest(); //.WithCancellation(cancellationTokenSource.Token); TODO fix error
                    if (!localWebRequest.isNetworkError && !localWebRequest.isHttpError)
                    {
                        if (Application.isEditor && _editorLogs)
                            Debug.Log($"FilesLoader GetAudioClipByUrlCoroutine founded in Cache");
                        UpdateDataBaseRecord(url, cacheGroup);
                        success?.Invoke(GetAudioClipWithName(localWebRequest));
                        return GetAudioClipWithName(localWebRequest);
                    }
                }

                // From path
                for (int loadTry = 0; loadTry < Config.RepeatFailLoadMaxCount; loadTry++)
                {
                    UnityWebRequest urlWebRequest = await UnityWebRequestMultimedia
                        .GetAudioClip(url, audioType)
                        .SendWebRequest(); //.WithCancellation(cancellationTokenSource.Token); TODO fix error
                    if (!urlWebRequest.isNetworkError && !urlWebRequest.isHttpError)
                    {
                        if (Application.isEditor && _editorLogs)
                            Debug.Log($"FilesLoader GetAudioClipByUrlCoroutine founded in Web");
                        if (cacheEnabled && !string.IsNullOrEmpty(cacheGroup))
                        {
                            WriteToCache(url, cacheGroup, urlWebRequest.downloadHandler.data);
                        }

                        success?.Invoke(GetAudioClipWithName(urlWebRequest));
                        return GetAudioClipWithName(urlWebRequest);
                    }
                    else
                    {
                        //Wait for next try to load
                        await UniTask.Delay(TimeSpan.FromSeconds(Config.RepeatFailLoadWaitSeconds), true);
                    }
                }

                Debug.LogError($"FilesLoader GetAudioClip asset not loaded: {url}");
                failed?.Invoke();
                return null;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return null;
            }
        }


        private async UniTask<Texture> GetTexture(string url, string cacheGroup,
            CancellationTokenSource cancellationTokenSource, Action<Texture> success, Action failed,
            bool nonReadable)
        {
            if (Application.isEditor && _editorLogs)
                Debug.Log($"FilesLoader GetTexture {url}");

            try
            {
                bool cacheEnabled = Config.CacheEnabled && !url.StartsWith("file://") &&
                                    Application.platform != RuntimePlatform.WebGLPlayer;
                Texture returned = null;

                // From Cache
                string cachedPath = CachedAssetPath(url);
                if (cacheEnabled && !string.IsNullOrEmpty(cacheGroup) && System.IO.File.Exists(cachedPath))
                {
                    var localWebRequestUnitask = await UnityWebRequestTexture
                        .GetTexture("file://" + cachedPath, nonReadable)
                        .SendWebRequest().ToUniTask(cancellationToken: cancellationTokenSource.Token).SuppressCancellationThrow();
                    //.WithCancellation(cancellationTokenSource.Token);
                   
                    if (localWebRequestUnitask.IsCanceled)
                    {
                        cancellationTokenSource.Dispose();
                        return null;
                    }
                    
                    if (localWebRequestUnitask.Result.result == UnityWebRequest.Result.Success)
                    {
                        if (Application.isEditor && _editorLogs)
                            Debug.Log($"FilesLoader GetTexture founded in Cache");
                        UpdateDataBaseRecord(url, cacheGroup);
                        success?.Invoke(DownloadHandlerTexture.GetContent(localWebRequestUnitask.Result));
                        returned = DownloadHandlerTexture.GetContent(localWebRequestUnitask.Result);
                        returned.name = $"{url}";
                        return returned;
                    }
                }

                // From path
                for (int loadTry = 0; loadTry < Config.RepeatFailLoadMaxCount; loadTry++)
                {
                    var urlWebRequestUnitask = await UnityWebRequestTexture
                        .GetTexture(url, nonReadable)
                        .SendWebRequest().ToUniTask(cancellationToken: cancellationTokenSource.Token).SuppressCancellationThrow();; //.WithCancellation(cancellationTokenSource.Token); TODO fix error with cancelation for catalog optimisation with cancel

                    if (urlWebRequestUnitask.IsCanceled)
                    {
                        cancellationTokenSource.Dispose();
                        return null;
                    }
                    
                    if (urlWebRequestUnitask.Result.result == UnityWebRequest.Result.Success)
                    {
                        if (Application.isEditor && _editorLogs)
                            Debug.Log($"FilesLoader GetTexture founded in Web");
                        if (cacheEnabled && !string.IsNullOrEmpty(cacheGroup))
                        {
                            WriteToCache(url, cacheGroup, urlWebRequestUnitask.Result.downloadHandler.data);
                        }

                        success?.Invoke(DownloadHandlerTexture.GetContent(urlWebRequestUnitask.Result));
                        returned = DownloadHandlerTexture.GetContent(urlWebRequestUnitask.Result);
                        returned.name = $"{url}";
                        return returned;
                    }
                    else
                    {
                        //Wait for next try to load
                        await UniTask.Delay(TimeSpan.FromSeconds(Config.RepeatFailLoadWaitSeconds), true);
                    }
                }

                Debug.LogError($"FilesLoader GetTexture asset not loaded: {url}");
                failed?.Invoke();
                return returned;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return null;
            }
        }


        private async UniTask<byte[]> GetFile(string url, string cacheGroup,
            CancellationTokenSource cancellationTokenSource, Action<byte[]> success, Action failed)
        {
            if (Application.isEditor && _editorLogs)
                Debug.Log($"FilesLoader GetFileByUrlCoroutine {url}");

            try
            {
                bool cacheEnabled = Config.CacheEnabled && !url.StartsWith("file://") &&
                                    Application.platform != RuntimePlatform.WebGLPlayer;

                // From Cache
                string cachedPath = CachedAssetPath(url);
                if (cacheEnabled && !string.IsNullOrEmpty(cacheGroup) && System.IO.File.Exists(cachedPath))
                {
                    UnityWebRequest localWebRequest = await UnityWebRequest
                        .Get("file://" + cachedPath)
                        .SendWebRequest(); //.WithCancellation(cancellationTokenSource.Token); TODO fix error
                    if (!localWebRequest.isNetworkError && !localWebRequest.isHttpError)
                    {
                        if (Application.isEditor && _editorLogs)
                            Debug.Log($"FilesLoader GetFile founded in Cache");
                        UpdateDataBaseRecord(url, cacheGroup);
                        success?.Invoke(localWebRequest.downloadHandler.data);
                        return localWebRequest.downloadHandler.data;
                    }
                }

                // From path
                for (int loadTry = 0; loadTry < Config.RepeatFailLoadMaxCount; loadTry++)
                {
                    UnityWebRequest urlWebRequest = await UnityWebRequest
                        .Get(url)
                        .SendWebRequest(); //.WithCancellation(cancellationTokenSource.Token); TODO fix error
                    if (!urlWebRequest.isNetworkError && !urlWebRequest.isHttpError)
                    {
                        if (Application.isEditor && _editorLogs)
                            Debug.Log($"FilesLoader GetFile founded in Web");
                        if (cacheEnabled && !string.IsNullOrEmpty(cacheGroup))
                        {
                            WriteToCache(url, cacheGroup, urlWebRequest.downloadHandler.data);
                        }

                        success?.Invoke(urlWebRequest.downloadHandler.data);
                        return urlWebRequest.downloadHandler.data;
                    }
                    else
                    {
                        //Wait for next try to load
                        await UniTask.Delay(TimeSpan.FromSeconds(Config.RepeatFailLoadWaitSeconds), true);
                    }
                }

                Debug.LogError($"FilesLoader GetFile asset not loaded: {url}");
                failed?.Invoke();
                return null;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return null;
            }
        }

        // CACHE

        /// <summary>
        /// Saved as md5 hash cached file path
        /// </summary>
        public string CachedAssetPath(string assetAdress)
        {
            return $"{CachePath}/{HashedAssetAddress(assetAdress)}";
        }

        private string HashedAssetAddress(string assetAdress)
        {
            return HashString.Hash(assetAdress);
        }

        /// <summary>
        /// Delete all cached files
        /// /// </summary>
        public void ClearCache()
        {
            Debug.LogWarning("AssetsManager Cache cleared!");
            Debug.LogError(CachePath);
            if (Directory.Exists(CachePath))
            {
                DirectoryInfo directory = new DirectoryInfo(CachePath);

                foreach (FileInfo file in directory.GetFiles())
                {
                    file.Delete();
                }
            }

            //Directory.Delete(CachePath);
        }

        public void WriteToCache(string assetAdress, string cacheGroup, byte[] assetBytes)
        {
            string cachedAddress = CachedAssetPath(assetAdress);
            if (string.IsNullOrEmpty(cacheGroup))
                cacheGroup = CACHE_DEFAULT_GROUP_NAME;

            WriteFileToCacheTask tws = new WriteFileToCacheTask(cachedAddress, assetBytes);
#if UNITY_WEBGL
            // one thread only support
            tws.ThreadProc();
#else
            // multithreading for other platforms
            Thread t = new Thread(new ThreadStart(tws.ThreadProc));
            t.Start();
#endif
            UpdateDataBaseRecord(assetAdress, cacheGroup);
        }

        private void UpdateDataBaseRecord(string assetAddress, string cacheGroup)
        {
            string hash = HashedAssetAddress(assetAddress);

            // Group settings
            int groupLimit = CACHE_DEFAULT_GROUP_LIMIT;
            AssetsCacheGroupConfig groupConfig = Config.cacheGroups.FirstOrDefault(p => p.cacheGroup == cacheGroup);
            if (groupConfig != null)
                groupLimit = groupConfig.itemsLimit;
            groupLimit = Mathf.Max(1, groupLimit);

            // Find or create group in database
            AssetsCacheGroupVO group = cacheVO.groups.FirstOrDefault(p => p.cacheGroup == cacheGroup);
            if (group == null)
            {
                group = new AssetsCacheGroupVO(cacheGroup);
                cacheVO.groups.Add(group);
            }

            // Add unique record in end of list
            if (group.hashes.Contains(hash))
                group.hashes.Remove(hash);
            group.hashes.Add(hash);

            // Check cache limits and remove older files
            while (group.hashes.Count > groupLimit)
            {
                RemoveFromCache($"{CachePath}/{group.hashes[0]}");
                group.hashes.RemoveAt(0);
            }

            // Update datebase file
            SaveCacheDataBase();
        }

        private void RemoveFromCache(string cachedAddress)
        {
            RemoveFileFromCacheTask tws = new RemoveFileFromCacheTask(cachedAddress);
#if UNITY_WEBGL
            // one thread only support
            tws.ThreadProc();
#else
            // multithreading for other platforms
            Thread t = new Thread(new ThreadStart(tws.ThreadProc));
            t.Start();
#endif
        }

        private void LoadCacheDataBase()
        {
            if (System.IO.File.Exists(CACHE_VO_PATH))
                JsonUtility.FromJsonOverwrite(System.IO.File.ReadAllText(CACHE_VO_PATH), cacheVO);
        }

        private void SaveCacheDataBase()
        {
            if (!cacheNeedBeSaved)
            {
                cacheNeedBeSaved = true;
                Invoke("SaveCacheDataBaseInvoked", 2f);
            }
        }

        private void SaveCacheDataBaseInvoked()
        {
            cacheNeedBeSaved = false;
            SaveCacheDataBaseTask tws = new SaveCacheDataBaseTask(cacheVO, $"{CachePath}/{CACHE_VO_PATH}");
#if UNITY_WEBGL
            // one thread only support
            tws.ThreadProc();
#else
            // multithreading for other platforms
            Thread t = new Thread(new ThreadStart(tws.ThreadProc));
            t.Start();
#endif
        }
    }
}