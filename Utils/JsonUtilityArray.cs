using System;
using System.Collections.Generic;
using UnityEngine;

namespace Playcus.Utils
{
    public static class JsonUtilityArray
    {
        public static List<T> FromJsonArray<T>(string json)
        {
            Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(json);
            return wrapper.Items;
        }

        public static string ToJsonArray<T>(T[] array)
        {
            Wrapper<T> wrapper = new Wrapper<T>();
            wrapper.Items = new List<T>(array);
            return JsonUtility.ToJson(wrapper);
        }

        public static string ToJsonArray<T>(T[] array, bool prettyPrint)
        {
            Wrapper<T> wrapper = new Wrapper<T>();
            wrapper.Items = new List<T>(array);
            return JsonUtility.ToJson(wrapper, prettyPrint);
        }

        [Serializable]
        private class Wrapper<T>
        {
            public List<T> Items;
        }

       
    }
}