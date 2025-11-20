using System;
using System.Collections.Generic;
using System.Linq; // LINQ 추가
using UnityEngine;

[Serializable]
public struct DailyExpenseReservation
{
    public long maintenanceCost;      
    public long salaryCost;           
    public long resourceShortageCost; 
    public long playerTradeCost;      
    public long playerTradeRevenue;   
    
    public long TotalExpenses => maintenanceCost + salaryCost + resourceShortageCost + playerTradeCost;
    public long NetDelta => playerTradeRevenue - TotalExpenses;
}

public class GameDataManager : MonoBehaviour
{
    // ==================== 싱글톤 ====================
    public static GameDataManager Instance { get; private set; }
    
    // ==================== 일일 비용 예약 ====================
    private DailyExpenseReservation _reservedDailyExpenses;
    public DailyExpenseReservation ReservedDailyExpenses => _reservedDailyExpenses;
    public bool IsProcessingReservedExpenses { get; private set; } = false;

    #region 인스펙터 설정 (Initial Data)
    [Header("Initial Data")]
    [SerializeField] private InitialResourceData _initialResourceData;
    [SerializeField] private InitialMarketData _initialMarketData;

    [Header("Time Settings")]
    [SerializeField] private TimeSettingsData _timeSettingsData;
    #endregion

    #region 데이터 핸들러 (Services - Public Access)
    // 외부에서는 GameDataManager.Instance.Resource.SomeMethod() 형태로 접근합니다.
    public TimeDataHandler Time { get; private set; }
    public ThreadDataHandler Thread { get; private set; }
    public ThreadPlacementDataHandler ThreadPlacement { get; private set; }
    public ResourceDataHandler Resource { get; private set; }
    public MarketDataHandler Market { get; private set; }
    public FinancesDataHandler Finances { get; private set; }
    public EmployeeDataHandler Employee { get; private set; }
    public BuildingDataHandler Building { get; private set; }
    public SaveLoadHandler SaveLoad { get; private set; }
    #endregion

    #region 이벤트 중계 (주요 이벤트만 유지)
    // UI 등에서 자주 쓰이는 핵심 이벤트는 중계 유지 (선택 사항)
    public event Action OnResourceChanged
    {
        add => Resource.OnResourceChanged += value;
        remove => Resource.OnResourceChanged -= value;
    }
    public event Action OnSilverChanged
    {
        add => Finances.OnCreditChanged += value;
        remove => Finances.OnCreditChanged -= value;
    }
    public event Action OnThreadPlacementChanged;
    #endregion

