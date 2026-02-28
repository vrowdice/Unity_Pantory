using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 이펙트(버프/디버프)를 한 곳에서 관리합니다.
/// 전역 이펙트(targetType별)와 인스턴스별 이펙트(직원/자원 등)를 모두 보관·조회합니다.
/// </summary>
public class EffectDataHandler : ITimeChangeHandler
{
    private readonly DataManager _dataManager;
    
    private readonly Dictionary<EffectTargetType, Dictionary<EffectStatType, List<EffectState>>> _effects = new();
    private readonly Dictionary<string, Dictionary<EffectStatType, List<EffectState>>> _instanceEffects = new();

    public EffectDataHandler(DataManager dataManager)
    {
        _dataManager = dataManager;

        foreach (EffectTargetType target in System.Enum.GetValues(typeof(EffectTargetType)))
        {
            _effects[target] = new Dictionary<EffectStatType, List<EffectState>>();
            foreach (EffectStatType stat in System.Enum.GetValues(typeof(EffectStatType)))
            {
                _effects[target][stat] = new List<EffectState>();
            }
        }
    }

    private static string GetInstanceKey(EffectTargetType targetType, string instanceId)
    {
        return $"{targetType}:{instanceId}";
    }

    public void HandleDayChanged()
    {
        ReducedEffectDuration(1);
    }

