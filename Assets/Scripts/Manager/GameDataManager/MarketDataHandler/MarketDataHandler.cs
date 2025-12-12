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

    // 전쟁 상태 관리
    private bool _isWarState = false;
    private const string SYSTEM_POPULACE_ID = "sys_populace";
    private const string SYSTEM_TRADE_PORT_ID = "sys_trade_port";

    public MarketDataHandler(GameDataManager manager)
    {
        _gameDataManager = manager;
        AutoLoadAllActors(); // 게임 시작 시 자동으로 모든 액터 데이터 로드
        CreateSystemActors(); // 시스템 액터 생성 (일반 시민, 무역항)
    }

    /// <summary>
    /// 시스템 액터(일반 시민)를 생성합니다.
    /// </summary>
    private void CreateSystemActors()
    {
        if (_gameDataManager?.Resource == null) return;
        var allResources = _gameDataManager.Resource.GetAllResources();
        if (allResources == null || allResources.Count == 0) return;

        // 1. 일반 시민 (기초 수요 담당)
        var populaceData = ScriptableObject.CreateInstance<MarketActorData>();
        populaceData.id = SYSTEM_POPULACE_ID;
        populaceData.displayName = "General Population";
        populaceData.description = "The general populace that provides stable demand for basic consumer goods.";
        populaceData.archetype = MarketActorArchetype.Generalist;
        populaceData.scale = MarketActorScale.Large;
        populaceData.useDynamicResourceAllocation = false; // 직업 변경 안 함
        
        // Consumer 프로필 설정
        populaceData.consumerProfile = new ConsumerProfile();
        float budgetMin = _marketSettings != null ? _marketSettings.systemPopulaceBudget.x : 1000000f;
        float budgetMax = _marketSettings != null ? _marketSettings.systemPopulaceBudget.y : 5000000f;
        populaceData.consumerProfile.budgetRange = new BudgetRange { min = budgetMin, max = budgetMax }; 
        populaceData.consumerProfile.desiredResources = new List<ResourcePreference>();
        populaceData.consumerProfile.patienceSeconds = 7200f;
        populaceData.consumerProfile.satisfactionDecay = 0.05f;
        populaceData.consumerProfile.allowBulkBuying = true;
        populaceData.consumerProfile.persistentOrders = true;
        
        // 생필품(Essentials) 및 1차 가공품 일부 소비
        var basicConsumerTypes = new[] { ResourceType.Essentials }; // ConsumerGoods가 있다면 포함

        foreach (var resource in allResources.Values)
        {
            if (resource?.resourceData == null) continue;

            // 해당 타입이거나, 특정 ID(예: wood_log)도 일부 소비 가능
            bool isTarget = System.Array.IndexOf(basicConsumerTypes, resource.resourceData.type) >= 0;

            if (isTarget)
            {
                float quantityMin = _marketSettings != null ? _marketSettings.systemPopulaceQuantityRange.x : 100f;
                float quantityMax = _marketSettings != null ? _marketSettings.systemPopulaceQuantityRange.y : 300f;
                float priceSens = _marketSettings != null ? _marketSettings.systemPopulacePriceSensitivity : 1.2f;
                float urgency = _marketSettings != null ? _marketSettings.systemPopulaceUrgency : 0.2f;
                
                var preference = new ResourcePreference
                {
                    resource = resource.resourceData,
                    desiredMin = (long)quantityMin,
                    desiredMax = (long)quantityMax,
                    priceSensitivity = priceSens,
                    urgency = urgency
                };
                populaceData.consumerProfile.desiredResources.Add(preference);
            }
        }
        
        RegisterActor(populaceData);
    }

    /// <summary>
    /// 시스템 액터들을 초기화합니다 (무한 예산 설정 등).
    /// </summary>
    public void InitializeSystemActors()
    {
        // 일반 시민: 무한 예산 설정
        if (_actors.TryGetValue(SYSTEM_POPULACE_ID, out var populaceEntry))
        {
            if (populaceEntry?.state != null)
            {
                float populaceWealth = _marketSettings != null ? _marketSettings.systemPopulaceWealth : 1000000f;
                populaceEntry.state.wealth = populaceWealth; // 무한에 가까운 자산
                populaceEntry.state.previousWealth = populaceEntry.state.wealth;
                populaceEntry.state.health = 1f; // 항상 건강
                
                if (populaceEntry.state.consumer != null)
                {
                    var profile = populaceEntry.GetConsumerProfile();
                    if (profile != null)
                    {
                        populaceEntry.state.consumer.currentBudget = profile.budgetRange.max;
                    }
                }
            }
        }
    }

    /// <summary>
    /// [초기화] 시장 마중물: 재고 충전 및 액터 초기 자금 강제 주입
    /// </summary>
    public void InitializeMarketChaos()
    {
        if (_gameDataManager?.Resource == null) return;
        var allResources = _gameDataManager.Resource.GetAllResources();
        if (allResources == null) return;

        // 1. 자원 재고 차등 충전 (원자재는 더 많이)
        foreach (var res in allResources.Values)
        {
            if (res?.resourceState == null || res.resourceData == null) continue;

            long initialCount = (res.resourceData.type == ResourceType.raw) 
                ? (_marketSettings != null ? _marketSettings.initialResourceCount * 2L : 2000L)
                : (_marketSettings != null ? _marketSettings.initialResourceCount : 500L);
            
            res.resourceState.count = initialCount;
            res.resourceState.lastSupply = initialCount * 0.5f;
        }

        // 2. [핵심] 액터들에게 "초기 자본금(Startup Capital)" 강제 지급
        foreach (var actor in _actors.Values)
        {
            if (actor?.state == null || actor.data == null) continue;
            if (actor.data.id.StartsWith("sys_")) continue;

            // 스케일별 목표 자산 설정 (기획 의도: 잘나가는 기업)
            float targetWealth = actor.data.scale switch
            {
                MarketActorScale.Large => 2000000f,  // 대기업 200만
                MarketActorScale.Medium => 500000f,  // 중견 50만
                _ => 100000f                         // 소기업 10만
            };

            // 현재 돈이 목표치보다 적으면 목표치로 강제 설정 (리셋)
            if (actor.state.wealth < targetWealth)
            {
                actor.state.wealth = targetWealth;
                actor.state.previousWealth = targetWealth;
                actor.state.health = 1.0f;
            }
            
            // 예산도 재설정
            if (actor.state.consumer != null)
            {
                var profile = actor.GetConsumerProfile();
                if (profile != null && profile.budgetRange.max > 0f)
                {
                    actor.state.consumer.currentBudget = profile.budgetRange.max;
                }
                else
                {
                    actor.state.consumer.currentBudget = actor.state.wealth * 0.2f;
                }
            }
        }

        OnMarketUpdated?.Invoke();
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
    /// 에디터에서는 AssetDatabase를 사용하고, 빌드된 게임에서는 Resources 폴더를 사용합니다.
    /// </summary>
    /// <param name="actorPaths">검색할 폴더 경로 배열 (예: "Datas/MarketActor")</param>
    public void AutoLoadActors(string[] actorPaths)
    {
#if UNITY_EDITOR
        // 에디터 모드: AssetDatabase를 사용하여 지정된 경로에서 MarketActorData 찾기
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
        // 빌드 모드: Resources 폴더에서 로드
        // 주의: actorPaths의 첫 번째 경로를 사용하거나, 기본 경로 "Datas/MarketActor" 사용
        string resourcePath = actorPaths != null && actorPaths.Length > 0 ? actorPaths[0] : "Datas/MarketActor";
        MarketActorData[] actorDataList = Resources.LoadAll<MarketActorData>(resourcePath);
        if (actorDataList != null && actorDataList.Length > 0)
        {
            RegisterActors(actorDataList);
            Debug.Log($"[MarketDataHandler] Runtime load completed: {actorDataList.Length} actors registered from {resourcePath}.");
        }
        else
        {
            Debug.LogWarning($"[MarketDataHandler] No MarketActorData found in Resources/{resourcePath}. Make sure MarketActorData files are placed in the Resources folder.");
        }
#endif
    }

    /// <summary>
    /// 모든 MarketActorData를 자동으로 검색하여 등록합니다. (전체 Assets 폴더)
    /// 에디터에서는 AssetDatabase를 사용하고, 빌드된 게임에서는 Resources 폴더를 사용합니다.
    /// </summary>
    public void AutoLoadAllActors()
    {
#if UNITY_EDITOR
        // 에디터 모드: AssetDatabase를 사용하여 모든 MarketActorData 찾기
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
        // 빌드 모드: Resources 폴더에서 로드
        // 주의: MarketActorData 파일들이 Resources/Datas/MarketActor 폴더에 있어야 합니다.
        MarketActorData[] actorDataList = Resources.LoadAll<MarketActorData>("Datas/MarketActor");
        if (actorDataList != null && actorDataList.Length > 0)
        {
            RegisterActors(actorDataList);
            Debug.Log($"[MarketDataHandler] Runtime load completed: {actorDataList.Length} actors registered.");
        }
        else
        {
            Debug.LogWarning("[MarketDataHandler] No MarketActorData found in Resources/Datas/MarketActor. Make sure MarketActorData files are placed in the Resources folder.");
        }
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
        
        // 초기 마켓 설정 적용 (settings가 null이어도 기본값으로 초기화됨)
        entry.ApplyInitialMarketSettings(_marketSettings);
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

        // [필수] 0. 유동성 강제 확보 (여기서 0원을 멸종시킴)
        EnsureMarketLiquidity();

        // 전일 자산 저장 (초기화 로직은 제거 - 무한 부활 버그 방지)
        foreach (var entry in _actors.Values)
        {
            if (entry?.state == null)
            {
                continue;
            }
            
            entry.state.previousWealth = entry.state.wealth;
        }

        // [솔루션 2] 국가 보조금 지급: 파산 직전 액터 구제
        ProvideStimulusPackages();

        // 1단계: 경쟁자 수 계산 (안정화 전략: 경쟁 과열 방지)
        Dictionary<string, int> globalProviderCounts = CalculateProviderCounts();

        // 1단계: 액터 할당 및 상태 갱신
        foreach (var entry in _actors.Values)
        {
            if (entry?.state == null)
            {
                continue;
            }
            
            // 파산한 액터는 경제 활동에서 제외 (CPU 절약 및 좀비 방지)
            if (entry.state.wealth <= 0.1f)
            {
                continue;
            }
            
            MarketActorDynamicAllocator.UpdateAssignments(entry, resourceSnapshot, _marketSettings, globalProviderCounts);
            RefreshActor(entry);
            ResetDailyTradingStats(entry);
        }

        // 2단계: Provider 생산량 계산 및 총 공급량 집계
        foreach (var entry in _actors.Values)
        {
            if (entry?.state == null)
            {
                continue;
            }
            
            // 파산한 액터는 생산 불가
            if (entry.state.wealth <= 0.1f)
            {
                continue;
            }
            
            SimulateProviderProduction(entry, resourceSnapshot, totalSupply);
        }

        // 3단계: Consumer 수요 계산
        foreach (var entry in _actors.Values)
        {
            if (entry?.state == null)
            {
                continue;
            }
            
            // 파산한 액터는 소비 불가
            if (entry.state.wealth <= 0.1f)
            {
                continue;
            }
            
            SimulateConsumerDemand(entry, resourceSnapshot, totalDemand);
        }

        // 4단계: 시장 풀 방식으로 거래 정산
        ExecuteTrades(resourceSnapshot, totalSupply, totalDemand);

        // 5단계: 가격 조정 및 후처리
        ApplyPriceAdjustments(resourceSnapshot, totalSupply, totalDemand);
        ApplyBusinessHealthEffects();
        UpdateRevenueRankings();
        
        // [솔루션 1] 무역항 처리: 잉여 물량 수출, 부족 물량 수입
        ProcessGlobalTradePort(resourceSnapshot);
        
        // 플레이어 자동 거래 실행 (playerTransactionDelta 기반)
        ExecutePlayerAutoTrades(resourceSnapshot);
        OnMarketUpdated?.Invoke();
    }



    /// <summary>
    /// 각 자원별로 생산하는 액터 수를 계산합니다 (경쟁 과열 방지용).
    /// </summary>


    /// <summary>
    /// 자산 기준으로 정렬된 액터 리스트를 반환합니다 (캐시 최적화).
    /// </summary>
    public List<MarketActorEntry> GetActorsSortedByWealth(bool ascending = false)
    {
        // UpdateRevenueRankings에서 이미 내림차순(부자 순)으로 정렬된 캐시가 있습니다.
        // 굳이 새로 만들지 않고 캐시를 활용합니다.
        
        if (!ascending)
        {
            // 내림차순(기본)이라면 복사본만 반환 (원본 보호)
            return new List<MarketActorEntry>(_cachedActorList);
        }
        else
        {
            // 오름차순이라면 뒤집어서 반환
            var list = new List<MarketActorEntry>(_cachedActorList);
            list.Reverse();
            return list;
        }
    }

    /// <summary>
    /// 특정 액터의 자산 순위를 반환합니다 (캐시 최적화).
    /// </summary>
    public int GetActorWealthRank(string actorId)
    {
        if (!_actors.TryGetValue(actorId, out var entry))
        {
            return -1;
        }

        // 캐시된 리스트 활용 (UpdateRevenueRankings에서 이미 정렬됨)
        for (int i = 0; i < _cachedActorList.Count; i++)
        {
            if (_cachedActorList[i].data?.id == actorId)
            {
                return i + 1;
            }
        }

        return -1;
    }


    /// <summary>
    /// [솔루션 1] 무역항 처리: 잉여 물량 수출(시장에서 제거), 부족 물량 수입(시장에 공급)
    /// [강화] 가격 안정화 장치: 가격이 비쌀 때 수입품 대량 공급
    /// </summary>


    /// <summary>
    /// 전쟁 상태를 설정합니다.
    /// </summary>
    public void SetWarState(bool isWar)
    {
        if (_isWarState == isWar) return;
        _isWarState = isWar;
        Debug.Log($"[Market] War State Changed: {isWar}");

        foreach (var entry in _actors.Values)
        {
            if (entry?.data == null || entry.state == null) continue;
            if (entry.data.id.StartsWith("sys_")) continue;

            // 군사 액터 판단 (Archetype 우선, 없으면 이름 체크)
            bool isMilitary = entry.data.archetype == MarketActorArchetype.Generalist || 
                              entry.data.displayName.Contains("Defense") ||
                              entry.data.displayName.Contains("Military") ||
                              entry.data.displayName.Contains("Armory") ||
                              entry.data.displayName.Contains("Security");

            if (isMilitary)
            {
                var profile = entry.GetOrCreateConsumerProfile();
                if (profile != null && entry.state.consumer != null)
                {
                    if (isWar)
                    {
                        // 전쟁 시: 예산 5배 증액
                        float warBudgetMult = _marketSettings != null ? _marketSettings.warBudgetMultiplier : 5.0f;
                        entry.state.consumer.currentBudget *= warBudgetMult;
                        
                        // 긴급도 최대
                        float warUrg = _marketSettings != null ? _marketSettings.warUrgency : 1.0f;
                        float warPriceSens = _marketSettings != null ? _marketSettings.warPriceSensitivity : 0.1f;
                        
                        foreach (var r in profile.desiredResources)
                        {
                            r.urgency = warUrg;
                            r.priceSensitivity = warPriceSens;
                        }
                    }
                    else
                    {
                        // 평화 시: 예산 및 긴급도 복구
                        entry.ApplyInitialMarketSettings(_marketSettings);
                        
                        float peaceUrg = _marketSettings != null ? _marketSettings.peaceUrgency : 0.25f;
                        float peacePriceSens = _marketSettings != null ? _marketSettings.peacePriceSensitivity : 0.55f;
                        
                        foreach (var r in profile.desiredResources)
                        {
                            r.urgency = peaceUrg;
                            r.priceSensitivity = peacePriceSens;
                        }
                    }
                }
            }
            else
            {
                // 민간 액터 (사치재 등): 불황
                bool isLuxury = entry.data.displayName.Contains("Luxury") || 
                                entry.data.displayName.Contains("Fashion");
                                
                if (isLuxury && entry.state.consumer != null)
                {
                    if (isWar)
                    {
                        float reduction = _marketSettings != null ? _marketSettings.civilianBudgetReduction : 0.5f;
                        entry.state.consumer.currentBudget *= reduction;
                    }
                    else
                    {
                        entry.ApplyInitialMarketSettings(_marketSettings);
                    }
                }
            }
        }

        OnMarketUpdated?.Invoke(); // [필수] 전쟁 선포 시 UI 즉시 반영
    }

    public bool IsWarState() => _isWarState;

    /// <summary>
    /// [절대 방어선] 매 틱마다 자산이 비정상적으로 낮은 액터를 강제로 회복시킵니다.
    /// </summary>
    private void EnsureMarketLiquidity()
    {
        foreach (var entry in _actors.Values)
        {
            if (entry?.state == null || entry.data == null) continue;
            if (entry.data.id.StartsWith("sys_")) continue;

            // 최소 생존 자금 기준 (대기업 10만, 중소 1만)
            float survivalWealth = entry.data.scale == MarketActorScale.Large ? 100000f : 10000f;

            // 0원이거나 생존 자금 미만이면 -> 즉시 강제 주입
            if (entry.state.wealth < survivalWealth)
            {
                // 목표치(50만/5만)로 리셋
                float targetWealth = survivalWealth * 5f;
                entry.state.wealth = targetWealth;
                
                // 그래프 튀는 것 방지
                if (entry.state.previousWealth <= 1f) entry.state.previousWealth = targetWealth;

                // 예산 복구
                if (entry.state.consumer != null && entry.state.consumer.currentBudget < 1000f)
                {
                    entry.state.consumer.currentBudget = targetWealth * 0.2f;
                }
                
                // Debug.Log($"[Market] 🚑 Emergency Liquidity: {entry.data.displayName} reset to {targetWealth}");
            }
        }
    }

    /// <summary>
    /// [수정] 구제 금융 (Bailout): 파산 위기 기업 지원
    /// 규모에 비례한 빈곤선을 설정하여 현실적인 구제금융을 제공합니다.
    /// </summary>
    private void ProvideStimulusPackages()
    {
        foreach (var entry in _actors.Values)
        {
            if (entry?.state == null || entry.data == null) continue;
            if (entry.data.id.StartsWith("sys_")) continue;

            // 빈곤선 (Poverty Line): 기업 규모에 따라 다름
            float povertyLine = entry.data.scale == MarketActorScale.Large 
                ? (_marketSettings != null ? _marketSettings.stimulusWealthThreshold * 100f : 100000f)
                : (_marketSettings != null ? _marketSettings.stimulusWealthThreshold * 10f : 10000f);

            // 자산이 빈곤선 미만이면 구조 자금 투입
            if (entry.state.wealth < povertyLine)
            {
                // 빈곤선의 2배까지 회복
                float bailout = (povertyLine * 2f) - entry.state.wealth;
                entry.state.wealth += bailout;
                
                // 패널티: 신용 등급(건강도) 하락
                float healthPenalty = _marketSettings != null ? _marketSettings.stimulusHealthPenalty : 0.1f;
                entry.state.health = Mathf.Max(0.1f, entry.state.health - healthPenalty);
                
                // 예산 재설정
                if (entry.state.consumer != null)
                {
                    entry.state.consumer.currentBudget = entry.state.wealth * 0.2f;
                }
                
                // Debug.Log($"[Market] Bailout: {entry.data.displayName} (+{bailout:F0})");
            }
            
            // 예산이 아예 없는 경우 긴급 지원 (Small 액터 등)
            if (entry.state.consumer != null && entry.state.consumer.currentBudget < 100f)
            {
                entry.state.consumer.currentBudget += 5000f; // 최소 활동비
            }
        }
    }
}