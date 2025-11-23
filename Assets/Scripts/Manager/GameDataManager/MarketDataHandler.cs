using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 시장 행위자 데이터를 관리하고 하루 단위로 공급/수요를 시뮬레이션해 자원 가격에 반영하는 핸들러
/// 시장 풀(Market Pool) 방식을 사용하여 O(N+M) 복잡도로 효율적으로 처리합니다.
/// </summary>
public partial class MarketDataHandler
{
    private readonly Dictionary<string, MarketActorEntry> _actors = new();
    private readonly GameDataManager _gameDataManager;
    private InitialMarketData _marketSettings;

    public event Action OnMarketUpdated;

    public MarketDataHandler(GameDataManager manager)
    {
        _gameDataManager = manager;
        AutoLoadAllActors(); // 게임 시작 시 자동으로 모든 액터 데이터 로드
    }

    /// <summary>
    /// 현재 시장 수수료율을 반환합니다.
    /// </summary>
    public float GetMarketFeeRate()
    {
        return _marketSettings != null ? _marketSettings.marketFeeRate : 0.05f;
    }

    /// <summary>
    /// 지정된 경로에서 모든 MarketActorData를 자동으로 로드하여 등록합니다.
    /// </summary>
    /// <param name="actorPaths">검색할 폴더 경로 배열 (예: "Datas/MarketActor")</param>
    public void AutoLoadActors(string[] actorPaths)
    {
#if UNITY_EDITOR
        int loadedCount = 0;
        
        foreach (string path in actorPaths)
        {
            // AssetDatabase를 사용하여 모든 MarketActorData 찾기
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:MarketActorData", new[] { "Assets/" + path });
            
            foreach (string guid in guids)
            {
                string assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                MarketActorData actorData = UnityEditor.AssetDatabase.LoadAssetAtPath<MarketActorData>(assetPath);
                
                if (actorData != null)
                {
                    RegisterActor(actorData);
                    loadedCount++;
                }
            }
        }
        
        Debug.Log($"[MarketDataHandler] Auto load completed: {loadedCount} actors registered");
#else
        Debug.LogWarning("[MarketDataHandler] AutoLoadActors is only available in editor mode.");
#endif
    }

    /// <summary>
    /// 모든 MarketActorData를 자동으로 검색하여 등록합니다. (전체 Assets 폴더)
    /// </summary>
    public void AutoLoadAllActors()
    {
#if UNITY_EDITOR
        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:MarketActorData");
        int loadedCount = 0;
        
        foreach (string guid in guids)
        {
            string assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            MarketActorData actorData = UnityEditor.AssetDatabase.LoadAssetAtPath<MarketActorData>(assetPath);
            
            if (actorData != null)
            {
                RegisterActor(actorData);
                loadedCount++;
            }
        }
        
        Debug.Log($"[MarketDataHandler] Full auto load completed: {loadedCount} actors registered");
#else
        Debug.LogWarning("[MarketDataHandler] AutoLoadAllActors is only available in editor mode.");
#endif
    }

    /// <summary>
    /// 마켓 설정을 적용합니다.
    /// </summary>
    public void SetMarketSettings(InitialMarketData settings)
    {
        _marketSettings = settings;
        
        // 기존 액터들의 초기 상태 업데이트
        if (_marketSettings != null)
        {
            foreach (var entry in _actors.Values)
            {
                entry?.ApplyInitialMarketSettings(_marketSettings);
            }
        }
    }

    public void RegisterActor(MarketActorData data)
    {
        if (data == null || string.IsNullOrEmpty(data.id))
        {
            Debug.LogWarning("[MarketDataHandler] Invalid actor data.");
            return;
        }

        if (_actors.ContainsKey(data.id))
        {
            Debug.LogWarning($"[MarketDataHandler] Actor already registered: {data.id}");
            return;
        }

        var entry = new MarketActorEntry(data);
        _actors[data.id] = entry;
        
        // 초기 마켓 설정 적용
        if (_marketSettings != null)
        {
            entry.ApplyInitialMarketSettings(_marketSettings);
        }
    }

