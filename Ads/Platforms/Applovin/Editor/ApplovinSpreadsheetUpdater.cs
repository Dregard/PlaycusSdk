using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using UnityEngine.Networking;

namespace AppLovinMax.Scripts.IntegrationManager.Editor
{
    /// <summary>
    /// Parse info about all mediated networks and send info to google spreadsheets
    /// https://docs.google.com/spreadsheets/d/1rHrx2ZgHQdQiXH4lVAZreaXczF-HlK1VFiJlb31SNIA/edit#gid=0
    /// google script sources (Simonenko Alexey or Ilia Kuprin) 
    /// https://script.google.com/a/playcus.com/d/158swp9EJMtN25do8v288AxJgXU0Q9WClRXQtT59Bsw0u_rhJi0vypIiK/edit?usp=drive_web
    /// </summary>
    [InitializeOnLoad]
    public class ApplovinSpreadsheetUpdater : MonoBehaviour
    {
        private const string webServiceUrl =
            "https://script.google.com/macros/s/AKfycbyA9kL5qKnIL_K16xsZbwnYMeRkMO0lofLhce5_UU-bdyBQTg4M/exec";

        private const string spreadsheetPassword = "sdnvwsdh";
        private const string spreadsheetKey = "1rHrx2ZgHQdQiXH4lVAZreaXczF-HlK1VFiJlb31SNIA";

        private const string actionImport = "ImportNetworks";


        public const string SplitArraySymbol = "[*]";
        public const string ActionExport = "ExportPartners";
        public const string PartnersListFilePath = "Assets/Resources/SkAdNetworksIos";

#if PL_SDK_APPLOVIN_ON && (UNITY_IOS || UNITY_ANDROID) 

        static ApplovinSpreadsheetUpdater()
        {
            Debug.Log("ApplovinSpreadsheetUpdater started");
            SendNetworksVersionInGoogle();
        }

        private static UnityWebRequest CreateRequestToGoogleSheet(string data, string action, bool typeGET)
        {
            string query =
                $"{webServiceUrl}?key={spreadsheetKey}&project={Application.productName}&action={action}&data={data}&password={spreadsheetPassword}";

            UnityWebRequest www;
            if (typeGET)
            {
                www = UnityWebRequest.Get(query);
            }
            else
            {
                www = UnityWebRequest.PostWwwForm(query, "");
            }

            www.downloadHandler = new DownloadHandlerBuffer();
            return www;
        }


        [MenuItem("AppLovin/Send networks version in google")]
        private static void SendNetworksVersionInGoogle()
        {
            Debug.Log("ApplovinSpreadsheetUpdater parsing network info");

            AppLovinEditorCoroutine.StartCoroutine(AppLovinIntegrationManager.Instance.LoadPluginData(pluginData =>
            {
                if (pluginData == null)
                {
                    Debug.LogError($"ApplovinSpreadsheetUpdater failed loading plugin data");
                }
                else
                {
                    // Prepare data from applovin integration manager utils
                    string data = "";
                    string s = SplitArraySymbol;

                    // Applovin plugin current version
                    data +=
                        $"Applovin{s}android_{pluginData.AppLovinMax.CurrentVersions.Android}_ios_{pluginData.AppLovinMax.CurrentVersions.Ios}";

                    // Applovin plugin latest version
                    data +=
                        $"{s}android_{pluginData.AppLovinMax.LatestVersions.Android}_ios_{pluginData.AppLovinMax.LatestVersions.Ios}";


                    var networks = pluginData.MediatedNetworks;
                    foreach (var network in networks)
                    {
                        // Mediator network name
                        data += $"{s}{network.DisplayName}";

                        // Mediator current version
                        if (string.IsNullOrEmpty(network.CurrentVersions.Unity))
                        {
                            data += $"{s}-";
                        }
                        else
                        {
                            data += $"{s}android_{network.CurrentVersions.Android}_ios_{network.CurrentVersions.Ios}";
                        }

                        // Mediator latest version
                        data += $"{s}android_{network.LatestVersions.Android}_ios_{network.LatestVersions.Ios}";
                    }

                    // Send prepared data to google
                    AppLovinEditorCoroutine.StartCoroutine(SendNetworks(data));
                }
            }));
        }

        private static IEnumerator SendNetworks(string data)
        {
            UnityWebRequest www = CreateRequestToGoogleSheet(data, actionImport, false);

            yield return www.SendWebRequest();

            Debug.Log("ApplovinSpreadsheetUpdater SendNetworks request sent");
            if (www.isNetworkError || www.isHttpError)
            {
                Debug.Log(www.error);
            }
            else
            {
                Debug.Log("ApplovinSpreadsheetUpdater SendNetworks request complete!" + " Response Code: " +
                          www.responseCode);
                if (www.downloadHandler != null && www.downloadHandler.data != null)
                {
                    string responseText = System.Text.Encoding.UTF8.GetString(www.downloadHandler.data, 0, www.downloadHandler.data.Length);
                    Debug.Log("ApplovinSpreadsheetUpdater Response data :" + responseText);
                }
                else
                {
                    Debug.Log("ApplovinSpreadsheetUpdater Response data is null"); 
                }
            }
        }

        /*//DEPRECATED duplicate of applivin official functional
        [MenuItem("AppLovin/Download ios SKAdNetwork partners ids")]
        private static void DownloadSkAdNetworkPartners()
        {
            Debug.Log("ApplovinSpreadsheetUpdater Test DownloadSkAdNetworkPartners");
            AppLovinEditorCoroutine.StartCoroutine(ExportPartners());
        }

        private static IEnumerator ExportPartners()
        {
            UnityWebRequest www = CreateRequestToGoogleSheet("data", ActionExport, true);

            Debug.Log("ApplovinSpreadsheetUpdater ExportPartners request sent");
            yield return www.SendWebRequest();

            while (!www.isDone)
            {
                yield return new WaitForSecondsRealtime(1.0f);
            }
            
            if (www.isNetworkError || www.isHttpError)
            {
                Debug.Log(www.error);
            }
            else
            {
                Debug.Log("ApplovinSpreadsheetUpdater ExportPartners request complete!" + " Response Code: " +
                          www.responseCode);
                string responseText = System.Text.Encoding.UTF8.GetString(www.downloadHandler.data, 0, www.downloadHandler.data.Length);
                Debug.Log("ApplovinSpreadsheetUpdater Response data: " + responseText);

                if (!string.IsNullOrEmpty(responseText))
                {
                    // write uids to file
                    File.WriteAllText(PartnersListFilePath, responseText, Encoding.UTF8);
                }
                else
                {
                    Debug.LogError("ApplovinSpreadsheetUpdater ERROR: no any partner id get from google Spreadsheet!");
                }
            }
        }
        */


        [MenuItem("AppLovin/Open main config in google sheets")]
        private static void OpenNetworksVersionInGoogle()
        {
            Application.OpenURL(
                "https://docs.google.com/spreadsheets/d/1rHrx2ZgHQdQiXH4lVAZreaXczF-HlK1VFiJlb31SNIA/edit#gid=0");
        }


#endif

    }
    
}