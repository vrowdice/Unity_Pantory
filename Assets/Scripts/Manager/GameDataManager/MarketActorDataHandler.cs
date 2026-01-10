using Evo.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 시장 행위자(Market Actor) 데이터를 관리하고, 일 단위로 공급과 수요를 시뮬레이션하여 자원 가격에 반영하는 핸들러입니다.
/// </summary>
public partial class MarketActorDataHandler
{
    private DataManager _dataManager;
    private InitialMarketActorData _initialMarketActorData;

    private List<MarketActorData> _actors = new List<MarketActorData>();
    private Dictionary<string, MarketActorEntry> _actorDic = new();

    public MarketActorDataHandler(DataManager manager, List<MarketActorData> marketActorDataList, InitialMarketActorData initData)
    {
        _dataManager = manager;
        _actors = marketActorDataList;
        _initialMarketActorData = initData;

        InitDictionary();
    }

    private void InitDictionary()
    {
        foreach (MarketActorData item in _actors)
        {
            MarketActorEntry entry = new MarketActorEntry(item);

            _actorDic.Add(item.id, entry);
        }
    }

    public MarketActorEntry GetMarketActorEntry(string actorId)
    {
        if (_actorDic.TryGetValue(actorId, out var entry))
        {
            return entry;
        }

        return null;
    }

    public Dictionary<string, MarketActorEntry> GetAllMarketActors()
    {
        return new Dictionary<string, MarketActorEntry>(_actorDic);
    }

    public void HandleDayChanged()
    {
        DayActorWealthChange();
    }

    private void DayActorWealthChange()
    {
        foreach (MarketActorEntry actorEntry in _actorDic.Values)
        {
            float businessSuccess = UnityEngine.Random.Range(0.8f, 1.3f);
            long totalDailyEarnings = 0;
            float totalBaseValue = 0;

            foreach (ResourceData resourceData in actorEntry.data.productionResources)
            {
                ResourceEntry resourceEntry = _dataManager.Resource.GetResourceEntry(resourceData.id);
                if (resourceEntry == null) continue;

                long virtualProduction = (long)(actorEntry.data.baseProductionCount * businessSuccess);
                totalDailyEarnings += virtualProduction * resourceEntry.state.currentValue;

                totalBaseValue += resourceData.baseValue;
            }

            if (actorEntry.data.productionResources.Count > 0)
            {
                float averageBaseValue = totalBaseValue / actorEntry.data.productionResources.Count;

                long baseCostCount = actorEntry.data.baseProductionCount;
                long virtualCost = (long)(baseCostCount * UnityEngine.Random.Range(0.9f, 1.1f) * averageBaseValue);

                long dailyNetProfit = totalDailyEarnings - virtualCost;
                actorEntry.state.wealth += dailyNetProfit;
                actorEntry.state.currentChangeWealth = dailyNetProfit;
            }

            if (actorEntry.state.wealth < 0) actorEntry.state.wealth = 0;
        }
    }

    public void ClearAllSubscriptions()
    {

    } 
}