using System;
using System.Collections.Generic;

namespace Playcus.Iap
{
    [Serializable]
    public class IapManagerSaveVO
    {
        /// <summary>
        /// List of all anytime purchased user's productId 
        /// </summary>
        public List<string> AnytimePurchased = new List<string>();
        
        /// <summary>
        /// List of all anytime subscription charged analytics tracked
        /// </summary>
        public List<string> AnalyticsSubscriptionTracked = new List<string>();
    }

}
