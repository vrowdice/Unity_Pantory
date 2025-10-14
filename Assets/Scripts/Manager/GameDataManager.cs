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

    private TimeService _timeService;
    public TimeService Time => _timeService;

    private ThreadService _threadService;
    public ThreadService Thread => _threadService;

    private ResourceService _resourceService;
    public ResourceService Resource => _resourceService;

    private FinancesService _financesService;
    public FinancesService Finances => _financesService;

    private EmployeeService _employeeService;
    public EmployeeService Employee => _employeeService;

    private BuildingService _buildingService;
    public BuildingService Building => _buildingService;

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
    public event Action OnBuildingChanged
    {
        add => _buildingService.OnBuildingChanged += value;
        remove => _buildingService.OnBuildingChanged -= value;
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
        _timeService = new TimeService();
        _resourceService = new ResourceService(); // 자동으로 ResourceData 로드
        _financesService = new FinancesService();
        _employeeService = new EmployeeService(); // 자동으로 EmployeeData 로드
        _buildingService = new BuildingService(); // 자동으로 BuildingData 로드
        Debug.Log("[GameDataManager] All services initialized.");

        // 시간 설정 적용
        ApplyTimeSettings();

        // 초기 자원 적용
        ApplyInitialResources();
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

    // 특정 건물의 레벨 반환
    public int GetBuildingLevel(string buildingId) => _buildingService.GetBuildingLevel(buildingId);

    // 특정 건물의 작업 효율 반환
    public float GetBuildingEfficiency(string buildingId) => _buildingService.GetBuildingEfficiency(buildingId);

    // 특정 건물이 건설되어 있는지 확인
    public bool IsBuildingConstructed(string buildingId) => _buildingService.IsBuildingConstructed(buildingId);

    // 특정 건물의 BuildingEntry 반환
    public BuildingEntry GetBuildingEntry(string buildingId) => _buildingService.GetBuildingEntry(buildingId);

    // 모든 건물 정보 반환
    public Dictionary<string, BuildingEntry> GetAllBuildings() => _buildingService.GetAllBuildings();

    // 특정 타입의 건물 리스트 반환
    public List<BuildingEntry> GetBuildingEntryList(BuildingType buildingType) => _buildingService.GetBuildingEntryList(buildingType);

    // 건물 건설
    public bool ConstructBuilding(string buildingId) => _buildingService.ConstructBuilding(buildingId);

    // 건물 철거
    public bool DemolishBuilding(string buildingId) => _buildingService.DemolishBuilding(buildingId);

    // 건물 업그레이드
    public bool UpgradeBuilding(string buildingId) => _buildingService.UpgradeBuilding(buildingId);

    // 건물 레벨 설정
    public void SetBuildingLevel(string buildingId, int level) => _buildingService.SetBuildingLevel(buildingId, level);

    // 건물 효율 설정
    public void SetBuildingEfficiency(string buildingId, float efficiency) => _buildingService.SetBuildingEfficiency(buildingId, efficiency);

    // 총 유지비 반환
    public int GetTotalMaintenanceCost() => _buildingService.GetTotalMaintenanceCost();

    // 건설된 건물 수 반환
    public int GetConstructedBuildingCount() => _buildingService.GetConstructedBuildingCount();

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
        // 초기화 로직
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
