using System;
using System.Collections.Generic;
using UnityEngine;

public class DataManager : Singleton<DataManager>
{

    [Header("Initial Data")]
    [SerializeField] private InitialResourceData _initialResourceData;
    [SerializeField] private InitialMarketActorData _initialMarketActorData;
    [SerializeField] private InitialEmployeeData _initialEmployeeData;
    [SerializeField] private InitialTimeData _timeSettingsData;
    [SerializeField] private InitialResearchData _initialResearchData;
    [SerializeField] private InitialFinancesData _initialFinancesData;
    [SerializeField] private InitialEffectData _initialEffectData;
    [SerializeField] private InitialOrderData _initialOrderData;
    [SerializeField] private InitialNewsData _initialNewsData;
    [SerializeField] private InitialPolicyData _initialFactoryPolicyData;
    [SerializeField] private InitialUnionMainEventData _initialUnionMainEventData;
    [SerializeField] private InitialWarMainEventData _initialWarMainEventData;
    [SerializeField] private InitialAutomationMainEventData _initialAutomationMainEventData;

    [Header("Game Data Lists")]
    [SerializeField] private List<BuildingData> _buildingDataList = new List<BuildingData>();
    [SerializeField] private List<ResourceData> _resourceDataList = new List<ResourceData>();
    [SerializeField] private List<EmployeeData> _employeeDataList = new List<EmployeeData>();
    [SerializeField] private List<ResearchData> _researchDataList = new List<ResearchData>();
    [SerializeField] private List<MarketActorData> _marketActorDataList = new List<MarketActorData>();
    [SerializeField] private List<OrderData> _orderDataList = new List<OrderData>();
    [SerializeField] private List<NewsData> _newsDataList = new List<NewsData>();
    [SerializeField] private List<PolicyData> _policyDataList = new List<PolicyData>();
    [SerializeField] private List<UnionRequestData> _unionRequestDataList = new List<UnionRequestData>();

    public InitialTimeData InitialTimeData => _timeSettingsData;
    public InitialEmployeeData InitialEmployeeData => _initialEmployeeData;
    public InitialResearchData InitialResearchData => _initialResearchData;
    public InitialEffectData InitialEffectData => _initialEffectData;
    public InitialOrderData InitialOrderData => _initialOrderData;
    public InitialPolicyData InitialFactoryPolicyData => _initialFactoryPolicyData;
    public InitialUnionMainEventData InitialUnionMainEventData => _initialUnionMainEventData;
    public InitialWarMainEventData InitialWarMainEventData => _initialWarMainEventData;
    public InitialAutomationMainEventData InitialAutomationMainEventData => _initialAutomationMainEventData;

    public TimeDataHandler Time { get; private set; }
    public ResourceDataHandler Resource { get; private set; }
    public MarketActorDataHandler MarketActor { get; private set; }
    public FinancesDataHandler Finances { get; private set; }
    public EmployeeDataHandler Employee { get; private set; }
    public BuildingDataHandler Building { get; private set; }
    public EffectDataHandler Effect { get; private set; }
    public ResearchDataHandler Research { get; private set; }

    /// <summary>연구 완료 시 발행됩니다. 인자는 완료된 연구 ID입니다.</summary>
    public event Action<string> OnResearchCompleted
    {
        add
        {
            if (Research != null)
                Research.OnResearchCompleted += value;
        }
        remove
        {
            if (Research != null)
                Research.OnResearchCompleted -= value;
        }
    }
    public OrderDataHandler Order { get; private set; }
    public NewsDataHandler News { get; private set; }
    public PolicyDataHandler Policy { get; private set; }
    public MainEventDataHandler MainEvent { get; private set; }
    public UnionRequestDataHandler UnionRequest { get; private set; }

    public PlayerDataHandler Player { get; private set; }

    public PlacedObjectLayoutDataHandler PlacedLayout { get; private set; }
    public BlueprintLayoutDataHandler BlueprintLayout { get; private set; }

    private readonly List<IDataHandlerEvents> _eventHandlers = new List<IDataHandlerEvents>();
    private readonly List<ITimeChangeHandler> _dayHandlers = new List<ITimeChangeHandler>();
    private readonly List<IMonthChangeHandler> _monthHandlers = new List<IMonthChangeHandler>();
    private readonly List<IGameSaveHandler> _saveHandlers = new List<IGameSaveHandler>();
    private readonly List<ICrossHandlerEvents> _crossHandlerEvents = new List<ICrossHandlerEvents>();

    private bool _servicesInitialized;

    void Update()
    {
        float deltaTime = UnityEngine.Time.deltaTime;
        Time?.Update(deltaTime);
        Resource?.FlushPendingResourceChangedNotify();
    }

    void LateUpdate()
    {
        Resource?.FlushPendingResourceChangedNotify();
    }

