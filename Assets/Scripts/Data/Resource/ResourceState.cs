using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ResourceState
{
    public int count;
    public int threadDeltaCount;
    public int marketDeltaCount;
    public int currnetChangeCount;

    public long currentEventValue;
    public long currentValue;
    public long currentChangeValue;

    /// <summary>
    /// 가격 히스토리 최대 개수. InitialResourceData.priceHistoryCapacity에서 설정.
    /// </summary>
    public int PriceHistoryCapacity { get; private set; }

    public List<float> _priceHistory;

    public Dictionary<EffectStatType, List<EffectState>> activeEffects;

    public ResourceState(int priceHistoryCapacity = 60)
    {
        PriceHistoryCapacity = priceHistoryCapacity;
        _priceHistory = new List<float>(PriceHistoryCapacity);
        InitializeDefaults();
    }

    private void InitializeDefaults()
    {
        currentValue = 0;
        count = 0;
        threadDeltaCount = 0;

        if (_priceHistory == null)
        {
            _priceHistory = new List<float>(PriceHistoryCapacity);
        }
        else
        {
            _priceHistory.Clear();
        }

        activeEffects = new Dictionary<EffectStatType, List<EffectState>>();
        foreach (EffectStatType statType in Enum.GetValues(typeof(EffectStatType)))
        {
            activeEffects[statType] = new List<EffectState>();
        }
    }

}
