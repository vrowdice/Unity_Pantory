using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Evo;

/// <summary>
/// 스레드 정보를 표시하고 직원을 할당할 수 있는 패널
/// (버튼 로직 제거됨: 오직 슬라이더로만 제어)
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

    // [수정] 효율 및 진행도 텍스트 참조
    [SerializeField] private TextMeshProUGUI _productionEfficiencyText;
    [SerializeField] private TextMeshProUGUI _productionProgressText;

    // 전체 고용된 인원 표시용 (Global Hired)
    [SerializeField] private TextMeshProUGUI _currentWorkersText;
    [SerializeField] private TextMeshProUGUI _currentTechniciansText;

    // 현재 스레드의 최대 수용 인원 표시용 (Local Max)
    [SerializeField] private TextMeshProUGUI _maxWorkersText;
    [SerializeField] private TextMeshProUGUI _maxTechniciansText;

    // 현재 스레드에 할당된 인원 표시용 (Local Assigned)
    [SerializeField] private TextMeshProUGUI _assignedWorkersText;
    [SerializeField] private TextMeshProUGUI _assignedTechniciansText;

    [SerializeField] private Slider _workerSlider;
    [SerializeField] private Slider _technicianSlider;

    // 상수 정의
    private const string ID_WORKER = "worker";
    private const string ID_TECHNICIAN = "technician";

    private ThreadState _currentThreadState;
    private GameDataManager _dataManager;
    private MainUiManager _mainUiManager;
    private bool _isSubscribed = false;

    // UI 갱신 중 이벤트 발생을 차단하기 위한 플래그
    private bool _isUpdatingUI = false;

    #region 초기화 및 이벤트 관리

    public void OnInitialize(ThreadState threadState, MainUiManager mainUiManager, GameDataManager dataManager)
    {
        _currentThreadState = threadState;
        _mainUiManager = mainUiManager;
        _dataManager = dataManager;

        if (_currentThreadState == null)
        {
            Debug.LogWarning("[ThreadInfoPanel] ThreadState is null!");
            return;
        }

        SubscribeToDayChanged();
        UpdateUI();
    }

    void OnEnable()
    {
        if (!_isSubscribed) SubscribeToDayChanged();
    }

    void OnDisable()
    {
        UnsubscribeFromDayChanged();
    }

    void OnDestroy()
    {
        UnsubscribeFromDayChanged();
    }

    private void SubscribeToDayChanged()
    {
        if (_dataManager?.Time != null && !_isSubscribed)
        {
            _dataManager.Time.OnDayChanged += OnDayChanged;
            _isSubscribed = true;
        }
    }

    private void UnsubscribeFromDayChanged()
    {
        if (_dataManager?.Time != null && _isSubscribed)
        {
            _dataManager.Time.OnDayChanged -= OnDayChanged;
            _isSubscribed = false;
        }
    }

    private void OnDayChanged()
    {
        if (gameObject.activeSelf) UpdateUI();
    }

    #endregion

    #region UI 업데이트

    private void UpdateUI()
    {
        if (_currentThreadState == null) return;

        // UI 업데이트 시작 시 플래그 설정 (이벤트 루프 방지)
        _isUpdatingUI = true;

        try
        {
            // 1. 텍스트 정보 업데이트
            if (_nameText != null) _nameText.text = _currentThreadState.threadName;

            if (_categoryText != null)
                _categoryText.text = GetCategoryName(_currentThreadState.categoryId);

            if (_maintenanceText != null)
                _maintenanceText.text = $"Maintenance: {_currentThreadState.totalMaintenanceCost:N0}/month";

            // 2. 이미지 및 리소스 업데이트
            LoadPreviewImage();
            UpdateResourceDisplay();

            // 3. 직원 및 슬라이더 업데이트
            UpdateEmployeeAssignment();
            UpdateProductionStatus();
        }
        finally
        {
            // UI 업데이트가 끝나면 반드시 플래그 해제
            _isUpdatingUI = false;
        }
    }

    private void UpdateResourceDisplay()
    {
        if (_currentThreadState == null || _dataManager == null || _mainUiManager == null) return;

        if (_provideContentTransform != null) GameObjectUtils.ClearChildren(_provideContentTransform);
        if (_consumeContentTransform != null) GameObjectUtils.ClearChildren(_consumeContentTransform);

        if (_currentThreadState.TryGetAggregatedResourceCounts(out var consumption, out var production))
        {
            SpawnResourceIcons(consumption, _provideContentTransform);
            SpawnResourceIcons(production, _consumeContentTransform);
        }
    }

    private void SpawnResourceIcons(Dictionary<string, int> resources, Transform parent)
    {
        if (resources == null || parent == null) return;

        foreach (var kvp in resources)
        {
            var entry = _dataManager.Resource.GetResourceEntry(kvp.Key);
            if (entry != null && _mainUiManager.ProductionInfoImage != null)
            {
                Instantiate(_mainUiManager.ProductionInfoImage, parent)
                    .GetComponent<ProductionInfoImage>().OnInitialize(entry, kvp.Value);
            }
        }
    }

    private void UpdateEmployeeAssignment()
    {
        if (_currentThreadState == null || _dataManager?.Employee == null) return;

        int requiredTotal = _currentThreadState.requiredEmployees;

        // 전역 데이터 조회
        int hiredWorkers = _dataManager.Employee.GetEmployeeCount(ID_WORKER);
        int hiredTechs = _dataManager.Employee.GetEmployeeCount(ID_TECHNICIAN);
        int availWorkers = Mathf.Max(0, _dataManager.Employee.GetAvailableEmployeeCount(ID_WORKER));
        int availTechs = Mathf.Max(0, _dataManager.Employee.GetAvailableEmployeeCount(ID_TECHNICIAN));

        // --- 텍스트 업데이트 ---

        // 1. 최대 수용 인원 (Local Max)
        if (_maxWorkersText != null) _maxWorkersText.text = $"Max: {requiredTotal}";
        if (_maxTechniciansText != null) _maxTechniciansText.text = $"Max: {requiredTotal}";

        // 2. 전체 고용 인원 (Global Hired)
        if (_currentWorkersText != null) _currentWorkersText.text = hiredWorkers.ToString("N0");
        if (_currentTechniciansText != null) _currentTechniciansText.text = hiredTechs.ToString("N0");

        // 3. 현재 스레드 배치 인원 (Local Assigned)
        if (_assignedWorkersText != null)
            _assignedWorkersText.text = _currentThreadState.currentWorkers.ToString("N0");

        if (_assignedTechniciansText != null)
            _assignedTechniciansText.text = _currentThreadState.currentTechnicians.ToString("N0");


        // --- 슬라이더 업데이트 ---
        UpdateSliderState(_workerSlider, _currentThreadState.currentWorkers, availWorkers, requiredTotal);
        UpdateSliderState(_technicianSlider, _currentThreadState.currentTechnicians, availTechs, requiredTotal);
    }

    private void UpdateSliderState(Slider slider, int currentAssigned, int availableGlobal, int maxRequired)
    {
        if (slider == null) return;

        int logicMax = Mathf.Min(maxRequired, currentAssigned + availableGlobal);

        // 1. 값 안전 초기화 (OnValueChanged 트리거 방지용 SetValueWithoutNotify)
        slider.SetValueWithoutNotify(Mathf.Clamp(currentAssigned, 0, logicMax));

        // 2. MaxValue 설정
        slider.maxValue = logicMax;

        // 3. 값 재확정
        slider.SetValueWithoutNotify(Mathf.Clamp(currentAssigned, 0, logicMax));
    }

    /// <summary>
    /// [수정됨] 생산 진행도 및 효율 슬라이더와 텍스트를 업데이트합니다.
    /// </summary>
    private void UpdateProductionStatus()
    {
        if (_currentThreadState == null) return;

        float efficiency = _currentThreadState.currentProductionEfficiency;
        float progress = _currentThreadState.currentProductionProgress;

        // --- 슬라이더 업데이트 ---
        if (_productionEfficiencySlider != null)
            _productionEfficiencySlider.value = efficiency;

        if (_productionProgressSlider != null)
            _productionProgressSlider.value = progress;

        // --- 텍스트 업데이트 (추가됨) ---
        if (_productionEfficiencyText != null)
        {
            // 0.0 ~ 1.0 값을 0% ~ 100% 형식으로 변환
            _productionEfficiencyText.text = $"{Mathf.RoundToInt(efficiency * 100)}%";
        }

        if (_productionProgressText != null)
        {
            _productionProgressText.text = $"{Mathf.RoundToInt(progress * 100)}%";
        }
    }

    #endregion

    #region 상호작용 (오직 슬라이더)

    // --- 슬라이더 콜백 ---

    public void OnWorkerSliderChanged()
    {
        // UI 갱신 중(초기화 중)이라면 로직 실행 차단
        if (_isUpdatingUI) return;

        HandleEmployeeSliderChanged(ID_WORKER, _workerSlider,
            ref _currentThreadState.currentWorkers, _currentThreadState.currentTechnicians);
    }

    public void OnTechnicianSliderChanged()
    {
        // UI 갱신 중(초기화 중)이라면 로직 실행 차단
        if (_isUpdatingUI) return;

        HandleEmployeeSliderChanged(ID_TECHNICIAN, _technicianSlider,
            ref _currentThreadState.currentTechnicians, _currentThreadState.currentWorkers);
    }

    private void HandleEmployeeSliderChanged(string empId, Slider slider, ref int currentCount, int otherCount)
    {
        if (_currentThreadState == null || _dataManager == null || slider == null) return;

        int desiredCount = Mathf.RoundToInt(slider.value);
        int delta = desiredCount - currentCount;

        if (delta == 0) return;

        bool success = false;

        if (delta > 0) // 추가
        {
            int remainingSlots = Mathf.Max(0, _currentThreadState.requiredEmployees - (currentCount + otherCount));
            int availableGlobal = _dataManager.Employee.GetAvailableEmployeeCount(empId);

            int addCount = Mathf.Min(delta, availableGlobal, remainingSlots);

            if (addCount > 0 && _dataManager.Employee.TryAssignEmployee(empId, addCount))
            {
                currentCount += addCount;
                success = true;
            }
        }
        else // 제거
        {
            int removeCount = Mathf.Min(-delta, currentCount);

            if (removeCount > 0 && _dataManager.Employee.TryUnassignEmployee(empId, removeCount))
            {
                currentCount -= removeCount;
                success = true;
            }
        }

        if (success)
        {
            SyncAndRefreshUI();
        }
        else
        {
            // 실패 시 UI 갱신 플래그를 켜고 값을 되돌림
            _isUpdatingUI = true;
            slider.SetValueWithoutNotify(currentCount);
            _isUpdatingUI = false;
        }
    }

    #endregion

    #region 유틸리티

    private void SyncAndRefreshUI()
    {
        if (_dataManager != null)
        {
            _dataManager.Employee.SyncAssignedCountsFromThreads(_dataManager.ThreadPlacement);
        }

        UpdateUI(); // 전체 UI 갱신
    }

    private string GetCategoryName(string categoryId)
    {
        if (string.IsNullOrEmpty(categoryId) || _dataManager == null) return "None";
        var category = _dataManager.Thread.GetCategory(categoryId);
        return category != null ? category.categoryName : "None";
    }

    private void LoadPreviewImage()
    {
        if (_image == null) return;

        if (string.IsNullOrEmpty(_currentThreadState.previewImagePath))
        {
            _image.enabled = false;
            return;
        }

        Sprite loadedSprite = SpriteUtils.LoadSpriteFromFile(_currentThreadState.previewImagePath);
        if (loadedSprite != null)
        {
            _image.sprite = loadedSprite;
            _image.enabled = true;
        }
        else
        {
            _image.enabled = false;
        }
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    #endregion
}