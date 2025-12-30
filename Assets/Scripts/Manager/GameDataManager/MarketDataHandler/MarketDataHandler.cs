using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 시장 행위자(Market Actor) 데이터를 관리하고, 일 단위로 공급과 수요를 시뮬레이션하여 자원 가격에 반영하는 핸들러입니다.
/// </summary>
public partial class MarketDataHandler
{
    // 상수 및 식별자 정의
    private const string ID_SYSTEM_POPULACE = "sys_populace";
    private const string ID_PREFIX_SYSTEM = "sys_";

    // 기본값 상수 (설정 파일 누락 시 사용)
    private const float DEFAULT_POPULACE_WEALTH = 1_000_000f;
    private const float MIN_SURVIVAL_WEALTH_LARGE = 100_000f;
    private const float MIN_SURVIVAL_WEALTH_SMALL = 10_000f;

    private readonly Dictionary<string, MarketActorEntry> _actors = new();
    private readonly DataManager _dataManager;

    private InitialMarketData _marketSettings;
    private bool _isWarState = false;

    // 캐싱된 리스트 (UpdateRevenueRankings에서 갱신된다고 가정)
    private List<MarketActorEntry> _cachedActorList = new();

    public event Action OnMarketUpdated;

    public MarketDataHandler(DataManager manager, List<MarketActorData> marketActorDataList, InitialMarketData initData)
    {
        _dataManager = manager;
        SetMarketSettings(initData);

        // 일반 액터 등록
        RegisterActors(marketActorDataList);

        // 시스템 액터 생성 및 등록
        CreateAndRegisterSystemActors();

        // 초기 자본금 설정 및 리소스 분배
        InitializeMarketState();
    }

    #region Initialization & Registration

    /// <summary>
    /// 시스템 운용에 필수적인 액터(예: 일반 시민)를 생성하고 등록합니다.
    /// </summary>
    private void CreateAndRegisterSystemActors()
    {
        if (_dataManager?.Resource == null) return;

        var populaceData = ScriptableObject.CreateInstance<MarketActorData>();
        populaceData.id = ID_SYSTEM_POPULACE;
        populaceData.displayName = "General Population";
        populaceData.description = "Stable demand for basic consumer goods.";
        populaceData.archetype = MarketActorArchetype.Generalist;
        populaceData.scale = MarketActorScale.Large;
        populaceData.useDynamicResourceAllocation = false;

        // 예산 범위 설정
        float minBudget = _marketSettings?.systemPopulaceBudget.x ?? 1_000_000f;
        float maxBudget = _marketSettings?.systemPopulaceBudget.y ?? 5_000_000f;

        populaceData.consumerProfile = new ConsumerProfile
        {
            budgetRange = new BudgetRange { min = minBudget, max = maxBudget },
            desiredResources = new List<ResourcePreference>(),
            patienceSeconds = 7200f,
            satisfactionDecay = 0.05f,
            allowBulkBuying = true,
            persistentOrders = true
        };

        // 생필품(Essentials) 수요 자동 추가
        var allResources = _dataManager.Resource.GetAllResources();
        foreach (var res in allResources.Values)
        {
            if (res?.resourceData?.type == ResourceType.Essentials)
            {
                populaceData.consumerProfile.desiredResources.Add(new ResourcePreference
                {
                    resource = res.resourceData,
                    desiredMin = (long)(_marketSettings?.systemPopulaceQuantityRange.x ?? 100f),
                    desiredMax = (long)(_marketSettings?.systemPopulaceQuantityRange.y ?? 300f),
                    priceSensitivity = _marketSettings?.systemPopulacePriceSensitivity ?? 1.2f,
                    urgency = _marketSettings?.systemPopulaceUrgency ?? 0.2f
                });
            }
        }

        RegisterActor(populaceData);
        ResetSystemActorState(populaceData.id); // 초기 상태 리셋
    }

