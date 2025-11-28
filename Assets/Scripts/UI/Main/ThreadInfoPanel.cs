using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 스레드 정보를 표시하고 직원을 할당할 수 있는 패널
/// </summary>
public class ThreadInfoPanel : MonoBehaviour
{
    [Header("Resource References")]
    [SerializeField] private Transform _provideContentTransform;
    [SerializeField] private Transform _consumeContentTransform;

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private TextMeshProUGUI _maintenanceText;
    [SerializeField] private TextMeshProUGUI _categoryText;
    [SerializeField] private Image _image;

    [Header("Employee Assignment")]
    [SerializeField] private Slider _productionEfficiencySlider;
    [SerializeField] private Slider _productionProgressSlider;
    [SerializeField] private TextMeshProUGUI _currentWorkersText;
    [SerializeField] private TextMeshProUGUI _currentTechniciansText;

    private ThreadState _currentThreadState;
    private GameDataManager _dataManager;
    private MainUiManager _mainUiManager;
    private bool _isSubscribed = false;

    /// <summary>
    /// 패널을 초기화합니다.
    /// </summary>
    public void OnInitialize(ThreadState threadState, MainUiManager mainUiManager, GameDataManager dataManager)
    {
        _currentThreadState = threadState;
        _mainUiManager = mainUiManager;
        _dataManager = dataManager;

        if (threadState == null)
        {
            Debug.LogWarning("[ThreadInfoPanel] ThreadState is null!");
            return;
        }

        // 이벤트 구독 (하루마다 정보 업데이트)
        SubscribeToDayChanged();
        
        UpdateUI();
    }

    void OnEnable()
    {
        // 패널이 활성화될 때 이벤트 구독
        if (!_isSubscribed)
        {
            SubscribeToDayChanged();
        }
    }

    void OnDisable()
    {
        // 패널이 비활성화될 때 이벤트 구독 해제
        UnsubscribeFromDayChanged();
    }

    void OnDestroy()
    {
        // 파괴될 때 이벤트 구독 해제
        UnsubscribeFromDayChanged();
    }

    /// <summary>
    /// 하루 변경 이벤트를 구독합니다.
    /// </summary>
    private void SubscribeToDayChanged()
    {
        if (_dataManager?.Time != null && !_isSubscribed)
        {
            _dataManager.Time.OnDayChanged += OnDayChanged;
            _isSubscribed = true;
        }
    }

    /// <summary>
    /// 하루 변경 이벤트 구독을 해제합니다.
    /// </summary>
    private void UnsubscribeFromDayChanged()
    {
        if (_dataManager?.Time != null && _isSubscribed)
        {
            _dataManager.Time.OnDayChanged -= OnDayChanged;
            _isSubscribed = false;
        }
    }

    /// <summary>
    /// 하루가 지날 때 호출되는 콜백
    /// </summary>
    private void OnDayChanged()
    {
        if (gameObject.activeSelf)
        {
            UpdateUI();
        }
    }

    /// <summary>
    /// UI를 업데이트합니다.
    /// </summary>
    private void UpdateUI()
    {
        if (_currentThreadState == null)
            return;

        // 기본 정보 업데이트
        if (_nameText != null)
            _nameText.text = _currentThreadState.threadName;

        // 카테고리 이름 가져오기
        if (_categoryText != null)
        {
            string categoryName = GetCategoryName(_currentThreadState.categoryId);
            _categoryText.text = categoryName;
        }

        // 유지비는 ThreadState에 저장된 값 사용
        if (_maintenanceText != null)
        {
            _maintenanceText.text = $"Maintenance: {_currentThreadState.totalMaintenanceCost:N0}/month";
        }

        // 이미지 로딩
        if (_image != null)
        {
            LoadPreviewImage();
        }

        // 리소스 정보 업데이트
        UpdateResourceDisplay();

        // 직원 할당 정보 업데이트
        UpdateEmployeeAssignment();

        // 생산 진행도 및 효율 슬라이더 업데이트
        UpdateProductionStatus();
    }

    /// <summary>
    /// 카테고리 이름을 가져옵니다.
    /// </summary>
    private string GetCategoryName(string categoryId)
    {
        if (string.IsNullOrEmpty(categoryId) || _dataManager == null)
            return "None";

        var category = _dataManager.Thread.GetCategory(categoryId);
        if (category != null)
        {
            return category.categoryName;
        }

        return "None";
    }

