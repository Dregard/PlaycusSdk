using UnityEngine;
using Playcus;

namespace Playcus.Analytics
{
    /// <summary>
    /// Track platform key on loading started
    /// All playcus events list https://docs.google.com/spreadsheets/d/1JjSoB1pyAnKDIZ4Ir3PsFESau0e4rwD1WF_-PFT0bTg
    /// </summary>
    public class PlatformTracker : MonoBehaviour
    {
        private void Start()
        {
            ServiceLocator.Get<IAnalyticsManager>().CustomEvent(AnalyticsEvents.pl_store_.ToString() + StoreConstants.GetCurrentStore().ToString());
        }

    }
}