    /// <summary>
    /// 게임 시작 시 초기 자원 재고 설정 및 액터 자본금 초기화
    /// </summary>
    private void InitializeMarketState()
    {
        if (_dataManager?.Resource == null) return;

        // 1. 자원 초기 재고 설정
        long defaultCount = _marketSettings?.initialResourceCount ?? 500L;
        foreach (var res in _dataManager.Resource.GetAllResources().Values)
        {
            if (res?.resourceState == null) continue;
            long count = (res.resourceData.type == ResourceType.raw) ? defaultCount * 2 : defaultCount;

            res.resourceState.count = count;
            res.resourceState.lastSupply = count * 0.5f;
        }

        // 2. 액터 초기 자본금 강제 주입
        foreach (var entry in _actors.Values)
        {
            if (IsSystemActor(entry.data)) continue;

            float targetWealth = entry.data.scale switch
            {
                MarketActorScale.Large => 2_000_000f,
                MarketActorScale.Medium => 500_000f,
                _ => 100_000f
            };

            // 초기 자본보다 적으면 리셋
            if (entry.state.wealth < targetWealth)
            {
                SetActorWealth(entry, targetWealth);
            }
        }

        OnMarketUpdated?.Invoke();
    }

    private void ResetSystemActorState(string actorId)
    {
        if (!_actors.TryGetValue(actorId, out var entry) || entry.state == null) return;

        float wealth = _marketSettings?.systemPopulaceWealth ?? DEFAULT_POPULACE_WEALTH;
        SetActorWealth(entry, wealth);

        // 예산은 Max치로 설정
        if (entry.state.consumer != null)
        {
            var profile = entry.GetConsumerProfile();
            entry.state.consumer.currentBudget = profile?.budgetRange.max ?? wealth * 0.2f;
        }
    }

    // 단일 액터 등록 (중복 체크 포함)
    public void RegisterActor(MarketActorData data)
    {
        if (data == null || string.IsNullOrEmpty(data.id)) return;
        if (_actors.ContainsKey(data.id))
        {
            Debug.LogWarning($"[Market] Actor already registered: {data.id}");
            return;
        }

        var entry = new MarketActorEntry(data);
        _actors[data.id] = entry;
        entry.ApplyInitialMarketSettings(_marketSettings);
    }

    // 다중 액터 등록
    public void RegisterActors(IEnumerable<MarketActorData> actors)
    {
        if (actors == null) return;
        foreach (var data in actors) RegisterActor(data);
    }

    #endregion

    #region Core Simulation Loop

    public void TickDailyMarket()
    {
        if (_dataManager?.Resource == null || _actors.Count == 0) return;
        var resourceSnapshot = _dataManager.Resource.GetAllResources();
        if (resourceSnapshot == null || resourceSnapshot.Count == 0) return;

        // 1. 경제 안전장치 (유동성 공급 및 구제 금융)
        ProcessEconomicSafetyNets();

        // 2. 자산 변화 추적 준비
        foreach (var entry in _actors.Values)
        {
            if (entry.state != null) entry.state.previousWealth = entry.state.wealth;
        }

        // 3. 공급(Supply) & 수요(Demand) 시뮬레이션 준비
        var totalSupply = new Dictionary<string, float>();
        var totalDemand = new Dictionary<string, float>();
        var globalProviderCounts = CalculateProviderCounts();

        // 4. 액터별 로직 수행
        foreach (var entry in _actors.Values)
        {
            if (ShouldSkipActor(entry)) continue;

            // 역할 할당 및 상태 갱신
            MarketActorDynamicAllocator.UpdateAssignments(entry, resourceSnapshot, _marketSettings, globalProviderCounts);
            RefreshActor(entry);
            ResetDailyTradingStats(entry);

            // 생산 및 소비 계획 수립
            SimulateProviderProduction(entry, resourceSnapshot, totalSupply);
            SimulateConsumerDemand(entry, resourceSnapshot, totalDemand);
        }

        // 5. 거래 체결 및 후처리
        ExecuteTrades(resourceSnapshot, totalSupply, totalDemand);
        ApplyPriceAdjustments(resourceSnapshot, totalSupply, totalDemand);
        ApplyBusinessHealthEffects();
        UpdateRevenueRankings();

        // 6. 무역 및 플레이어 자동화
        ProcessGlobalTradePort(resourceSnapshot);
        ExecutePlayerAutoTrades(resourceSnapshot);

        OnMarketUpdated?.Invoke();
    }

