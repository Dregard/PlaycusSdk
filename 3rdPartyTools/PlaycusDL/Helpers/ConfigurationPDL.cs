
using System;
using System.Xml.Serialization;

namespace PlaycusDL {

    [Serializable, XmlRoot("configuration")]
    public sealed class ConfigurationPDL {

        [XmlElement("environment_key_dev")]
        public string environmentKeyDev;
        [XmlElement("environment_key_live")]
        public string environmentKeyLive;
        [XmlElement("environment_key")]
        public int environmentKey;
        [XmlElement("collect_url")]
        public string collectUrl;

        [XmlElement("hash_secret")]
        public string hashSecret;
        [XmlElement("client_version")]
        public string clientVersion;
        [XmlElement("use_application_version")]
        public bool useApplicationVersion;

        public ConfigurationPDL() {
            environmentKeyDev = "";
            environmentKeyLive = "";
            environmentKey = 0;
            collectUrl = "";

            hashSecret = "";
            clientVersion = "";
            useApplicationVersion = true;
        }
    }
}
