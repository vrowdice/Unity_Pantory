using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 시장 행위자(Market Actor) 데이터를 관리하고, 일 단위로 공급과 수요를 시뮬레이션하여 자원 가격에 반영하는 핸들러입니다.
/// 시장 풀(Market Pool) 방식을 사용하여 O(N+M) 복잡도로 효율적인 거래를 처리합니다.
/// </summary>
public partial class MarketDataHandler
{
    private readonly Dictionary<string, MarketActorEntry> _actors = new();
    private readonly DataManager _dataManager;

    private InitialMarketData _marketSettings;
    private bool _isWarState = false;

    private const string SYSTEM_POPULACE_ID = "sys_populace";

    /// <summary>
    /// 시장 데이터가 업데이트되었을 때 호출되는 이벤트입니다.
    /// </summary>
    public event Action OnMarketUpdated;

    public MarketDataHandler(DataManager manager, List<MarketActorData> marketActorDataList, InitialMarketData initData)
    {
        _dataManager = manager;
        SetMarketSettings(initData);

        // 리스트에서 딕셔너리로 등록
        if (marketActorDataList != null && marketActorDataList.Count > 0)
        {
            foreach (var data in marketActorDataList)
            {
                if (data == null || string.IsNullOrEmpty(data.id)) continue;
                if (_actors.ContainsKey(data.id))
                {
                    Debug.LogWarning($"[MarketDataHandler] Actor already registered: {data.id}");
                    continue;
                }

                var entry = new MarketActorEntry(data);
                _actors[data.id] = entry;
                entry.ApplyInitialMarketSettings(_marketSettings);
            }
        }

        CreateSystemActors();
        InitializeMarketChaos();
        InitializeSystemActors();
    }

    /// <summary>
    /// 시스템 운용에 필수적인 액터(예: 일반 시민)를 생성하고 등록합니다.
    /// </summary>
    private void CreateSystemActors()
    {
        if (_dataManager?.Resource == null) return;
        var allResources = _dataManager.Resource.GetAllResources();
        if (allResources == null || allResources.Count == 0) return;

        // 1. 일반 시민 (기초 수요 담당) 생성
        var populaceData = ScriptableObject.CreateInstance<MarketActorData>();
        populaceData.id = SYSTEM_POPULACE_ID;
        populaceData.displayName = "General Population";
        populaceData.description = "Stable demand for basic consumer goods.";
        populaceData.archetype = MarketActorArchetype.Generalist;
        populaceData.scale = MarketActorScale.Large;
        populaceData.useDynamicResourceAllocation = false;

        // Consumer 프로필 설정
        populaceData.consumerProfile = new ConsumerProfile
        {
            budgetRange = new BudgetRange
            {
                min = _marketSettings?.systemPopulaceBudget.x ?? 1000000f,
                max = _marketSettings?.systemPopulaceBudget.y ?? 5000000f
            },
            desiredResources = new List<ResourcePreference>(),
            patienceSeconds = 7200f,
            satisfactionDecay = 0.05f,
            allowBulkBuying = true,
            persistentOrders = true
        };

        // 생필품(Essentials) 수요 설정
        var basicConsumerTypes = new[] { ResourceType.Essentials };
        foreach (var resource in allResources.Values)
        {
            if (resource?.resourceData == null) continue;

            bool isTarget = System.Array.IndexOf(basicConsumerTypes, resource.resourceData.type) >= 0;
            if (isTarget)
            {
                populaceData.consumerProfile.desiredResources.Add(new ResourcePreference
                {
                    resource = resource.resourceData,
                    desiredMin = (long)(_marketSettings?.systemPopulaceQuantityRange.x ?? 100f),
                    desiredMax = (long)(_marketSettings?.systemPopulaceQuantityRange.y ?? 300f),
                    priceSensitivity = _marketSettings?.systemPopulacePriceSensitivity ?? 1.2f,
                    urgency = _marketSettings?.systemPopulaceUrgency ?? 0.2f
                });
            }
        }

        RegisterActor(populaceData);
    }

