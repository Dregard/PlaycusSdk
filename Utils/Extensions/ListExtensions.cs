using System;
using System.Collections.Generic;


public static class ListExtensions
{
    private static readonly Random rng = new Random();

    public static T Find<T>(this T[] array, Predicate<T> predicate)
    {
        if (array == null) return default;
        for (int i = 0; i < array.Length; i++)
        {
            if (predicate.Invoke(array[i]))
            {
                return array[i];
            }
        }

        return default;
    }

    /// <summary>
    /// Use System.Random
    /// </summary>
    /// <param name="list"></param>
    /// <param name="random">is null use internal System.Random</param>
    /// <typeparam name="T"></typeparam>
    public static void Shuffle<T>(this IList<T> list, System.Random random = null)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = random?.Next(n + 1) ?? rng.Next(n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }

    /// <summary>
    /// Use System.Random
    /// </summary>
    /// <param name="list"></param>
    /// <param name="random">is null than use internal System.Random</param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static T Random<T>(this IList<T> list, System.Random random = null)
    {
        if (list.Count == 0)
        {
            return default(T);
        }

        var index = random?.Next(0, list.Count) ?? rng.Next(0, list.Count);
        return list[index];
    }


    /// <summary>
    /// Stable Sort
    /// http://www.csharp411.com/c-stable-sort/
    /// </summary>
    /// <param name="list"></param>
    /// <param name="comparison"></param>
    /// <typeparam name="T"></typeparam>
    /// <exception cref="ArgumentNullException"></exception>
    public static void InsertionSort<T>(this IList<T> list, Comparison<T> comparison)
    {
        if (list == null)
            throw new ArgumentNullException(nameof(list));
        if (comparison == null)
            throw new ArgumentNullException(nameof(comparison));

        int count = list.Count;
        for (int j = 1; j < count; j++)
        {
            T key = list[j];

            int i = j - 1;
            for (; i >= 0 && comparison(list[i], key) > 0; i--)
            {
                list[i + 1] = list[i];
            }

            list[i + 1] = key;
        }
    }
}