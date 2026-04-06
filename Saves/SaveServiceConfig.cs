using System;
using UnityEngine;
using UnityEngine.Serialization;
using Playcus;

namespace Playcus.Saves
{
    public class SaveServiceConfig : ServiceConfig
    {
        [Header("Compare progress int key")]
        public string CompareProgressKey = "Currencies/currency_values/0";
        [Header("Configs by store")] public SaveServiceStoreConfig[] Stores;

        [NonSerialized] private SaveServiceStoreConfig _currentStore = null;
        public SaveServiceStoreConfig CurrentStore => GetCurrentStore();

        [Serializable]
        public class SaveServiceStoreConfig
        {
            public STORE Store;
            public BaseStorage[] StoragesPrefabs;
        }

        private SaveServiceStoreConfig GetCurrentStore()
        {
            if (_currentStore == null)
            {
                foreach (var store in Stores)
                {
                    if (store.Store == StoreConstants.GetCurrentStore())
                    {
                        _currentStore = store;
                    }
                }
            }

            return _currentStore;
        }
    }
}