    /// <summary>
    /// 스레드 설명을 가져옵니다.
    /// </summary>
    private string GetThreadDescription()
    {
        if (_currentThreadState.buildingStateList == null || _currentThreadState.buildingStateList.Count == 0)
            return "No buildings in this thread.";

        // 건물들의 이름을 나열하여 설명으로 사용
        List<string> buildingNames = new List<string>();
        foreach (var buildingState in _currentThreadState.buildingStateList)
        {
            if (buildingState == null || string.IsNullOrEmpty(buildingState.buildingId))
                continue;

            if (_dataManager != null)
            {
                var buildingData = _dataManager.Building.GetBuildingData(buildingState.buildingId);
                if (buildingData != null)
                {
                    buildingNames.Add(buildingData.displayName);
                }
            }
        }

        if (buildingNames.Count > 0)
        {
            return $"Buildings: {string.Join(", ", buildingNames)}";
        }

        return "Thread description";
    }


    /// <summary>
    /// 미리보기 이미지를 로드합니다.
    /// </summary>
    private void LoadPreviewImage()
    {
        if (string.IsNullOrEmpty(_currentThreadState.previewImagePath))
        {
            _image.enabled = false;
            return;
        }

        // 파일 시스템 경로에서 이미지 로드 (ThreadBtn과 동일한 방식)
        Sprite loadedSprite = SpriteUtils.LoadSpriteFromFile(_currentThreadState.previewImagePath);
        if (loadedSprite != null)
        {
            _image.sprite = loadedSprite;
            _image.enabled = true;
        }
        else
        {
            Debug.LogWarning($"[ThreadInfoPanel] Failed to load image from path: {_currentThreadState.previewImagePath}");
            _image.enabled = false;
        }
    }

    /// <summary>
    /// 입력/출력 자원 정보를 표시합니다.
    /// </summary>
    private void UpdateResourceDisplay()
    {
        if (_currentThreadState == null || _dataManager == null || _mainUiManager == null)
            return;

        // 기존 내용 지우기
        if (_provideContentTransform != null)
            GameObjectUtils.ClearChildren(_provideContentTransform);
        if (_consumeContentTransform != null)
            GameObjectUtils.ClearChildren(_consumeContentTransform);

        // ThreadState에서 집계된 자원 정보 가져오기
        if (_currentThreadState.TryGetAggregatedResourceCounts(
            out Dictionary<string, int> consumptionCounts,
            out Dictionary<string, int> productionCounts))
        {
            // 입력 자원 표시
            if (consumptionCounts != null && consumptionCounts.Count > 0 && _provideContentTransform != null)
            {
                foreach (var kvp in consumptionCounts)
                {
                    ResourceEntry resourceEntry = _dataManager.Resource.GetResourceEntry(kvp.Key);
                    if (resourceEntry != null && _mainUiManager.ProductionInfoImage != null)
                    {
                        Instantiate(_mainUiManager.ProductionInfoImage, _provideContentTransform)
                            .GetComponent<ProductionInfoImage>().OnInitialize(resourceEntry, kvp.Value);
                    }
                }
            }

            // 출력 자원 표시
            if (productionCounts != null && productionCounts.Count > 0 && _consumeContentTransform != null)
            {
                foreach (var kvp in productionCounts)
                {
                    ResourceEntry resourceEntry = _dataManager.Resource.GetResourceEntry(kvp.Key);
                    if (resourceEntry != null && _mainUiManager.ProductionInfoImage != null)
                    {
                        Instantiate(_mainUiManager.ProductionInfoImage, _consumeContentTransform)
                            .GetComponent<ProductionInfoImage>().OnInitialize(resourceEntry, kvp.Value);
                    }
                }
            }
        }
    }

    /// <summary>
    /// 직원 할당 UI를 업데이트합니다.
    /// </summary>
    private void UpdateEmployeeAssignment()
    {
        if (_currentThreadState == null)
            return;

        // 필요한 직원 수 계산 (건물들의 requiredEmployees 합계)
        int requiredWorkers = CalculateRequiredWorkers();
        int requiredTechnicians = CalculateRequiredTechnicians();

        // Worker 텍스트 업데이트: "현재 / 필요한"
        if (_currentWorkersText != null)
        {
            _currentWorkersText.text = $"{_currentThreadState.currentWorkers} / {requiredWorkers}";
        }

        // Technician 텍스트 업데이트: "현재 / 필요한"
        if (_currentTechniciansText != null)
        {
            _currentTechniciansText.text = $"{_currentThreadState.currentTechnicians} / {requiredTechnicians}";
        }
    }

