using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
        // 1. Enum의 모든 유효한 값을 가져옵니다.
        List<T> allValues = GetAllEnumValues<T>();

        List<T> usableValues = new List<T>(allValues); // 복사본 생성

        // None/Max 필터링 (가장 일반적인 패턴에 대한 추정)
        if (usableValues.Count > 0)
        {
            // Enum 값이 0인 'None'을 제외 (만약 첫 번째 요소가 None이고 값이 0이라면)
            if (Convert.ToInt32(usableValues[0]) == 0 && usableValues[0].ToString().Equals("None", StringComparison.OrdinalIgnoreCase))
            {
                // .RemoveAt(0) 보다 Skip(1)이 더 안전합니다.
                usableValues = usableValues.Skip(1).ToList();
            }

            // Enum 값이 마지막이고 'Max'인 경우 (보통 총 개수를 나타냄)
            if (usableValues.Count > 0) // Skip 후에도 요소가 남아있는지 확인
            {
                T lastValue = usableValues[usableValues.Count - 1];
                // 마지막 요소의 이름이 'Max'이고 해당 값이 Enum의 개수를 나타낼 때
                if (lastValue.ToString().Equals("Max", StringComparison.OrdinalIgnoreCase))
                {
                    // Max가 순수한 Enum 값의 개수를 나타내는 경우만 제외
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
            return -1; // 또는 0 등 적절한 기본값
        }

        // 3. 필터링된 멤버들 중에서 랜덤 인덱스를 선택합니다.
        int randomIndex = UnityEngine.Random.Range(0, usableValues.Count);

        // 4. 선택된 Enum 멤버를 int로 캐스팅하여 반환합니다.
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