    private bool ShouldSkipActor(MarketActorEntry entry)
    {
        return entry?.state == null || entry.state.wealth <= 0.1f;
    }

    #endregion

    #region Economic Logic (Safety Nets)

    /// <summary>
    /// 시장 유동성 공급 및 구제 금융을 통합 처리합니다.
    /// </summary>
    private void ProcessEconomicSafetyNets()
    {
        foreach (var entry in _actors.Values)
        {
            if (entry?.state == null || IsSystemActor(entry.data)) continue;

            // 1. 절대 방어선 (Liquidity)
            float survivalThreshold = (entry.data.scale == MarketActorScale.Large) ? MIN_SURVIVAL_WEALTH_LARGE : MIN_SURVIVAL_WEALTH_SMALL;
            if (entry.state.wealth < survivalThreshold)
            {
                RescueActor(entry, survivalThreshold * 5f, penalty: false);
                continue; // 방어선 혜택을 받으면 구제 금융 체크 건너뜀
            }

            // 2. 빈곤선 구제 금융 (Stimulus)
            float stimulusThreshold = (_marketSettings?.stimulusWealthThreshold ?? 1000f) * ((entry.data.scale == MarketActorScale.Large) ? 100f : 10f);

            if (entry.state.wealth < stimulusThreshold)
            {
                RescueActor(entry, stimulusThreshold * 2f, penalty: true);
            }

            // 3. 극빈층 소비자 긴급 지원
            if (entry.state.consumer != null && entry.state.consumer.currentBudget < 100f)
            {
                entry.state.consumer.currentBudget += 5000f;
            }
        }
    }

    private void RescueActor(MarketActorEntry entry, float targetWealth, bool penalty)
    {
        float bailoutAmount = targetWealth - entry.state.wealth;
        if (bailoutAmount <= 0) return;

        entry.state.wealth += bailoutAmount;

        // 그래프 튀는 현상 방지
        if (entry.state.previousWealth <= 1f) entry.state.previousWealth = targetWealth;

        // 예산 복구
        if (entry.state.consumer != null)
        {
            entry.state.consumer.currentBudget = Mathf.Max(entry.state.consumer.currentBudget, targetWealth * 0.2f);
        }

        // 구제 금융 패널티 (신용 하락)
        if (penalty)
        {
            float healthDrop = _marketSettings?.stimulusHealthPenalty ?? 0.1f;
            entry.state.health = Mathf.Max(0.1f, entry.state.health - healthDrop);
        }
    }

    private void SetActorWealth(MarketActorEntry entry, float amount)
    {
        if (entry.state == null) return;
        entry.state.wealth = amount;
        entry.state.previousWealth = amount;
        entry.state.health = 1.0f;
    }

    #endregion

    #region War State Logic

    public void SetWarState(bool isWar)
    {
        if (_isWarState == isWar) return;
        _isWarState = isWar;
        Debug.Log($"[Market] War State Changed: {isWar}");

        foreach (var entry in _actors.Values)
        {
            if (entry?.data == null || entry.state?.consumer == null || IsSystemActor(entry.data)) continue;

            var profile = entry.GetOrCreateConsumerProfile();
            if (profile == null) continue;

            if (IsMilitaryActor(entry.data))
            {
                ApplyMilitaryPolicy(entry, profile, isWar);
            }
            else if (IsLuxuryActor(entry.data))
            {
                ApplyCivilianPolicy(entry, isWar);
            }
        }
        OnMarketUpdated?.Invoke();
    }

