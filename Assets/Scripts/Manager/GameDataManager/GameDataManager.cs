using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 게임 데이터를 관리하는 싱글톤 허브 클래스 (실제 데이터 관리는 각 서비스 클래스에 위임)
/// </summary>
public class GameDataManager : MonoBehaviour
{
    // ==================== 싱글톤 인스턴스 ====================
    public static GameDataManager Instance { get; private set; }

    #region 인스펙터 설정 (Initial Data)

    [Header("Initial Data")]
    [SerializeField] private InitialResourceData _initialResourceData;
    [SerializeField] private InitialMarketData _initialMarketData;

    [Header("Time Settings")]
    [SerializeField] private TimeSettingsData _timeSettingsData;

    #endregion

    #region 데이터 핸들러 (Services)

    private TimeDataHandler _timeHandler;
    public TimeDataHandler Time => _timeHandler;

    private ThreadDataHandler _threadHandler;
    public ThreadDataHandler Thread => _threadHandler;

    private ThreadPlacementDataHandler _threadPlacementHandler;
    public ThreadPlacementDataHandler ThreadPlacement => _threadPlacementHandler;

    private ResourceDataHandler _resourceHandler;
    public ResourceDataHandler Resource => _resourceHandler;

    private MarketDataHandler _marketHandler;
    public MarketDataHandler Market => _marketHandler;

    private FinancesDataHandler _financesHandler;
    public FinancesDataHandler Finances => _financesHandler;

    private EmployeeDataHandler _employeeHandler;
    public EmployeeDataHandler Employee => _employeeHandler;

    private BuildingDataHandler _buildingHandler;
    public BuildingDataHandler Building => _buildingHandler;

    private SaveLoadHandler _saveLoadHandler;
    public SaveLoadHandler SaveLoad => _saveLoadHandler;

    // 레거시 호환 프로퍼티
    public long Silver => _financesHandler.GetCredit();

    #endregion

