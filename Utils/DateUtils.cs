using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Playcus.Utils
{
    public class DateUtils
    {
        public const string MAIN_FORMAT = "dd/MM/yyyy HH:mm:ss";
        public const string VARIANT_A_FORMAT = "MM/dd/yyyy HH:mm:ss";
        public const string VARIANT_B_FORMAT = "MM'-'dd'-'yyyy HH:mm:ss";
        public const string VARIANT_C_FORMAT = "dd'-'MM'-'yyyy HH:mm:ss";

        public static string Now
        {
            get
            {
                return DateTime.Now.ToString(MAIN_FORMAT, CultureInfo.InvariantCulture);
            }
        }

        public static bool TryParse(string value, out DateTime result)
        {
            result = DateTime.Now;

            if (string.IsNullOrEmpty(value))
            {
                Debug.LogWarning($"DateUtils, value null or empty, defaultDateTime: {Now}");
                return false;
            }

            // dd mm yyyy hh mm ss
            if (DateTime.TryParseExact(value, MAIN_FORMAT, CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
            {
                return true;
            }
            else if (DateTime.TryParseExact(value, VARIANT_C_FORMAT, CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
            {
                return true;
            }

            // mm dd yyyy hh mm ss

            else if (DateTime.TryParseExact(value, VARIANT_A_FORMAT, CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
            {
                return true;
            }
            else if (DateTime.TryParseExact(value, VARIANT_B_FORMAT, CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
            {
                return true;
            }

            // last chance 

            if (DateTime.TryParse(value, out result))
            {
                return true;
            }

            Debug.LogError($"DateUtils, Not support format, value: {value}");
            return false;
        }

        public DateTime Parse(string value)
        {
            return DateTime.ParseExact(value, MAIN_FORMAT, CultureInfo.InvariantCulture);
        }
    }
}
