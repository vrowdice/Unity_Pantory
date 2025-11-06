using System;
using System.Collections.Generic;
using UnityEngine;

// 게임 데이터를 관리하는 싱글톤 허브 클래스 (실제 데이터 관리는 각 서비스 클래스에 위임)
public class GameDataManager : MonoBehaviour
{
    public static GameDataManager Instance { get; private set; }

    [Header("Initial Data")]
    [SerializeField] private InitialResourceData _initialResourceData;
    
    [Header("Time Settings")]
    [SerializeField] private TimeSettingsData _timeSettingsData;

    private TimeDataHandler _timeService;
    public TimeDataHandler Time => _timeService;

    private ThreadDataHandler _threadService;
    public ThreadDataHandler Thread => _threadService;

    private ResourceDataHandler _resourceService;
    public ResourceDataHandler Resource => _resourceService;

    private FinancesDataHandler _financesService;
    public FinancesDataHandler Finances => _financesService;

    private EmployeeDataHandler _employeeService;
    public EmployeeDataHandler Employee => _employeeService;

    private BuildingDataHandler _buildingService;
    public BuildingDataHandler Building => _buildingService;

    private SaveLoadHandler _saveLoadHandler;
    public SaveLoadHandler SaveLoad => _saveLoadHandler;

    // 자원 변경 이벤트 (ResourceService의 이벤트를 중계)
    public event Action OnResourceChanged
    {
        add => _resourceService.OnResourceChanged += value;
        remove => _resourceService.OnResourceChanged -= value;
    }

    // 금액 변경 이벤트 (FinancesService의 이벤트를 중계)
    public event Action OnSilverChanged
    {
        add => _financesService.OnCreditChanged += value;
        remove => _financesService.OnCreditChanged -= value;
    }

    // 직원 변경 이벤트 (EmployeeService의 이벤트를 중계)
    public event Action OnEmployeeChanged
    {
        add => _employeeService.OnEmployeeChanged += value;
        remove => _employeeService.OnEmployeeChanged -= value;
    }

    // 건물 변경 이벤트 (BuildingService의 이벤트를 중계)
/*    public event Action OnBuildingChanged
    {
        add => _buildingService.OnBuildingChanged += value;
        remove => _buildingService.OnBuildingChanged -= value;
    }*/

    // Thread 변경 이벤트 (ThreadService의 이벤트를 중계)
    public event Action OnThreadChanged
    {
        add => _threadService.OnThreadChanged += value;
        remove => _threadService.OnThreadChanged -= value;
    }

    // Category 변경 이벤트 (ThreadService의 이벤트를 중계)
    public event Action OnCategoryChanged
    {
        add => _threadService.OnCategoryChanged += value;
        remove => _threadService.OnCategoryChanged -= value;
    }

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

    // 모든 서비스를 초기화
    private void InitializeServices()
    {
        _timeService = new TimeDataHandler();
        _resourceService = new ResourceDataHandler(); // 자동으로 ResourceData 로드
        _financesService = new FinancesDataHandler();
        _employeeService = new EmployeeDataHandler(); // 자동으로 EmployeeData 로드
        _buildingService = new BuildingDataHandler(); // 자동으로 BuildingData 로드
        _threadService = new ThreadDataHandler();
        
        // SaveLoadHandler 초기화
        _saveLoadHandler = GetComponent<SaveLoadHandler>();
        if (_saveLoadHandler == null)
        {
            _saveLoadHandler = gameObject.AddComponent<SaveLoadHandler>();
        }
        
        Debug.Log("[GameDataManager] All services initialized.");

        // 시간 설정 적용
        ApplyTimeSettings();

        // 초기 자원 적용
        ApplyInitialResources();

        // Thread 데이터 자동 로드
        LoadThreadData();
    }

    /// <summary>
    /// Thread 데이터를 자동으로 로드합니다.
    /// </summary>
    private void LoadThreadData()
    {
        if (_saveLoadHandler != null && _threadService != null)
        {
            _saveLoadHandler.LoadThreadData(_threadService);
        }
    }

    /// <summary>
    /// Thread 데이터를 저장합니다.
    /// </summary>
    public void SaveThreadData()
    {
        if (_saveLoadHandler != null && _threadService != null)
        {
            _saveLoadHandler.SaveThreadData(_threadService);
        }
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

        _timeSettingsData.ApplyToTimeService(_timeService);
    }