    #region Unity 생명주기 및 초기화 (Awake, Start, Update)

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
        // TimeService 업데이트
        if (_timeHandler != null)
        {
            _timeHandler.Update(UnityEngine.Time.deltaTime);
        }
    }

    /// <summary>
    /// 모든 서비스를 초기화하고 SaveLoadHandler를 연결합니다.
    /// </summary>
    private void InitializeServices()
    {
        _saveLoadHandler = new SaveLoadHandler(this);
        _threadHandler = new ThreadDataHandler(this);
        _threadPlacementHandler = new ThreadPlacementDataHandler(this);
        _threadPlacementHandler.OnPlacementChanged += HandleThreadPlacementChanged;
        _timeHandler = new TimeDataHandler(this);
        _timeHandler.OnDayChanged += HandleDayChanged;
        _resourceHandler = new ResourceDataHandler(this);
        _marketHandler = new MarketDataHandler(this);
        _financesHandler = new FinancesDataHandler(this);
        _employeeHandler = new EmployeeDataHandler(this);
        _buildingHandler = new BuildingDataHandler(this);

        Debug.Log("[GameDataManager] All services initialized.");

        // 초기 데이터 및 설정 적용
        ApplyTimeSettings();
        ApplyInitialResources();
        ApplyInitialMarketData();

        // Thread 데이터 자동 로드 시도
        LoadThreadData();
        UpdateResourceDeltasFromPlacedThreads();
    }

    #endregion

    #region 이벤트 중계 (Event Proxy)

    // 자원 변경 이벤트 (ResourceService의 이벤트를 중계)
    public event Action OnResourceChanged
    {
        add => _resourceHandler.OnResourceChanged += value;
        remove => _resourceHandler.OnResourceChanged -= value;
    }

    // 금액 변경 이벤트 (FinancesService의 이벤트를 중계)
    public event Action OnSilverChanged
    {
        add => _financesHandler.OnCreditChanged += value;
        remove => _financesHandler.OnCreditChanged -= value;
    }

    // 직원 변경 이벤트 (EmployeeService의 이벤트를 중계)
    public event Action OnEmployeeChanged
    {
        add => _employeeHandler.OnEmployeeChanged += value;
        remove => _employeeHandler.OnEmployeeChanged -= value;
    }

    /* // 건물 변경 이벤트 (BuildingService의 이벤트를 중계)
    public event Action OnBuildingChanged
    {
        add => _buildingService.OnBuildingChanged += value;
        remove => _buildingService.OnBuildingChanged -= value;
    }*/

    // Thread 변경 이벤트 (ThreadService의 이벤트를 중계)
    public event Action OnThreadChanged
    {
        add => _threadHandler.OnThreadChanged += value;
        remove => _threadHandler.OnThreadChanged -= value;
    }

    // Category 변경 이벤트 (ThreadService의 이벤트를 중계)
    public event Action OnCategoryChanged
    {
        add => _threadHandler.OnCategoryChanged += value;
        remove => _threadHandler.OnCategoryChanged -= value;
    }

    public event Action OnThreadPlacementChanged;

    #endregion

    void OnDestroy()
    {
        if (_threadPlacementHandler != null)
        {
            _threadPlacementHandler.OnPlacementChanged -= HandleThreadPlacementChanged;
        }

        if (_timeHandler != null)
        {
            _timeHandler.OnDayChanged -= HandleDayChanged;
        }
    }

    #region 초기 데이터 및 저장/로드 로직 (ThreadDataHandler로 저장 권한 위임)

    /// <summary>
    /// Thread 데이터를 자동으로 로드합니다. 로드 실패 시 데이터를 초기화하고 저장합니다.
    /// </summary>
    private void LoadThreadData()
    {
        if (_saveLoadHandler != null && _threadHandler != null)
        {
            if (!_saveLoadHandler.LoadThreadData(_threadHandler))
            {
                Debug.LogWarning("[GameDataManager] Failed to load Thread data. Attempting to reset and save default data.");
                ResetThreadData();
            }
        }
    }

    /// <summary>
    /// Thread 데이터를 초기화합니다.
    /// </summary>
    private void ResetThreadData()
    {
        _threadHandler.ResetThreadData();
        _threadPlacementHandler?.ClearAll();
    }

    /// <summary>
    /// 시간 설정 데이터를 적용합니다.
    /// </summary>
    private void ApplyTimeSettings()
    {
        if (_timeSettingsData == null)
        {
            Debug.LogWarning("[GameDataManager] TimeSettingsData is not assigned. Using default values.");
            return;
        }

        _timeSettingsData.ApplyToTimeService(_timeHandler);
    }

    /// <summary>
    /// 초기 자원 데이터를 적용합니다.
    /// </summary>
    private void ApplyInitialResources()
    {
        if (_initialResourceData == null)
        {
            Debug.LogWarning("[GameDataManager] InitialResourceData is not assigned. Starting with default values (0).");
            return;
        }

        _initialResourceData.ApplyToServices(_resourceHandler, _financesHandler);
    }

    /// <summary>
    /// 초기 마켓 데이터를 적용합니다.
    /// </summary>
    private void ApplyInitialMarketData()
    {
        if (_initialMarketData == null)
        {
            Debug.LogWarning("[GameDataManager] InitialMarketData is not assigned. Using default market values.");
            return;
        }

        _initialMarketData.ApplyToMarket(_marketHandler);
    }

    #endregion

    #region 편의 메서드: Time Service

    public void PauseTime() => _timeHandler.PauseTime();
    public void ResumeTime() => _timeHandler.ResumeTime();
    public void SetTimeSpeed(float speed) => _timeHandler.SetTimeSpeed(speed);
    public bool IsTimePaused() => _timeHandler.IsTimePaused();
    public float GetTimeSpeed() => _timeHandler.GetTimeSpeed();
    public string GetDateString() => _timeHandler.GetDateString();

    #endregion

    #region 편의 메서드: Resource Service

    /// <summary> 특정 자원의 현재 보유량 반환 </summary>
    public long GetResourceQuantity(string resourceId) => _resourceHandler.GetResourceQuantity(resourceId);

    /// <summary> 특정 자원의 현재 가격 반환 </summary>
    public float GetResourcePrice(string resourceId) => _resourceHandler.GetResourcePrice(resourceId);

    /// <summary> 특정 자원의 ResourceEntry 반환 </summary>
    public ResourceEntry GetResourceEntry(string resourceId) => _resourceHandler.GetResourceEntry(resourceId);

    /// <summary> 모든 자원 정보 반환 </summary>
    public Dictionary<string, ResourceEntry> GetAllResources() => _resourceHandler.GetAllResources();

    /// <summary> 특정 자원 추가 </summary>
    public void AddResource(string resourceId, long amount) => _resourceHandler.AddResource(resourceId, amount);

    /// <summary> 특정 자원 제거 시도 </summary>
    public bool TryRemoveResource(string resourceId, long amount) => _resourceHandler.TryRemoveResource(resourceId, amount);

    /// <summary> 특정 자원 충분 여부 확인 </summary>
    public bool HasEnoughResource(string resourceId, long amount) => _resourceHandler.HasEnoughResource(resourceId, amount);

    #endregion

    #region 편의 메서드: Finances Service

    /// <summary> 현재 보유 금액 반환 </summary>
    public long GetCredit() => _financesHandler.GetCredit();

    /// <summary> 금액 추가 </summary>
    public void AddCredit(long amount) => _financesHandler.AddCredit(amount);

    /// <summary> 금액 차감 시도 </summary>
    public bool TryRemoveCredit(long amount) => _financesHandler.TryRemoveCredit(amount);

    /// <summary> 금액 충분 여부 확인 </summary>
    public bool HasEnoughCredit(long amount) => _financesHandler.HasEnoughCredit(amount);

    /// <summary> 일일 예상 크레딧 변화량을 계산합니다 (배치된 스레드의 유지비 + 자원 부족 구매 비용 + 플레이어 자동 거래 비용) </summary>
    public long CalculateDailyCreditDelta()
    {
        Debug.Log("[GameDataManager] CalculateDailyCreditDelta called");
        
        if (_threadPlacementHandler == null)
        {
            Debug.LogWarning("[GameDataManager] ThreadPlacementHandler is null. Cannot calculate daily credit delta.");
            return 0;
        }
        
        if (_threadHandler == null)
        {
            Debug.LogWarning("[GameDataManager] ThreadHandler is null. Cannot calculate daily credit delta.");
            return 0;
        }

        if (_resourceHandler == null)
        {
            Debug.LogWarning("[GameDataManager] ResourceHandler is null. Cannot calculate daily credit delta.");
            return 0;
        }

        if (_marketHandler == null)
        {
            Debug.LogWarning("[GameDataManager] MarketHandler is null. Cannot calculate daily credit delta.");
            return 0;
        }

        var placedThreads = _threadPlacementHandler.GetAllPlacedThreads();
        int placedThreadCount = placedThreads != null ? placedThreads.Count : 0;
        Debug.Log($"[GameDataManager] Found {placedThreadCount} placed threads");

        // 1. 스레드 유지비 계산
        long totalMaintenanceCost = 0;
        int processedThreadCount = 0;

        if (placedThreads != null)
        {
            foreach (var kvp in placedThreads)
            {
                ThreadPlacementState placementState = kvp.Value;
                if (placementState == null || string.IsNullOrEmpty(placementState.ThreadId))
                {
                    Debug.LogWarning($"[GameDataManager] Invalid placement state at {kvp.Key}");
                    continue;
                }

                ThreadState threadState = _threadHandler.GetThread(placementState.ThreadId);
                if (threadState == null)
                {
                    Debug.LogWarning($"[GameDataManager] Thread not found: {placementState.ThreadId}");
                    continue;
                }

                Debug.Log($"[GameDataManager] Thread '{threadState.threadName}' (ID: {threadState.threadId}) maintenance cost: {threadState.totalMaintenanceCost}");
                totalMaintenanceCost += threadState.totalMaintenanceCost;
                processedThreadCount++;
            }
        }

        // 2. 자원 부족으로 인한 예상 구매 비용 계산
        long totalShortageCost = 0;
        Dictionary<string, ResourceEntry> allResources = _resourceHandler.GetAllResources();
        
        if (allResources != null && placedThreads != null)
        {
            // 모든 스레드의 자원 소비량 집계
            Dictionary<string, long> totalConsumption = new Dictionary<string, long>();
            
            foreach (var kvp in placedThreads)
            {
                ThreadPlacementState placementState = kvp.Value;
                if (placementState == null || string.IsNullOrEmpty(placementState.ThreadId))
                {
                    continue;
                }

                ThreadState threadState = _threadHandler.GetThread(placementState.ThreadId);
                if (threadState == null)
                {
                    continue;
                }

                // 스레드의 소비 자원 집계
                if (threadState.TryGetAggregatedResourceCounts(out var consumptionCounts, out var productionCounts))
                {
                    foreach (var resourceKvp in consumptionCounts)
                    {
                        string resourceId = resourceKvp.Key;
                        int amount = resourceKvp.Value;
                        
                        if (totalConsumption.TryGetValue(resourceId, out long current))
                        {
                            totalConsumption[resourceId] = current + amount;
                        }
                        else
                        {
                            totalConsumption[resourceId] = amount;
                        }
                    }
                }
            }

            // 각 자원의 부족량 계산 및 구매 비용 산정
            foreach (var consumptionKvp in totalConsumption)
            {
                string resourceId = consumptionKvp.Key;
                long requiredAmount = consumptionKvp.Value;
                
                if (!allResources.TryGetValue(resourceId, out var resourceEntry))
                {
                    continue;
                }

                long availableAmount = _resourceHandler.GetResourceQuantity(resourceId);
                long shortage = requiredAmount - availableAmount;
                
                if (shortage > 0)
                {
                    float unitPrice = resourceEntry.resourceState?.currentValue ?? 0f;
                    long shortageCost = (long)Math.Ceiling(unitPrice * shortage);
                    totalShortageCost += shortageCost;
                    
                    Debug.Log($"[GameDataManager] Resource '{resourceId}' shortage: {shortage} units, cost: {shortageCost} credits (required: {requiredAmount}, available: {availableAmount}, price: {unitPrice})");
                }
            }
        }

        // 3. 플레이어 자동 거래 비용/수익 계산 (playerTransactionDelta 기반)
        long totalPlayerTradeCost = 0;
        long totalPlayerTradeRevenue = 0;
        
        if (allResources != null)
        {
            float marketFeeRate = _marketHandler.GetMarketFeeRate();
            
            foreach (var kvp in allResources)
            {
                var entry = kvp.Value;
                if (entry?.resourceState == null)
                {
                    continue;
                }

                long transactionDelta = entry.resourceState.playerTransactionDelta;
                if (transactionDelta == 0)
                {
                    continue; // 거래 설정이 없으면 스킵
                }

                float unitPrice = entry.resourceState.currentValue;
                
                if (transactionDelta > 0)
                {
                    // 양수: 매수 (크레딧 차감)
                    long amount = transactionDelta;
                    long baseCost = (long)Mathf.Ceil(unitPrice * amount);
                    long marketFee = (long)Mathf.Ceil(baseCost * marketFeeRate);
                    long totalCost = baseCost + marketFee;
                    totalPlayerTradeCost += totalCost;
                    
                    Debug.Log($"[GameDataManager] Player auto-buy '{entry.resourceData.displayName}': {amount} units, cost: {totalCost} credits (base: {baseCost}, fee: {marketFee})");
                }
                else
                {
                    // 음수: 매도 (크레딧 증가, 수익이므로 양수로 더함)
                    long amount = -transactionDelta;
                    long availableAmount = _resourceHandler.GetResourceQuantity(kvp.Key);
                    long sellAmount = Math.Min(amount, availableAmount); // 보유량만큼만 판매 가능
                    
                    if (sellAmount > 0)
                    {
                        long baseRevenue = (long)Mathf.Floor(unitPrice * sellAmount);
                        long marketFee = (long)Mathf.Floor(baseRevenue * marketFeeRate);
                        long totalRevenue = baseRevenue - marketFee;
                        totalPlayerTradeRevenue += totalRevenue;
                        
                        Debug.Log($"[GameDataManager] Player auto-sell '{entry.resourceData.displayName}': {sellAmount} units, revenue: {totalRevenue} credits (base: {baseRevenue}, fee: {marketFee})");
                    }
                }
            }
        }

        // 최종 계산: 수익 - 지출
        // 수익: 플레이어 매도 수익
        // 지출: 유지비 + 부족 구매 비용 + 플레이어 매수 비용
        long totalExpenses = totalMaintenanceCost + totalShortageCost + totalPlayerTradeCost;
        long netDelta = totalPlayerTradeRevenue - totalExpenses;
        
        Debug.Log($"[GameDataManager] Daily credit delta calculated: {netDelta} (expenses: {totalExpenses} = maintenance: {totalMaintenanceCost} + shortage: {totalShortageCost} + buy: {totalPlayerTradeCost}, revenue: {totalPlayerTradeRevenue}, from {processedThreadCount}/{placedThreadCount} threads)");
        
        return netDelta;
    }

    #endregion

    #region 편의 메서드: Market Service (플레이어 거래)

    /// <summary> 플레이어가 자원 구매 시도 </summary>
    public bool TryPlayerBuyResource(string resourceId, long amount) => _marketHandler?.TryPlayerBuyResource(resourceId, amount) ?? false;

    /// <summary> 플레이어가 자원 판매 시도 </summary>
    public bool TryPlayerSellResource(string resourceId, long amount) => _marketHandler?.TryPlayerSellResource(resourceId, amount) ?? false;

    /// <summary> 모든 액터를 자산 기준으로 정렬하여 반환 </summary>
    public List<MarketActorEntry> GetActorsSortedByWealth(bool ascending = false) => _marketHandler?.GetActorsSortedByWealth(ascending) ?? new List<MarketActorEntry>();

    /// <summary> 특정 액터의 자산 순위 반환 </summary>
    public int GetActorWealthRank(string actorId) => _marketHandler?.GetActorWealthRank(actorId) ?? -1;

    #endregion

    #region 편의 메서드: Employee Service

    /// <summary> 특정 직원 유형의 인원 수 반환 </summary>
    public int GetEmployeeCount(string employeeId) => _employeeHandler.GetEmployeeCount(employeeId);

    /// <summary> 특정 직원 유형의 총 급여 반환 </summary>
    public long GetEmployeeTotalSalary(string employeeId) => _employeeHandler.GetEmployeeTotalSalary(employeeId);

    /// <summary> 모든 직원의 총 급여 반환 </summary>
    public long GetTotalSalary() => _employeeHandler.GetTotalSalary();

    /// <summary> 직원 고용 </summary>
    public void HireEmployee(string employeeId, int count) => _employeeHandler.HireEmployee(employeeId, count);

    /// <summary> 직원 해고 시도 </summary>
    public bool TryFireEmployee(string employeeId, int count) => _employeeHandler.TryFireEmployee(employeeId, count);

    /// <summary> 직원 인원 수 설정 </summary>
    public void SetEmployeeCount(string employeeId, int count) => _employeeHandler.SetEmployeeCount(employeeId, count);

    /// <summary> 모든 직원 정보 반환 </summary>
    public Dictionary<string, EmployeeEntry> GetAllEmployees() => _employeeHandler.GetAllEmployees();

    #endregion

    #region 편의 메서드: Building Service

    /// <summary> 특정 건물의 BuildingData 반환 </summary>
    public BuildingData GetBuildingData(string buildingId) => _buildingHandler.GetBuildingData(buildingId);

    /// <summary> 모든 건물 데이터 반환 </summary>
    public Dictionary<string, BuildingData> GetAllBuildings() => _buildingHandler.GetAllBuildings();

    /// <summary> 특정 타입의 건물 데이터 리스트 반환 </summary>
    public List<BuildingData> GetBuildingDataList(BuildingType buildingType) => _buildingHandler.GetBuildingDataList(buildingType);

    /// <summary> 건물 등록 여부 확인 </summary>
    public bool IsBuildingRegistered(string buildingId) => _buildingHandler.IsBuildingRegistered(buildingId);

    /// <summary> 등록된 건물 타입 개수 반환 </summary>
    public int GetBuildingTypeCount() => _buildingHandler.GetBuildingTypeCount();

    #endregion

    #region 편의 메서드: Thread Service (Thread 관련)

    /// <summary> 모든 Thread 데이터 초기화 </summary>
    public void ClearAllThreads() => _threadHandler.ResetThreadData();

    /// <summary> 특정 Thread 반환 </summary>
    public ThreadState GetThread(string threadId) => _threadHandler.GetThread(threadId);

    /// <summary> 모든 Thread 반환 (Dictionary) </summary>
    public Dictionary<string, ThreadState> GetAllThreads() => _threadHandler.GetAllThreads();

    /// <summary> 모든 Thread 반환 (List) </summary>
    public List<ThreadState> GetAllThreadList() => _threadHandler.GetAllThreadList();

    /// <summary> 모든 Thread ID 반환 </summary>
    public List<string> GetAllThreadIds() => _threadHandler.GetAllThreadIds();

    /// <summary> Thread의 건물 리스트 반환 </summary>
    public List<BuildingState> GetBuildingStates(string threadId) => _threadHandler.GetBuildingStates(threadId);

    /// <summary> Thread 존재 여부 확인 </summary>
    public bool HasThread(string threadId) => _threadHandler.HasThread(threadId);

    /// <summary> Thread 추가 </summary>
    public bool AddThread(ThreadState threadState) => _threadHandler.AddThread(threadState);

    /// <summary> Thread 생성 및 추가 </summary>
    public ThreadState CreateThread(string threadId, string threadName) => _threadHandler.CreateThread(threadId, threadName);

    /// <summary> Thread 제거 </summary>
    public bool RemoveThread(string threadId) => _threadHandler.RemoveThread(threadId);

    /// <summary> Thread에 건물 추가 </summary>
    public bool AddBuildingToThread(string threadId, BuildingState buildingState) => _threadHandler.AddBuilding(threadId, buildingState);

    /// <summary> Thread에서 건물 제거 </summary>
    public bool RemoveBuildingFromThread(string threadId, Vector2Int position) => _threadHandler.RemoveBuilding(threadId, position);

    /// <summary> 특정 위치의 건물 반환 </summary>
    public BuildingState GetBuildingAt(string threadId, Vector2Int position) => _threadHandler.GetBuildingAt(threadId, position);

    /// <summary> Thread 개수 반환 </summary>
    public int GetThreadCount() => _threadHandler.GetThreadCount();

    /// <summary> 특정 Thread의 자원 소비/생산 요약 반환 </summary>
    public ThreadResourceSummary GetThreadResourceSummary(string threadId) => _threadHandler.GetThreadResourceSummary(threadId);

    /// <summary> 특정 Thread의 자원 소비/생산 요약 반환 시도 </summary>
    public bool TryGetThreadResourceSummary(string threadId, out ThreadResourceSummary summary) => _threadHandler.TryGetThreadResourceSummary(threadId, out summary);

    /// <summary> 모든 Thread의 자원 소비/생산 요약 리스트 반환 </summary>
    public List<ThreadResourceSummary> GetAllThreadResourceSummaries() => _threadHandler.GetAllThreadResourceSummaries();

    #endregion

    #region 편의 메서드: Thread Service (Category 관련)

    /// <summary> 카테고리 추가 </summary>
    public bool AddCategory(ThreadCategory category) => _threadHandler.AddCategory(category);

    /// <summary> 카테고리 생성 및 추가 </summary>
    public ThreadCategory CreateCategory(string categoryId, string categoryName) => _threadHandler.CreateCategory(categoryId, categoryName);

    /// <summary> 카테고리 제거 </summary>
    public bool RemoveCategory(string categoryId) => _threadHandler.RemoveCategory(categoryId);

    /// <summary> 카테고리 이름 변경 </summary>
    public bool RenameCategory(string categoryId, string newName) => _threadHandler.RenameCategory(categoryId, newName);

    /// <summary> 특정 카테고리 반환 </summary>
    public ThreadCategory GetCategory(string categoryId) => _threadHandler.GetCategory(categoryId);

    /// <summary> 모든 카테고리 반환 </summary>
    public Dictionary<string, ThreadCategory> GetAllCategories() => _threadHandler.GetAllCategories();

    /// <summary> 모든 카테고리 ID 반환 </summary>
    public List<string> GetAllCategoryIds() => _threadHandler.GetAllCategoryIds();

    /// <summary> 카테고리 존재 여부 확인 </summary>
    public bool HasCategory(string categoryId) => _threadHandler.HasCategory(categoryId);

    /// <summary> 카테고리 개수 반환 </summary>
    public int GetCategoryCount() => _threadHandler.GetCategoryCount();

    /// <summary> 카테고리에 스레드 추가 </summary>
    public bool AddThreadToCategory(string categoryId, string threadId) => _threadHandler.AddThreadToCategory(categoryId, threadId);

    /// <summary> 카테고리에서 스레드 제거 </summary>
    public bool RemoveThreadFromCategory(string categoryId, string threadId) => _threadHandler.RemoveThreadFromCategory(categoryId, threadId);

    /// <summary> 특정 카테고리에 속한 스레드 ID 목록 반환 </summary>
    public List<string> GetThreadIdsInCategory(string categoryId) => _threadHandler.GetThreadIdsInCategory(categoryId);

    /// <summary> 특정 카테고리에 속한 스레드 상태 목록 반환 </summary>
    public List<ThreadState> GetThreadsInCategory(string categoryId) => _threadHandler.GetThreadsInCategory(categoryId);

    #endregion

    #region Thread Placement Delta 계산

    private void HandleDayChanged()
    {
        _resourceHandler?.ApplyResourceDeltas();
        HandleThreadPlacementChanged();
        _marketHandler?.TickDailyMarket();
    }

    private void HandleThreadPlacementChanged()
    {
        UpdateResourceDeltasFromPlacedThreads();
        OnThreadPlacementChanged?.Invoke();
    }

    private void UpdateResourceDeltasFromPlacedThreads()
    {
        if (_resourceHandler == null)
        {
            return;
        }

        Dictionary<string, ResourceEntry> allResources = _resourceHandler.GetAllResources();
        if (allResources == null)
        {
            return;
        }

        foreach (var entry in allResources.Values)
        {
            if (entry?.resourceState != null)
            {
                entry.resourceState.deltaCount = 0;
            }
        }

        if (_threadPlacementHandler == null || _threadHandler == null)
        {
            return;
        }

        foreach (var kvp in _threadPlacementHandler.GetAllPlacedThreads())
        {
            ThreadPlacementState placementState = kvp.Value;
            if (placementState == null || string.IsNullOrEmpty(placementState.ThreadId))
            {
                continue;
            }

            ThreadState threadState = _threadHandler.GetThread(placementState.ThreadId);
            if (threadState == null)
            {
                continue;
            }

            if (threadState.TryGetAggregatedResourceCounts(out var consumptionCounts, out var productionCounts))
            {
                ApplyResourceDelta(allResources, productionCounts, 1);
                ApplyResourceDelta(allResources, consumptionCounts, -1);
            }

            _financesHandler.TryRemoveCredit(threadState.totalMaintenanceCost);
            Debug.Log($"[GameDataManager] Removed {threadState.totalMaintenanceCost} credit for thread {threadState.threadName}");
        }
    }

    private void ApplyResourceDelta(Dictionary<string, ResourceEntry> allResources, Dictionary<string, int> resourceCounts, int sign)
    {
        if (resourceCounts == null)
        {
            return;
        }

        foreach (var kvp in resourceCounts)
        {
            string resourceId = kvp.Key;
            int amount = kvp.Value;

            if (string.IsNullOrEmpty(resourceId) || amount == 0)
            {
                continue;
            }

            if (allResources.TryGetValue(resourceId, out var entry) && entry?.resourceState != null)
            {
                entry.resourceState.deltaCount += sign * amount;
            }
        }
    }

    #endregion
}