    #region 초기화 및 Unity 생명주기

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        InitializeServices();
    }

    void Update()
    {
        Time?.Update(UnityEngine.Time.deltaTime);
    }

    void OnDestroy()
    {
        if (ThreadPlacement != null) ThreadPlacement.OnPlacementChanged -= HandleThreadPlacementChanged;
        if (Time != null) Time.OnDayChanged -= HandleDayChanged;
    }

    private void InitializeServices()
    {
        SaveLoad = new SaveLoadHandler(this);
        Thread = new ThreadDataHandler(this);
        
        ThreadPlacement = new ThreadPlacementDataHandler(this);
        ThreadPlacement.OnPlacementChanged += HandleThreadPlacementChanged;
        
        Time = new TimeDataHandler(this);
        Time.OnDayChanged += HandleDayChanged;
        
        Resource = new ResourceDataHandler(this);
        Market = new MarketDataHandler(this);
        Finances = new FinancesDataHandler(this);
        Employee = new EmployeeDataHandler(this);
        Building = new BuildingDataHandler(this);

        Debug.Log("[GameDataManager] All services initialized.");

        // 초기 데이터 적용
        _timeSettingsData?.ApplyToTimeService(Time);
        _initialResourceData?.ApplyToServices(Resource, Finances);
        _initialMarketData?.ApplyToMarket(Market);

        // 데이터 로드 및 초기 상태 계산
        LoadThreadData();
        UpdateResourceDeltasFromPlacedThreads(); // 초기 델타 계산
        ReserveDailyExpenses();                  // 초기 비용 계산
    }
    #endregion

    #region 저장/로드 로직
    private void LoadThreadData()
    {
        if (SaveLoad != null && Thread != null)
        {
            if (!SaveLoad.LoadThreadData(Thread))
            {
                Debug.LogWarning("[GameDataManager] Failed to load Thread data. Resetting.");
                Thread.ResetThreadData();
                ThreadPlacement?.ClearAll();
            }
        }
    }
    #endregion

    #region 일일 로직 (Day Change & Expenses)

    private void HandleDayChanged()
    {
        // 1. 다음 날 비용 예약 (현재 상태 기준)
        ReserveDailyExpenses();
        
        // 2. 예약된 비용 처리 (자금 차감/지급)
        ApplyReservedDailyExpenses();
        
        // 3. 자원 증감 적용 (생산/소비)
        Resource?.ApplyResourceDeltas();
        
        // 4. 배치 변경에 따른 델타 갱신 (배치가 바뀌지 않았어도 일일 루틴으로 호출)
        HandleThreadPlacementChanged();
        
        // 5. 시장 업데이트 (가격 변동 및 자동 거래 실행)
        Market?.TickDailyMarket();
    }

    /// <summary>
    /// 현재 배치된 스레드들의 자원 생산/소비 총합과 유지비를 계산하여 반환하는 헬퍼 함수
    /// (중복 로직 제거용)
    /// </summary>
    private void CalculateThreadAggregates(out long totalMaintenance, out Dictionary<string, long> totalProduction, out Dictionary<string, long> totalConsumption)
    {
        totalMaintenance = 0;
        totalProduction = new Dictionary<string, long>();
        totalConsumption = new Dictionary<string, long>();

        var placedThreads = ThreadPlacement.GetAllPlacedThreads();
        if (placedThreads == null) return;

        foreach (var placement in placedThreads.Values)
        {
            if (placement == null || string.IsNullOrEmpty(placement.ThreadId)) continue;

            var threadState = Thread.GetThread(placement.ThreadId);
            if (threadState == null) continue;

            // 유지비 합산
            totalMaintenance += threadState.totalMaintenanceCost;

            // 자원 합산
            if (threadState.TryGetAggregatedResourceCounts(out var cons, out var prod))
            {
                AddToDict(totalProduction, prod);
                AddToDict(totalConsumption, cons);
            }
        }

        // 로컬 헬퍼
        void AddToDict(Dictionary<string, long> target, Dictionary<string, int> source)
        {
            if (source == null) return;
            foreach (var kvp in source)
            {
                if (target.ContainsKey(kvp.Key)) target[kvp.Key] += kvp.Value;
                else target[kvp.Key] = kvp.Value;
            }
        }
    }

    /// <summary>
    /// 일일 예상 크레딧 변화량을 반환합니다 (예약된 비용 기준)
    /// </summary>
    public long CalculateDailyCreditDelta()
    {
        return _reservedDailyExpenses.NetDelta;
    }
    
    public void ReserveDailyExpenses()
    {
        _reservedDailyExpenses = new DailyExpenseReservation();

        // 1. 스레드 집계 (유지비, 생산, 소비)
        CalculateThreadAggregates(out long maintenanceCost, out var totalProduction, out var totalConsumption);

        // 2. 자원 부족 비용 계산
        long shortageCost = CalculateResourceShortageCost(totalProduction, totalConsumption);

        // 3. 플레이어 자동 거래 비용/수익 계산
        CalculatePlayerTradeEconomics(totalProduction, totalConsumption, out long tradeCost, out long tradeRevenue);

        // 4. 직원 급여
        long salaryCost = Employee != null ? Employee.GetTotalSalary() : 0;
        _reservedDailyExpenses.maintenanceCost = maintenanceCost;
        _reservedDailyExpenses.salaryCost = salaryCost;
        _reservedDailyExpenses.resourceShortageCost = shortageCost;
        _reservedDailyExpenses.playerTradeCost = tradeCost;
        _reservedDailyExpenses.playerTradeRevenue = tradeRevenue;

        Debug.Log($"[GameDataManager] Daily expenses reserved. Net: {_reservedDailyExpenses.NetDelta}");
    }

    private long CalculateResourceShortageCost(Dictionary<string, long> production, Dictionary<string, long> consumption)
    {
        long totalCost = 0;
        var allResources = Resource.GetAllResources();

        foreach (var kvp in consumption)
        {
            string id = kvp.Key;
            long consumeAmount = kvp.Value;
            long produceAmount = production.ContainsKey(id) ? production[id] : 0;
            long currentAmount = Resource.GetResourceQuantity(id);

            // 예상 보유량 = 현재 + 생산 - 소비
            long expectedAmount = currentAmount + produceAmount - consumeAmount;

            if (expectedAmount < 0)
            {
                long shortage = -expectedAmount;
                if (allResources.TryGetValue(id, out var entry))
                {
                    float price = entry.resourceState?.currentValue ?? 0f;
                    totalCost += (long)Math.Ceiling(price * shortage);
                }
            }
        }
        return totalCost;
    }

    private void CalculatePlayerTradeEconomics(Dictionary<string, long> production, Dictionary<string, long> consumption, out long cost, out long revenue)
    {
        cost = 0;
        revenue = 0;
        
        var allResources = Resource.GetAllResources();
        float feeRate = Market.GetMarketFeeRate();

        foreach (var kvp in allResources)
        {
            string id = kvp.Key;
            var state = kvp.Value.resourceState;
            if (state == null || state.playerTransactionDelta == 0) continue;

            long delta = state.playerTransactionDelta;
            float price = state.currentValue;

            if (delta > 0) // 매수
            {
                long baseCost = (long)Mathf.Ceil(price * delta);
                cost += baseCost + (long)Mathf.Ceil(baseCost * feeRate);
            }
            else // 매도
            {
                long sellRequest = -delta;
                long current = Resource.GetResourceQuantity(id);
                long prod = production.ContainsKey(id) ? production[id] : 0;
                long cons = consumption.ContainsKey(id) ? consumption[id] : 0;

                // 델타 적용 후 예상 보유량 기준 판매 가능 수량 확인
                long expectedAmount = current + prod - cons;
                long actualSell = Math.Max(0, Math.Min(sellRequest, expectedAmount));

                if (actualSell > 0)
                {
                    long baseRevenue = (long)Mathf.Floor(price * actualSell);
                    revenue += baseRevenue - (long)Mathf.Floor(baseRevenue * feeRate);
                }
            }
        }
    }

    private void ApplyReservedDailyExpenses()
    {
        IsProcessingReservedExpenses = true;

        long expenses = _reservedDailyExpenses.TotalExpenses;
        long revenue = _reservedDailyExpenses.playerTradeRevenue;

        if (expenses > 0) Finances.TryRemoveCredit(expenses);
        if (revenue > 0) Finances.AddCredit(revenue);

        _reservedDailyExpenses = new DailyExpenseReservation(); // 초기화
        IsProcessingReservedExpenses = false;
    }

    #endregion

    #region 스레드 배치 변경 핸들링

    private void HandleThreadPlacementChanged()
    {
        UpdateResourceDeltasFromPlacedThreads();
        ReserveDailyExpenses(); // 배치 변경 시 즉시 비용 예측 갱신
        OnThreadPlacementChanged?.Invoke();
    }

    private void UpdateResourceDeltasFromPlacedThreads()
    {
        // 1. 모든 자원의 델타 초기화
        foreach (var entry in Resource.GetAllResources().Values)
        {
            if (entry?.resourceState != null) entry.resourceState.deltaCount = 0;
        }

        // 2. 집계된 생산/소비량을 델타에 적용
        // (여기서 CalculateThreadAggregates를 재사용할 수도 있지만, DeltaCount를 직접 수정해야 하므로 로직 분리 유지하되 간소화)
        
        var placedThreads = ThreadPlacement.GetAllPlacedThreads();
        if (placedThreads == null) return;

        foreach (var placement in placedThreads.Values)
        {
            if (placement == null || string.IsNullOrEmpty(placement.ThreadId)) continue;
            
            var threadState = Thread.GetThread(placement.ThreadId);
            if (threadState == null) continue;

            if (threadState.TryGetAggregatedResourceCounts(out var cons, out var prod))
            {
                ApplyDelta(prod, 1);
                ApplyDelta(cons, -1);
            }
        }

        void ApplyDelta(Dictionary<string, int> counts, int sign)
        {
            if (counts == null) return;
            foreach (var kvp in counts)
            {
                var entry = Resource.GetResourceEntry(kvp.Key);
                if (entry?.resourceState != null)
                {
                    entry.resourceState.deltaCount += sign * kvp.Value;
                }
            }
        }
    }
    #endregion
}