    protected override void Awake()
    {
        base.Awake();
    }

    public void Init()
    {
        if (Instance != this) return;
        if (_servicesInitialized) return;

        _servicesInitialized = true;
        InitializeServices();
    }

    private void InitializeServices()
    {
        Time = new TimeDataHandler(this, _timeSettingsData);
        Resource = new ResourceDataHandler(this, _resourceDataList, _initialResourceData);
        MarketActor = new MarketActorDataHandler(this, _marketActorDataList, _initialMarketActorData);
        Finances = new FinancesDataHandler(this, _initialFinancesData);
        Employee = new EmployeeDataHandler(this, _employeeDataList, _initialEmployeeData);
        Building = new BuildingDataHandler(this, _buildingDataList);
        Effect = new EffectDataHandler();
        Research = new ResearchDataHandler(this, _researchDataList);
        Order = new OrderDataHandler(this, _orderDataList, _initialOrderData);
        News = new NewsDataHandler(this, _newsDataList, _initialNewsData);
        Policy = new PolicyDataHandler(this, _policyDataList, _initialFactoryPolicyData);
        UnionRequest = new UnionRequestDataHandler(this, _unionRequestDataList, _initialUnionMainEventData);
        MainEvent = new MainEventDataHandler(
            this,
            _initialUnionMainEventData,
            _initialWarMainEventData,
            _initialAutomationMainEventData);

        Player = new PlayerDataHandler();

        PlacedLayout = new PlacedObjectLayoutDataHandler();
        BlueprintLayout = new BlueprintLayoutDataHandler();

        _eventHandlers.Clear();
        _eventHandlers.Add(Time);
        _eventHandlers.Add(Resource);
        _eventHandlers.Add(Finances);
        _eventHandlers.Add(Employee);
        _eventHandlers.Add(Research);
        _eventHandlers.Add(MarketActor);
        _eventHandlers.Add(News);
        _eventHandlers.Add(Order);
        _eventHandlers.Add(Policy);
        _eventHandlers.Add(UnionRequest);
        _eventHandlers.Add(MainEvent);

        _dayHandlers.Clear();
        _dayHandlers.Add(Resource);
        _dayHandlers.Add(MarketActor);
        _dayHandlers.Add(Research);
        _dayHandlers.Add(Employee);
        _dayHandlers.Add(Finances);
        _dayHandlers.Add(Effect);
        _dayHandlers.Add(News);
        _dayHandlers.Add(Order);
        _dayHandlers.Add(MainEvent);

        _monthHandlers.Clear();
        _monthHandlers.Add(Finances);
        _monthHandlers.Add(Policy);

        _saveHandlers.Clear();
        _saveHandlers.Add(Finances);
        _saveHandlers.Add(Research);
        _saveHandlers.Add(Order);
        _saveHandlers.Add(News);
        _saveHandlers.Add(MainEvent);
        _saveHandlers.Add(UnionRequest);
        _saveHandlers.Add(Effect);
        _saveHandlers.Add(Policy);
        _saveHandlers.Add(Player);

        _crossHandlerEvents.Clear();
        _crossHandlerEvents.Add(MainEvent);

        Research.ReapplyEffectsFromCompletedResearch();
        Policy.ReapplyEffectsFromActivePolicies();
    }

    /// <summary>
    /// 튜토리얼 씬 진입용 초기화. 새 게임 상태에 시간을 일시정지합니다.
    /// </summary>
    public void ResetToTutorialGame()
    {
        ResetToNewGame();
        Time?.PauseTime();
    }

    /// <summary>
    /// Initial 데이터 기준으로 모든 핸들러 상태를 재생성합니다. 타이틀 새 게임 시 호출.
    /// </summary>
    public void ResetToNewGame()
    {
        if (Instance != this)
        {
            return;
        }

        InitializeServices();
        ClearAllEventSubscriptions();
    }

#if UNITY_EDITOR
    private const string PolicyDataSearchFolder = "Assets/Datas/Policy";
    private const string UnionRequestDataSearchFolder = "Assets/ScriptableObjects/UnionRequestData";

    [ContextMenu("Auto Load All Data to Lists")]
    private void AutoLoadAllDataToLists()
    {
        AutoLoadToList(_buildingDataList);
        AutoLoadToList(_resourceDataList);
        AutoLoadToList(_employeeDataList);
        AutoLoadToList(_researchDataList);
        AutoLoadToList(_marketActorDataList);
        AutoLoadToList(_orderDataList);
        AutoLoadToList(_newsDataList);
        AutoLoadToList(_unionRequestDataList, UnionRequestDataSearchFolder);
        AutoLoadPolicyDataList();

        UnityEditor.EditorUtility.SetDirty(this);
        Debug.Log("[DataManager] All data auto-loaded to lists.");
    }

