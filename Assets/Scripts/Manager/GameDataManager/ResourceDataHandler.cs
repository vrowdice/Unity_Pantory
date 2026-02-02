using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 게임 자원을 관리하는 서비스 클래스
/// ResourceData ScriptableObject를 기반으로 자원을 동적으로 관리합니다.
/// </summary>
public class ResourceDataHandler
{
    private DataManager _dataManager;

    private InitialResourceData _initialResourceData = null;
    private Dictionary<string, ResourceEntry> _resourceDic;

    public event Action OnResourceChanged;

    /// <summary>
    /// ResourceService 생성자
    /// </summary>
    public ResourceDataHandler(DataManager gameDataManager, List<ResourceData> resourceDataList, InitialResourceData initialResourceData)
    {
        _dataManager = gameDataManager;
        _resourceDic = new Dictionary<string, ResourceEntry>();
        _initialResourceData = initialResourceData;

        if (resourceDataList != null && resourceDataList.Count > 0)
        {
            foreach (var data in resourceDataList)
            {
                if (data == null || string.IsNullOrEmpty(data.id)) continue;
                if (_resourceDic.ContainsKey(data.id))
                {
                    Debug.LogWarning($"[ResourceService] Resource already registered: {data.id}");
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
        long credit = 0;

        foreach (ResourceEntry entry in _resourceDic.Values)
        {
            credit += entry.state.threadDeltaCount * entry.state.currentValue;
            credit += entry.state.marketDeltaCount * entry.state.currentValue;
        }

        return credit;
    }

    public void ModifyThreadDelta(string resourceId, int count)
    {
        ResourceEntry resourceEntry = null;
        _resourceDic.TryGetValue(resourceId, out resourceEntry);

        if (resourceEntry != null)
        {
            resourceEntry.ModifyThreadDelta(count);
        }
    }

    public void ModifyMarketDelta(string resourceId, int count)
    {
        ResourceEntry resourceEntry = null;
        _resourceDic.TryGetValue(resourceId, out resourceEntry);

        if (resourceEntry != null)
        {
            resourceEntry.ModifyMarketDelta(count);
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

    private void ApplyCurrentEventValue()
    {
        foreach (ResourceEntry entry in _resourceDic.Values)
        {
            entry.state.currentEventValue = entry.data.baseValue;
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
    /// 모든 이벤트 구독을 초기화합니다.
    /// </summary>
    public void ClearAllSubscriptions()
    {
        OnResourceChanged = null;
    }
}
