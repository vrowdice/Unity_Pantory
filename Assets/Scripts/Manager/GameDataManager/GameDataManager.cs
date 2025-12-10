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
    public ThreadCalculateHandler ThreadCalculate { get; private set; }
    public EffectDataHandler Effect { get; private set; }
    public ResearchDataHandler Research { get; private set; }
    #endregion

    #region 이벤트 중계 (주요 이벤트만 유지)
    // UI 등에서 자주 쓰이는 핵심 이벤트는 중계 유지 (선택 사항)
    public event Action OnResourceChanged
    {
        add => Resource.OnResourceChanged += value;
        remove => Resource.OnResourceChanged -= value;
    }
    public event Action OnCreditChanged
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
        float dt = UnityEngine.Time.deltaTime;

        // 1. 시간 흐름 업데이트
        Time?.Update(dt);

        // 2. 기간제 효과 시간 업데이트
        // Time이 일시정지 상태가 아닐 때만 효과 시간도 흐르게 함
        if (Time != null && !Time.IsTimePaused())
        {
            Effect?.UpdateEffectsTime(dt);
        }
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
        ThreadCalculate = new ThreadCalculateHandler(this);
        Effect = new EffectDataHandler(this);
        Research = new ResearchDataHandler(this);

        Debug.Log("[GameDataManager] All services initialized.");

        // 초기 데이터 적용
        _timeSettingsData?.ApplyToTimeService(Time);
        _initialResourceData?.ApplyToServices(Resource, Finances);
        _initialMarketData?.ApplyToMarket(Market);
        _initialEmployeeData?.ApplyToEmployeeHandler(Employee);

        // [솔루션 4] 초기 마중물: 시장에 재고와 자금 충전
        Market?.InitializeMarketChaos();
        
        // 시스템 액터 초기화 (일반 시민, 무역항)
        Market?.InitializeSystemActors();

        // 데이터 로드 및 초기 상태 계산
        LoadThreadData();
        
        // 모든 스레드의 생산량 등 초기화
        ThreadCalculate?.InitializeAllThreads(Thread);
        
        ReserveDailyExpenses();
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
        
        // 2. 스레드 직원 할당 상태 동기화 (assignedCount 업데이트)
        // 실제 배치된 인스턴스만 집계하여 배치된 인원이 다른 스레드에 배치될 수 없도록 보장
        Employee?.SyncAssignedCountsFromThreads(ThreadPlacement);
        
        // 3. 예약된 비용 처리 (이전 날에 예약된 비용을 먼저 적용)
        ApplyReservedDailyExpenses();
        
        // 4. 스레드 생산/소비 적용 (하루가 지날 때만 실제 생산이 일어남)
        UpdateResourceDeltasFromPlacedThreads();
        
        // 5. 자원 증감 적용 (생산/소비 델타를 실제 재고에 반영)
        Resource?.ApplyResourceDeltas();
        
        // 6. 다음 날 비용 예약 (현재 상태 기준)
        ReserveDailyExpenses();
        
        // 7. 연구력 생산 및 특허 수익 처리
        Research?.OnDayChanged();
        
        // 8. 시장 업데이트 (가격 변동 및 자동 거래 실행)
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
        // 스레드 배치 변경 시에는 생산을 즉시 적용하지 않음
        // 생산은 하루가 지날 때만 적용됩니다 (HandleDayChanged에서 처리)
        ReserveDailyExpenses(); // 배치 변경 시 즉시 비용 예측 갱신
        OnThreadPlacementChanged?.Invoke();
    }

    /// <summary>
    /// 배치된 스레드의 생산/소비를 계산하여 플레이어 인벤토리에 적용합니다.
    /// 하루가 지날 때만 호출됩니다 (즉시 생산 방지).
    /// </summary>
    private void UpdateResourceDeltasFromPlacedThreads()
    {
        // 1. 모든 자원의 시장 재고 델타 초기화 (시장 재고용, 플레이어 인벤토리와는 무관)
        foreach (var entry in Resource.GetAllResources().Values)
        {
            if (entry?.resourceState != null) entry.resourceState.deltaCount = 0;
        }

        var placedThreads = ThreadPlacement.GetAllPlacedThreads();
        if (placedThreads == null) return;

        foreach (var placement in placedThreads.Values)
        {
            if (placement == null || placement.RuntimeState == null) continue;
            
            // 각 배치된 인스턴스의 독립적인 상태를 가져옴
            ThreadState threadState = placement.RuntimeState;

            // 2-1. 직원 수 기반 생산 효율 및 진행도 계산
            UpdateThreadProductionProgress(threadState);

            // 생산 진행도가 1.0 이상일 때만 생산 및 소비 로직을 실행
            if (threadState.currentProductionProgress >= 1.0f)
            {
                // 생산 횟수 계산 (예: 진행도 2.5면 2번 생산)
                int productionCount = Mathf.FloorToInt(threadState.currentProductionProgress);

                if (threadState.TryGetAggregatedResourceCounts(out var cons, out var prod))
                {
                    // 생산품 추가 (횟수만큼)
                    ApplyPlayerProduction(prod, productionCount);
                    
                    // 소비품 차감 (횟수만큼)
                    ApplyPlayerConsumption(cons, productionCount);
                }

                // 사용한 진행도 차감
                threadState.currentProductionProgress -= productionCount;
            }
        }
    }

    /// <summary>
    /// 스레드의 생산 진행도와 효율을 업데이트합니다.
    /// </summary>
    private void UpdateThreadProductionProgress(ThreadState threadState)
    {
        if (threadState == null) return;

        // requiredEmployees는 건물에서 필요한 실제 직원 수 (BuildingTileManager에서 계산된 값)
        // 덮어쓰지 않고 그대로 사용

        // 생산 효율 계산 (현재 직원 수 / 필요한 직원 수, 0~1 범위)
        // requiredEmployees가 0이면 효율 0, 그 외에는 현재 직원 수 비율로 계산
        if (threadState.requiredEmployees > 0)
        {
            int currentEmployees = threadState.currentWorkers + threadState.currentTechnicians;
            // 현재 직원 수가 필요한 직원 수의 백분율로 효율 결정
            threadState.currentProductionEfficiency = Mathf.Clamp01((float)currentEmployees / threadState.requiredEmployees);
        }
        else
        {
            // 직원이 필요 없으면 효율 0
            threadState.currentProductionEfficiency = 0f;
        }

        // 생산 진행도 증가 (합연산)
        threadState.currentProductionProgress += threadState.currentProductionEfficiency;
    }

    /// <summary>
    /// 생산품을 플레이어 창고에 추가합니다. (로직 단순화)
    /// </summary>
    private void ApplyPlayerProduction(Dictionary<string, int> production, int multiplier)
    {
        if (production == null || multiplier <= 0) return;

        foreach (var kvp in production)
        {
            // 생산량 = 기본 생산량 * 생산 횟수(multiplier)
            int actualProduction = kvp.Value * multiplier;
            
            // 생산품은 플레이어 창고로 직접 추가 (델타만 업데이트)
            // 실제 재고는 ApplyResourceDeltas에서 playerInventoryDelta를 playerInventory에 반영
            Resource.ModifyPlayerInventory(kvp.Key, actualProduction);
        }
    }

    /// <summary>
    /// 소비품을 플레이어 창고에서 차감합니다.
    /// </summary>
    private void ApplyPlayerConsumption(Dictionary<string, int> consumption, int multiplier)
    {
        if (consumption == null || multiplier <= 0) return;

        foreach (var kvp in consumption)
        {
            var resourceId = kvp.Key;
            // 소비량 = 기본 소비량 * 생산 횟수(multiplier)
            var requiredAmount = kvp.Value * multiplier;
            
            // 플레이어 창고에서 차감 시도
            long playerAmount = Resource.GetPlayerResourceQuantity(resourceId);
            
            if (playerAmount >= requiredAmount)
            {
                // 플레이어 창고에 충분하면 차감 (델타만 업데이트)
                Resource.ModifyPlayerInventory(resourceId, -requiredAmount);
            }
            else
            {
                // 플레이어 창고에 부족하면
                long shortage = requiredAmount - playerAmount;
                
                // 플레이어 창고 전부 차감
                if (playerAmount > 0)
                {
                    Resource.ModifyPlayerInventory(resourceId, -playerAmount);
                }
                
                // 부족분은 시장에서 구매 (시장 재고 감소, 플레이어 인벤토리 증가)
                // 마켓과의 관계: 없는 재고 구매 이외에는 관계 없음
                if (Resource.HasEnoughMarketInventory(resourceId, shortage))
                {
                    Resource.ModifyMarketInventory(resourceId, -shortage);
                    Resource.ModifyPlayerInventory(resourceId, shortage);
                    // 구매한 만큼 다시 차감 (구매 후 즉시 소모)
                    Resource.ModifyPlayerInventory(resourceId, -shortage);
                    Debug.Log($"[GameDataManager] Auto-purchased {shortage} {resourceId} from market for production.");
                }
                else
                {
                    // 시장에도 부족하면 생산 중단 (경고만 출력, 실제 재고는 차감하지 않음)
                    Debug.LogWarning($"[GameDataManager] Insufficient resources for production: {resourceId} (required: {requiredAmount}, player: {playerAmount})");
                }
            }
        }
    }
    #endregion
}