    /// <summary>
    /// 생산 진행도 및 효율 슬라이더를 업데이트합니다.
    /// </summary>
    private void UpdateProductionStatus()
    {
        if (_currentThreadState == null)
            return;

        // 생산 효율 슬라이더 업데이트 (0.0 ~ 1.0)
        if (_productionEfficiencySlider != null)
        {
            _productionEfficiencySlider.value = _currentThreadState.currentProductionEfficiency;
        }

        // 생산 진행도 슬라이더 업데이트 (0.0 ~ 1.0)
        if (_productionProgressSlider != null)
        {
            _productionProgressSlider.value = _currentThreadState.currentProductionProgress;
        }
    }

    /// <summary>
    /// 필요한 Worker 수를 반환합니다.
    /// requiredEmployees를 그대로 사용합니다 (구분 없이).
    /// </summary>
    private int CalculateRequiredWorkers()
    {
        if (_currentThreadState == null)
            return 0;

        // requiredEmployees를 그대로 반환 (구분 없이 사용)
        return _currentThreadState.requiredEmployees;
    }

    /// <summary>
    /// 필요한 Technician 수를 반환합니다.
    /// requiredEmployees를 그대로 사용합니다 (구분 없이).
    /// </summary>
    private int CalculateRequiredTechnicians()
    {
        if (_currentThreadState == null)
            return 0;

        // requiredEmployees를 그대로 반환 (구분 없이 사용)
        return _currentThreadState.requiredEmployees;
    }

    /// <summary>
    /// Worker를 1명 증가시킵니다 (버튼용).
    /// </summary>
    public void AddWorker()
    {
        if (_currentThreadState == null || _dataManager == null)
            return;

        int requiredWorkers = CalculateRequiredWorkers();
        int currentTotal = _currentThreadState.currentWorkers + _currentThreadState.currentTechnicians;

        // requiredEmployees를 초과하지 않도록 체크
        if (currentTotal >= _currentThreadState.requiredEmployees)
        {
            Debug.LogWarning($"[ThreadInfoPanel] Cannot add worker: Maximum employees reached ({_currentThreadState.requiredEmployees})");
            return;
        }

        // 할당 가능한 Worker가 있는지 확인
        int availableWorkers = _dataManager.Employee.GetAvailableEmployeeCount("worker");
        if (availableWorkers <= 0)
        {
            Debug.LogWarning($"[ThreadInfoPanel] Cannot add worker: No available workers (hired: {_dataManager.Employee.GetEmployeeCount("worker")}, assigned: {_dataManager.Employee.GetAssignedEmployeeCount("worker")})");
            return;
        }

        // Worker 할당 (TryAssignEmployee가 assignedCount를 체크하여 배치된 인원은 다른 스레드에 배치될 수 없도록 보장)
        if (_dataManager.Employee.TryAssignEmployee("worker", 1))
        {
            _currentThreadState.currentWorkers++;
            // 직원 할당 상태 즉시 동기화 (다른 스레드에서도 최신 상태 반영)
            _dataManager.Employee.SyncAssignedCountsFromThreads(_dataManager.ThreadPlacement);
            UpdateEmployeeAssignment();
            UpdateProductionStatus();
        }
    }

