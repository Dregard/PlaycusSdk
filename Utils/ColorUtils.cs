using UnityEngine;
using System.Collections;

namespace Playcus.Utils
{
    public static class ColorUtils
    {
        //    Извлечение компонентов цвета:

        ////24bit
        //var color:uint = 0x336699;
        //var r:uint = color >> 16;
        //var g:uint = color >> 8 & 0xFF;
        //var b:uint = color & 0xFF;
        ////32bit
        //var color:uint = 0xff336699;
        //var a:uint = color >>> 24;
        //var r:uint = color >>> 16 & 0xFF;
        //var g:uint = color >>> 8 & 0xFF;
        //var b:uint = color & 0xFF;

        public static bool IsEqualTo(this Color32 aCol, Color32 aRef)
        {
            return aCol.r == aRef.r && aCol.g == aRef.g && aCol.b == aRef.b && aCol.a == aRef.a;
        }

        public static Color32 ConvertHexToColor(string hex)
        {
            hex = hex.Replace("0x", ""); //in case the string is formatted 0xFFFFFF
            hex = hex.Replace("#", ""); //in case the string is formatted #FFFFFF
            byte a = 255; //assume fully visible unless specified in hex
            byte r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
            byte g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
            byte b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
            //Only use alpha if the string has enough characters
            if (hex.Length == 8)
            {
                a = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
            }

            return new Color32(r, g, b, a);
        }

        public static Color32 ConvertUintToColor(uint aCol)
        {
            Color32 c = new Color32();
            c.b = (byte) ((aCol) & 0xFF);
            c.g = (byte) ((aCol >> 8) & 0xFF);
            c.r = (byte) ((aCol >> 16) & 0xFF);
            c.a = (byte) ((aCol >> 24) & 0xFF);
            return c;
        }

        public static Color GetRandomColor(Color start, Color end)
        {
            return new Color(Random.Range(start.r, end.r), Random.Range(start.g, end.g), Random.Range(start.b, end.b));
        }

        public static Color GetRandomColor()
        {
            return new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f));
        }

        static public Color ColorLerp(Color _startColor, Color _endColor, float _value)
        {
            Color returnedColor = new Color(_startColor.r + ((_endColor.r - _startColor.r) * _value),
                _startColor.g + ((_endColor.g - _startColor.g) * _value),
                _startColor.b + ((_endColor.b - _startColor.b) * _value),
                _startColor.a + ((_endColor.a - _startColor.a) * _value));
            return returnedColor;
        }
    }
}