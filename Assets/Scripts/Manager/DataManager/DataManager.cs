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

    [Header("Game Data Lists")]
    [SerializeField] private List<BuildingData> _buildingDataList = new List<BuildingData>();
    [SerializeField] private List<ResourceData> _resourceDataList = new List<ResourceData>();
    [SerializeField] private List<EmployeeData> _employeeDataList = new List<EmployeeData>();
    [SerializeField] private List<ResearchData> _researchDataList = new List<ResearchData>();
    [SerializeField] private List<MarketActorData> _marketActorDataList = new List<MarketActorData>();
    [SerializeField] private List<OrderData> _orderDataList = new List<OrderData>();
    [SerializeField] private List<NewsData> _newsDataList = new List<NewsData>();

    public InitialTimeData InitialTimeData => _timeSettingsData;
    public InitialEmployeeData InitialEmployeeData => _initialEmployeeData;
    public InitialResearchData InitialResearchData => _initialResearchData;
    public InitialEffectData InitialEffectData => _initialEffectData;
    public InitialOrderData InitialOrderData => _initialOrderData;

    public TimeDataHandler Time { get; private set; }
    public ResourceDataHandler Resource { get; private set; }
    public MarketActorDataHandler MarketActor { get; private set; }
    public FinancesDataHandler Finances { get; private set; }
    public EmployeeDataHandler Employee { get; private set; }
    public BuildingDataHandler Building { get; private set; }
    public EffectDataHandler Effect { get; private set; }
    public ResearchDataHandler Research { get; private set; }
    public OrderDataHandler Order { get; private set; }
    public NewsDataHandler News { get; private set; }

    public PlayerDataHandler Player { get; private set; }

    public PlacedObjectLayoutDataHandler PlacedLayout { get; private set; }

    private readonly List<IDataHandlerEvents> _eventHandlers = new List<IDataHandlerEvents>();
    private readonly List<ITimeChangeHandler> _dayHandlers = new List<ITimeChangeHandler>();

    private bool _servicesInitialized;

    void Update()
    {
        float deltaTime = UnityEngine.Time.deltaTime;
        Time?.Update(deltaTime);
        Resource?.FlushPendingResourceChangedNotify();
    }

    void LateUpdate()
    {
        // Time 이후에 실행되는 다른 Update에서 자원이 바뀐 경우까지 같은 프레임에서 한 번 더 반영합니다.
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

        Player = new PlayerDataHandler();

        PlacedLayout = new PlacedObjectLayoutDataHandler();

        _eventHandlers.Clear();
        _eventHandlers.Add(Time);
        _eventHandlers.Add(Resource);
        _eventHandlers.Add(Finances);
        _eventHandlers.Add(Employee);
        _eventHandlers.Add(Research);
        _eventHandlers.Add(MarketActor);
        _eventHandlers.Add(News);
        _eventHandlers.Add(Order);

        _dayHandlers.Clear();
        _dayHandlers.Add(Resource);
        _dayHandlers.Add(MarketActor);
        _dayHandlers.Add(Research);
        _dayHandlers.Add(Employee);
        _dayHandlers.Add(Finances);
        _dayHandlers.Add(Effect);
        _dayHandlers.Add(News);
        _dayHandlers.Add(Order);

        Research.ReapplyEffectsFromCompletedResearch();
    }

#if UNITY_EDITOR
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

        UnityEditor.EditorUtility.SetDirty(this);
        Debug.Log("[DataManager] All data auto-loaded to lists.");
    }

    private void AutoLoadToList<T>(List<T> list) where T : UnityEngine.Object
    {
        list.Clear();
        string typeName = typeof(T).Name;
        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:" + typeName);
        foreach (string guid in guids)
        {
            string assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            T data = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (data != null && !list.Contains(data))
                list.Add(data);
        }
        Debug.Log($"[DataManager] Loaded {list.Count} {typeName} to list.");
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
    }

    private void HandleDayChanged()
    {
        foreach (ITimeChangeHandler handler in _dayHandlers)
            handler.HandleDayChanged();
    }

    private void HandleMonthChanged()
    {
        Finances.HandleMonthChanged();

        SaveLoadManager saveLoadManager = SaveLoadManager.Instance;
        if (saveLoadManager != null)
        {
            saveLoadManager.PerformAutoSave(this);
        }
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

        Finances?.CaptureTo(saveData);
        Research?.CaptureTo(saveData);
        Order?.CaptureTo(saveData);
        News?.CaptureTo(saveData);
        Effect?.CaptureTo(saveData);
        Player?.CaptureTo(saveData);

        MainRunner mainRunner = UnityEngine.Object.FindAnyObjectByType<MainRunner>();
        if (mainRunner != null && mainRunner.GridHandler != null)
        {
            saveData.placedBuildings = mainRunner.GridHandler.ExportPlacedBuildings();
            saveData.placedRoads = mainRunner.GridHandler.ExportPlacedRoads();
        }
    }

    public void ApplyGameStateFrom(GameSaveData saveData)
    {
        if (saveData == null)
        {
            return;
        }

        NormalizeLoadedSaveData(saveData);

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

        Finances?.ApplyFromSave(saveData);
        Research?.ApplyFromSave(saveData);
        Order?.ApplyFromSave(saveData);
        News?.ApplyFromSave(saveData);
        Effect?.ApplyFromSave(saveData);
        Player?.ApplyFromSave(saveData);

        PlacedLayout?.SetFromSave(saveData.placedBuildings, saveData.placedRoads);
    }

    private static void NormalizeLoadedSaveData(GameSaveData data)
    {
        data.employees ??= new List<EmployeeStateSaveData>();
        data.resources ??= new List<ResourceStateSaveData>();
        data.marketActors ??= new List<MarketActorStateSaveData>();
        data.monthlyCreditHistory ??= new List<long>();
        data.monthlyWealthHistory ??= new List<long>();
        data.researches ??= new List<ResearchStateSaveData>();
        data.activeOrders ??= new List<OrderState>();
        data.activeNews ??= new List<NewsState>();
        data.effects ??= new EffectStateSaveData();
        data.effects.globalEffects ??= new List<GlobalEffectStateSaveData>();
        data.effects.instanceEffects ??= new List<InstanceEffectStateSaveData>();
        data.placedBuildings ??= new List<PlacedBuildingSaveData>();
        data.placedRoads ??= new List<PlacedRoadSaveData>();
        data.tutorialAutoShowPending ??= new List<TutorialAutoShowPendingSaveData>();
    }

    private static T CloneState<T>(T state)
    {
        string json = JsonUtility.ToJson(state);
        return JsonUtility.FromJson<T>(json);
    }
}