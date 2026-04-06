using System;
using System.Collections.Generic;

namespace Unity.Usercentrics
{
    internal class StringParser
    {
        public static bool? ToBool(string input)
        {
            if (input == "true")
            {
                return true;
            }
            else if (input == "false")
            {
                return false;
            }
            return null;
        }

        public static int? ToInt(string input)
        {
            if (input == "null")
            {
                return null;
            }
            return int.Parse(input);
        }

        public static double? ToDouble(string input)
        {
            if (input == "null")
            {
                return null;
            }
            return double.Parse(input);
        }

        public static long? ToLong(string input)
        {
            if (input == "null")
            {
                return null;
            }
            return long.Parse(input);
        }
    }
}
