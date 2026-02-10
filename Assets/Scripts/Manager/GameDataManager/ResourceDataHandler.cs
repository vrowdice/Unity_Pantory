using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 게임 자원을 관리하는 서비스 클래스
/// ResourceData ScriptableObject를 기반으로 자원을 동적으로 관리합니다.
/// </summary>
public class ResourceDataHandler : IDataHandlerEvents, ITimeChangeHandler
{
    private readonly DataManager _dataManager;
    private readonly InitialResourceData _initialResourceData;
    private Dictionary<string, ResourceEntry> _resourceDic;

    public event Action OnResourceChanged;

    /// <summary>
    /// ResourceService 생성자
    /// </summary>
    public ResourceDataHandler(DataManager dataManager, List<ResourceData> resourceDataList, InitialResourceData initialResourceData)
    {
        _dataManager = dataManager;
        _initialResourceData = initialResourceData;
        _resourceDic = new Dictionary<string, ResourceEntry>();

        if (resourceDataList != null && resourceDataList.Count > 0)
        {
            foreach (ResourceData data in resourceDataList)
            {
                if (data == null || string.IsNullOrEmpty(data.id)) continue;
                if (_resourceDic.ContainsKey(data.id))
                {
                    Debug.LogWarning($"[ResourceDataHandler] Resource already registered: {data.id}");
                    continue;
                }
                _resourceDic[data.id] = new ResourceEntry(data, _initialResourceData != null ? _initialResourceData.priceHistoryCapacity : 60);
            }
        }
    }

    public ResourceEntry GetResourceEntry(string resourceId)
    {
        if (_resourceDic.TryGetValue(resourceId, out var entry))
        {
            return entry;
        }

        return null;
    }

    public Dictionary<string, ResourceEntry> GetAllResources()
    {
        return new Dictionary<string, ResourceEntry>(_resourceDic);
    }

    public long CalculateResourceDeltaChangeCredit()
    {
        long totalCreditChange = 0;

        foreach (ResourceEntry entry in _resourceDic.Values)
        {
            long basePrice = entry.state.currentValue;
            int marketDelta = entry.state.marketDeltaCount;
            int threadDelta = entry.state.threadDeltaCount;

            totalCreditChange += (long)threadDelta * basePrice;

            if (marketDelta > 0)
            {
                totalCreditChange += (long)marketDelta * GetSalePrice(entry.data.id);
            }
            else if (marketDelta < 0)
            {
                totalCreditChange += (long)marketDelta * GetPurchasePrice(entry.data.id);
            }
        }

        return totalCreditChange;
    }

    public void ModifyThreadDelta(string resourceId, int count)
    {
        ResourceEntry resourceEntry = null;
        _resourceDic.TryGetValue(resourceId, out resourceEntry);

        if (resourceEntry != null)
        {
            resourceEntry.state.threadDeltaCount += count;
        }
    }

    public void ModifyMarketDelta(string resourceId, int count)
    {
        ResourceEntry resourceEntry = null;
        _resourceDic.TryGetValue(resourceId, out resourceEntry);

        if (resourceEntry != null)
        {
            resourceEntry.state.marketDeltaCount += count;
        }
    }

    /// <summary>
    /// 누적된 deltaCount를 실제 자원 수량에 반영하고 초기화합니다.
    /// </summary>
    public void HandleDayChanged()
    {
        ApplyDeltaChange();

        ApplyCurrentEventValue();
        ApplyValueChange();

        OnResourceChanged?.Invoke();
    }

    private void ApplyDeltaChange()
    {
        foreach (ResourceEntry entry in _resourceDic.Values)
        {
            int changeCount = 0;
            changeCount += entry.state.threadDeltaCount;
            changeCount += entry.state.marketDeltaCount;

            entry.state.currnetChangeCount = changeCount;
            entry.ModifyCount(changeCount);

            entry.state.threadDeltaCount = 0;
        }
    }

    /// <summary>
    /// 기본 가격에 뉴스 이펙트를 적용하여 currentEventValue를 설정합니다.
    /// </summary>
    private void ApplyCurrentEventValue()
    {
        foreach (ResourceEntry entry in _resourceDic.Values)
        {
            long baseValue = entry.data.baseValue;
            string instanceId = entry.data.id;
            List<EffectState> priceEffects = _dataManager.Effect.GetEffectStatEffects(EffectTargetType.Resource, EffectStatType.Resource_Price, instanceId);
            float adjustedValue = EffectUtils.ComputeStatFromEffects(baseValue, priceEffects);
            entry.state.currentEventValue = (long)Mathf.Max(1, adjustedValue);
        }
    }

    private void ApplyValueChange()
    {
        foreach (ResourceEntry entry in _resourceDic.Values)
        {
            ResourceState resourceState = entry.state;

            long previousValue = resourceState.currentValue;

            float increaseProbability = _initialResourceData.baseIncreaseProbability;
            float offset = (float)(resourceState.currentEventValue - resourceState.currentValue) / resourceState.currentEventValue;
            increaseProbability += offset * _initialResourceData.probabilityOffsetMultiplier;
            increaseProbability = Math.Clamp(increaseProbability, _initialResourceData.minIncreaseProbability, _initialResourceData.maxIncreaseProbability);

            float volatility = _initialResourceData.volatilityMultiplier;
            float changeAmount = resourceState.currentEventValue * volatility * UnityEngine.Random.Range(_initialResourceData.changeAmountRandomMin, _initialResourceData.changeAmountRandomMax);

            long finalChangeAmount = Math.Max(_initialResourceData.minChangeAmount, (long)Math.Round(changeAmount));

            if (UnityEngine.Random.value < increaseProbability)
            {
                resourceState.currentValue += finalChangeAmount;
            }
            else
            {
                resourceState.currentValue -= finalChangeAmount;
            }

            float maxLimit = resourceState.currentEventValue * _initialResourceData.maxChangePriceMultiplier;
            float minLimit = resourceState.currentEventValue * (1f / _initialResourceData.maxChangePriceMultiplier);
            resourceState.currentValue = Math.Clamp(resourceState.currentValue, (long)minLimit, (long)maxLimit);

            resourceState.currentChangeValue = resourceState.currentValue - previousValue;

            entry.RecordPrice(resourceState.currentValue);
        }
    }

    /// <summary>
    /// 플레이어가 자원을 살 때의 최종 가격을 반환합니다. (기준가 + 수수료)
    /// </summary>
    public long GetPurchasePrice(string resourceId)
    {
        var entry = GetResourceEntry(resourceId);
        if (entry == null) return 0;

        float feeMultiplier = 1f + _initialResourceData.transactionFee;
        return (long)Math.Ceiling(entry.state.currentValue * feeMultiplier);
    }

    /// <summary>
    /// 플레이어가 자원을 팔 때의 최종 가격을 반환합니다. (기준가 - 수수료)
    /// </summary>
    public long GetSalePrice(string resourceId)
    {
        var entry = GetResourceEntry(resourceId);
        if (entry == null) return 0;

        float feeMultiplier = 1f - _initialResourceData.transactionFee;
        return (long)Math.Floor(entry.state.currentValue * feeMultiplier);
    }

    /// <summary>
    /// 모든 이벤트 구독을 초기화합니다.
    /// </summary>
    public void ClearAllSubscriptions()
    {
        OnResourceChanged = null;
    }
}
