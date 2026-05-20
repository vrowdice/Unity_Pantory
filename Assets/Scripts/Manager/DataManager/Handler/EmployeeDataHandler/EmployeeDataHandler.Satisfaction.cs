using UnityEngine;

/// <summary>
/// 직원 만족도 집계
/// </summary>
public partial class EmployeeDataHandler
{
    public const float MinEmployeeSatisfaction = -100f;
    public const float MaxEmployeeSatisfaction = 100f;

    /// <summary>
    /// 재직 인원 가중 평균 만족도. 재직자가 없으면 0.
    /// </summary>
    public float GetWeightedAverageSatisfaction()
    {
        int totalCount = 0;
        float weightedSum = 0f;

        foreach (EmployeeEntry entry in _employees.Values)
        {
            if (entry?.state == null || entry.state.count <= 0)
            {
                continue;
            }

            int count = entry.state.count;
            totalCount += count;
            weightedSum += entry.state.currentSatisfaction * count;
        }

        return totalCount > 0 ? weightedSum / totalCount : 0f;
    }

    public static float NormalizeSatisfactionTo01(float satisfaction)
    {
        float range = MaxEmployeeSatisfaction - MinEmployeeSatisfaction;
        return Mathf.Clamp01((satisfaction - MinEmployeeSatisfaction) / range);
    }
}
