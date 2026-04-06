using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Playcus
{
    /// <summary>
    /// Static class for utility methods with enums
    /// </summary>
    static class ConstantsConvert
    {

        public static T? StringToEnum<T>(string enumString) where T : struct, Enum
        {
            if (Enum.TryParse(enumString, true, out T type))
            {
                return type;
            }

            Debug.LogWarning($"ConstantsConvert {typeof(T).Name} doesn't contain any values with name: " + enumString);

            return null;
        }

        public static PLACE StringToPlace(string place)
        {
            // TODO like currency
            PLACE returned = (PLACE)0;
            try
            {
                returned = (PLACE)Enum.Parse(typeof(PLACE), place, true);
            }
            catch (System.Exception)
            {
                Debug.LogWarning("ConstantsConvert PLACE doesn't contain any values with name: " + place);
            }
            return returned;
        }

        // public static CURRENCY? StringToCurrency(string currency)
        // {
        //     return StringToEnum<CURRENCY>(currency);
        // }

        // public static SCREEN? StringToScreen(string screen)
        // {
        //     return StringToEnum<SCREEN>(screen);
        // }

        // public static REASON StringToReason(string reason)
        // {
        //     // TODO like currency
        //     REASON returned = (REASON)0;
        //     try
        //     {
        //         returned = (REASON)Enum.Parse(typeof(REASON), reason, true);
        //     }
        //     catch (System.Exception)
        //     {
        //         Debug.LogWarning("ConstantsConvert REASON doesn't contain any values with name: " + reason);
        //     }
        //     return returned;
        // }

        // public static POPUP StringToPopup(string popup)
        // {
        //     // TODO like currency
        //     POPUP returned = (POPUP)0;
        //     try
        //     {
        //         returned = (POPUP)Enum.Parse(typeof(POPUP), popup, true);
        //     }
        //     catch (System.Exception)
        //     {
        //         Debug.LogWarning("ConstantsConvert POPUP doesn't contain any values with name: " + popup);
        //     }
        //     return returned;
        // }

        public static STORE StringToStore(string store)
        {
            STORE returned = (STORE)0;
            try
            {
                returned = (STORE)Enum.Parse(typeof(STORE), store, true);
            }
            catch (System.Exception)
            {
                Debug.LogWarning("ConstantsConvert STORE doesn't contain any values with name: " + store);
            }
            return returned;
        }
    }

}