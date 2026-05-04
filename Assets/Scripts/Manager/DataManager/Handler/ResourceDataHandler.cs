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
    public long TotalCreditChange { get; private set; }

    /// <summary>한 프레임에 <see cref="ModifyResourceCount"/>가 여러 번 불려도 UI는 한 번만 갱신합니다.</summary>
    private bool _pendingResourceChangedNotify;

    /// <summary>
    /// ResourceService 생성자
    /// </summary>
    public ResourceDataHandler(DataManager dataManager, List<ResourceData> resourceDataList, InitialResourceData initialResourceData)
    {
        _dataManager = dataManager;
        _initialResourceData = initialResourceData;
        _resourceDic = new Dictionary<string, ResourceEntry>();

        if (resourceDataList == null) return;

        foreach (ResourceData data in resourceDataList)
        {
            if (data == null || string.IsNullOrEmpty(data.id)) continue;
            if (_resourceDic.ContainsKey(data.id))
            {
                Debug.LogWarning($"[ResourceDataHandler] Resource already registered: {data.id}");
                continue;
            }
            _resourceDic[data.id] = new ResourceEntry(data, _initialResourceData.priceHistoryCapacity);
        }
    }

    public ResourceEntry GetResourceEntry(string resourceId)
    {
        return _resourceDic.TryGetValue(resourceId, out ResourceEntry entry) ? entry : null;
    }

    public Dictionary<string, ResourceEntry> GetAllResources()
    {
        return new Dictionary<string, ResourceEntry>(_resourceDic);
    }

    public void ModifyThreadDelta(string resourceId, int count)
    {
        if (!_resourceDic.TryGetValue(resourceId, out ResourceEntry resourceEntry)) return;
        resourceEntry.state.threadDeltaCount += count;
    }

    public void ModifyMarketDelta(string resourceId, int count)
    {
        if (!_resourceDic.TryGetValue(resourceId, out ResourceEntry resourceEntry)) return;
        resourceEntry.state.marketDeltaCount += count;
    }

    public bool ModifyResourceCount(string resourceId, int count)
    {
        if (!_resourceDic.TryGetValue(resourceId, out ResourceEntry resourceEntry)) return false;
        if (!resourceEntry.ModifyCount(count)) return false;
        _pendingResourceChangedNotify = true;
        return true;
    }

    /// <summary>
    /// 누적된 deltaCount를 실제 자원 수량에 반영하고 초기화합니다.
    /// </summary>
    public void HandleDayChanged()
    {
        ApplyDeltaChange();
        ApplyCurrentEventValue();
        ApplyValueChange();

        _pendingResourceChangedNotify = true;
    }

    /// <summary>
    /// <see cref="DataManager"/>의 Update/LateUpdate에서 호출합니다.
    /// 같은 프레임 안에서 자원 변경 알림을 한 번만 보냅니다.
    /// </summary>
    public void FlushPendingResourceChangedNotify()
    {
        if (!_pendingResourceChangedNotify)
            return;

        _pendingResourceChangedNotify = false;
        OnResourceChanged?.Invoke();
    }

    private void ApplyDeltaChange()
    {
        long totalCreditChange = 0;

        foreach (ResourceEntry entry in _resourceDic.Values)
        {
            int threadDelta = entry.state.threadDeltaCount;
            int marketDelta = entry.state.marketDeltaCount;

            int toBuy = 0;
            int actualSell = 0;
            int changeCount;

            if (threadDelta < 0)
            {
                int consumAmount = -threadDelta;
                int fromInventory = Math.Min(consumAmount, entry.state.count);
                toBuy = consumAmount - fromInventory;
                int availableAfterConsumption = entry.state.count - fromInventory + toBuy;
                actualSell = marketDelta < 0 ? Math.Min(-marketDelta, availableAfterConsumption) : 0;
                changeCount = threadDelta + toBuy + (marketDelta > 0 ? marketDelta : -actualSell);
            }
            else
            {
                int availableAfterProduction = entry.state.count + threadDelta;
                actualSell = marketDelta < 0 ? Math.Min(-marketDelta, availableAfterProduction) : 0;
                changeCount = threadDelta + (marketDelta > 0 ? marketDelta : -actualSell);
            }
            
            if (toBuy > 0) totalCreditChange -= (long)toBuy * GetPurchasePrice(entry.data.id);
            if (marketDelta > 0) totalCreditChange -= (long)marketDelta * GetPurchasePrice(entry.data.id);
            else if (actualSell > 0) totalCreditChange += (long)actualSell * GetSalePrice(entry.data.id);

            entry.state.currnetChangeCount = changeCount;
            entry.ModifyCount(changeCount);
            entry.state.threadDeltaCount = 0;
        }

        TotalCreditChange = totalCreditChange;
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

            entry.RecordPrice(resourceState.currentValue, _initialResourceData.priceHistoryCapacity);
        }
    }

    /// <summary>
    /// 플레이어가 자원을 살 때의 최종 가격을 반환합니다. (기준가 + 수수료)
    /// </summary>
    public long GetPurchasePrice(string resourceId)
    {
        ResourceEntry entry = GetResourceEntry(resourceId);
        if (entry == null) return 0;

        float feeMultiplier = 1f + _initialResourceData.transactionFee;
        return (long)Math.Ceiling(entry.state.currentValue * feeMultiplier);
    }

    /// <summary>
    /// 플레이어가 자원을 팔 때의 최종 가격을 반환합니다. (기준가 - 수수료)
    /// </summary>
    public long GetSalePrice(string resourceId)
    {
        ResourceEntry entry = GetResourceEntry(resourceId);
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
        _pendingResourceChangedNotify = false;
    }
}
