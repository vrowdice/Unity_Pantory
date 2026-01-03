using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 시장 행위자(Market Actor) 데이터를 관리하고, 일 단위로 공급과 수요를 시뮬레이션하여 자원 가격에 반영하는 핸들러입니다.
/// </summary>
public partial class MarketDataHandler
{
    private Dictionary<string, MarketActorEntry> _actors = new();
    private DataManager _dataManager;
    private InitialMarketData _marketSettings;

    public event Action OnMarketUpdated;

    public MarketDataHandler(DataManager manager, List<MarketActorData> marketActorDataList, InitialMarketData initData)
    {
        _dataManager = manager;
    }

    public void ClearAllSubscriptions()
    {
        OnMarketUpdated = null;
    } 
}