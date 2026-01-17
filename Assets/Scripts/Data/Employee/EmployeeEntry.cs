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
    /// 직원 이펙트 적용
    /// </summary>
    /// <param name="effectData">이펙트 데이터</param>
    /// <param name="value">이펙트 값 (기본값: NaN)</param>
    public void ApplyEffect(EffectData effectData, float value = float.NaN)
    {
        if (effectData == null || state.activeEffects == null)
        {
            return;
        }

        EffectState effectState = new EffectState(effectData);

        if (!float.IsNaN(value))
        {
            effectState.value = value;
        }

        // 딕셔너리에 해당 StatType이 없으면 생성
        if (!state.activeEffects.ContainsKey(effectData.statType))
        {
            state.activeEffects[effectData.statType] = new List<EffectState>();
        }

        // 같은 ID의 이펙트가 있으면 갱신
        foreach (EffectState item in state.activeEffects[effectData.statType])
        {
            if (item.id == effectState.id)
            {
                item.remainingDays = effectState.durationDays;
                item.value = effectState.value;
                return;
            }
        }

        // 새 이펙트 추가
        state.activeEffects[effectData.statType].Add(effectState);
    }

    /// <summary>
    /// 직원 이펙트 제거
    /// </summary>
    /// <param name="effectData">이펙트 데이터</param>
    public void RemoveEffect(EffectData effectData)
    {
        if (effectData == null || state.activeEffects == null)
        {
            return;
        }

        // 해당 StatType의 리스트가 없으면 종료
        if (!state.activeEffects.ContainsKey(effectData.statType))
        {
            return;
        }

        List<EffectState> effectList = state.activeEffects[effectData.statType];
        for (int i = effectList.Count - 1; i >= 0; i--)
        {
            if (effectList[i].id == effectData.id)
            {
                effectList.RemoveAt(i);
                return;
            }
        }
    }

    /// <summary>
    /// 직원 이펙트 가져오기
    /// </summary>
    /// <param name="effectData">이펙트 데이터</param>
    /// <returns>EffectState 또는 null</returns>
    public EffectState GetEffect(EffectData effectData)
    {
        if (effectData == null || state.activeEffects == null)
        {
            return null;
        }

        // 해당 StatType의 리스트가 없으면 null 반환
        if (!state.activeEffects.ContainsKey(effectData.statType))
        {
            return null;
        }

        foreach (EffectState item in state.activeEffects[effectData.statType])
        {
            if (item.id == effectData.id)
            {
                return item;
            }
        }

        return null;
    }

    /// <summary>
    /// 특정 스탯 타입의 이펙트 목록을 가져옵니다.
    /// </summary>
    /// <param name="statType">스탯 타입</param>
    /// <returns>이펙트 목록</returns>
    public List<EffectState> GetEffectStatEffects(EffectStatType statType)
    {
        if (state.activeEffects == null)
        {
            return new List<EffectState>();
        }

        if (!state.activeEffects.ContainsKey(statType))
        {
            return new List<EffectState>();
        }

        return state.activeEffects[statType];
    }

    /// <summary>
    /// 직원의 모든 이펙트를 가져옵니다.
    /// </summary>
    /// <returns>모든 이펙트 목록</returns>
    public List<EffectState> GetAllEffects()
    {
        if (state.activeEffects == null)
        {
            return new List<EffectState>();
        }

        return state.activeEffects.Values.SelectMany(list => list).ToList();
    }

    /// <summary>
    /// 이펙트 지속시간 처리 (일 경과)
    /// </summary>
    /// <param name="date">경과할 일 수</param>
    public void ProcessDayPass(int date)
    {
        if (state.activeEffects == null)
        {
            return;
        }

        foreach (KeyValuePair<EffectStatType, List<EffectState>> statTypePair in state.activeEffects)
        {
            if (statTypePair.Value == null) continue;

            for (int i = statTypePair.Value.Count - 1; i >= 0; i--)
            {
                EffectState effectState = statTypePair.Value[i];
                if (effectState?.ProcessDayPass(date) == true)
                {
                    statTypePair.Value.RemoveAt(i);
                }
            }
        }
    }
}