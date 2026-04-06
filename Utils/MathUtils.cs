using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Playcus.Utils
{
    public static class MathUtils
    {
        public static Vector3 AbsVector(Vector3 source)
        {
            source.x = Math.Abs(source.x);
            source.y = Math.Abs(source.y);
            source.z = Math.Abs(source.z);
            return source;
        }

        public static int Abs(int value)
        {
            return (value ^ (value >> 31)) - (value >> 31);
        }


        public static float SubtractPercent(float targetValue, float percent)
        {
            return targetValue - CalcPercentValue(targetValue, percent);
        }

        public static float AddPercent(float targetValue, float percent)
        {
            return targetValue + CalcPercentValue(targetValue, percent);
        }

        public static float CalcPercentValue(float currentValue, float percent)
        {
            return (currentValue * percent) * 0.01f;
        }

        public static float GetPercent(int currentValue, float maxValue)
        {
            if (maxValue == 0 && currentValue != 0) return 0f;
            return (currentValue / maxValue) * 100f;
        }

        public static float CalcPercent(float targetValue)
        {
            return targetValue / 100f;
        }

        /// <summary>
        /// Quadratic Bézier 
        /// from wikipedia https://en.wikipedia.org/wiki/B%C3%A9zier_curve
        ///  </summary>
        /// <param name="start"></param>
        /// <param name="middle"></param>
        /// <param name="end"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        public static Vector2 GetBezierQuadraticPoint(Vector2 start, Vector2 middle, Vector2 end, float t)
        {
            return ((1 - t) * (1 - t) * start + 2 * t * (1 - t) * middle + t * t * end);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="center"></param>
        /// <param name="radius"></param>
        /// <param name="angle">is equal 0 than use random point in 360 degrees</param>
        /// <returns></returns>
        public static Vector2 RandomCircle(Vector3 center, float radius, float angle = 0)
        {
            float ang = angle > 0 ? angle : Random.value * 360;
            Vector2 pos;
            pos.x = center.x + radius * Mathf.Sin(ang * Mathf.Deg2Rad);
            pos.y = center.y + radius * Mathf.Cos(ang * Mathf.Deg2Rad);
            return pos;
        }
        
        public static Vector2[] GetBezierQuadraticPoints(float x0, float y0, float x1, float y1, float x2, float y2,
            float step = 0.01f)
        {
            List<Vector2> positions = new List<Vector2>();
            Vector2 pos;
            for (float t = 0f; t <= 1f; t += step)
            {
                pos = new Vector2();
                pos.x = Mathf.Pow(1 - t, 2) * x0 + 2 * t * (1 - t) * x1 + Mathf.Pow(t, 2) * x2;
                pos.y = Mathf.Pow(1 - t, 2) * y0 + 2 * t * (1 - t) * y1 + Mathf.Pow(t, 2) * y2;

                positions.Add(pos);
            }

            return positions.ToArray();
        }

        public static float ClampAngle(float angle, float min, float max)
        {
            if (angle < -360F)
                angle += 360F;
            if (angle > 360F)
                angle -= 360F;
            return Mathf.Clamp(angle, min, max);
        }

        /// <summary>
        /// Re-maps a number from one range to another.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="from1"></param>
        /// <param name="to1"></param>
        /// <param name="from2"></param>
        /// <param name="to2"></param>
        /// <returns></returns>
        public static float Remap(float value, float from1, float to1, float from2, float to2)
        {
            return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
        }

        public static int GetUniqRandomNumber(int min, int max, int previousRandom)
        {
            int random = Random.Range(min, max);
            if (previousRandom != random)
            {
                return random;
            }

            return GetUniqRandomNumber(min, max, previousRandom);
        }


        public static float GetUniqRandomNumber(float min, float max, float previousRandom)
        {
            float random = Random.Range(min, max);
            if (previousRandom.CompareTo(random) != 0)
            {
                return random;
            }

            return GetUniqRandomNumber(min, max, previousRandom);
        }
    }
}