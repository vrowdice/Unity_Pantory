using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class ResourceEntry
{
    public ResourceData data;   // 자원의 정적 데이터 (ScriptableObject)
    public ResourceState state; // 자원의 동적 상태
    
    public ResourceEntry(ResourceData data)
    {
        this.data = data;

        state = new ResourceState();
        state.currentEventValue = data.baseValue;
        state.count = data.initialAmount;
    }

    /// <summary>
    /// 가격을 기록하고 히스토리에 추가합니다.
    /// </summary>
    public void RecordPrice(float price)
    {
        if (state._priceHistory == null)
        {
            state._priceHistory = new List<float>(ResourceState.PriceHistoryCapacity);
        }

        float clampedPrice = Mathf.Max(0.01f, price);

        state._priceHistory.Add(clampedPrice);

        if (state._priceHistory.Count > ResourceState.PriceHistoryCapacity)
        {
            state._priceHistory.RemoveAt(0);
        }
    }

    /// <summary>
    /// 자원 수량을 안전하게 수정합니다. (0 이하로 내려가지 않음)
    /// </summary>
    /// <param name="amount">변경할 수량 (음수 가능)</param>
    /// <returns>실제로 변경된 수량</returns>
    public int ModifyCount(int amount)
    {
        int oldCount = state.count;
        state.count = Mathf.Max(0, state.count + amount);
        return state.count - oldCount;
    }

    /// <summary>
    /// Thread Delta를 안전하게 수정합니다.
    /// </summary>
    /// <param name="amount">변경할 수량</param>
    public void ModifyThreadDelta(int amount)
    {
        state.threadDeltaCount += amount;
    }

    /// <summary>
    /// Market Delta를 안전하게 수정합니다.
    /// </summary>
    /// <param name="amount">변경할 수량</param>
    public void ModifyMarketDelta(int amount)
    {
        state.marketDeltaCount += amount;
    }

    /// <summary>
    /// 자원 수량을 직접 설정합니다. (0 이하로 내려가지 않음)
    /// </summary>
    /// <param name="newCount">설정할 수량</param>
    public void SetCount(int newCount)
    {
        state.count = Mathf.Max(0, newCount);
    }

    /// <summary>
    /// 자원 이펙트 적용
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
    /// 자원 이펙트 제거
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
    /// 자원 이펙트 가져오기
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
    /// 자원의 모든 이펙트를 가져옵니다.
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
