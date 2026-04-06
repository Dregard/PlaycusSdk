using System;
using System.Collections;
using System.Collections.Generic;
using Playcus.Utils;
using UnityEngine;
using UnityEngine.Serialization;

namespace Playcus
{
    /// <summary>
    /// Base class for service config scriptable object
    /// Documentation https://docs.google.com/document/d/1A6Ce52Xh3iLBiVgYKnjTHNCm4g21k5jln3PdMnZE0To/edit#
    /// </summary>
    public abstract class ServiceConfig : ScriptableObject
    {
        [HelpBox("Config values loaded from IRemoteConfigManager")]
        public string RemoteVarID;
        /// <summary>
        /// if current StoreConstants.GetCurrentStore() exsist in this array, IRemoteConfigManager will use name from array
        /// </summary>
        public List<ConfigPlatform> RemoteVarIDByPlatform=new List<ConfigPlatform>();

        public virtual void ParseObjectToJsonInLog()
        {
            Debug.Log($"RemoteJsonObject: {name} {JsonUtility.ToJson(this,true)}");
        }


    }
    [Serializable]
    public class ConfigPlatform
    {
        public STORE Platform;
        public string Name;
    }
}