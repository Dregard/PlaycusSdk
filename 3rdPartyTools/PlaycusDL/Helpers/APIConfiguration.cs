using System;
using System.Xml.Serialization;

namespace PlaycusDL {
    
    [Serializable, XmlRoot("configuration")]
    public sealed class APIConfiguration {
        
     
        [XmlElement("api_key")]
        public string ApiKey;
        
        public APIConfiguration(){
            ApiKey = "";
        }
    }
}