    public bool IsWarState() => _isWarState;

    private void ApplyMilitaryPolicy(MarketActorEntry entry, ConsumerProfile profile, bool isWar)
    {
        if (isWar)
        {
            // 전쟁: 예산 5배, 긴급도 상승, 가격 불문 구매
            entry.state.consumer.currentBudget *= (_marketSettings?.warBudgetMultiplier ?? 5.0f);
            UpdateProfileSensitivity(profile,
                urgency: _marketSettings?.warUrgency ?? 1.0f,
                sensitivity: _marketSettings?.warPriceSensitivity ?? 0.1f);
        }
        else
        {
            // 평화: 초기화
            entry.ApplyInitialMarketSettings(_marketSettings);
            UpdateProfileSensitivity(profile,
                urgency: _marketSettings?.peaceUrgency ?? 0.25f,
                sensitivity: _marketSettings?.peacePriceSensitivity ?? 0.55f);
        }
    }

    private void ApplyCivilianPolicy(MarketActorEntry entry, bool isWar)
    {
        if (isWar)
        {
            // 사치재 예산 감축
            entry.state.consumer.currentBudget *= (_marketSettings?.civilianBudgetReduction ?? 0.5f);
        }
        else
        {
            entry.ApplyInitialMarketSettings(_marketSettings);
        }
    }

    private void UpdateProfileSensitivity(ConsumerProfile profile, float urgency, float sensitivity)
    {
        foreach (var r in profile.desiredResources)
        {
            r.urgency = urgency;
            r.priceSensitivity = sensitivity;
        }
    }

    // 이름 기반 판별 (추후 태그 시스템으로 변경 권장)
    private bool IsMilitaryActor(MarketActorData data)
    {
        if (data.archetype == MarketActorArchetype.Generalist) return true;
        string n = data.displayName;
        return n.Contains("Defense") || n.Contains("Military") || n.Contains("Armory") || n.Contains("Security");
    }

    private bool IsLuxuryActor(MarketActorData data)
    {
        return data.displayName.Contains("Luxury") || data.displayName.Contains("Fashion");
    }

    #endregion

    #region Getters & Setters

    public void SetMarketSettings(InitialMarketData settings)
    {
        _marketSettings = settings;
        if (_marketSettings != null)
        {
            foreach (var entry in _actors.Values) entry?.ApplyInitialMarketSettings(_marketSettings);
        }
    }

    public float GetMarketFeeRate() => _marketSettings?.marketFeeRate ?? 0.05f;

    public Dictionary<string, MarketActorEntry> GetAllActors() => new(_actors);

    private bool IsSystemActor(MarketActorData data) => data != null && data.id.StartsWith(ID_PREFIX_SYSTEM);

    // 정렬된 리스트 반환 (GC 할당을 줄이기 위해 캐시된 리스트 활용 권장)
    public List<MarketActorEntry> GetActorsSortedByWealth(bool ascending = false)
    {
        // _cachedActorList는 Tick 루프의 UpdateRevenueRankings에서 정렬된다고 가정
        if (!ascending) return new List<MarketActorEntry>(_cachedActorList); // 내림차순 (기본)

        var list = new List<MarketActorEntry>(_cachedActorList);
        list.Reverse();
        return list;
    }

    public int GetActorWealthRank(string actorId)
    {
        // 선형 탐색 (리스트가 크다면 Dictionary<Id, Rank> 캐싱 고려 필요)
        for (int i = 0; i < _cachedActorList.Count; i++)
        {
            if (_cachedActorList[i].data?.id == actorId) return i + 1;
        }
        return -1;
    }

    public MarketActorEntry GetActor(string actorId)
    {
        if (_actors.TryGetValue(actorId, out var entry)) return entry;
        return null;
    }

    public void ClearAllSubscriptions() => OnMarketUpdated = null;

    #endregion
}