    [ContextMenu("Auto Load Policy Data List")]
    private void AutoLoadPolicyDataListContextMenu()
    {
        AutoLoadPolicyDataList();
        UnityEditor.EditorUtility.SetDirty(this);
    }

    private void AutoLoadPolicyDataList()
    {
        AutoLoadToList(_policyDataList, PolicyDataSearchFolder);
    }

    private void AutoLoadToList<T>(List<T> list, string searchInFolder = null) where T : UnityEngine.Object
    {
        list.Clear();
        string typeName = typeof(T).Name;
        string filter = "t:" + typeName;
        string[] guids = string.IsNullOrEmpty(searchInFolder)
            ? UnityEditor.AssetDatabase.FindAssets(filter)
            : UnityEditor.AssetDatabase.FindAssets(filter, new[] { searchInFolder });
        foreach (string guid in guids)
        {
            string assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            T data = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (data != null && !list.Contains(data))
                list.Add(data);
        }
        Debug.Log($"[DataManager] Loaded {list.Count} {typeName} to list{(searchInFolder != null ? $" ({searchInFolder})" : "")}.");
    }
#endif

    /// <summary>
    /// 씬 전환 시 모든 핸들러의 이벤트 구독을 초기화합니다.
    /// 각 컴포넌트가 필요할 때 다시 구독하도록 합니다.
    /// </summary>
    public void ClearAllEventSubscriptions()
    {
        foreach (IDataHandlerEvents handler in _eventHandlers)
            handler.ClearAllSubscriptions();

        Time.OnDayChanged -= HandleDayChanged;
        Time.OnDayChanged += HandleDayChanged;
        Time.OnMonthChanged -= HandleMonthChanged;
        Time.OnMonthChanged += HandleMonthChanged;

        foreach (ICrossHandlerEvents handler in _crossHandlerEvents)
            handler.SubscribeCrossHandlerEvents();
    }

    private void HandleDayChanged()
    {
        foreach (ITimeChangeHandler handler in _dayHandlers)
            handler.HandleDayChanged();
    }

    private void HandleMonthChanged()
    {
        foreach (IMonthChangeHandler handler in _monthHandlers)
            handler.HandleMonthChanged();

        SaveLoadManager saveLoadManager = SaveLoadManager.Instance;
        if (saveLoadManager != null)
            saveLoadManager.PerformAutoSave(this);
    }

    public void CaptureGameStateTo(GameSaveData saveData)
    {
        if (saveData == null)
        {
            return;
        }

        if (Time != null)
        {
            saveData.year = Time.Year;
            saveData.month = Time.Month;
            saveData.day = Time.Day;
            saveData.currentHour = Time.CurrentHour;
            saveData.dayProgress = Time.DayProgress;
            saveData.isPaused = Time.IsPaused;
            saveData.timeSpeed = Time.TimeSpeed;
        }

        if (Employee != null)
        {
            foreach (KeyValuePair<EmployeeType, EmployeeEntry> kvp in Employee.GetAllEmployees())
            {
                saveData.employees.Add(new EmployeeStateSaveData(kvp.Key, CloneState(kvp.Value.state)));
            }
        }

        if (Resource != null)
        {
            foreach (KeyValuePair<string, ResourceEntry> kvp in Resource.GetAllResources())
            {
                saveData.resources.Add(new ResourceStateSaveData(kvp.Key, CloneState(kvp.Value.state)));
            }
        }

        if (MarketActor != null)
        {
            foreach (KeyValuePair<string, MarketActorEntry> kvp in MarketActor.GetAllMarketActors())
            {
                saveData.marketActors.Add(new MarketActorStateSaveData(kvp.Key, CloneState(kvp.Value.state)));
            }
        }

        foreach (IGameSaveHandler handler in _saveHandlers)
            handler.CaptureTo(saveData);

        BuildingSceneRunnerBase sceneRunner = FindActiveBuildingSceneRunner();
        if (sceneRunner != null && sceneRunner.GridHandler != null)
        {
            saveData.placedBuildings = sceneRunner.GridHandler.ExportPlacedBuildings();
            saveData.placedRoads = sceneRunner.GridHandler.ExportPlacedRoads();
        }

        if (BlueprintLayout != null)
            saveData.blueprintLayouts = BlueprintLayout.ExportSaveData();
    }

