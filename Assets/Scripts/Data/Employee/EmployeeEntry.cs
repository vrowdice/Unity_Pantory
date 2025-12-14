using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 직원 데이터와 상태를 포함하는 엔트리
/// </summary>
[Serializable]
public class EmployeeEntry
{
    public EmployeeData employeeData;      // 직원의 정적 데이터 (ScriptableObject)
    public EmployeeState state;    // 직원의 동적 상태

    public EmployeeEntry(EmployeeData data)
    {
        employeeData = data;
        state = new EmployeeState();

        if (data != null)
        {
            state.currentSatisfaction = data.baseSatisfaction;
            state.currentEfficiency = Mathf.Clamp(data.baseEfficiency, 0f, 2f);
            state.salaryLevel = 2;
        }
    }

    /// <summary>
    /// 새로운 효과를 적용합니다.
    /// </summary>
    public void AddEffect(EffectData data)
    {
        if (data == null) return;
        if (state.activeEffects == null) state.activeEffects = new List<EffectState>();

        var runtimeEffect = new EffectState(data);
        state.activeEffects.Add(runtimeEffect);
    }

    /// <summary>
    /// 특정 ID를 가진 효과를 제거합니다.
    /// </summary>
    public void RemoveEffectById(string effectId)
    {
        if (state.activeEffects == null) return;
        state.activeEffects.RemoveAll(e => e.Data.id == effectId);
    }

    /// <summary>
    /// 현재 활성화된 효과들을 반영하여 스탯을 계산합니다.
    /// </summary>
    public float CalculateStat(StatType statType, float baseValue)
    {
        if (state.activeEffects == null || state.activeEffects.Count == 0)
        {
            return baseValue;
        }

        float flatSum = 0f;
        float percentAddSum = 0f;
        float percentMultTotal = 1f;

        foreach (var effect in state.activeEffects)
        {
            // StatType 체크
            if (effect.Data.statType != statType) continue;

            switch (effect.Data.type)
            {
                case ModifierType.Flat:
                    flatSum += effect.Data.value;
                    break;
                case ModifierType.PercentAdd:
                    percentAddSum += effect.Data.value;
                    break;
                case ModifierType.PercentMult:
                    percentMultTotal *= effect.Data.value;
                    break;
            }
        }

        return (baseValue + flatSum) * (1f + percentAddSum) * percentMultTotal;
    }

    /// <summary>
    /// 기간제 효과의 시간을 업데이트합니다.
    /// </summary>
    public void UpdateEffectsTime(float daysPassed)
    {
        if (state.activeEffects == null) return;

        for (int i = state.activeEffects.Count - 1; i >= 0; i--)
        {
            var effect = state.activeEffects[i];
            if (effect.IsPermanent) continue;

            effect.RemainingDays -= daysPassed;
            if (effect.RemainingDays <= 0)
            {
                state.activeEffects.RemoveAt(i);
            }
        }
    }
}
