using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 시장 행위자(Market Actor) 데이터를 관리하고, 일 단위로 공급과 수요를 시뮬레이션하여 자원 가격에 반영하는 핸들러입니다.
/// </summary>
public partial class MarketDataHandler
{
    private DataManager _dataManager;
    private InitialMarketData _marketSettings;

    private List<MarketActorData> _actors = new List<MarketActorData>();
    private Dictionary<string, MarketActorEntry> _actorDic = new();

    public MarketDataHandler(DataManager manager, List<MarketActorData> marketActorDataList, InitialMarketData initData)
    {
        _dataManager = manager;
        _actors = marketActorDataList;
        _marketSettings = initData;

        InitDictionary();
    }

    private void InitDictionary()
    {
        foreach (MarketActorData item in _actors)
        {
            MarketActorEntry entry = new MarketActorEntry();
            MarketActorState state = new MarketActorState();

            entry.state = state;

            _actorDic.Add(item.id, entry);
        }
    }

    public void OnDayChanged()
    {
        DayResourceChange();
        DayActorChange();
    }

    private void DayResourceChange()
    {

    }

    private void DayActorChange()
    {

    }

    public void ClearAllSubscriptions()
    {

    } 
}