    public void RegisterActors(IEnumerable<MarketActorData> actors)
    {
        if (actors == null)
        {
            return;
        }

        foreach (var actor in actors)
        {
            RegisterActor(actor);
        }
    }

    public Dictionary<string, MarketActorEntry> GetAllActors()
    {
        return new Dictionary<string, MarketActorEntry>(_actors);
    }

    public void TickDailyMarket()
    {
        if (_gameDataManager?.Resource == null || _actors.Count == 0)
        {
            return;
        }

        Dictionary<string, ResourceEntry> resourceSnapshot = _gameDataManager.Resource.GetAllResources();
        if (resourceSnapshot == null || resourceSnapshot.Count == 0)
        {
            return;
        }

        var totalSupply = new Dictionary<string, float>();
        var totalDemand = new Dictionary<string, float>();

        // 전일 자산 저장
        foreach (var entry in _actors.Values)
        {
            if (entry?.state == null)
            {
                continue;
            }
            if (entry.state.provider != null)
            {
                entry.state.provider.previousWealth = entry.state.provider.wealth;
            }
            if (entry.state.consumer != null)
            {
                entry.state.consumer.previousWealth = entry.state.consumer.wealth;
            }
        }

        // 1단계: 액터 할당 및 상태 갱신
        foreach (var entry in _actors.Values)
        {
            MarketActorDynamicAllocator.UpdateAssignments(entry, resourceSnapshot, _marketSettings);
            RefreshActor(entry);
            ResetDailyTradingStats(entry);
        }

        // 2단계: Provider 생산량 계산 및 총 공급량 집계
        foreach (var entry in _actors.Values)
        {
            if (entry.data.roles.HasFlag(MarketRoleFlags.Provider))
            {
                SimulateProviderProduction(entry, resourceSnapshot, totalSupply);
            }
        }

        // 3단계: Consumer 수요 계산
        foreach (var entry in _actors.Values)
        {
            if (entry.data.roles.HasFlag(MarketRoleFlags.Consumer))
            {
                SimulateConsumerDemand(entry, resourceSnapshot, totalDemand);
            }
        }

        // 4단계: 시장 풀 방식으로 거래 정산
        ExecuteTrades(resourceSnapshot, totalSupply, totalDemand);

        // 5단계: 가격 조정 및 후처리
        ApplyPriceAdjustments(resourceSnapshot, totalSupply, totalDemand);
        ApplyBusinessHealthEffects();
        UpdateRevenueRankings();
        
        // 플레이어 자동 거래 실행 (playerTransactionDelta 기반)
        ExecutePlayerAutoTrades(resourceSnapshot);
        OnMarketUpdated?.Invoke();
    }

    // 메서드들이 partial 파일로 분리되었습니다:
    // - Simulation 관련: MarketDataHandler.Simulation.cs
    // - Player 거래 관련: MarketDataHandler.Player.cs
    // - Stats 및 후처리: MarketDataHandler.Stats.cs

    /// <summary>
    /// 모든 액터를 자산 기준으로 정렬하여 반환합니다.
    /// </summary>
    public List<MarketActorEntry> GetActorsSortedByWealth(bool ascending = false)
    {
        var sortedList = new List<MarketActorEntry>(_actors.Values);
        if (ascending)
        {
            sortedList.Sort((a, b) => a.state.GetWealth().CompareTo(b.state.GetWealth()));
        }
        else
        {
            sortedList.Sort((a, b) => b.state.GetWealth().CompareTo(a.state.GetWealth()));
        }
        return sortedList;
    }

    /// <summary>
    /// 특정 액터의 자산 순위를 반환합니다.
    /// </summary>
    public int GetActorWealthRank(string actorId)
    {
        if (!_actors.TryGetValue(actorId, out var entry))
        {
            return -1;
        }

        var sorted = GetActorsSortedByWealth(false);
        for (int i = 0; i < sorted.Count; i++)
        {
            if (sorted[i].data.id == actorId)
            {
                return i + 1;
            }
        }

        return -1;
    }

}

