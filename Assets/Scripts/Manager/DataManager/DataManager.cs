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
    [SerializeField] private List<GoalData> _goalDataList = new List<GoalData>();

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
    public GoalDataHandler Goal { get; private set; }
    public GameOverDataHandler GameOver { get; private set; }
    public PlacedObjectLayoutDataHandler PlacedLayout { get; private set; }
    public BlueprintLayoutDataHandler BlueprintLayout { get; private set; }

    private readonly List<IDataHandlerEvents> _eventHandlers = new List<IDataHandlerEvents>();
    private readonly List<ITimeChangeHandler> _dayHandlers = new List<ITimeChangeHandler>();
    private readonly List<IMonthChangeHandler> _monthHandlers = new List<IMonthChangeHandler>();
    private readonly List<IGameSaveHandler> _saveHandlers = new List<IGameSaveHandler>();
    private readonly List<ICrossHandlerEvents> _crossHandlerEvents = new List<ICrossHandlerEvents>();
    private readonly HashSet<string> _completedPanelTutorials = new HashSet<string>();

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
        Goal = new GoalDataHandler(this, _goalDataList);
        GameOver = new GameOverDataHandler(this);

        PlacedLayout = new PlacedObjectLayoutDataHandler(this);
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
        _eventHandlers.Add(Goal);
        _eventHandlers.Add(GameOver);

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
        _monthHandlers.Add(GameOver);

        _saveHandlers.Clear();
        _saveHandlers.Add(Finances);
        _saveHandlers.Add(Research);
        _saveHandlers.Add(Order);
        _saveHandlers.Add(News);
        _saveHandlers.Add(MainEvent);
        _saveHandlers.Add(UnionRequest);
        _saveHandlers.Add(Effect);
        _saveHandlers.Add(Policy);
        _saveHandlers.Add(Goal);
        _saveHandlers.Add(GameOver);

        _crossHandlerEvents.Clear();
        _crossHandlerEvents.Add(MainEvent);
        _crossHandlerEvents.Add(Goal);

        Research.ReapplyEffectsFromCompletedResearch();
        Policy.ReapplyEffectsFromActivePolicies();
    }

    /// <summary>
    /// 튜토리얼 씬 진입 시 단일 진입점. 새 게임(핸들러 재생성·오토세이브 삭제) 후 시간을 일시정지합니다.
    /// GameManager.OnSceneLoaded(Tutorial)에서 호출됩니다.
    /// </summary>
    public void ResetToTutorialGame()
    {
        if (Instance != this)
        {
            return;
        }

        SaveLoadManager saveLoadManager = SaveLoadManager.Instance;
        if (saveLoadManager != null)
        {
            saveLoadManager.StartNewGame(this);
        }
        else
        {
            ResetToNewGame();
        }

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

        _completedPanelTutorials.Clear();
        InitializeServices();
        ClearAllEventSubscriptions();
    }

    public bool IsPanelTutorialCompleted(string ownerGameObjectName)
    {
        return !string.IsNullOrEmpty(ownerGameObjectName)
            && _completedPanelTutorials.Contains(ownerGameObjectName);
    }

    public void MarkPanelTutorialCompleted(string ownerGameObjectName)
    {
        if (!string.IsNullOrEmpty(ownerGameObjectName))
            _completedPanelTutorials.Add(ownerGameObjectName);
    }

    private void CapturePanelTutorialsTo(GameSaveData saveData)
    {
        saveData.completedPanelTutorialOwnerKeys.Clear();
        foreach (string ownerKey in _completedPanelTutorials)
            saveData.completedPanelTutorialOwnerKeys.Add(ownerKey);
    }

    private void ApplyPanelTutorialsFrom(GameSaveData saveData)
    {
        _completedPanelTutorials.Clear();
        foreach (string ownerKey in saveData.completedPanelTutorialOwnerKeys)
        {
            if (!string.IsNullOrEmpty(ownerKey))
                _completedPanelTutorials.Add(ownerKey);
        }
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
        AutoLoadToList(_goalDataList);
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
        Time.OnYearChanged -= HandleYearChanged;
        Time.OnYearChanged += HandleYearChanged;

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

    private void HandleYearChanged()
    {
        GameOver?.HandleYearChanged();
    }

    public void CaptureGameStateTo(GameSaveData saveData)
    {
        if (saveData == null)
        {
            return;
        }

        if (Time != null)
            Time.CaptureTo(saveData);

        if (Employee != null)
        {
            foreach (KeyValuePair<EmployeeType, EmployeeEntry> kvp in Employee.GetAllEmployees())
            {
                saveData.employees.Add(new EmployeeStateSaveData(kvp.Key, CloneState(kvp.Value.state)));
            }
        }

        Resource?.CaptureTo(saveData);

        if (MarketActor != null)
        {
            foreach (KeyValuePair<string, MarketActorEntry> kvp in MarketActor.GetAllMarketActors())
            {
                saveData.marketActors.Add(new MarketActorStateSaveData(kvp.Key, CloneState(kvp.Value.state)));
            }
        }

        foreach (IGameSaveHandler handler in _saveHandlers)
            handler.CaptureTo(saveData);

        MainRunner sceneRunner = FindActiveBuildingSceneRunner();
        if (sceneRunner != null && sceneRunner.GridHandler != null)
        {
            List<PlacedBuildingSaveData> placedBuildings = new List<PlacedBuildingSaveData>();
            placedBuildings.AddRange(sceneRunner.GridHandler.ExportPlacedBuildings());
            if (sceneRunner.RawBuildingHandler != null)
                placedBuildings.AddRange(sceneRunner.RawBuildingHandler.ExportPlacedBuildings());

            saveData.placedBuildings = placedBuildings;
            saveData.placedRoads = sceneRunner.GridHandler.ExportPlacedRoads();
        }
        else
        {
            PlacedLayout?.CopyLayoutSnapshotTo(saveData);
        }

        if (BlueprintLayout != null)
            saveData.blueprintLayouts = BlueprintLayout.ExportSaveData();

        CapturePanelTutorialsTo(saveData);
    }

    public void ApplyGameStateFrom(GameSaveData saveData)
    {
        if (saveData == null)
        {
            return;
        }

        Time?.ApplyFromSave(
            saveData.year,
            saveData.month,
            saveData.day,
            saveData.currentHour,
            saveData.dayProgress);

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

        Resource?.ApplyFromSave(saveData);

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

        PlacedLayout?.SetFromSave(saveData.placedBuildings, saveData.placedRoads);
        BlueprintLayout?.SetFromSave(saveData.blueprintLayouts);

        ApplyPanelTutorialsFrom(saveData);

        Goal?.BeginLayoutRestore();
        foreach (IGameSaveHandler handler in _saveHandlers)
            handler.ApplyFromSave(saveData);
        Goal?.EndLayoutRestore();

        Resource?.NotifyResourceStateRestored();
    }

    /// <summary>
    /// 마지막으로 배치된 건물부터 직원 할당을 해제합니다. MainRunner가 없으면 PlacedLayout 데이터를 갱신합니다.
    /// </summary>
    public int UnassignEmployeesFromLastPlacedBuildings(EmployeeType type, int count)
    {
        if (count <= 0)
            return 0;

        MainRunner sceneRunner = FindActiveBuildingSceneRunner();
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
        MainRunner sceneRunner = FindActiveBuildingSceneRunner();
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

    private static T CloneState<T>(T state)
    {
        string json = JsonUtility.ToJson(state);
        return JsonUtility.FromJson<T>(json);
    }

    private static MainRunner FindActiveBuildingSceneRunner()
    {
        MainRunner sceneRunner = UnityEngine.Object.FindAnyObjectByType<MainRunner>();
        if (sceneRunner != null)
            return sceneRunner;

        MainRunner[] runners =
            UnityEngine.Object.FindObjectsByType<MainRunner>(FindObjectsSortMode.None);
        return runners.Length > 0 ? runners[0] : null;
    }
}