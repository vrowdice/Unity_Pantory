using Evo.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 시장 행위자(Market Actor) 데이터를 관리하고, 일 단위로 공급과 수요를 시뮬레이션하여 자원 가격에 반영하는 핸들러입니다.
/// </summary>
public partial class MarketActorDataHandler : IDataHandlerEvents, ITimeChangeHandler
{
    private readonly DataManager _dataManager;
    private readonly InitialMarketActorData _initialMarketActorData;

    private List<MarketActorData> _actors = new List<MarketActorData>();
    private Dictionary<string, MarketActorEntry> _actorDic = new();

    public MarketActorDataHandler(DataManager dataManager, List<MarketActorData> marketActorDataList, InitialMarketActorData initData)
    {
        _dataManager = dataManager;
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

    private void DayActorWealthChange()
    {
        foreach (MarketActorEntry actorEntry in _actorDic.Values)
        {
            if (actorEntry.data.marketActorType != MarketActorType.Company) continue;

            float businessSuccess = UnityEngine.Random.Range(_initialMarketActorData.businessSuccessMin, _initialMarketActorData.businessSuccessMax);
            long totalDailyEarnings = 0;
            long totalDailyCost = 0;

            foreach (ResourceData resourceData in actorEntry.data.productionResourceList)
            {
                ResourceEntry resourceEntry = _dataManager.Resource.GetResourceEntry(resourceData.id);
                if (resourceEntry == null) continue;

                long virtualProduction = (long)(actorEntry.data.baseProductionCount * businessSuccess);
                totalDailyEarnings += virtualProduction * resourceEntry.state.currentValue;

                float costVariation = UnityEngine.Random.Range(_initialMarketActorData.costVariationMin, _initialMarketActorData.costVariationMax);
                totalDailyCost += (long)(actorEntry.data.baseProductionCount * costVariation * resourceEntry.state.currentValue);
            }

            long dailyNetProfit = totalDailyEarnings - totalDailyCost;

            if (dailyNetProfit > _initialMarketActorData.maxDailyNetProfit)
            {
                dailyNetProfit = (long)(dailyNetProfit * _initialMarketActorData.profitLimitMultiplier);
            }

            actorEntry.state.wealth += dailyNetProfit;
            actorEntry.state.currentChangeWealth = dailyNetProfit;

            if (actorEntry.state.wealth < actorEntry.data.baseWealth)
            {
                actorEntry.state.wealth = actorEntry.data.baseWealth;
                actorEntry.state.currentChangeWealth = 0;
            }
        }
    }

    public MarketActorEntry GetMarketActorEntry(string actorId)
    {
        if (_actorDic.TryGetValue(actorId, out MarketActorEntry entry))
        {
            return entry;
        }

        return null;
    }

    public Dictionary<string, MarketActorEntry> GetAllMarketActors()
    {
        return new Dictionary<string, MarketActorEntry>(_actorDic);
    }

    public void ModifyMarketActorTrust(string actorId, int trustChange)
    {
        if (_actorDic.TryGetValue(actorId, out MarketActorEntry entry))
        {
            entry.state.trust += trustChange;
            if (entry.state.trust < 0) entry.state.trust = 0;
        }
    }

    public void HandleDayChanged()
    {
        DayActorWealthChange();
    }

    public void ClearAllSubscriptions()
    {

    } 
}