    /// <summary>
    /// 초기 자원 데이터를 적용합니다.
    /// </summary>
    private void ApplyInitialResources()
    {
        if (_initialResourceData == null)
        {
            Debug.LogWarning("[GameDataManager] InitialResourceData가 할당되지 않았습니다. 기본값(0)으로 시작합니다.");
            return;
        }

        _initialResourceData.ApplyToServices(_resourceService, _financesService);
    }

    // ----------------- 편의 메서드 (ResourceService 직접 호출) -----------------
    
    // 특정 자원의 현재 보유량 반환
    public long GetResourceQuantity(string resourceId) => _resourceService.GetResourceQuantity(resourceId);

    // 특정 자원의 현재 가격 반환
    public float GetResourcePrice(string resourceId) => _resourceService.GetResourcePrice(resourceId);

    // 특정 자원의 ResourceEntry 반환
    public ResourceEntry GetResourceEntry(string resourceId) => _resourceService.GetResourceEntry(resourceId);

    // 모든 자원 정보 반환
    public Dictionary<string, ResourceEntry> GetAllResources() => _resourceService.GetAllResources();

    // 특정 자원 추가
    public void AddResource(string resourceId, long amount) => _resourceService.AddResource(resourceId, amount);

    // 특정 자원 제거
    public bool TryRemoveResource(string resourceId, long amount) => _resourceService.TryRemoveResource(resourceId, amount);

    // 특정 자원 충분 여부 확인
    public bool HasEnoughResource(string resourceId, long amount) => _resourceService.HasEnoughResource(resourceId, amount);

    // ----------------- 편의 메서드 (FinancesService 직접 호출) -----------------

    // 현재 보유 금액 반환
    public long GetCredit() => _financesService.GetCredit();

    // 금액 추가
    public void AddCredit(long amount) => _financesService.AddCredit(amount);

    // 금액 차감
    public bool TryRemoveCredit(long amount) => _financesService.TryRemoveCredit(amount);

    // 금액 충분 여부 확인
    public bool HasEnoughCredit(long amount) => _financesService.HasEnoughCredit(amount);

    // ----------------- 편의 메서드 (EmployeeService 직접 호출) -----------------

    // 특정 직원 유형의 인원 수 반환
    public int GetEmployeeCount(string employeeId) => _employeeService.GetEmployeeCount(employeeId);

    // 특정 직원 유형의 총 급여 반환
    public long GetEmployeeTotalSalary(string employeeId) => _employeeService.GetEmployeeTotalSalary(employeeId);

    // 모든 직원의 총 급여 반환
    public long GetTotalSalary() => _employeeService.GetTotalSalary();

    // 직원 고용
    public void HireEmployee(string employeeId, int count) => _employeeService.HireEmployee(employeeId, count);

    // 직원 해고
    public bool TryFireEmployee(string employeeId, int count) => _employeeService.TryFireEmployee(employeeId, count);

    // 직원 인원 수 설정
    public void SetEmployeeCount(string employeeId, int count) => _employeeService.SetEmployeeCount(employeeId, count);

    // 모든 직원 정보 반환
    public Dictionary<string, EmployeeEntry> GetAllEmployees() => _employeeService.GetAllEmployees();

    // ----------------- 편의 메서드 (BuildingService 직접 호출) -----------------

    // 특정 건물의 BuildingData 반환
    public BuildingData GetBuildingData(string buildingId) => _buildingService.GetBuildingData(buildingId);

    // 모든 건물 데이터 반환
    public Dictionary<string, BuildingData> GetAllBuildings() => _buildingService.GetAllBuildings();

    // 특정 타입의 건물 데이터 리스트 반환
    public List<BuildingData> GetBuildingDataList(BuildingType buildingType) => _buildingService.GetBuildingDataList(buildingType);

    // 건물 등록 여부 확인
    public bool IsBuildingRegistered(string buildingId) => _buildingService.IsBuildingRegistered(buildingId);

    // 등록된 건물 타입 개수 반환
    public int GetBuildingTypeCount() => _buildingService.GetBuildingTypeCount();

    // ----------------- 편의 메서드 (ThreadService 직접 호출) -----------------

    // 특정 Thread 반환
    public ThreadState GetThread(string threadId) => _threadService.GetThread(threadId);

    // 모든 Thread 반환 (Dictionary)
    public Dictionary<string, ThreadState> GetAllThreads() => _threadService.GetAllThreads();

