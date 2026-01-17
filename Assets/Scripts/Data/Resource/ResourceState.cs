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

    public const int PriceHistoryCapacity = 60;
    [SerializeField] internal List<float> _priceHistory = new List<float>(PriceHistoryCapacity);
    public IReadOnlyList<float> PriceHistory => _priceHistory;

    public Dictionary<EffectStatType, List<EffectState>> activeEffects; // 자원 개별 적용 효과 목록 (StatType별로 분류)

    public ResourceState()
    {
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
