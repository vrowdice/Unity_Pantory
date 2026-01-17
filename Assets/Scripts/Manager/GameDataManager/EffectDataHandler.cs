using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

/// <summary>
/// 이펙트(버프/디버프)를 관리하는 관리자입니다.
/// 1. 월드 전체 2. 직원 전체 공통 3. 직원 개인별 효과를 각각 관리합니다.
/// </summary>
public class EffectDataHandler
{
    private readonly DataManager _dataManager;

    /// <summary>
    /// 전역 이펙트
    /// </summary>
    private readonly Dictionary<EffectTargetType, Dictionary<EffectStatType, List<EffectState>>> _effects = new();

    public Dictionary<EffectTargetType, Dictionary<EffectStatType, List<EffectState>>> Effects => _effects;

    public EffectDataHandler(DataManager gameDataManager) 
    {
        _dataManager = gameDataManager;

        foreach (EffectTargetType target in System.Enum.GetValues(typeof(EffectTargetType)))
        {
            _effects[target] = new Dictionary<EffectStatType, List<EffectState>>();
            foreach (EffectStatType stat in System.Enum.GetValues(typeof(EffectStatType)))
            {
                _effects[target][stat] = new List<EffectState>();
            }
        }
    }

    public void HandleDayChanged()
    {
        ReducedEffectDuration(1);
    }

    /// <summary>
    /// 이펙트 지속시간 감소 
    /// </summary>
    /// <param name="date">감소할 일</param>
    public void ReducedEffectDuration(int date)
    {
        foreach (KeyValuePair<EffectTargetType, Dictionary<EffectStatType, List<EffectState>>> targetTypePair in _effects)
        {
            foreach (KeyValuePair<EffectStatType, List<EffectState>> statTypePair in targetTypePair.Value)
            {
                for (int i = statTypePair.Value.Count - 1; i >= 0; i--)
                {
                    EffectState effectState = statTypePair.Value[i];
                    if (effectState.ProcessDayPass(date))
                    {
                        statTypePair.Value.RemoveAt(i);
                    }
                }
            }
        }

        foreach (EmployeeEntry employeeEntry in _dataManager.Employee.GetAllEmployees().Values)
        {
            employeeEntry.ProcessDayPass(date);
        }
    }

    /// <summary>
    /// 이펙트 적용
    /// </summary>
    /// <param name="effectData">데이터</param>
    public void ApplyEffect(EffectData effectData, float value = float.NaN)
    {
        EffectState effectState = new EffectState(effectData);

        if (!float.IsNaN(value))
        {
            effectState.value = value;
        }

        // 딕셔너리 초기화 확인
        if (!_effects.ContainsKey(effectData.targetType))
        {
            _effects[effectData.targetType] = new Dictionary<EffectStatType, List<EffectState>>();
        }
        if(!_effects[effectData.targetType].ContainsKey(effectData.statType))
        {
            _effects[effectData.targetType][effectData.statType] = new List<EffectState>();
        }

        // 같은 ID의 이펙트가 있으면 갱신
        foreach (EffectState item in _effects[effectState.targetType][effectState.statType])
        {
            if(item.id == effectState.id)
            {
                item.remainingDays = effectState.durationDays;
                item.value = effectState.value;
                return;
            }
        }

        // 새 이펙트 추가
        _effects[effectData.targetType][effectData.statType].Add(effectState);
    }

    /// <summary>
    /// 이펙트 제거
    /// </summary>
    /// <param name="effectData">이펙트 데이터</param>
    public void RemoveEffect(EffectData effectData)
    {
        // 딕셔너리에 해당 키가 없으면 종료
        if(!_effects.ContainsKey(effectData.targetType))
        {
            return;
        }
        if(!_effects[effectData.targetType].ContainsKey(effectData.statType))
        {
            return;
        }

        List<EffectState> effectList = _effects[effectData.targetType][effectData.statType];
        for(int i = effectList.Count - 1; i >= 0; i--)
        {
            if(effectList[i].id == effectData.id)
            {
                effectList.RemoveAt(i);
                return;
            }
        }
    }

    public EffectState GetEffect(EffectData effectData)
    {
        // 딕셔너리에 해당 키가 없으면 null 반환
        if(!_effects.ContainsKey(effectData.targetType))
        {
            return null;
        }
        if(!_effects[effectData.targetType].ContainsKey(effectData.statType))
        {
            return null;
        }

        foreach (EffectState item in _effects[effectData.targetType][effectData.statType])
        {
            if (item.id == effectData.id)
            {
                return item;
            }
        }

        return null;
    }

    public List<EffectState> GetEffectStatEffects(EffectTargetType targetType, EffectStatType statType)
    {
        if(!_effects.ContainsKey(targetType))
        {
            return new List<EffectState>();
        }
        if(!_effects[targetType].ContainsKey(statType))
        {
            return new List<EffectState>();
        }

        return _effects[targetType][statType];
    }

    public List<EffectState> GetAllEffects(EffectTargetType effectTargetType)
    {
        if (!_effects.ContainsKey(effectTargetType))
        {
            return new List<EffectState>();
        }

        return _effects[effectTargetType].Values.SelectMany(list => list).ToList();
    }

    /// <summary>
    /// 이펙트 값을 포맷팅합니다.
    /// </summary>
    /// <param name="value">이펙트 값</param>
    /// <param name="modifierType">연산 방식</param>
    /// <returns>포맷팅된 문자열</returns>
    public string FormatEffectValue(float value, ModifierType modifierType)
    {
        switch (modifierType)
        {
            case ModifierType.Flat:
                return value >= 0 ? $"+{value:F1}" : $"{value:F1}";

            case ModifierType.PercentAdd:
                float percentAdd = value * 100f;
                return percentAdd >= 0 ? $"+{percentAdd:F1}%" : $"{percentAdd:F1}%";

            case ModifierType.PercentMult:
                return $"x{value:F2}";

            default:
                return value.ToString("F1");
        }
    }
}