using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Playcus.Assets
{
    /// <summary>
    /// Can load from cdn, web or cache files of any type (audioclips, images, binary etc)
    /// </summary>
    public interface IFilesLoader
    {
        UniTask<AudioClip> GetAudioClipFromLocal(string address, string cacheGroup, AudioType audioType,
            CancellationTokenSource cancellationToken, Action<AudioClip> success = null, Action failed = null);

        UniTask<AudioClip> GetAudioClipFromUrl(string address, string cacheGroup, AudioType audioType,
            CancellationTokenSource cancellationToken, Action<AudioClip> success = null, Action failed = null);

        UniTask<AudioClip> GetAudioClipFromCdn(string address, string cacheGroup, AudioType audioType,
            CancellationTokenSource cancellationToken, Action<AudioClip> success = null, Action failed = null);

        UniTask<Texture> GetTextureFromLocal(string address, string cacheGroup,
            CancellationTokenSource cancellationToken, Action<Texture> success = null, Action failed = null, bool nonReadable = false);

        UniTask<Texture> GetTextureFromUrl(string address, string cacheGroup, CancellationTokenSource cancellationToken,
            Action<Texture> success = null, Action failed = null, bool nonReadable = false);

        UniTask<Texture> GetTextureFromCdn(string address, string cacheGroup, CancellationTokenSource cancellationToken,
            Action<Texture> success = null, Action failed = null, bool nonReadable = false);

        UniTask<byte[]> GetFileFromLocal(string address, string cacheGroup, CancellationTokenSource cancellationToken,
            Action<byte[]> success = null, Action failed = null);

        UniTask<byte[]> GetFileFromUrl(string address, string cacheGroup, CancellationTokenSource cancellationToken,
            Action<byte[]> success = null, Action failed = null);

        UniTask<byte[]> GetFileFromCdn(string address, string cacheGroup, CancellationTokenSource cancellationToken,
            Action<byte[]> success = null, Action failed = null);

        void WriteToCache(string assetAddress, string cacheGroup, byte[] assetBytes);

        string CachedAssetPath(string assetAdress);
        
        void ClearCache();
    }
}