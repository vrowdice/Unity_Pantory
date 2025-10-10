using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ProbabilityUtils
{
    /// <summary>
    /// Returns true with the given chance (between 0 and 100).
    /// </summary>
    /// <param name="percent">Chance between 0 and 100.</param>
    public static bool RollPercent(float percent)
    {
        if (percent <= 0f) return false;
        if (percent >= 100f) return true;
        return Random.Range(0f, 100f) < percent;
    }

    /// <summary>
    /// Returns a random element from a non-empty list. If the list is null or empty, returns default(T).
    /// </summary>
    public static T GetRandomElement<T>(List<T> list)
    {
        if (list == null || list.Count == 0)
        {
            return default;
        }

        int index = Random.Range(0, list.Count);
        return list[index];
    }

    /// <summary>
    /// Returns a random KeyValuePair from a non-empty dictionary. If the dictionary is null or empty, returns default(KeyValuePair<TKey, TValue>).
    /// </summary>
    public static KeyValuePair<TKey, TValue> GetRandomElement<TKey, TValue>(Dictionary<TKey, TValue> dictionary)
    {
        if (dictionary == null || dictionary.Count == 0)
        {
            return default;
        }

        int index = Random.Range(0, dictionary.Count);
        return dictionary.ElementAt(index);
    }
}
