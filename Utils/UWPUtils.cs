using UnityEngine;
using System;
#if UNITY_WSA && ENABLE_WINMD_SUPPORT
using Windows.Storage.Streams;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using System.Threading.Tasks;
#endif


namespace Playcus.Utils
{
    public static class UWPUtils
    {

#if UNITY_WSA && ENABLE_WINMD_SUPPORT

        public static void Share(string shareText = "", string imagePath = "", string url = "", string subject = "Share")
        {
            UnityEngine.WSA.Application.InvokeOnUIThread(
                async () =>
                {
                    await ShareUWP(shareText, imagePath, url, subject);
                },
                true
            );
        }
        
        private static async Task ShareUWP(string shareText, string imagePath, string url, string subject)
        {
            Debug.LogError($"UWPUtils::ShareUWP - shareText={shareText}, imagePath={imagePath}, url={url}, subject={subject}");
            try
            {
                RandomAccessStreamReference streamRef = null;
                if (!string.IsNullOrEmpty(imagePath))
                {
                    imagePath = imagePath.Replace("/", "\\");
                    StorageFile sFile = await StorageFile.GetFileFromPathAsync(imagePath);
                    streamRef = RandomAccessStreamReference.CreateFromFile(sFile);
                }
                DataTransferManager dtm = DataTransferManager.GetForCurrentView();
                dtm.DataRequested +=
                    (sender, args) =>
                    {
                        DataRequest dr = args.Request;
                        DataRequestDeferral def = dr.GetDeferral();
                        dr.Data.SetText(shareText);
                        if (streamRef != null)
                            dr.Data.SetBitmap(streamRef);
                        else if (!string.IsNullOrEmpty(url))
                            dr.Data.SetWebLink(new Uri(url));
                        dr.Data.Properties.Title = subject;
                        dr.Data.Properties.Description = shareText;
                        def.Complete();
                    };
                DataTransferManager.ShowShareUI();
            }
            catch (Exception e)
            {
                Debug.LogError("UWPUtils::ShareUWP error=" + e.Message);
            }
        }
#else         
        public static void Share(string shareText = "", string imagePath = "", string url = "", string subject = "") { }
#endif
    }
}