using System;
using System.Collections.Generic;

namespace Playcus.Analytics
{
    public class AnalyticsEvent
    {
        public string EventKey => _eventKey;
        public Dictionary<string, object> Parameters => _parameters;
            
        private string _eventKey;
        private Dictionary<string, object> _parameters;

        public AnalyticsEvent(string eventKey, Dictionary<string, object> parameters)
        {
            _eventKey = PrepareEventKey(eventKey);
            _parameters = parameters;
        }
            
        private string PrepareEventKey(string eventName)
        {
            eventName.Replace(':', '_');
            eventName.Replace(' ', '_');
            eventName.Replace('.', '_');
            return eventName;
        }
        
        // PARAMETER HELPERS

        public string pr_amount => GetStringParameter(AnalyticsProperties.pr_amount);
        public string pr_currency => GetStringParameter(AnalyticsProperties.pr_currency);
        public string pr_revenue => GetStringParameter(AnalyticsProperties.pr_revenue);
        public string pr_content_id => GetStringParameter(AnalyticsProperties.pr_content_id);
        public string pr_content_type => GetStringParameter(AnalyticsProperties.pr_content_type);
        public string pr_placement => GetStringParameter(AnalyticsProperties.pr_placement);
        public string pr_reason => GetStringParameter(AnalyticsProperties.pr_reason);
        public string pr_receipt => GetStringParameter(AnalyticsProperties.pr_receipt);
        public int pr_level => GetIntParameter(AnalyticsProperties.pr_level);
        public int pr_level_score => GetIntParameter(AnalyticsProperties.pr_level_score);
        public string pr_level_type => GetStringParameter(AnalyticsProperties.pr_level_type);
        public int pr_step => GetIntParameter(AnalyticsProperties.pr_step);
        public string pr_ad_network => GetStringParameter(AnalyticsProperties.pr_ad_network);
        public string pr_ad_format => GetStringParameter(AnalyticsProperties.pr_ad_format);
        
        
        private string GetStringParameter(AnalyticsProperties propertyKey)
        {
            if (_parameters != null && _parameters.ContainsKey(propertyKey.ToString()))
            {
                return _parameters[propertyKey.ToString()].ToString();
            }
            else
            {
                return "";
            }
        }
        
        private int GetIntParameter(AnalyticsProperties propertyKey)
        {
            int returned = 0;
            if (_parameters != null && _parameters.ContainsKey(propertyKey.ToString()))
            {
                Int32.TryParse(_parameters[propertyKey.ToString()].ToString(), out returned);
            }
            return returned;
        }
        
        

        
        
    }
}