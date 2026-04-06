using System;
using System.Collections.Generic;
using UnityEngine.Purchasing;

namespace Playcus.Analytics
{
    /// <summary>
    /// Controlling analytic system choosing.
    /// IAnalyticsManager it's a bridge between Analytic Systems (AnalyticSystem) and events from App.
    /// All playcus events list descriptions https://docs.google.com/spreadsheets/d/1JjSoB1pyAnKDIZ4Ir3PsFESau0e4rwD1WF_-PFT0bTg
    /// </summary>
    public interface IAnalyticsManager
    {
        // CUSTOM
        void CustomEvent(
            string eventKey, 
            int amount = 0, 
            Dictionary<string, object> parameters = null, 
            Dictionary<string, object> additionalParameters = null);

        // INTERNAL
        bool IsTesterUser();
        void LogDebug(string newMessage, bool force = false);

        void SetGeneralParameterToAllEvents(string parameterKey, object parameterValue);
    }
}