    /// <summary>
    /// Technician을 1명 증가시킵니다 (버튼용).
    /// </summary>
    public void AddTechnician()
    {
        if (_currentThreadState == null || _dataManager == null)
            return;

        int requiredTechnicians = CalculateRequiredTechnicians();
        int currentTotal = _currentThreadState.currentWorkers + _currentThreadState.currentTechnicians;

        // requiredEmployees를 초과하지 않도록 체크
        if (currentTotal >= _currentThreadState.requiredEmployees)
        {
            Debug.LogWarning($"[ThreadInfoPanel] Cannot add technician: Maximum employees reached ({_currentThreadState.requiredEmployees})");
            return;
        }

        // 할당 가능한 Technician이 있는지 확인
        int availableTechnicians = _dataManager.Employee.GetAvailableEmployeeCount("technician");
        if (availableTechnicians <= 0)
        {
            Debug.LogWarning($"[ThreadInfoPanel] Cannot add technician: No available technicians (hired: {_dataManager.Employee.GetEmployeeCount("technician")}, assigned: {_dataManager.Employee.GetAssignedEmployeeCount("technician")})");
            return;
        }

        // Technician 할당 (TryAssignEmployee가 assignedCount를 체크하여 배치된 인원은 다른 스레드에 배치될 수 없도록 보장)
        if (_dataManager.Employee.TryAssignEmployee("technician", 1))
        {
            _currentThreadState.currentTechnicians++;
            // 직원 할당 상태 즉시 동기화 (다른 스레드에서도 최신 상태 반영)
            _dataManager.Employee.SyncAssignedCountsFromThreads(_dataManager.ThreadPlacement);
            UpdateEmployeeAssignment();
            UpdateProductionStatus();
        }
    }

    /// <summary>
    /// Worker로 requiredEmployees의 최대 직원까지 추가합니다 (이미 들어가있는 직원 제외, 버튼용).
    /// </summary>
    public void FillWorkersToMax()
    {
        if (_currentThreadState == null || _dataManager == null)
            return;

        int currentTotal = _currentThreadState.currentWorkers + _currentThreadState.currentTechnicians;
        int availableSlots = _currentThreadState.requiredEmployees - currentTotal;

        if (availableSlots <= 0)
        {
            Debug.LogWarning($"[ThreadInfoPanel] Cannot fill workers: No available slots (current: {currentTotal}, max: {_currentThreadState.requiredEmployees})");
            return;
        }

        // 할당 가능한 Worker 수 확인
        int availableWorkers = _dataManager.Employee.GetAvailableEmployeeCount("worker");
        int workersToAdd = Mathf.Min(availableSlots, availableWorkers);

        if (workersToAdd > 0)
        {
            if (_dataManager.Employee.TryAssignEmployee("worker", workersToAdd))
            {
                _currentThreadState.currentWorkers += workersToAdd;
                // 직원 할당 상태 즉시 동기화 (다른 스레드에서도 최신 상태 반영)
                _dataManager.Employee.SyncAssignedCountsFromThreads(_dataManager.ThreadPlacement);
                UpdateEmployeeAssignment();
                UpdateProductionStatus();
            }
        }
        else
        {
            Debug.LogWarning($"[ThreadInfoPanel] Cannot fill workers: No available workers (hired: {_dataManager.Employee.GetEmployeeCount("worker")}, assigned: {_dataManager.Employee.GetAssignedEmployeeCount("worker")})");
        }
    }

    /// <summary>
    /// Technician으로 requiredEmployees의 최대 직원까지 추가합니다 (이미 들어가있는 직원 제외, 버튼용).
    /// </summary>
    public void FillTechniciansToMax()
    {
        if (_currentThreadState == null || _dataManager == null)
            return;

        int currentTotal = _currentThreadState.currentWorkers + _currentThreadState.currentTechnicians;
        int availableSlots = _currentThreadState.requiredEmployees - currentTotal;

        if (availableSlots <= 0)
        {
            Debug.LogWarning($"[ThreadInfoPanel] Cannot fill technicians: No available slots (current: {currentTotal}, max: {_currentThreadState.requiredEmployees})");
            return;
        }

        // 할당 가능한 Technician 수 확인
        int availableTechnicians = _dataManager.Employee.GetAvailableEmployeeCount("technician");
        int techniciansToAdd = Mathf.Min(availableSlots, availableTechnicians);

        if (techniciansToAdd > 0)
        {
            if (_dataManager.Employee.TryAssignEmployee("technician", techniciansToAdd))
            {
                _currentThreadState.currentTechnicians += techniciansToAdd;
                // 직원 할당 상태 즉시 동기화 (다른 스레드에서도 최신 상태 반영)
                _dataManager.Employee.SyncAssignedCountsFromThreads(_dataManager.ThreadPlacement);
                UpdateEmployeeAssignment();
                UpdateProductionStatus();
            }
        }
        else
        {
            Debug.LogWarning($"[ThreadInfoPanel] Cannot fill technicians: No available technicians (hired: {_dataManager.Employee.GetEmployeeCount("technician")}, assigned: {_dataManager.Employee.GetAssignedEmployeeCount("technician")})");
        }
    }

