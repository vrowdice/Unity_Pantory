using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/// <summary>
/// 게임 내 효과(버프/디버프)를 관리하는 핸들러
/// 기간제 효과의 시간 추적 및 스탯 계산을 담당합니다.
/// </summary>
public class EffectDataHandler
{
    private readonly GameDataManager _manager;
    
    // 활성화된 효과 목록 (StatType별로 분류하여 검색 속도 최적화)
    private readonly Dictionary<StatType, List<EffectState>> _activeEffects = new();

    public EffectDataHandler(GameDataManager manager)
    {
        _manager = manager;
    }

    // ========================================================================
    // 1. 효과 등록 및 해제
    // ========================================================================

    /// <summary>
    /// 새로운 효과를 적용합니다 (영구 또는 기간제).
    /// </summary>
    public void AddEffect(EffectData data)
    {
        if (data == null) return;

        if (!_activeEffects.ContainsKey(data.statType))
        {
            _activeEffects[data.statType] = new List<EffectState>();
        }

        var runtimeEffect = new EffectState(data);
        _activeEffects[data.statType].Add(runtimeEffect);

        Debug.Log($"[Effect] Added: {data.id} ({data.statType} {data.type} {data.value}) Duration: {(runtimeEffect.IsPermanent ? "Infinite" : data.durationDays + " days")}");
    }

    /// <summary>
    /// 특정 ID를 가진 효과를 즉시 제거합니다.
    /// </summary>
    public void RemoveEffectById(string effectId)
    {
        foreach (var list in _activeEffects.Values)
        {
            int removed = list.RemoveAll(e => e.Data.id == effectId);
            if (removed > 0)
            {
                Debug.Log($"[Effect] Removed: {effectId}");
            }
        }
    }

    /// <summary>
    /// 특정 StatType의 모든 효과를 제거합니다.
    /// </summary>
    public void RemoveEffectsByStatType(StatType statType)
    {
        if (_activeEffects.ContainsKey(statType))
        {
            _activeEffects[statType].Clear();
            Debug.Log($"[Effect] Removed all effects of type: {statType}");
        }
    }

    /// <summary>
    /// 모든 효과를 제거합니다.
    /// </summary>
    public void ClearAllEffects()
    {
        _activeEffects.Clear();
        Debug.Log("[Effect] All effects cleared.");
    }

    // ========================================================================
    // 2. 시간 업데이트 (GameDataManager Update에서 호출)
    // ========================================================================

    /// <summary>
    /// 기간제 효과의 시간을 감소시키고 만료된 효과를 제거합니다.
    /// </summary>
    /// <param name="deltaTime">지난 프레임 시간 (초)</param>
    public void UpdateEffectsTime(float deltaTime)
    {
        if (_manager?.Time == null) return;

        // TimeDataHandler에서 현재 시간 배속과 하루 길이를 가져옴
        float timeSpeed = _manager.Time.GetTimeSpeed();
        float realSecondsPerDay = _manager.Time.GetRealSecondsPerDay();

        if (realSecondsPerDay <= 0) return;

        // [중요] 실제 흐른 시간(초)을 게임 내 '일(Day)' 단위로 변환
        // 예: 1일이 2초고, 0.5초 지났다면 -> 0.25일 경과
        float daysPassed = (deltaTime / realSecondsPerDay) * timeSpeed;

        if (daysPassed <= 0) return;

        // 모든 효과 리스트 순회
        foreach (var key in _activeEffects.Keys.ToList())
        {
            var effectList = _activeEffects[key];
            
            // 역순 순회 (삭제 안전하게)
            for (int i = effectList.Count - 1; i >= 0; i--)
            {
                var effect = effectList[i];
                
                // 영구 효과는 건너뜀
                if (effect.IsPermanent) continue;

                // 시간 차감
                effect.RemainingDays -= daysPassed;

                // 만료 체크
                if (effect.RemainingDays <= 0)
                {
                    Debug.Log($"[Effect] Expired: {effect.Data.id}");
                    effectList.RemoveAt(i);
                }
            }
        }
    }

    // ========================================================================
    // 3. 스탯 계산 (핵심 로직)
    // ========================================================================

    /// <summary>
    /// 현재 활성화된 효과들을 반영하여 최종 스탯 값을 계산합니다.
    /// </summary>
    /// <param name="statType">계산할 스탯 종류</param>
    /// <param name="baseValue">기본 값 (직원 효율 등)</param>
    /// <param name="category">카테고리 필터 (선택 사항)</param>
    /// <returns>최종 적용된 값</returns>
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
            // 카테고리 필터링 (효과에 타겟 카테고리가 있는데, 요청한 카테고리와 다르면 패스)
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
                    percentAddSum += effect.Data.value; // 예: 0.1 (10%)
                    break;
                case ModifierType.PercentMult:
                    percentMultTotal *= effect.Data.value; // 예: 1.5 (1.5배)
                    break;
            }
        }

        // [공식 적용]
        // (기본값 + 고정합) * (1 + 합연산%) * 곱연산%
        // ※ 직원이 0명이라 baseValue가 0이어도, flatSum이 있으면 생산됨 (자동화)
        float result = (baseValue + flatSum) * (1f + percentAddSum) * percentMultTotal;
        
        return result;
    }

    // ========================================================================
    // 4. 조회 및 디버깅
    // ========================================================================

    /// <summary>
    /// 특정 StatType의 활성 효과 목록을 반환합니다.
    /// </summary>
    public List<EffectState> GetActiveEffects(StatType statType)
    {
        if (_activeEffects.TryGetValue(statType, out var effects))
        {
            return new List<EffectState>(effects);
        }
        return new List<EffectState>();
    }

    /// <summary>
    /// 모든 활성 효과를 반환합니다.
    /// </summary>
    public Dictionary<StatType, List<EffectState>> GetAllActiveEffects()
    {
        var result = new Dictionary<StatType, List<EffectState>>();
        foreach (var kvp in _activeEffects)
        {
            result[kvp.Key] = new List<EffectState>(kvp.Value);
        }
        return result;
    }

    /// <summary>
    /// 특정 효과가 활성화되어 있는지 확인합니다.
    /// </summary>
    public bool HasEffect(string effectId)
    {
        foreach (var list in _activeEffects.Values)
        {
            if (list.Any(e => e.Data.id == effectId))
            {
                return true;
            }
        }
        return false;
    }
}

