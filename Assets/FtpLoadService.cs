using System;
using System.Net;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Playcus.Assets
{
    [ServiceBind(typeof(FtpLoadService))]
    public class FtpLoadService : ServiceWithConfig
    {
        protected override Type ConfigType => typeof(FtpLoaderConfig);
        protected FtpLoaderConfig Config => (FtpLoaderConfig) _serviceConfig;

        // public async UniTask<Texture> GetTextureFromUrl(string address, string cacheGroup, Action<Texture> success = null, Action failed = null)
        // {
        //     return null;
        // }
        //
        // public async UniTask<byte[]> GetFileFromUrl(string address, Action<byte[]> success = null, Action failed = null)
        // {
        //     Uri uri = new Uri(Config.UrlPath + "/" + address);
        //     Debug.Log("FtpLoadService: GetFileFromUrl = " + uri);
        //     
        //     WebClient client = new WebClient();
        //     client.Credentials = new NetworkCredential(Config.Login, Config.Password);
        //     
        //     var task = client.DownloadFileTaskAsync(uri, Application.persistentDataPath + "/gg");
        //     await task;
        //
        //     // if (!client.IsBusy)
        //     if(task.IsCompleted)
        //     {
        //         Debug.Log("File download complete");
        //         // return client.DownloadData()
        //     }
        //     else
        //     {
        //         Debug.LogError("File download failed - " + task.Status);
        //     }
        //     return null;
        // }

        public async UniTask CreateDirectory(string directoryName)
        {
            var request = GetRequest(directoryName, WebRequestMethods.Ftp.MakeDirectory);
            
            try
            {
                await request.GetResponseAsync();
                Debug.Log($"FtpLoadService.CreateDirectory = {directoryName}");
            }
            catch (WebException ex)
            {
                // Debug.LogError("FtpLoadService.CreateDirectory exception = " + ex.Message);
            }
        }

        public async UniTask DeleteFile(string address)
        {
            var request = GetRequest(address, WebRequestMethods.Ftp.DeleteFile);
            
            try
            {
                await request.GetResponseAsync();
            }
            catch (WebException ex)
            {
                // Debug.LogError("FtpLoadService.DeleteFile exception = " + ex.Message);
            }
        }
        
        public async UniTask UploadFile(string address, string filename)
        {
            WebClient client = new WebClient();
            client.Credentials = new NetworkCredential(Config.Login, Config.Password);
            
            Uri uri = new Uri(Config.UrlPath + "/" + address);
            
            var task = client.UploadFileTaskAsync(uri, filename);
            // client.
            await task;
            
            if(task.IsCompleted)
                Debug.Log($"FtpLoadService: Success upload file: '{address}' to '{filename}'");
            else
                Debug.LogError($"FtpLoadService: Fail upload file: '{address}' to '{filename}'");
        }

        private FtpWebRequest GetRequest(string path, string method)
        {
            var uri = new Uri($"{Config.UrlPath}/{path}");
            
            var request = (FtpWebRequest)WebRequest.Create(uri);
            request.Method = method;
            request.Credentials = new NetworkCredential(Config.Login, Config.Password);

            return request;
        }
    }
}