using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DataManager : MonoBehaviour
{
    public static DataManager Instance { get; private set; }

    [Header("Initial Data")]
    [SerializeField] private InitialResourceData _initialResourceData;
    [SerializeField] private InitialMarketData _initialMarketData;
    [SerializeField] private InitialEmployeeData _initialEmployeeData;
    [SerializeField] private InitialTimeData _timeSettingsData;
    [SerializeField] private InitialResearchData _initialResearchData;
    [SerializeField] private InitialFinancesData _initialFinancesData;

    [Header("Game Data Lists")]
    [SerializeField] private List<BuildingData> _buildingDataList = new List<BuildingData>();
    [SerializeField] private List<ResourceData> _resourceDataList = new List<ResourceData>();
    [SerializeField] private List<EmployeeData> _employeeDataList = new List<EmployeeData>();
    [SerializeField] private List<ResearchData> _researchDataList = new List<ResearchData>();
    [SerializeField] private List<MarketActorData> _marketActorDataList = new List<MarketActorData>();

    public InitialTimeData InitialTimeData => _timeSettingsData;
    public InitialEmployeeData InitialEmployeeData => _initialEmployeeData;
    public InitialResearchData InitialResearchData => _initialResearchData;

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

    void Update()
    {
        float deltaTime = UnityEngine.Time.deltaTime;
        Time?.Update(deltaTime);
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
        Resource = new ResourceDataHandler(this, _resourceDataList, _initialResourceData);
        Market = new MarketDataHandler(this, _marketActorDataList, _initialMarketData);
        Finances = new FinancesDataHandler(this, _initialFinancesData);
        Employee = new EmployeeDataHandler(this, _employeeDataList, _initialEmployeeData);
        Building = new BuildingDataHandler(this, _buildingDataList);
        ThreadCalculate = new ThreadCalculateHandler(this);
        Effect = new EffectDataHandler(this);
        Research = new ResearchDataHandler(this, _researchDataList);

        LoadThreadData();
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

    /// <summary>
    /// 씬 전환 시 모든 핸들러의 이벤트 구독을 초기화합니다.
    /// 각 컴포넌트가 필요할 때 다시 구독하도록 합니다.
    /// </summary>
    public void ClearAllEventSubscriptions()
    {
        ThreadPlacement.ClearAllSubscriptions();
        Time.ClearAllSubscriptions();
        Resource.ClearAllSubscriptions();
        Thread.ClearAllSubscriptions();
        Finances.ClearAllSubscriptions();
        Employee.ClearAllSubscriptions();
        Research.ClearAllSubscriptions();
        Market.ClearAllSubscriptions();

        ThreadPlacement.OnPlacementChanged -= HandleThreadPlacementChanged;
        ThreadPlacement.OnPlacementChanged += HandleThreadPlacementChanged;
        Time.OnDayChanged -= HandleDayChanged;
        Time.OnDayChanged += HandleDayChanged;

        Debug.Log("[DataManager] All event subscriptions cleared.");
    }

    private void HandleThreadPlacementChanged()
    {
        Employee.HandleDayChanged();
    }

    private void HandleDayChanged()
    {
        Resource.HandleDayChanged();
        Research.HandleDayChanged();
        Employee.HandleDayChanged();
        Employee.SyncAssignedCountsFromThreads(ThreadPlacement);
        Effect.ProcessDayPass(1);

        Finances.HandleDayChanged();

        UpdateResourceDeltasFromPlacedThreads();
    }

    /// <summary>
    /// 배치된 스레드의 생산/소비를 계산합니다
    /// </summary>
    private void UpdateResourceDeltasFromPlacedThreads()
    {
        IReadOnlyDictionary<Vector2Int, ThreadPlacementState> placedThreads = ThreadPlacement.GetAllPlacedThreads();
        if (placedThreads == null) return;

        foreach (ThreadPlacementState placement in placedThreads.Values)
        {
            if (placement == null || placement.RuntimeState == null) continue;

            ThreadState threadState = placement.RuntimeState;
            UpdateThreadProductionProgress(threadState);

            if (threadState.currentProductionProgress >= 1.0f)
            {
                int productionCount = Mathf.FloorToInt(threadState.currentProductionProgress);

                if (threadState.TryGetAggregatedResourceCounts(out Dictionary<string, int> cons, out Dictionary<string, int> prod))
                {
                    ModifyPlayerProduction(prod, productionCount);
                    ModifyPlayerProduction(cons, -productionCount);
                }

                threadState.currentProductionProgress -= productionCount;
            }
        }
    }

    /// <summary>
    /// 스레드의 생산 진행도와 효율을 업데이트합니다
    /// </summary>
    private void UpdateThreadProductionProgress(ThreadState threadState)
    {
        if (threadState == null) return;

        float quantityEfficiency = 0f;
        float qualityEfficiency = 1.0f;

        if (threadState.requiredEmployees > 0)
        {
            int currentEmployees = threadState.currentWorkers + threadState.currentTechnicians;
            quantityEfficiency = Mathf.Clamp01((float)currentEmployees / threadState.requiredEmployees);

            if (Employee != null && currentEmployees > 0)
            {
                float totalEfficiencySum = 0f;
                if (threadState.currentWorkers > 0)
                {
                    EmployeeEntry workerEntry = Employee.GetEmployeeEntry(EmployeeType.Worker);
                    float workerEff = workerEntry.state.currentEfficiency;
                    totalEfficiencySum += threadState.currentWorkers * workerEff;
                }

                if (threadState.currentTechnicians > 0)
                {
                    EmployeeEntry techEntry = Employee.GetEmployeeEntry(EmployeeType.Technician);
                    float techEff = techEntry.state.currentEfficiency;
                    totalEfficiencySum += threadState.currentTechnicians * techEff;
                }

                qualityEfficiency = totalEfficiencySum / currentEmployees;
            }
        }
        else
        {
            quantityEfficiency = 0f;
        }

        threadState.currentProductionEfficiency = quantityEfficiency * qualityEfficiency;
        threadState.currentProductionProgress += threadState.currentProductionEfficiency;
    }

    /// <summary>
    /// 스레드의 생산품 가감을 적용합니다
    /// </summary>
    private void ModifyPlayerProduction(Dictionary<string, int> resourceDic, int multiplier)
    {
        foreach (KeyValuePair<string, int> kvp in resourceDic)
        {
            string resourceId = kvp.Key;
            int requiredAmount = kvp.Value * multiplier;

            Resource.ModifyThreadDelta(resourceId, requiredAmount);
        }
    }
}