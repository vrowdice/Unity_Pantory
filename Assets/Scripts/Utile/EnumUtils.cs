using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Localization;

public static class EnumUtils
{
    /// <summary>
    /// Returns all values of a given Enum type as a List.
    /// </summary>
    public static List<T> GetAllEnumValues<T>() where T : Enum
    {
        return Enum.GetValues(typeof(T)).Cast<T>().ToList();
    }

    /// <summary>
    /// Returns a random integer value from the specified Enum type.
    /// Excludes 'None' and 'Max' if they exist as the first/last elements.
    /// </summary>
    public static int GetRandomEnumValueInt<T>() where T : Enum
    {
        List<T> allValues = GetAllEnumValues<T>();
        List<T> usableValues = new List<T>(allValues);

        if (usableValues.Count > 0)
        {
            if (Convert.ToInt32(usableValues[0]) == 0 && usableValues[0].ToString().Equals("None", StringComparison.OrdinalIgnoreCase))
            {
                usableValues = usableValues.Skip(1).ToList();
            }

            if (usableValues.Count > 0)
            {
                T lastValue = usableValues[usableValues.Count - 1];
                if (lastValue.ToString().Equals("Max", StringComparison.OrdinalIgnoreCase))
                {
                    if (Convert.ToInt32(lastValue) == (allValues.Count - (allValues[0].ToString().Equals("None", StringComparison.OrdinalIgnoreCase) ? 1 : 0)))
                    {
                        usableValues.RemoveAt(usableValues.Count - 1);
                    }
                }
            }
        }


        if (usableValues.Count == 0)
        {
            Debug.LogWarning($"No usable enum values found for type {typeof(T).Name} after filtering. Returning -1.");
            return -1;
        }

        int randomIndex = UnityEngine.Random.Range(0, usableValues.Count);
        return Convert.ToInt32(usableValues[randomIndex]);
    }

    /// <summary>
    /// Returns a random value from the specified Enum type.
    /// Excludes 'None' and 'Max' if they exist as the first/last elements.
    /// </summary>
    public static T GetRandomEnumValue<T>() where T : Enum
    {
        int randomInt = GetRandomEnumValueInt<T>();
        if (randomInt == -1) // 에러 처리
        {
            return default(T); // Enum의 기본값 (보통 0에 해당하는 멤버) 반환
        }
        return (T)Enum.ToObject(typeof(T), randomInt);
    }
}