    /// <summary>
    /// 전역·인스턴스 모든 이펙트의 지속시간을 감소시키고 만료된 항목을 제거합니다.
    /// </summary>
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
                        statTypePair.Value.RemoveAt(i);
                }
            }
        }

        foreach (Dictionary<EffectStatType, List<EffectState>> statDict in _instanceEffects.Values)
        {
            foreach (KeyValuePair<EffectStatType, List<EffectState>> statTypePair in statDict)
            {
                List<EffectState> list = statTypePair.Value;
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    if (list[i].ProcessDayPass(date))
                        list.RemoveAt(i);
                }
            }
        }
    }

    /// <summary>
    /// 이펙트 적용. instanceId가 null이면 전역, 아니면 해당 인스턴스(예: 직원 유형)에 적용합니다.
    /// </summary>
    /// <param name="effectData">이펙트 데이터</param>
    /// <param name="value">값 (미지정 시 effectData.value 사용)</param>
    /// <param name="instanceId">인스턴스 식별자 (예: EmployeeType.ToString()). null이면 전역</param>
    public void ApplyEffect(EffectData effectData, float value = float.NaN, string instanceId = null)
    {
        EffectState effectState = new EffectState(effectData);
        if (!float.IsNaN(value))
            effectState.value = value;

        if (string.IsNullOrEmpty(instanceId))
        {
            ApplyToGlobal(effectData, effectState);
            return;
        }

        string key = GetInstanceKey(effectData.targetType, instanceId);
        if (!_instanceEffects.ContainsKey(key))
        {
            _instanceEffects[key] = new Dictionary<EffectStatType, List<EffectState>>();
            foreach (EffectStatType stat in System.Enum.GetValues(typeof(EffectStatType)))
                _instanceEffects[key][stat] = new List<EffectState>();
        }
        if (!_instanceEffects[key].ContainsKey(effectData.statType))
            _instanceEffects[key][effectData.statType] = new List<EffectState>();

        List<EffectState> list = _instanceEffects[key][effectData.statType];
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i].id == effectState.id)
            {
                list[i].remainingDays = effectState.durationDays;
                list[i].value = effectState.value;
                return;
            }
        }
        list.Add(effectState);
    }

    private void ApplyToGlobal(EffectData effectData, EffectState effectState)
    {
        if (!_effects.ContainsKey(effectData.targetType))
            _effects[effectData.targetType] = new Dictionary<EffectStatType, List<EffectState>>();
        if (!_effects[effectData.targetType].ContainsKey(effectData.statType))
            _effects[effectData.targetType][effectData.statType] = new List<EffectState>();

        List<EffectState> list = _effects[effectData.targetType][effectData.statType];
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i].id == effectState.id)
            {
                list[i].remainingDays = effectState.durationDays;
                list[i].value = effectState.value;
                return;
            }
        }
        list.Add(effectState);
    }

    /// <summary>
    /// 이펙트 제거. instanceId가 null이면 전역에서, 아니면 해당 인스턴스에서 제거합니다.
    /// </summary>
    public void RemoveEffect(EffectData effectData, string instanceId = null)
    {
        if (string.IsNullOrEmpty(instanceId))
        {
            RemoveFromGlobal(effectData);
            return;
        }
        string key = GetInstanceKey(effectData.targetType, instanceId);
        if (!_instanceEffects.ContainsKey(key) || !_instanceEffects[key].ContainsKey(effectData.statType))
            return;
        List<EffectState> list = _instanceEffects[key][effectData.statType];
        for (int i = list.Count - 1; i >= 0; i--)
        {
            if (list[i].id == effectData.id)
            {
                list.RemoveAt(i);
                return;
            }
        }
    }

    private void RemoveFromGlobal(EffectData effectData)
    {
        if (!_effects.ContainsKey(effectData.targetType) || !_effects[effectData.targetType].ContainsKey(effectData.statType))
            return;
        List<EffectState> list = _effects[effectData.targetType][effectData.statType];
        for (int i = list.Count - 1; i >= 0; i--)
        {
            if (list[i].id == effectData.id)
            {
                list.RemoveAt(i);
                return;
            }
        }
    }

    /// <summary>
    /// 특정 이펙트 조회. instanceId가 null이면 전역에서만, 아니면 전역+인스턴스에서 먼저 인스턴스를 검사합니다.
    /// </summary>
    public EffectState GetEffect(EffectData effectData, string instanceId = null)
    {
        if (!string.IsNullOrEmpty(instanceId))
        {
            string key = GetInstanceKey(effectData.targetType, instanceId);
            if (_instanceEffects.TryGetValue(key, out Dictionary<EffectStatType, List<EffectState>> statDict) && statDict.TryGetValue(effectData.statType, out List<EffectState> list))
            {
                foreach (EffectState item in list)
                {
                    if (item.id == effectData.id) return item;
                }
            }
        }
        if (!_effects.ContainsKey(effectData.targetType) || !_effects[effectData.targetType].ContainsKey(effectData.statType))
            return null;
        foreach (EffectState item in _effects[effectData.targetType][effectData.statType])
        {
            if (item.id == effectData.id) return item;
        }
        return null;
    }

    /// <summary>
    /// 해당 스탯 타입의 이펙트 목록. instanceId가 null이면 전역만, 아니면 전역+해당 인스턴스 목록을 합쳐 반환합니다.
    /// </summary>
    public List<EffectState> GetEffectStatEffects(EffectTargetType targetType, EffectStatType statType, string instanceId = null)
    {
        List<EffectState> result = new List<EffectState>();
        if (_effects.ContainsKey(targetType) && _effects[targetType].ContainsKey(statType))
            result.AddRange(_effects[targetType][statType]);
        if (!string.IsNullOrEmpty(instanceId))
        {
            string key = GetInstanceKey(targetType, instanceId);
            if (_instanceEffects.TryGetValue(key, out Dictionary<EffectStatType, List<EffectState>> statDict) && statDict.TryGetValue(statType, out List<EffectState> list))
                result.AddRange(list);
        }
        return result;
    }

    /// <summary>
    /// targetType에 해당하는 전체 이펙트. instanceId가 null이면 전역만, 아니면 전역+해당 인스턴스를 합쳐 반환합니다.
    /// </summary>
    public List<EffectState> GetAllEffects(EffectTargetType effectTargetType, string instanceId = null)
    {
        List<EffectState> result = new List<EffectState>();
        if (_effects.ContainsKey(effectTargetType))
            result.AddRange(_effects[effectTargetType].Values.SelectMany(list => list));
        if (!string.IsNullOrEmpty(instanceId))
        {
            string key = GetInstanceKey(effectTargetType, instanceId);
            if (_instanceEffects.TryGetValue(key, out Dictionary<EffectStatType, List<EffectState>> statDict))
                result.AddRange(statDict.Values.SelectMany(list => list));
        }
        return result;
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