    // 모든 Thread 반환 (List)
    public List<ThreadState> GetAllThreadList() => _threadService.GetAllThreadList();

    // 모든 Thread ID 반환
    public List<string> GetAllThreadIds() => _threadService.GetAllThreadIds();

    // Thread의 건물 리스트 반환
    public List<BuildingState> GetBuildingStates(string threadId) => _threadService.GetBuildingStates(threadId);

    // Thread 존재 여부 확인
    public bool HasThread(string threadId) => _threadService.HasThread(threadId);

    // Thread 추가
    public bool AddThread(ThreadState threadState) => _threadService.AddThread(threadState);

    // Thread 생성 및 추가
    public ThreadState CreateThread(string threadId, string threadName, string division = "") => _threadService.CreateThread(threadId, threadName, division);

    // Thread 제거
    public bool RemoveThread(string threadId) => _threadService.RemoveThread(threadId);

    // Thread에 건물 추가
    public bool AddBuildingToThread(string threadId, BuildingState buildingState) => _threadService.AddBuilding(threadId, buildingState);

    // Thread에서 건물 제거
    public bool RemoveBuildingFromThread(string threadId, Vector2Int position) => _threadService.RemoveBuilding(threadId, position);

    // 특정 위치의 건물 반환
    public BuildingState GetBuildingAt(string threadId, Vector2Int position) => _threadService.GetBuildingAt(threadId, position);

    // Thread 개수 반환
    public int GetThreadCount() => _threadService.GetThreadCount();

    // ----------------- 편의 메서드 (ThreadService - Category 관련) -----------------

    // 카테고리 추가
    public bool AddCategory(ThreadCategory category) => _threadService.AddCategory(category);

    // 카테고리 생성 및 추가
    public ThreadCategory CreateCategory(string categoryId, string categoryName) => _threadService.CreateCategory(categoryId, categoryName);

    // 카테고리 제거
    public bool RemoveCategory(string categoryId) => _threadService.RemoveCategory(categoryId);

    // 카테고리 이름 변경
    public bool RenameCategory(string categoryId, string newName) => _threadService.RenameCategory(categoryId, newName);

    // 특정 카테고리 반환
    public ThreadCategory GetCategory(string categoryId) => _threadService.GetCategory(categoryId);

    // 모든 카테고리 반환
    public Dictionary<string, ThreadCategory> GetAllCategories() => _threadService.GetAllCategories();

    // 모든 카테고리 ID 반환
    public List<string> GetAllCategoryIds() => _threadService.GetAllCategoryIds();

    // 카테고리 존재 여부 확인
    public bool HasCategory(string categoryId) => _threadService.HasCategory(categoryId);

    // 카테고리 개수 반환
    public int GetCategoryCount() => _threadService.GetCategoryCount();

    // 카테고리에 스레드 추가
    public bool AddThreadToCategory(string categoryId, string threadId) => _threadService.AddThreadToCategory(categoryId, threadId);

    // 카테고리에서 스레드 제거
    public bool RemoveThreadFromCategory(string categoryId, string threadId) => _threadService.RemoveThreadFromCategory(categoryId, threadId);

    // 특정 카테고리에 속한 스레드 ID 목록 반환
    public List<string> GetThreadIdsInCategory(string categoryId) => _threadService.GetThreadIdsInCategory(categoryId);

    // 특정 카테고리에 속한 스레드 상태 목록 반환
    public List<ThreadState> GetThreadsInCategory(string categoryId) => _threadService.GetThreadsInCategory(categoryId);

    // ----------------- 레거시 호환 프로퍼티 -----------------
    
    public long Silver => _financesService.GetCredit();

    // ----------------- 편의 메서드 (TimeService 직접 호출) -----------------

    public void PauseTime() => _timeService.PauseTime();
    public void ResumeTime() => _timeService.ResumeTime();
    public void SetTimeSpeed(float speed) => _timeService.SetTimeSpeed(speed);
    public bool IsTimePaused() => _timeService.IsTimePaused();
    public float GetTimeSpeed() => _timeService.GetTimeSpeed();
    public string GetDateString() => _timeService.GetDateString();

    void Start()
    {
        
    }

    void Update()
    {
        // TimeService 업데이트
        if (_timeService != null)
        {
            _timeService.Update(UnityEngine.Time.deltaTime);
        }
    }
}