    public void ApplyGameStateFrom(GameSaveData saveData)
    {
        if (saveData == null)
        {
            return;
        }

        NormalizeLoadedSaveData(saveData);
        LegacyIdMigration.MigrateSaveData(saveData);

        if (Time != null)
        {
            Time.SetDate(saveData.year, saveData.month, saveData.day);
            Time.SetTimeSpeed(saveData.timeSpeed);
            Time.PauseTime();
        }

        if (Employee != null)
        {
            foreach (EmployeeStateSaveData employeeSave in saveData.employees)
            {
                EmployeeEntry entry = Employee.GetEmployeeEntry(employeeSave.type);
                if (entry != null)
                {
                    entry.state = employeeSave.state;
                }
            }
        }

        if (Resource != null)
        {
            foreach (ResourceStateSaveData resourceSave in saveData.resources)
            {
                ResourceEntry entry = Resource.GetResourceEntry(resourceSave.resourceId);
                if (entry != null)
                {
                    entry.state = resourceSave.state;
                }
            }
        }

        if (MarketActor != null)
        {
            foreach (MarketActorStateSaveData actorSave in saveData.marketActors)
            {
                MarketActorEntry entry = MarketActor.GetMarketActorEntry(actorSave.actorId);
                if (entry != null)
                {
                    entry.state = actorSave.state;
                }
            }
        }

        foreach (IGameSaveHandler handler in _saveHandlers)
            handler.ApplyFromSave(saveData);

        PlacedLayout?.SetFromSave(saveData.placedBuildings, saveData.placedRoads);
        BlueprintLayout?.SetFromSave(saveData.blueprintLayouts);
    }

    /// <summary>
    /// 마지막으로 배치된 건물부터 직원 할당을 해제합니다. MainRunner가 없으면 PlacedLayout 데이터를 갱신합니다.
    /// </summary>
    public int UnassignEmployeesFromLastPlacedBuildings(EmployeeType type, int count)
    {
        if (count <= 0)
            return 0;

        BuildingSceneRunnerBase sceneRunner = FindActiveBuildingSceneRunner();
        if (sceneRunner != null && sceneRunner.GridHandler != null)
            return sceneRunner.GridHandler.UnassignEmployeesFromLastBuildings(type, count);

        if (PlacedLayout == null)
            return 0;

        int removed = PlacedLayout.UnassignEmployeesFromLastBuildings(type, count);
        if (removed > 0)
            Employee?.UnassignUpTo(type, removed);

        return removed;
    }

    public int GetPlacedBuildingAssignedEmployeeCount(EmployeeType type)
    {
        BuildingSceneRunnerBase sceneRunner = FindActiveBuildingSceneRunner();
        if (sceneRunner != null && sceneRunner.GridHandler != null)
            return sceneRunner.GridHandler.GetTotalAssignedEmployeeCount(type);

        return PlacedLayout?.GetTotalAssignedEmployeeCount(type) ?? 0;
    }

    /// <summary>
    /// 자동 직원 배치: 건물 슬롯 채우기 전후로 필요 매니저·부족 인원 고용을 시도합니다.
    /// </summary>
    public void TryAutoStaffBuilding(BuildingObject building)
    {
        if (building == null || Employee == null)
            return;

        Employee.TryEnsureRequiredManagers();
        building.TryAutoAssignEmployeesToFill();
        Employee.TryEnsureRequiredManagers();
    }

    private static void NormalizeLoadedSaveData(GameSaveData data)
    {
        data.employees ??= new List<EmployeeStateSaveData>();
        data.resources ??= new List<ResourceStateSaveData>();
        data.marketActors ??= new List<MarketActorStateSaveData>();
        data.monthlyCreditHistory ??= new List<long>();
        data.monthlyWealthHistory ??= new List<long>();
        data.researches ??= new List<ResearchStateSaveData>();
        data.factoryPolicies ??= new List<PolicyStateSaveData>();
        data.activeOrders ??= new List<OrderState>();
        data.activeNews ??= new List<NewsState>();
        data.activeUnionRequests ??= new List<UnionRequestState>();
        data.effects ??= new EffectStateSaveData();
        data.effects.globalEffects ??= new List<GlobalEffectStateSaveData>();
        data.effects.instanceEffects ??= new List<InstanceEffectStateSaveData>();
        data.placedBuildings ??= new List<PlacedBuildingSaveData>();
        data.placedRoads ??= new List<PlacedRoadSaveData>();
        data.blueprintLayouts ??= new List<BlueprintLayoutSaveData>();
        data.tutorialAutoShowPending ??= new List<TutorialAutoShowPendingSaveData>();
    }

    private static T CloneState<T>(T state)
    {
        string json = JsonUtility.ToJson(state);
        return JsonUtility.FromJson<T>(json);
    }

    private static BuildingSceneRunnerBase FindActiveBuildingSceneRunner()
    {
        BuildingSceneRunnerBase sceneRunner = UnityEngine.Object.FindAnyObjectByType<MainRunner>();
        if (sceneRunner != null)
            return sceneRunner;

        BuildingSceneRunnerBase[] runners =
            UnityEngine.Object.FindObjectsByType<BuildingSceneRunnerBase>(FindObjectsSortMode.None);
        return runners.Length > 0 ? runners[0] : null;
    }
}