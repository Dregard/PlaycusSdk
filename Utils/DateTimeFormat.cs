using System.Globalization;
using UnityEngine;
namespace Playcus.Utils
{
    public class DateTimeFormat
    {
        static public string SecondsToTime(int _seconds)
        {
            System.TimeSpan t = System.TimeSpan.FromSeconds(_seconds);
            return $"{t.Seconds:D2}:{t.Seconds:D2}";
        }

        static public string SecondsToTimeHMS(int _seconds)
        {
            System.TimeSpan t = System.TimeSpan.FromSeconds(_seconds);
            return $"{t.Hours:D2}:{t.Minutes:D2}:{t.Seconds:D2}";
        }
        
        public static bool ParseSavedDate(string dateString, out System.DateTime parsedDate)
        {
            // Update date-save string if date not save in Long
            if (long.TryParse(dateString, out var ticks))
            {
                parsedDate = new System.DateTime(ticks);
                return false;
            }
            else
            {
                // if date save as string
                if (System.DateTime.TryParse(dateString,  out parsedDate))
                {
                    // if parsed date bigger then Now date
                    if (parsedDate > System.DateTime.Now)
                    {
                        // Try switch days & months number
                        var fixedDate = TryFixDateString(dateString);
                        if (System.DateTime.TryParse(fixedDate, out parsedDate))
                        {
                            if (parsedDate <= System.DateTime.Now)
                            {
                                return true;
                            }
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    // If can't parse date from string
                    // Try switch days & months number
                    var fixedDate = TryFixDateString(dateString);
                    if (System.DateTime.TryParse(fixedDate, out parsedDate))
                    {
                        if (parsedDate <= System.DateTime.Now)
                        {
                            return true;
                        }
                    }
                }
                parsedDate = System.DateTime.Now;
            }
            if (parsedDate == System.DateTime.MinValue)
            {
                parsedDate = System.DateTime.Now;
            }
            return false;
        }

        public static string TryFixDateString(string dateString)
        {
            if (dateString.Contains("."))
            {
              return TryFixDateString(dateString, '.');
            }
            else if (dateString.Contains("/"))
            {
                return TryFixDateString(dateString, '/');
            }
            else if (dateString.Contains("\\"))
            {
                return  TryFixDateString(dateString, '\\');
            }

            return dateString;
        }

        private static string TryFixDateString(string dateString, char symbol)
        {
            // Switch days & months number
            var s = dateString.Split(symbol);
            if (s.Length > 2)
            {
                (s[0], s[1]) = (s[1], s[0]);
                dateString = "";
                for (var i = 0; i < s.Length - 1; i++)
                {
                    dateString += s[i] + symbol;
                }

                dateString += s[s.Length - 1];
            }

            return dateString;
        }
    }
}