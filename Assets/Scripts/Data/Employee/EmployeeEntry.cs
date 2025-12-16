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
    public EmployeeData data;
    public EmployeeState state;

    public EmployeeEntry(EmployeeData data)
    {
        this.data = data;
        state = new EmployeeState();

        if (data != null)
        {
            state.currentSatisfaction = data.baseSatisfaction;
            state.currentEfficiency = Mathf.Clamp(data.baseEfficiency, 0f, 2f);
            state.salaryLevel = 2;
        }
    }

    /// <summary>
    /// 직원별 이펙트를 설정/업데이트/제거합니다.
    /// </summary>
    public void SetOrUpdateEffect(string id, StatType type, float value, string displayName, ModifierType modType, float duration = 0f)
    {
        if (state == null) return;

        // 값이 사실상 0이면 이펙트 제거
        if (Mathf.Abs(value) <= 0.001f)
        {
            EffectState existing = GetEffect(type, id);
            if (existing != null) RemoveEffect(existing);
            return;
        }

        // 이펙트 조회
        EffectState effect = GetEffect(type, id);

        if (effect != null)
        {
            // 갱신 (값이 다를 때만)
            if (!Mathf.Approximately(effect.value, value))
            {
                effect.value = value;
                effect.displayName = displayName;
            }
        }
        else
        {
            // 신규 생성
            effect = new EffectState
            {
                id = id,
                statType = type,
                value = value,
                displayName = displayName,
                type = modType,
                durationDays = duration,
                remainingDays = duration
            };
            ApplyEffect(effect);
        }
    }

    /// <summary>
    /// 이펙트 ID로 이펙트를 조회합니다.
    /// </summary>
    public EffectState GetEffect(string effectId)
    {
        if (state?.activeEffects == null) return null;
        return state.activeEffects.FirstOrDefault(e => e != null && e.id == effectId);
    }

    /// <summary>
    /// StatType과 이펙트 ID로 이펙트를 조회합니다.
    /// </summary>
    public EffectState GetEffect(StatType statType, string effectId)
    {
        if (state?.activeEffects == null) return null;
        return state.activeEffects.FirstOrDefault(e => e != null && e.statType == statType && e.id == effectId);
    }

    /// <summary>
    /// 이펙트를 추가합니다.
    /// </summary>
    public void ApplyEffect(EffectState effect)
    {
        if (effect == null || state == null) return;
        if (state.activeEffects == null) state.activeEffects = new List<EffectState>();
        state.activeEffects.Add(effect);
    }

    /// <summary>
    /// 이펙트를 제거합니다.
    /// </summary>
    public void RemoveEffect(EffectState effect)
    {
        if (effect == null || state?.activeEffects == null) return;
        state.activeEffects.Remove(effect);
    }

    /// <summary>
    /// 특정 StatType의 활성 이펙트 목록을 반환합니다.
    /// </summary>
    public List<EffectState> GetActiveEffects(StatType statType)
    {
        if (state?.activeEffects == null) return new List<EffectState>();
        return state.activeEffects.Where(e => e != null && e.statType == statType).ToList();
    }

    /// <summary>
    /// 날짜 경과에 따라 이펙트의 남은 시간을 업데이트하고 만료된 이펙트를 제거합니다.
    /// </summary>
    public void ProcessDayPass()
    {
        if (state?.activeEffects == null) return;

        for (int i = state.activeEffects.Count - 1; i >= 0; i--)
        {
            EffectState effect = state.activeEffects[i];
            if (effect == null || effect.IsPermanent) continue;

            effect.remainingDays -= 1;

            if (effect.remainingDays <= 0)
            {
                state.activeEffects.RemoveAt(i);
            }
        }
    }
}