    /// <summary>
    /// Worker를 1명 감소시킵니다 (버튼용).
    /// </summary>
    public void RemoveWorker()
    {
        if (_currentThreadState == null || _dataManager == null)
            return;

        if (_currentThreadState.currentWorkers <= 0)
        {
            Debug.LogWarning("[ThreadInfoPanel] Cannot remove worker: No workers assigned");
            return;
        }

        // Worker 할당 해제
        if (_dataManager.Employee.TryUnassignEmployee("worker", 1))
        {
            _currentThreadState.currentWorkers--;
            // 직원 할당 상태 즉시 동기화 (다른 스레드에서도 최신 상태 반영)
            _dataManager.Employee.SyncAssignedCountsFromThreads(_dataManager.ThreadPlacement);
            UpdateEmployeeAssignment();
            UpdateProductionStatus();
        }
    }

    /// <summary>
    /// Technician을 1명 감소시킵니다 (버튼용).
    /// </summary>
    public void RemoveTechnician()
    {
        if (_currentThreadState == null || _dataManager == null)
            return;

        if (_currentThreadState.currentTechnicians <= 0)
        {
            Debug.LogWarning("[ThreadInfoPanel] Cannot remove technician: No technicians assigned");
            return;
        }

        // Technician 할당 해제
        if (_dataManager.Employee.TryUnassignEmployee("technician", 1))
        {
            _currentThreadState.currentTechnicians--;
            // 직원 할당 상태 즉시 동기화 (다른 스레드에서도 최신 상태 반영)
            _dataManager.Employee.SyncAssignedCountsFromThreads(_dataManager.ThreadPlacement);
            UpdateEmployeeAssignment();
            UpdateProductionStatus();
        }
    }

    /// <summary>
    /// 모든 Worker를 제거합니다 (버튼용).
    /// </summary>
    public void RemoveAllWorkers()
    {
        if (_currentThreadState == null || _dataManager == null)
            return;

        if (_currentThreadState.currentWorkers <= 0)
        {
            return;
        }

        int workersToRemove = _currentThreadState.currentWorkers;
        if (_dataManager.Employee.TryUnassignEmployee("worker", workersToRemove))
        {
            _currentThreadState.currentWorkers = 0;
            // 직원 할당 상태 즉시 동기화 (다른 스레드에서도 최신 상태 반영)
            _dataManager.Employee.SyncAssignedCountsFromThreads(_dataManager.ThreadPlacement);
            UpdateEmployeeAssignment();
            UpdateProductionStatus();
        }
    }

    /// <summary>
    /// 모든 Technician을 제거합니다 (버튼용).
    /// </summary>
    public void RemoveAllTechnicians()
    {
        if (_currentThreadState == null || _dataManager == null)
            return;

        if (_currentThreadState.currentTechnicians <= 0)
        {
            return;
        }

        int techniciansToRemove = _currentThreadState.currentTechnicians;
        if (_dataManager.Employee.TryUnassignEmployee("technician", techniciansToRemove))
        {
            _currentThreadState.currentTechnicians = 0;
            // 직원 할당 상태 즉시 동기화 (다른 스레드에서도 최신 상태 반영)
            _dataManager.Employee.SyncAssignedCountsFromThreads(_dataManager.ThreadPlacement);
            UpdateEmployeeAssignment();
            UpdateProductionStatus();
        }
    }

    /// <summary>
    /// 직원을 스레드에 할당합니다.
    /// </summary>
    public void AssignEmployee(string employeeId, int count)
    {
        if (_currentThreadState == null || _dataManager == null)
            return;

        // 직원 할당 로직은 AddWorker/AddTechnician으로 처리
        Debug.Log($"[ThreadInfoPanel] AssignEmployee called: {employeeId}, count: {count}");
    }

    /// <summary>
    /// 스레드에서 직원 할당을 해제합니다.
    /// </summary>
    public void UnassignEmployee(string employeeId, int count)
    {
        if (_currentThreadState == null || _dataManager == null)
            return;

        // 직원 할당 해제 로직은 RemoveWorker/RemoveTechnician으로 처리
        Debug.Log($"[ThreadInfoPanel] UnassignEmployee called: {employeeId}, count: {count}");
    }

    /// <summary>
    /// 패널을 숨깁니다.
    /// </summary>
    public void Hide()
    {
        gameObject.SetActive(false);
    }
}