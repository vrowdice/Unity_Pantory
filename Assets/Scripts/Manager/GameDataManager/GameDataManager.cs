using System;
using System.Collections.Generic;
using System.Linq; // LINQ 추가
using UnityEngine;

public class GameDataManager : MonoBehaviour
{
    // ==================== 싱글톤 ====================
    public static GameDataManager Instance { get; private set; }

    #region 인스펙터 설정 (Initial Data)
    [Header("Initial Data")]
    [SerializeField] private InitialResourceData _initialResourceData;
    [SerializeField] private InitialMarketData _initialMarketData;
    [SerializeField] private InitialEmployeeData _initialEmployeeData;
    [SerializeField] private InitialTimeData _timeSettingsData;
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
        _initialEmployeeData?.ApplyToEmployeeHandler(Employee);

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
        // 1. 직원 상태 업데이트 (만족도 및 효율성)
        Employee?.UpdateDailyEmployeeStatus();
        
        // 2. 다음 날 비용 예약 (현재 상태 기준)
        ReserveDailyExpenses();
        
        // 3. 예약된 비용 처리 (자금 차감/지급)
        ApplyReservedDailyExpenses();
        
        // 4. 자원 증감 적용 (생산/소비)
        Resource?.ApplyResourceDeltas();
        
        // 5. 배치 변경에 따른 델타 갱신 (배치가 바뀌지 않았어도 일일 루틴으로 호출)
        HandleThreadPlacementChanged();
        
        // 6. 시장 업데이트 (가격 변동 및 자동 거래 실행)
        Market?.TickDailyMarket();
    }


    /// <summary>
    /// 일일 예상 크레딧 변화량을 반환합니다 (예약된 비용 기준)
    /// </summary>
    public long CalculateDailyCreditDelta()
    {
        return Finances?.CalculateDailyCreditDelta() ?? 0;
    }

    /// <summary>
    /// 일일 비용 예약 (FinancesDataHandler로 위임)
    /// </summary>
    public void ReserveDailyExpenses()
    {
        Finances?.ReserveDailyExpenses();
    }

    /// <summary>
    /// 예약된 일일 비용 적용 (FinancesDataHandler로 위임)
    /// </summary>
    public void ApplyReservedDailyExpenses()
    {
        Finances?.ApplyReservedDailyExpenses();
    }

    /// <summary>
    /// 예약된 일일 비용 정보 반환
    /// </summary>
    public DailyExpenseReservation ReservedDailyExpenses => Finances?.ReservedDailyExpenses ?? new DailyExpenseReservation();

    /// <summary>
    /// 예약된 비용 처리 중 여부
    /// </summary>
    public bool IsProcessingReservedExpenses => Finances?.IsProcessingReservedExpenses ?? false;

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