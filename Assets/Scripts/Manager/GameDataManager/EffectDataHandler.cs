using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/// <summary>
/// 게임 내 효과(버프/디버프)의 상태를 관리하고 스탯 연산을 수행하는 핸들러 클래스입니다.
/// </summary>
public class EffectDataHandler
{
    private GameDataManager _gameDataManager;
    private readonly Dictionary<StatType, List<EffectState>> _activeEffects = new();

    public EffectDataHandler(GameDataManager gameDataManager) 
    {
        _gameDataManager = gameDataManager;
    }

    /// <summary>
    /// 새로운 효과를 등록합니다. (영구 또는 기간제)
    /// </summary>
    /// <param name="data">등록할 효과 데이터</param>
    public void AddEffect(EffectData data)
    {
        if (data == null) return;

        if (!_activeEffects.ContainsKey(data.statType))
        {
            _activeEffects[data.statType] = new List<EffectState>();
        }

        var runtimeEffect = new EffectState(data);
        _activeEffects[data.statType].Add(runtimeEffect);

        string durationInfo = runtimeEffect.IsPermanent ? "Permanent" : $"{data.durationDays} Days";
        Debug.Log($"[Effect] Added: {data.id} ({data.statType} / {data.type} / {data.value}) [{durationInfo}]");
    }

    /// <summary>
    /// 지정된 ID를 가진 효과를 즉시 제거합니다.
    /// </summary>
    /// <param name="effectId">제거할 효과의 ID</param>
    public void RemoveEffectById(string effectId)
    {
        foreach (var list in _activeEffects.Values)
        {
            int removedCount = list.RemoveAll(e => e.Data.id == effectId);
            if (removedCount > 0)
            {
                Debug.Log($"[Effect] Removed: {effectId}");
            }
        }
    }

    /// <summary>
    /// 지정된 스탯 타입에 해당하는 모든 효과를 제거합니다.
    /// </summary>
    /// <param name="statType">제거할 스탯 타입</param>
    public void RemoveEffectsByStatType(StatType statType)
    {
        if (_activeEffects.ContainsKey(statType))
        {
            _activeEffects[statType].Clear();
            Debug.Log($"[Effect] Cleared all effects for Stat: {statType}");
        }
    }

    /// <summary>
    /// 현재 적용 중인 모든 효과를 제거합니다.
    /// </summary>
    public void ClearAllEffects()
    {
        _activeEffects.Clear();
        Debug.Log("[Effect] All effects cleared.");
    }

    /// <summary>
    /// 하루가 지날 때 호출되어 기간제 효과의 남은 일수를 차감하고, 만료된 효과를 제거합니다.
    /// </summary>
    /// <param name="daysPassed">경과한 일수 (기본값: 1)</param>
    public void ProcessDayPass(int daysPassed = 1)
    {
        if (daysPassed <= 0) return;

        foreach (var key in _activeEffects.Keys.ToList())
        {
            var effectList = _activeEffects[key];

            for (int i = effectList.Count - 1; i >= 0; i--)
            {
                var effect = effectList[i];

                if (effect.IsPermanent) continue;

                effect.RemainingDays -= daysPassed;

                if (effect.RemainingDays <= 0)
                {
                    Debug.Log($"[Effect] Expired: {effect.Data.id}");
                    effectList.RemoveAt(i);
                }
            }
        }
    }

    /// <summary>
    /// 활성화된 효과들을 반영하여 최종 스탯 값을 계산합니다.
    /// 계산 공식: (기본값 + 고정합) * (1 + 합연산%) * 곱연산%
    /// </summary>
    /// <param name="statType">계산할 스탯의 종류</param>
    /// <param name="baseValue">기본 값</param>
    /// <param name="category">필터링할 카테고리 (옵션)</param>
    /// <returns>모든 효과가 적용된 최종 값</returns>
    public float CalculateStat(StatType statType, float baseValue, string category = null)
    {
        if (!_activeEffects.TryGetValue(statType, out var effects) || effects.Count == 0)
        {
            return baseValue;
        }

        float flatSum = 0f;
        float percentAddSum = 0f;
        float percentMultTotal = 1f;

        foreach (var effect in effects)
        {
            if (!string.IsNullOrEmpty(effect.Data.targetCategory) &&
                effect.Data.targetCategory != category)
            {
                continue;
            }

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
    /// 특정 스탯 타입에 적용 중인 모든 효과 목록을 반환합니다.
    /// </summary>
    /// <param name="statType">조회할 스탯 타입</param>
    /// <returns>EffectState 리스트 (없으면 빈 리스트 반환)</returns>
    public List<EffectState> GetActiveEffects(StatType statType)
    {
        if (_activeEffects.TryGetValue(statType, out var effects))
        {
            return new List<EffectState>(effects);
        }
        return new List<EffectState>();
    }
}