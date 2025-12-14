using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GameDataManager : MonoBehaviour
{
    public static GameDataManager Instance { get; private set; }

    [Header("Initial Data")]
    [SerializeField] private InitialResourceData _initialResourceData;
    [SerializeField] private InitialMarketData _initialMarketData;
    [SerializeField] private InitialEmployeeData _initialEmployeeData;
    [SerializeField] private InitialTimeData _timeSettingsData;

    [Header("Game Data Lists")]
    [SerializeField] private List<BuildingData> _buildingDataList = new List<BuildingData>();
    [SerializeField] private List<ResourceData> _resourceDataList = new List<ResourceData>();
    [SerializeField] private List<EmployeeData> _employeeDataList = new List<EmployeeData>();
    [SerializeField] private List<ResearchData> _researchDataList = new List<ResearchData>();
    [SerializeField] private List<MarketActorData> _marketActorDataList = new List<MarketActorData>();

    public InitialTimeData InitialTimeData => _timeSettingsData;
    public InitialEmployeeData InitialEmployeeData => _initialEmployeeData;

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

    public event Action OnThreadPlacementChanged;

    void Update()
    {
        float deltaTime = UnityEngine.Time.deltaTime;
        Time?.Update(deltaTime);
    }

    void OnDestroy()
    {
        ThreadPlacement.OnPlacementChanged -= HandleThreadPlacementChanged;
        Time.OnDayChanged -= HandleDayChanged;
    }

    public void OnInitialize()
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

    private void InitializeServices()
    {
        SaveLoad = new SaveLoadHandler(this);
        Thread = new ThreadDataHandler(this);
        ThreadPlacement = new ThreadPlacementDataHandler(this);
        Time = new TimeDataHandler(this, _timeSettingsData);
        Resource = new ResourceDataHandler(this, _resourceDataList);
        Market = new MarketDataHandler(this, _marketActorDataList, _initialMarketData);
        Finances = new FinancesDataHandler(this, _initialResourceData);
        Employee = new EmployeeDataHandler(this, _employeeDataList, _initialEmployeeData);
        Building = new BuildingDataHandler(this, _buildingDataList);
        ThreadCalculate = new ThreadCalculateHandler(this);
        Effect = new EffectDataHandler(this);
        Research = new ResearchDataHandler(this, _researchDataList);

        ThreadPlacement.OnPlacementChanged += HandleThreadPlacementChanged;
        Time.OnDayChanged += HandleDayChanged;

        LoadThreadData();

        Debug.Log("[GameDataManager] All services initialized.");
    }

#if UNITY_EDITOR
    [ContextMenu("Auto Load All Data to Lists")]
    private void AutoLoadAllDataToLists()
    {
        AutoLoadBuildings();
        AutoLoadResources();
        AutoLoadEmployees();
        AutoLoadResearch();
        AutoLoadMarketActors();
        
        UnityEditor.EditorUtility.SetDirty(this);
        Debug.Log("[GameDataManager] All data auto-loaded to lists.");
    }

    private void AutoLoadBuildings()
    {
        _buildingDataList.Clear();
        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:BuildingData");
        foreach (string guid in guids)
        {
            string assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            BuildingData data = UnityEditor.AssetDatabase.LoadAssetAtPath<BuildingData>(assetPath);
            if (data != null && !_buildingDataList.Contains(data))
            {
                _buildingDataList.Add(data);
            }
        }
        Debug.Log($"[GameDataManager] Loaded {_buildingDataList.Count} BuildingData to list.");
    }

    private void AutoLoadResources()
    {
        _resourceDataList.Clear();
        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:ResourceData");
        foreach (string guid in guids)
        {
            string assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            ResourceData data = UnityEditor.AssetDatabase.LoadAssetAtPath<ResourceData>(assetPath);
            if (data != null && !_resourceDataList.Contains(data))
            {
                _resourceDataList.Add(data);
            }
        }
        Debug.Log($"[GameDataManager] Loaded {_resourceDataList.Count} ResourceData to list.");
    }

    private void AutoLoadEmployees()
    {
        _employeeDataList.Clear();
        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:EmployeeData");
        foreach (string guid in guids)
        {
            string assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            EmployeeData data = UnityEditor.AssetDatabase.LoadAssetAtPath<EmployeeData>(assetPath);
            if (data != null && !_employeeDataList.Contains(data))
            {
                _employeeDataList.Add(data);
            }
        }
        Debug.Log($"[GameDataManager] Loaded {_employeeDataList.Count} EmployeeData to list.");
    }

    private void AutoLoadResearch()
    {
        _researchDataList.Clear();
        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:ResearchData");
        foreach (string guid in guids)
        {
            string assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            ResearchData data = UnityEditor.AssetDatabase.LoadAssetAtPath<ResearchData>(assetPath);
            if (data != null && !_researchDataList.Contains(data))
            {
                _researchDataList.Add(data);
            }
        }
        Debug.Log($"[GameDataManager] Loaded {_researchDataList.Count} ResearchData to list.");
    }

    private void AutoLoadMarketActors()
    {
        _marketActorDataList.Clear();
        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:MarketActorData");
        foreach (string guid in guids)
        {
            string assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            MarketActorData data = UnityEditor.AssetDatabase.LoadAssetAtPath<MarketActorData>(assetPath);
            if (data != null && !_marketActorDataList.Contains(data))
            {
                _marketActorDataList.Add(data);
            }
        }
        Debug.Log($"[GameDataManager] Loaded {_marketActorDataList.Count} MarketActorData to list.");
    }
#endif
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

    private void HandleDayChanged()
    {
        Employee.UpdateDailyEmployeeStatus();
        Employee.SyncAssignedCountsFromThreads(ThreadPlacement);

        Finances.ApplyReservedDailyExpenses();

        UpdateResourceDeltasFromPlacedThreads();
        Resource.ApplyResourceDeltas();

        Finances.ReserveDailyExpenses();
        Research.OnDayChanged();
        Market.TickDailyMarket();
        Effect.ProcessDayPass(1);
    }

    private void HandleThreadPlacementChanged()
    {
        Finances.ReserveDailyExpenses();
        Employee.UpdateDailyEmployeeStatus();
        OnThreadPlacementChanged.Invoke();
    }

    /// <summary>
    /// 배치된 스레드의 생산/소비를 계산하여 플레이어 인벤토리에 적용합니다.
    /// </summary>
    private void UpdateResourceDeltasFromPlacedThreads()
    {
        foreach (var entry in Resource.GetAllResources().Values)
        {
            if (entry?.resourceState != null) entry.resourceState.deltaCount = 0;
        }

        var placedThreads = ThreadPlacement.GetAllPlacedThreads();
        if (placedThreads == null) return;

        foreach (var placement in placedThreads.Values)
        {
            if (placement == null || placement.RuntimeState == null) continue;

            ThreadState threadState = placement.RuntimeState;
            UpdateThreadProductionProgress(threadState);

            if (threadState.currentProductionProgress >= 1.0f)
            {
                int productionCount = Mathf.FloorToInt(threadState.currentProductionProgress);

                if (threadState.TryGetAggregatedResourceCounts(out var cons, out var prod))
                {
                    ApplyPlayerProduction(prod, productionCount);
                    ApplyPlayerConsumption(cons, productionCount);
                }

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

        float quantityEfficiency = 0f;
        float qualityEfficiency = 1.0f;

        // 1. 수량 효율 계산 (현재 직원 수 / 필요한 직원 수, 0~1 범위)
        if (threadState.requiredEmployees > 0)
        {
            int currentEmployees = threadState.currentWorkers + threadState.currentTechnicians;
            quantityEfficiency = Mathf.Clamp01((float)currentEmployees / threadState.requiredEmployees);
            
            // 2. 품질 효율 계산 (직원들의 현재 효율성 반영)
            if (Employee != null && currentEmployees > 0)
            {
                float totalEfficiencySum = 0f;
                if (threadState.currentWorkers > 0)
                {
                    var workerEntry = Employee.GetEmployeeEntry(EmployeeType.Worker);
                    float workerEff = workerEntry?.state?.currentEfficiency ?? 1.0f;
                    totalEfficiencySum += threadState.currentWorkers * workerEff;
                }

                if (threadState.currentTechnicians > 0)
                {
                    var techEntry = Employee.GetEmployeeEntry(EmployeeType.Technician);
                    float techEff = techEntry?.state?.currentEfficiency ?? 1.0f;
                    totalEfficiencySum += threadState.currentTechnicians * techEff;
                }

                qualityEfficiency = totalEfficiencySum / currentEmployees;
            }
        }
        else
        {
            quantityEfficiency = 0f;
        }

        // 3. 최종 효율 = 수량 효율 * 품질 효율
        threadState.currentProductionEfficiency = quantityEfficiency * qualityEfficiency;
        threadState.currentProductionProgress += threadState.currentProductionEfficiency;
    }

    /// <summary>
    /// 생산품을 플레이어 창고에 추가합니다. (로직 단순화)
    /// </summary>
    private void ApplyPlayerProduction(Dictionary<string, int> production, int multiplier)
    {
        if (production == null || multiplier <= 0) return;

        foreach (KeyValuePair<string, int> item in production)
        {
            int actualProduction = item.Value * multiplier;
            Resource.ModifyPlayerInventory(item.Key, actualProduction);
        }
    }

    /// <summary>
    /// 소비품을 플레이어 창고에서 차감합니다.
    /// </summary>
    private void ApplyPlayerConsumption(Dictionary<string, int> consumption, int multiplier)
    {
        if (consumption == null || multiplier <= 0) return;

        foreach (KeyValuePair<string, int> kvp in consumption)
        {
            string resourceId = kvp.Key;
            int requiredAmount = kvp.Value * multiplier;
            long playerAmount = Resource.GetPlayerResourceQuantity(resourceId);
            
            if (playerAmount >= requiredAmount)
            {
                Resource.ModifyPlayerInventory(resourceId, -requiredAmount);
            }
            else
            {
                long shortage = requiredAmount - playerAmount;
                if (playerAmount > 0)
                {
                    Resource.ModifyPlayerInventory(resourceId, -playerAmount);
                }

                if (Resource.HasEnoughMarketInventory(resourceId, shortage))
                {
                    Resource.ModifyMarketInventory(resourceId, -shortage);
                    Resource.ModifyPlayerInventory(resourceId, shortage);
                    Resource.ModifyPlayerInventory(resourceId, -shortage);
                }
                else
                {
                    Debug.LogWarning($"[GameDataManager] Insufficient resources for production: {resourceId} (required: {requiredAmount}, player: {playerAmount})");
                }
            }
        }
    }
}