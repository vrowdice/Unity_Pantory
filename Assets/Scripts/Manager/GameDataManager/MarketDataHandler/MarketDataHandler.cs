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
    /// 시스템 액터(일반 시민, 무역항)를 생성합니다.
    /// </summary>
    private void CreateSystemActors()
    {
        if (_gameDataManager?.Resource == null)
        {
            return;
        }

        var allResources = _gameDataManager.Resource.GetAllResources();
        if (allResources == null || allResources.Count == 0)
        {
            return;
        }

        // 1. 일반 시민 (기초 수요 담당)
        var populaceData = ScriptableObject.CreateInstance<MarketActorData>();
        populaceData.id = SYSTEM_POPULACE_ID;
        populaceData.displayName = "General Population";
        populaceData.description = "The general populace that provides stable demand for basic consumer goods.";
        populaceData.archetype = MarketActorArchetype.Generalist;
        populaceData.scale = MarketActorScale.Large;
        populaceData.useDynamicResourceAllocation = false; // 직업 변경 안 함
        
        // Consumer 프로필 설정: 모든 생필품 소비
        populaceData.consumerProfile = new ConsumerProfile();
        float budgetMin = _marketSettings != null ? _marketSettings.systemPopulaceBudget.x : 5000f;
        float budgetMax = _marketSettings != null ? _marketSettings.systemPopulaceBudget.y : 10000f;
        populaceData.consumerProfile.budgetRange = new BudgetRange { min = budgetMin, max = budgetMax }; // 무한 예산 (절대 파산 안 함)
        populaceData.consumerProfile.desiredResources = new List<ResourcePreference>();
        populaceData.consumerProfile.patienceSeconds = 7200f;
        populaceData.consumerProfile.satisfactionDecay = 0.05f; // 낮은 감소율
        populaceData.consumerProfile.allowBulkBuying = true;
        populaceData.consumerProfile.persistentOrders = true;
        
        // 생필품 자원 타입: furniture, clothing, tool (기초 소비재)
        var basicConsumerTypes = new[] { ResourceType.Essentials, ResourceType.Luxuries };
        foreach (var resource in allResources.Values)
        {
            if (resource?.resourceData == null)
            {
                continue;
            }

            // 생필품 타입만 소비
            if (System.Array.IndexOf(basicConsumerTypes, resource.resourceData.type) >= 0)
            {
                float quantityMin = _marketSettings != null ? _marketSettings.systemPopulaceQuantityRange.x : 50f;
                float quantityMax = _marketSettings != null ? _marketSettings.systemPopulaceQuantityRange.y : 150f;
                float priceSens = _marketSettings != null ? _marketSettings.systemPopulacePriceSensitivity : 2.0f;
                float urgency = _marketSettings != null ? _marketSettings.systemPopulaceUrgency : 0.0f;
                
                var preference = new ResourcePreference
                {
                    resource = resource.resourceData,
                    // [수정] 일반 시민의 구매 수량 감소
                    // 기존: 200 ~ 500 -> 수정: 50 ~ 150
                    // 시민들은 '최소한의 생필품'만 소비하게 하고, 나머지는 다른 액터들이 소비하게 유도
                    desiredMin = (long)quantityMin,
                    desiredMax = (long)quantityMax,
                    priceSensitivity = priceSens, // [수정] 가격이 비싸면 정말 안 사게 민감도 상향
                    urgency = urgency          // [수정] 급하지 않음 (비싸면 안 먹고 맒)
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
    /// [솔루션 4] 초기 마중물: 게임 시작 시 시장에 재고와 자금을 충전합니다.
    /// </summary>
    public void InitializeMarketChaos()
    {
        if (_gameDataManager?.Resource == null)
        {
            return;
        }

        var allResources = _gameDataManager.Resource.GetAllResources();
        if (allResources == null)
        {
            return;
        }

        // 1. 모든 자원 재고 1000개씩 충전
        foreach (var res in allResources.Values)
        {
            if (res?.resourceState == null || res.resourceData == null)
            {
                continue;
            }

            long initialCount = _marketSettings != null ? _marketSettings.initialResourceCount : 1000L;
            float initialSupply = _marketSettings != null ? _marketSettings.initialLastSupply : 500f;
            res.resourceState.count = initialCount;
            res.resourceState.lastSupply = initialSupply; // 어제 공급이 있었던 것처럼 속임
        }

        // 2. 모든 액터에게 초기 자금 보너스
        foreach (var actor in _actors.Values)
        {
            if (actor?.state == null)
            {
                continue;
            }

            float wealthBonus = _marketSettings != null ? _marketSettings.initialWealthBonus : 10000f;
            actor.state.wealth += wealthBonus; // 부자 되세요
            
            // Consumer 예산도 충전
            if (actor.state.consumer != null)
            {
                var consumerProfile = actor.GetConsumerProfile();
                if (consumerProfile != null && consumerProfile.budgetRange.max > 0f)
                {
                    actor.state.consumer.currentBudget = consumerProfile.budgetRange.GetRandomBudget();
                }
                else if (actor.state.consumer.currentBudget <= 0f)
                {
                    // 예산이 없으면 자산의 일부를 예산으로
                    actor.state.consumer.currentBudget = actor.state.wealth * 0.3f;
                }
            }
        }
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

    // 메서드들이 partial 파일로 분리되었습니다:
    // - Simulation 관련: MarketDataHandler.Simulation.cs
    // - Player 거래 관련: MarketDataHandler.Player.cs
    // - Stats 및 후처리: MarketDataHandler.Stats.cs

    /// <summary>
    /// 각 자원별로 생산하는 액터 수를 계산합니다 (경쟁 과열 방지용).
    /// </summary>
    private Dictionary<string, int> CalculateProviderCounts()
    {
        var counts = new Dictionary<string, int>();
        
        foreach (var entry in _actors.Values)
        {
            if (entry?.state?.provider == null || entry.data == null)
            {
                continue;
            }

            // 동적 할당이 활성화된 경우 activeResourceIds 사용
            if (entry.data.useDynamicResourceAllocation && entry.state.provider.activeResourceIds != null)
            {
                foreach (var resourceId in entry.state.provider.activeResourceIds)
                {
                    if (!string.IsNullOrEmpty(resourceId))
                    {
                        counts.TryGetValue(resourceId, out int currentCount);
                        counts[resourceId] = currentCount + 1;
                    }
                }
            }
            else
            {
                // 정적 할당인 경우 profile의 outputs 사용
                var profile = entry.GetProviderProfile();
                if (profile?.outputs != null)
                {
                    foreach (var output in profile.outputs)
                    {
                        if (output?.resource != null && !string.IsNullOrEmpty(output.resource.id))
                        {
                            counts.TryGetValue(output.resource.id, out int currentCount);
                            counts[output.resource.id] = currentCount + 1;
                        }
                    }
                }
            }
        }

        return counts;
    }

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
    private void ProcessGlobalTradePort(Dictionary<string, ResourceEntry> resources)
    {
        if (resources == null)
        {
            return;
        }

        foreach (var res in resources.Values)
        {
            if (res?.resourceState == null || res.resourceData == null)
            {
                continue;
            }

            float currentPrice = res.resourceState.currentValue;
            float basePrice = res.resourceData.baseValue;
            float priceThreshold = _marketSettings != null ? _marketSettings.tradePortPriceThreshold : 1.3f;
            float targetPrice = basePrice * priceThreshold; // 기준가의 배율을 '적정 가격 상한선'으로 설정

            // [핵심] 가격 안정화 로직 (Price Stabilization)
            // 가격이 너무 비싸면 무역항에서 '저렴한 수입품'을 대량 공급하여 가격을 강제로 떨어뜨림
            if (currentPrice > targetPrice && basePrice > 0.01f)
            {
                // 가격이 비쌀수록 더 많은 물량을 공급 (시장 수요의 비율만큼 추가 공급)
                float supplyBoostRatio = _marketSettings != null ? _marketSettings.tradePortSupplyBoostRatio : 0.5f;
                float supplyBoost = res.resourceState.lastDemand * supplyBoostRatio;
                
                // 최소 공급량 보장
                float minSupply = _marketSettings != null ? _marketSettings.tradePortMinSupply : 100f;
                supplyBoost = Mathf.Max(supplyBoost, minSupply);

                res.resourceState.lastSupply += supplyBoost;
                _gameDataManager.Resource.ModifyMarketInventory(res.resourceData.id, (long)supplyBoost);

                // [핵심] 공급을 늘렸으니, 가격도 즉시 하락 압력을 줌 (수입품이 싸게 들어옴)
                float priceDropRate = _marketSettings != null ? _marketSettings.tradePortPriceDropRate : 0.9f;
                res.resourceState.currentValue *= priceDropRate;
            }
            // [수정] 안전 재고 유지 (Minimum Stock Maintenance)
            // 자원 타입에 따라 최소 유지량을 다르게 설정
            long minStockTarget = 100L; // 기본값

            // 1. 많이 쓰이는 중간재(Component, Essentials)는 재고를 넉넉히 확보
            if (res.resourceData.type == ResourceType.component || 
                res.resourceData.type == ResourceType.Essentials)
            {
                minStockTarget = 1000L; // 최소 1000개는 항상 깔려있게 함
            }
            // 2. 원자재는 더 많이
            else if (res.resourceData.type == ResourceType.raw)
            {
                minStockTarget = 2000L;
            }

            // 현재 재고가 목표치보다 적으면 부족분만큼 즉시 수입(채워넣기)
            if (res.resourceState.count < minStockTarget)
            {
                long deficit = minStockTarget - res.resourceState.count;
                
                // 너무 조금씩(1~2개) 채우면 연산 낭비니까, 한 번 채울 때 넉넉히(최소 100개 단위)
                long importAmount = Math.Max(deficit, 100L);

                res.resourceState.lastSupply += importAmount;
                _gameDataManager.Resource.ModifyMarketInventory(res.resourceData.id, importAmount);
                
                // 수입 비용 반영 (재고가 없어서 급히 채웠으니 가격 소폭 상승 유도)
                // 단, 너무 자주 발생하면 인플레가 오므로 1.01~1.05배 정도로 살짝만
                res.resourceState.currentValue *= 1.02f;
            }
            // [기존 로직 유지] 악성 재고 처리 (수출): 공급이 수요보다 너무 많고 가격이 너무 쌀 때만 수출
            float exportSurplusRatio = _marketSettings != null ? _marketSettings.tradePortExportSurplusRatio : 2.0f;
            float exportPriceThreshold = _marketSettings != null ? _marketSettings.tradePortExportPriceThreshold : 0.8f;
            if (res.resourceState.lastSupply > res.resourceState.lastDemand * exportSurplusRatio && currentPrice < basePrice * exportPriceThreshold)
            {
                float surplus = res.resourceState.lastSupply - res.resourceState.lastDemand;
                float dumpRatio = _marketSettings != null ? _marketSettings.tradePortExportDumpRatio : 0.5f;
                float dumpAmount = surplus * dumpRatio; // 잉여분의 비율만큼 걷어감
                
                // 시장에서 물량 제거 (수출됨)
                res.resourceState.lastSupply -= dumpAmount;
                _gameDataManager.Resource.ModifyMarketInventory(res.resourceData.id, -(long)dumpAmount);
            }
        }
    }

    /// <summary>
    /// 전쟁 상태를 설정합니다. 전쟁 발발 시 군사/정부 액터들의 예산과 긴급도를 조작합니다.
    /// </summary>
    public void SetWarState(bool isWar)
    {
        if (_isWarState == isWar)
        {
            return; // 이미 같은 상태
        }

        _isWarState = isWar;
        Debug.Log($"[MarketDataHandler] War state changed: {isWar}");

        foreach (var entry in _actors.Values)
        {
            if (entry?.data == null || entry.state == null)
            {
                continue;
            }

            // 시스템 액터는 제외
            if (entry.data.id == SYSTEM_POPULACE_ID || entry.data.id == SYSTEM_TRADE_PORT_ID)
            {
                continue;
            }

            // 군사/정부 태그가 있는 액터 찾기 (archetype이 Generalist이고 이름에 "Defense", "Military", "Armory", "Security" 포함)
            bool isMilitaryActor = entry.data.displayName.Contains("Defense") ||
                                   entry.data.displayName.Contains("Military") ||
                                   entry.data.displayName.Contains("Armory") ||
                                   entry.data.displayName.Contains("Security") ||
                                   entry.data.displayName.Contains("Armaments");

            if (isMilitaryActor)
            {
                var consumerProfile = entry.GetOrCreateConsumerProfile();
                if (consumerProfile != null && entry.state.consumer != null)
                {
                    if (isWar)
                    {
                        // 전쟁 시: 예산 폭증, 긴급도 상승
                        float warBudgetMult = _marketSettings != null ? _marketSettings.warBudgetMultiplier : 10f;
                        entry.state.consumer.currentBudget *= warBudgetMult;
                        
                        // 모든 desiredResources의 urgency를 최대로 설정
                        float warUrg = _marketSettings != null ? _marketSettings.warUrgency : 1.0f;
                        float warPriceSens = _marketSettings != null ? _marketSettings.warPriceSensitivity : 0.1f;
                        foreach (var pref in consumerProfile.desiredResources)
                        {
                            if (pref != null)
                            {
                                pref.urgency = warUrg; // 최대 긴급도
                                pref.priceSensitivity = warPriceSens; // 가격 불문하고 매수
                            }
                        }
                        
                        Debug.Log($"[MarketDataHandler] War mode activated for: {entry.data.displayName}");
                    }
                    else
                    {
                        // 평화 시: 예산 정상화
                        entry.ApplyInitialMarketSettings(_marketSettings);
                        
                        // urgency를 기본값으로 복원
                        float peaceUrg = _marketSettings != null ? _marketSettings.peaceUrgency : 0.25f;
                        float peacePriceSens = _marketSettings != null ? _marketSettings.peacePriceSensitivity : 0.55f;
                        foreach (var pref in consumerProfile.desiredResources)
                        {
                            if (pref != null)
                            {
                                pref.urgency = peaceUrg; // 기본 긴급도
                                pref.priceSensitivity = peacePriceSens; // 기본 가격 민감도
                            }
                        }
                        
                        Debug.Log($"[MarketDataHandler] Peace mode restored for: {entry.data.displayName}");
                    }
                }
            }
            else
            {
                // 민간 액터 (사치재 등)
                if (entry.data.displayName.Contains("Luxury") || 
                    entry.data.displayName.Contains("Fashion") ||
                    entry.data.displayName.Contains("Furniture"))
                {
                    if (isWar)
                    {
                        // 전쟁 시: 민간 위축 (예산 삭감)
                        float civilianReduction = _marketSettings != null ? _marketSettings.civilianBudgetReduction : 0.5f;
                        if (entry.state.consumer != null)
                        {
                            entry.state.consumer.currentBudget *= civilianReduction;
                        }
                        Debug.Log($"[MarketDataHandler] Civilian budget reduced for: {entry.data.displayName}");
                    }
                    else
                    {
                        // 평화 시: 예산 정상화
                        entry.ApplyInitialMarketSettings(_marketSettings);
                    }
                }
            }
        }

        // 시장 재계산 강제 실행 (선택사항)
        // TickDailyMarket();
    }

    /// <summary>
    /// 현재 전쟁 상태를 반환합니다.
    /// </summary>
    public bool IsWarState()
    {
        return _isWarState;
    }

    /// <summary>
    /// [솔루션 2] 국가 보조금 지급: 파산 직전 액터 구제 및 예산 부족 액터 지원
    /// 시스템 액터는 제외합니다.
    /// </summary>
    private void ProvideStimulusPackages()
    {
        foreach (var entry in _actors.Values)
        {
            if (entry?.state == null || entry.data == null)
            {
                continue;
            }

            // 시스템 액터는 제외 (무한 예산이므로 보조금 불필요)
            if (entry.data.id == SYSTEM_POPULACE_ID || entry.data.id == SYSTEM_TRADE_PORT_ID)
            {
                continue;
            }

            // 1. 파산 직전인 액터 구제
            float wealthThreshold = _marketSettings != null ? _marketSettings.stimulusWealthThreshold : 1000f;
            if (entry.state.wealth < wealthThreshold)
            {
                float subsidy = _marketSettings != null ? _marketSettings.stimulusSubsidyAmount : 500f;
                entry.state.wealth += subsidy;
                
                // (선택) 패널티: 건강도 감소 (좀비 기업이라는 표시)
                float healthPenalty = _marketSettings != null ? _marketSettings.stimulusHealthPenalty : 0.05f;
                entry.state.health = Mathf.Max(0.1f, entry.state.health - healthPenalty);
            }
            
            // 2. 예산이 없는 Consumer에게 "재난 지원금" 지급
            float budgetThreshold = _marketSettings != null ? _marketSettings.stimulusBudgetThreshold : 100f;
            if (entry.state.consumer != null && entry.state.consumer.currentBudget < budgetThreshold)
            {
                float disasterRelief = _marketSettings != null ? _marketSettings.stimulusDisasterRelief : 500f;
                entry.state.consumer.currentBudget += disasterRelief;
            }
        }
    }

}

