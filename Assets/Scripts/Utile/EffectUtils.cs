using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 이펙트(EffectState) 리스트를 이용한 스탯 계산 표준화.
/// 최종 수치 = (기본값 + Σ Flat) × (1 + Σ PercentAdd) × Π PercentMult
/// PercentAdd: 증가율 합산(10% + 10% = 20%). PercentMult: 복리 계산(1.1 × 1.1 = 1.21).
/// </summary>
public static class EffectUtils
{
    /// <summary>
    /// 기본값과 이펙트 목록으로 표준 공식을 적용한 최종 스탯을 계산합니다.
    /// (기본값 + Σ Flat) × (1 + Σ PercentAdd) × Π PercentMult
    /// </summary>
    /// <param name="baseValue">기본 스탯 값</param>
    /// <param name="effects">해당 스탯에 적용되는 이펙트 목록 (Flat/PercentAdd/PercentMult 혼합 가능)</param>
    /// <returns>계산된 최종 스탯 값</returns>
    public static float ComputeStatFromEffects(float baseValue, IReadOnlyList<EffectState> effects)
    {
        if (effects == null || effects.Count == 0)
            return baseValue;

        float sumFlat = 0f;
        float sumPercentAdd = 0f;
        float productPercentMult = 1f;

        for (int i = 0; i < effects.Count; i++)
        {
            EffectState e = effects[i];
            if (e == null) continue;

            switch (e.modifierType)
            {
                case ModifierType.Flat:
                    sumFlat += e.value;
                    break;
                case ModifierType.PercentAdd:
                    sumPercentAdd += e.value;
                    break;
                case ModifierType.PercentMult:
                    productPercentMult *= e.value;
                    break;
            }
        }

        return (baseValue + sumFlat) * (1f + sumPercentAdd) * productPercentMult;
    }

    /// <summary>
    /// 여러 이펙트 리스트를 하나로 합친 뒤 표준 공식으로 최종 스탯을 계산합니다.
    /// </summary>
    /// <param name="baseValue">기본 스탯 값</param>
    /// <param name="effectLists">합칠 이펙트 리스트들 (예: 전역 이펙트 + 개별 이펙트)</param>
    /// <returns>계산된 최종 스탯 값</returns>
    public static float ComputeStatFromEffects(float baseValue, params IReadOnlyList<EffectState>[] effectLists)
    {
        if (effectLists == null || effectLists.Length == 0)
            return baseValue;

        var combined = new List<EffectState>();
        foreach (IReadOnlyList<EffectState> list in effectLists)
        {
            if (list == null) continue;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] != null)
                    combined.Add(list[i]);
            }
        }

        return ComputeStatFromEffects(baseValue, combined);
    }
}