    /// <summary>
    /// 시스템 액터들의 상태를 초기화합니다 (예: 시민의 무한 예산 설정).
    /// </summary>
    public void InitializeSystemActors()
    {
        if (_actors.TryGetValue(SYSTEM_POPULACE_ID, out var populaceEntry) && populaceEntry?.state != null)
        {
            float populaceWealth = _marketSettings?.systemPopulaceWealth ?? 1000000f;

            // 일반 시민은 마르지 않는 자산을 보유
            populaceEntry.state.wealth = populaceWealth;
            populaceEntry.state.previousWealth = populaceWealth;
            populaceEntry.state.health = 1f;

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

    /// <summary>
    /// 게임 시작 시 자원 재고 충전 및 액터 초기 자금 강제 주입
    /// </summary>
    public void InitializeMarketChaos()
    {
        if (_dataManager?.Resource == null) return;
        var allResources = _dataManager.Resource.GetAllResources();
        if (allResources == null) return;

        // 1. 자원 초기 재고 설정 (원자재는 더 많이)
        foreach (var res in allResources.Values)
        {
            if (res?.resourceState == null || res.resourceData == null) continue;

            long initialCount = (res.resourceData.type == ResourceType.raw)
                ? (_marketSettings?.initialResourceCount * 2L ?? 2000L)
                : (_marketSettings?.initialResourceCount ?? 500L);

            res.resourceState.count = initialCount;
            res.resourceState.lastSupply = initialCount * 0.5f;
        }

        // 2. 액터 초기 자본금(Startup Capital) 강제 지급
        foreach (var actor in _actors.Values)
        {
            if (actor?.state == null || actor.data == null) continue;
            if (actor.data.id.StartsWith("sys_")) continue; // 시스템 액터 제외

            float targetWealth = actor.data.scale switch
            {
                MarketActorScale.Large => 2000000f,
                MarketActorScale.Medium => 500000f,
                _ => 100000f
            };

            // 목표 자산보다 적다면 강제 리셋
            if (actor.state.wealth < targetWealth)
            {
                actor.state.wealth = targetWealth;
                actor.state.previousWealth = targetWealth;
                actor.state.health = 1.0f;
            }

            // 예산 재설정
            if (actor.state.consumer != null)
            {
                var profile = actor.GetConsumerProfile();
                actor.state.consumer.currentBudget = (profile != null && profile.budgetRange.max > 0f)
                    ? profile.budgetRange.max
                    : actor.state.wealth * 0.2f;
            }
        }

        OnMarketUpdated?.Invoke();
    }

    /// <summary>
    /// 현재 적용된 시장 수수료율을 반환합니다.
    /// </summary>
    public float GetMarketFeeRate()
    {
        return _marketSettings?.marketFeeRate ?? 0.05f;
    }

    /// <summary>
    /// 마켓 시뮬레이션 설정 데이터를 적용합니다.
    /// </summary>
    public void SetMarketSettings(InitialMarketData settings)
    {
        _marketSettings = settings;
        if (_marketSettings != null)
        {
            foreach (var entry in _actors.Values)
            {
                entry?.ApplyInitialMarketSettings(_marketSettings);
            }
        }
    }

    /// <summary>
    /// 하루 단위 시장 시뮬레이션을 실행합니다. (핵심 로직)
    /// </summary>
    public void TickDailyMarket()
    {
        if (_dataManager?.Resource == null || _actors.Count == 0) return;

        var resourceSnapshot = _dataManager.Resource.GetAllResources();
        if (resourceSnapshot == null || resourceSnapshot.Count == 0) return;

        var totalSupply = new Dictionary<string, float>();
        var totalDemand = new Dictionary<string, float>();

        // 경제 안전장치 가동
        EnsureMarketLiquidity();   // 유동성 고갈 방지
        ProvideStimulusPackages(); // 구제 금융

        // 자산 변화 추적을 위한 이전 틱 자산 저장
        foreach (var entry in _actors.Values)
        {
            if (entry?.state != null) entry.state.previousWealth = entry.state.wealth;
        }

        // 액터 상태 갱신 및 직업 할당
        // 경쟁자 수 계산 (과열 방지 로직용)
        var globalProviderCounts = CalculateProviderCounts();

        foreach (var entry in _actors.Values)
        {
            if (ShouldSkipActor(entry)) continue;

            MarketActorDynamicAllocator.UpdateAssignments(entry, resourceSnapshot, _marketSettings, globalProviderCounts);
            RefreshActor(entry);
            ResetDailyTradingStats(entry);
        }

        // 생산 (Supply) 시뮬레이션
        foreach (var entry in _actors.Values)
        {
            if (ShouldSkipActor(entry)) continue;
            SimulateProviderProduction(entry, resourceSnapshot, totalSupply);
        }

        // 소비 (Demand) 시뮬레이션
        foreach (var entry in _actors.Values)
        {
            if (ShouldSkipActor(entry)) continue;
            SimulateConsumerDemand(entry, resourceSnapshot, totalDemand);
        }

        // 거래 체결 (Market Pool 방식)
        ExecuteTrades(resourceSnapshot, totalSupply, totalDemand);

        // 후처리 (가격 변동, 랭킹 갱신, 무역)
        ApplyPriceAdjustments(resourceSnapshot, totalSupply, totalDemand);
        ApplyBusinessHealthEffects();
        UpdateRevenueRankings();

        // 무역항 처리 (수출/수입)
        ProcessGlobalTradePort(resourceSnapshot);

        // 플레이어 자동 거래
        ExecutePlayerAutoTrades(resourceSnapshot);

        OnMarketUpdated?.Invoke();
    }

    /// <summary>
    /// 액터가 경제 활동에서 제외되어야 하는지 확인합니다 (파산 상태 등).
    /// </summary>
    private bool ShouldSkipActor(MarketActorEntry entry)
    {
        return entry?.state == null || entry.state.wealth <= 0.1f;
    }

    /// <summary>
    /// [절대 방어선] 매 틱마다 자산이 비정상적으로 낮은 액터에게 생존 자금을 강제로 주입합니다.
    /// </summary>
    private void EnsureMarketLiquidity()
    {
        foreach (var entry in _actors.Values)
        {
            if (entry?.state == null || entry.data == null) continue;
            if (entry.data.id.StartsWith("sys_")) continue;

            // 최소 생존 자금 (대기업 10만, 중소 1만)
            float survivalWealth = entry.data.scale == MarketActorScale.Large ? 100000f : 10000f;

            if (entry.state.wealth < survivalWealth)
            {
                float targetWealth = survivalWealth * 5f;
                entry.state.wealth = targetWealth;

                // 그래프가 튀는 것을 방지
                if (entry.state.previousWealth <= 1f) entry.state.previousWealth = targetWealth;

                // 예산 복구
                if (entry.state.consumer != null && entry.state.consumer.currentBudget < 1000f)
                {
                    entry.state.consumer.currentBudget = targetWealth * 0.2f;
                }
            }
        }
    }

    /// <summary>
    /// [구제 금융] 빈곤선 이하로 떨어진 기업에게 자금을 지원하고 페널티(신용 하락)를 부여합니다.
    /// </summary>
    private void ProvideStimulusPackages()
    {
        foreach (var entry in _actors.Values)
        {
            if (entry?.state == null || entry.data == null) continue;
            if (entry.data.id.StartsWith("sys_")) continue;

            float thresholdMultiplier = (entry.data.scale == MarketActorScale.Large) ? 100f : 10f;
            float baseThreshold = _marketSettings?.stimulusWealthThreshold ?? 1000f;
            float povertyLine = baseThreshold * thresholdMultiplier;

            // 빈곤선 미만일 경우 구제 금융 실행
            if (entry.state.wealth < povertyLine)
            {
                float bailout = (povertyLine * 2f) - entry.state.wealth;
                entry.state.wealth += bailout;

                // 패널티: 건강도(신용) 하락
                float healthPenalty = _marketSettings?.stimulusHealthPenalty ?? 0.1f;
                entry.state.health = Mathf.Max(0.1f, entry.state.health - healthPenalty);

                // 예산 재조정
                if (entry.state.consumer != null)
                {
                    entry.state.consumer.currentBudget = entry.state.wealth * 0.2f;
                }
            }

            // 극빈층 긴급 지원
            if (entry.state.consumer != null && entry.state.consumer.currentBudget < 100f)
            {
                entry.state.consumer.currentBudget += 5000f;
            }
        }
    }

    /// <summary>
    /// 전쟁 상태를 활성화하거나 비활성화하며, 이에 따른 액터(군수/민간)들의 예산 및 소비 패턴을 조정합니다.
    /// </summary>
    /// <param name="isWar">전쟁 발발 여부</param>
    public void SetWarState(bool isWar)
    {
        if (_isWarState == isWar) return;
        _isWarState = isWar;
        Debug.Log($"[Market] War State Changed: {isWar}");

        foreach (var entry in _actors.Values)
        {
            if (entry?.data == null || entry.state?.consumer == null) continue;
            if (entry.data.id.StartsWith("sys_")) continue;

            var profile = entry.GetOrCreateConsumerProfile();
            if (profile == null) continue;

            if (IsMilitaryActor(entry.data))
            {
                ApplyMilitaryWarState(entry, profile, isWar);
            }
            else if (IsLuxuryActor(entry.data))
            {
                ApplyCivilianWarState(entry, isWar);
            }
        }

        OnMarketUpdated?.Invoke();
    }

    /// <summary>
    /// 현재 전쟁 상태인지 여부를 반환합니다.
    /// </summary>
    public bool IsWarState() => _isWarState;

    private bool IsMilitaryActor(MarketActorData data)
    {
        if (data.archetype == MarketActorArchetype.Generalist) return true;
        string name = data.displayName;
        return name.Contains("Defense") || name.Contains("Military") || name.Contains("Armory") || name.Contains("Security");
    }

    private bool IsLuxuryActor(MarketActorData data)
    {
        string name = data.displayName;
        return name.Contains("Luxury") || name.Contains("Fashion");
    }

    private void ApplyMilitaryWarState(MarketActorEntry entry, ConsumerProfile profile, bool isWar)
    {
        if (isWar)
        {
            // 전쟁 시: 예산 5배, 긴급도 최대, 가격 민감도 하락(무조건 구매)
            float multiplier = _marketSettings?.warBudgetMultiplier ?? 5.0f;
            entry.state.consumer.currentBudget *= multiplier;

            float urgency = _marketSettings?.warUrgency ?? 1.0f;
            float sens = _marketSettings?.warPriceSensitivity ?? 0.1f;
            SetProfileParams(profile, urgency, sens);
        }
        else
        {
            // 평화 시: 초기 설정 복구
            entry.ApplyInitialMarketSettings(_marketSettings);

            float urgency = _marketSettings?.peaceUrgency ?? 0.25f;
            float sens = _marketSettings?.peacePriceSensitivity ?? 0.55f;
            SetProfileParams(profile, urgency, sens);
        }
    }

    private void ApplyCivilianWarState(MarketActorEntry entry, bool isWar)
    {
        if (isWar)
        {
            // 전쟁 시: 민간(사치재) 예산 감축
            float reduction = _marketSettings?.civilianBudgetReduction ?? 0.5f;
            entry.state.consumer.currentBudget *= reduction;
        }
        else
        {
            entry.ApplyInitialMarketSettings(_marketSettings);
        }
    }

    private void SetProfileParams(ConsumerProfile profile, float urgency, float sensitivity)
    {
        foreach (var r in profile.desiredResources)
        {
            r.urgency = urgency;
            r.priceSensitivity = sensitivity;
        }
    }

    /// <summary>
    /// 새로운 액터를 시스템에 등록합니다.
    /// </summary>
    public void RegisterActor(MarketActorData data)
    {
        if (data == null || string.IsNullOrEmpty(data.id)) return;
        if (_actors.ContainsKey(data.id))
        {
            Debug.LogWarning($"[MarketDataHandler] Actor already registered: {data.id}");
            return;
        }

        var entry = new MarketActorEntry(data);
        _actors[data.id] = entry;
        entry.ApplyInitialMarketSettings(_marketSettings);
    }

    public void RegisterActors(IEnumerable<MarketActorData> actors)
    {
        foreach (var data in actors)
        {
            var entry = new MarketActorEntry(data);
            _actors[data.id] = entry;
            entry.ApplyInitialMarketSettings(_marketSettings);
        }
    }


    public Dictionary<string, MarketActorEntry> GetAllActors()
    {
        return new Dictionary<string, MarketActorEntry>(_actors);
    }

    /// <summary>
    /// 자산(Wealth) 기준으로 정렬된 액터 리스트를 반환합니다. (캐시된 데이터 활용)
    /// </summary>
    public List<MarketActorEntry> GetActorsSortedByWealth(bool ascending = false)
    {
        // UpdateRevenueRankings에서 갱신된 _cachedActorList 사용
        if (!ascending) return new List<MarketActorEntry>(_cachedActorList); // 내림차순 (기본)

        var list = new List<MarketActorEntry>(_cachedActorList);
        list.Reverse();
        return list;
    }

    /// <summary>
    /// 특정 액터의 자산 순위를 반환합니다. (1위부터 시작)
    /// </summary>
    public int GetActorWealthRank(string actorId)
    {
        for (int i = 0; i < _cachedActorList.Count; i++)
        {
            if (_cachedActorList[i].data?.id == actorId) return i + 1;
        }
        return -1;
    }
}