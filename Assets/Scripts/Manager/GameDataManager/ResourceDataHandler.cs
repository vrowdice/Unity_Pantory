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
    private Dictionary<string, ResourceEntry> _resources;

    public event Action OnResourceChanged;

    /// <summary>
    /// ResourceService 생성자
    /// </summary>
    public ResourceDataHandler(DataManager gameDataManager, List<ResourceData> resourceDataList, InitialResourceData initialResourceData)
    {
        _dataManager = gameDataManager;
        _resources = new Dictionary<string, ResourceEntry>();
        _initialResourceData = initialResourceData;

        if (resourceDataList != null && resourceDataList.Count > 0)
        {
            foreach (var data in resourceDataList)
            {
                if (data == null || string.IsNullOrEmpty(data.id)) continue;
                if (_resources.ContainsKey(data.id))
                {
                    Debug.LogWarning($"[ResourceService] Resource already registered: {data.id}");
                    continue;
                }
                _resources[data.id] = new ResourceEntry(data);
            }
        }
    }

    public ResourceEntry GetResourceEntry(string resourceId)
    {
        if (_resources.TryGetValue(resourceId, out var entry))
        {
            return entry;
        }

        return null;
    }

    public Dictionary<string, ResourceEntry> GetAllResources()
    {
        return new Dictionary<string, ResourceEntry>(_resources);
    }

    public long CalculateResourceDeltaChangeCredit()
    {
        long credit = 0;

        foreach (ResourceEntry entry in _resources.Values)
        {
            credit += entry.state.threadDeltaCount * entry.state.currentValue;
            credit += entry.state.marketDeltaCount * entry.state.currentValue;
        }

        return credit;
    }

    public void ModifyThreadDelta(string resourceId, int count)
    {
        ResourceEntry resourceEntry = null;
        _resources.TryGetValue(resourceId, out resourceEntry);

        if (resourceEntry != null)
        {
            resourceEntry.state.threadDeltaCount += count;
        }
    }

    public void ModifyMarketDelta(string resourceId, int count)
    {
        ResourceEntry resourceEntry = null;
        _resources.TryGetValue(resourceId, out resourceEntry);

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
        ApplyValueChange();

        OnResourceChanged?.Invoke();
    }

    private void ApplyDeltaChange()
    {
        foreach (ResourceEntry entry in _resources.Values)
        {
            entry.state.count += entry.state.threadDeltaCount;
            entry.state.count += entry.state.marketDeltaCount;

            entry.state.threadDeltaCount = 0;
        }
    }

    private void ApplyValueChange()
    {
        foreach (ResourceEntry entry in _resources.Values)
        {
            ResourceData resourceData = entry.data;
            ResourceState resourceState = entry.state;

            float difference = Math.Abs(resourceData.baseValue - resourceState.currentValue);
            float differenceMul = Math.Clamp(difference / resourceData.baseValue, 0f, 1f);

            float changeAmount = resourceData.baseValue * _initialResourceData.volatilityMultiplier;

            float maxLimit = resourceData.baseValue * _initialResourceData.maxChangePriceMultiplier;
            float minLimit = resourceData.baseValue * (1f / _initialResourceData.maxChangePriceMultiplier);

            if (UnityEngine.Random.value < differenceMul)
            {
                //현재가가 최대치보다 작을 때만 상승
                if (resourceState.currentValue < maxLimit)
                {
                    resourceState.currentValue += (long)changeAmount;
                }
            }
            else
            {
                //현재가가 최소치보다 클 때만 하락
                if (resourceState.currentValue > minLimit)
                {
                    resourceState.currentValue -= (long)changeAmount;
                }
            }

            resourceState.currentValue = Math.Clamp(resourceState.currentValue, (long)minLimit, (long)maxLimit);
            resourceState.RecordPrice(resourceState.currentValue);
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
