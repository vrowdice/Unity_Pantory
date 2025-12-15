using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class EffectDataHandler
{
    private GameDataManager _gameDataManager;
    private readonly Dictionary<StatType, List<EffectState>> _activeEffects = new();

    public EffectDataHandler(GameDataManager gameDataManager)
    {
        _gameDataManager = gameDataManager;
    }

    /// <summary>
    /// 이펙트가 있으면 갱신하고, 없으면 새로 생성하며, 값이 0이면 제거합니다.
    /// </summary>
    public void SetOrUpdateEffect(string id, StatType type, float value, string displayName, ModifierType modType, float duration = 0f)
    {
        // 1. 값이 사실상 0이면 이펙트 제거 (의미 없는 이펙트 정리)
        if (Mathf.Abs(value) <= 0.001f)
        {
            EffectState existing = GetEffect(type, id);
            if (existing != null) RemoveEffect(existing);
            return;
        }

        // 2. 이펙트 조회
        EffectState effect = GetEffect(type, id);

        if (effect != null)
        {
            // 3. 갱신 (값이 다를 때만)
            if (!Mathf.Approximately(effect.value, value))
            {
                effect.value = value;
                effect.displayName = displayName;
            }
        }
        else
        {
            // 4. 신규 생성
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

    public EffectState GetEffect(string effectId)
    {
        foreach (var effectList in _activeEffects.Values)
        {
            var effect = effectList.FirstOrDefault(e => e.id == effectId);
            if (effect != null) return effect;
        }
        return null;
    }

    public EffectState GetEffect(StatType statType, string effectId)
    {
        if (_activeEffects.TryGetValue(statType, out var list))
        {
            return list.FirstOrDefault(e => e.id == effectId);
        }
        return null;
    }

    public void UpdateEffect(EffectState effect)
    {
        if (effect == null) return;
        EffectState existingEffect = GetEffect(effect.statType, effect.id);

        if (existingEffect != null)
        {
            existingEffect.value = effect.value;
            existingEffect.durationDays = effect.durationDays;
            existingEffect.remainingDays = effect.remainingDays;
            existingEffect.displayName = effect.displayName;
        }
    }

    public void ProcessDayPass(int date)
    {
        var statTypes = new List<StatType>(_activeEffects.Keys);

        foreach (var statType in statTypes)
        {
            List<EffectState> effects = _activeEffects[statType];

            for (int i = effects.Count - 1; i >= 0; i--)
            {
                EffectState effect = effects[i];
                if (effect.IsPermanent) continue;

                effect.remainingDays -= 1;

                if (effect.remainingDays <= 0)
                {
                    effects.RemoveAt(i);
                }
            }
        }
    }

    public void ApplyEffect(EffectState effect)
    {
        if (effect == null) return;
        if (!_activeEffects.ContainsKey(effect.statType))
        {
            _activeEffects[effect.statType] = new List<EffectState>();
        }
        _activeEffects[effect.statType].Add(effect);
    }

    public void RemoveEffect(EffectState effect)
    {
        if (effect == null) return;
        if (_activeEffects.TryGetValue(effect.statType, out var list))
        {
            list.Remove(effect);
        }
    }

    public List<EffectState> GetActiveEffects(StatType statType)
    {
        if (_activeEffects.TryGetValue(statType, out var list))
        {
            return list;
        }
        return null;
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