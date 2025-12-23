using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Evo;

/// <summary>
/// 스레드(생산 시설)의 정보를 표시하고 직원을 할당/해제하는 UI 패널을 관리합니다.
/// <para>슬라이더를 통해 직원 수를 제어하며, 생산 효율 및 리소스 소비/생산 현황을 시각화합니다.</para>
/// </summary>
public class ThreadInfoPanel : MonoBehaviour
{
    [Header("Resource Visualization")]
    [SerializeField] private Transform _provideContentTransform;
    [SerializeField] private Transform _consumeContentTransform;

    [Header("Basic Info")]
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private TextMeshProUGUI _maintenanceText;
    [SerializeField] private TextMeshProUGUI _categoryText;
    [SerializeField] private Image _previewImage;

    [Header("Production Stats")]
    [SerializeField] private Slider _productionEfficiencySlider;
    [SerializeField] private TextMeshProUGUI _productionEfficiencyText;
    [SerializeField] private Slider _productionProgressSlider;
    [SerializeField] private TextMeshProUGUI _productionProgressText;

    [Header("Employee Assignment (Global Stats)")]
    [SerializeField] private TextMeshProUGUI _currentWorkersText;
    [SerializeField] private TextMeshProUGUI _currentTechniciansText;

    [Header("Employee Assignment (Local Stats)")]
    [SerializeField] private TextMeshProUGUI _maxWorkersText;
    [SerializeField] private TextMeshProUGUI _maxTechniciansText;
    [SerializeField] private TextMeshProUGUI _assignedWorkersText;
    [SerializeField] private TextMeshProUGUI _assignedTechniciansText;
    [SerializeField] private Slider _workerSlider;
    [SerializeField] private Slider _technicianSlider;

    private ThreadState _currentThreadState;
    private DataManager _dataManager;
    private MainUiManager _mainUiManager;

    private bool _isSubscribed = false;

    /// <summary>
    /// 패널을 초기화하고 데이터를 연결합니다.
    /// </summary>
    public void OnInitialize(ThreadState threadState, MainUiManager mainUiManager, DataManager dataManager)
    {
        _currentThreadState = threadState;
        _mainUiManager = mainUiManager;
        _dataManager = dataManager;

        SubscribeToDayChanged();
        RefreshAllUI();

        gameObject.SetActive(true);
    }

    private void OnEnable() => SubscribeToDayChanged();
    private void OnDisable() => UnsubscribeFromDayChanged();
    private void OnDestroy() => UnsubscribeFromDayChanged();

    private void SubscribeToDayChanged()
    {
        if (_isSubscribed || _dataManager?.Time == null) return;

        _dataManager.Time.OnDayChanged += OnDayChanged;
        _isSubscribed = true;
    }

    private void UnsubscribeFromDayChanged()
    {
        if (!_isSubscribed || _dataManager?.Time == null) return;

        _dataManager.Time.OnDayChanged -= OnDayChanged;
        _isSubscribed = false;
    }

    private void OnDayChanged()
    {
        if (gameObject.activeSelf) RefreshAllUI();
    }

    /// <summary>
    /// 모든 UI 요소를 현재 데이터 기반으로 갱신합니다.
    /// <para>이벤트 루프를 방지하기 위해 <see cref="_isUpdatingUI"/> 플래그를 사용합니다.</para>
    /// </summary>
    private void RefreshAllUI()
    {
        if (_currentThreadState == null) return;

        UpdateBasicInfo();
        UpdateResourceIcons();
        UpdateEmployeeStatus();
        UpdateProductionStatus();
    }

    private void UpdateBasicInfo()
    {
        if (_nameText != null)
            _nameText.text = _currentThreadState.threadName;

        if (_categoryText != null)
            _categoryText.text = GetCategoryName(_currentThreadState.categoryId);

        if (_maintenanceText != null)
            _maintenanceText.text = $"Maintenance: {_currentThreadState.totalMaintenanceCost:N0}/month";

        LoadPreviewImage();
    }

    private void UpdateResourceIcons()
    {
        if (_dataManager == null || _mainUiManager == null) return;

        // 기존 아이콘 제거
        if (_provideContentTransform != null) GameObjectUtils.ClearChildren(_provideContentTransform);
        if (_consumeContentTransform != null) GameObjectUtils.ClearChildren(_consumeContentTransform);

        // 소비 및 생산 리소스 가져오기
        if (_currentThreadState.TryGetAggregatedResourceCounts(out var consumption, out var production))
        {
            SpawnIcons(consumption, _provideContentTransform);
            SpawnIcons(production, _consumeContentTransform);
        }
    }

    private void SpawnIcons(Dictionary<string, int> resources, Transform parent)
    {
        if (resources == null || parent == null || _mainUiManager.ProductionInfoImage == null) return;

        foreach (var kvp in resources)
        {
            var entry = _dataManager.Resource.GetResourceEntry(kvp.Key);
            if (entry != null)
            {
                var iconObj = Instantiate(_mainUiManager.ProductionInfoImage, parent);
                iconObj.GetComponent<ProductionInfoImage>().OnInitialize(entry, kvp.Value);
            }
        }
    }

    private void UpdateEmployeeStatus()
    {
        if (_dataManager?.Employee == null) return;

        int requiredTotal = _currentThreadState.requiredEmployees;
        int currentWorkers = _currentThreadState.currentWorkers;
        int currentTechs = _currentThreadState.currentTechnicians;

        // 전역 데이터 조회
        int hiredWorkers = _dataManager.Employee.GetEmployeeEntry(EmployeeType.Worker).state.count;
        int hiredTechs = _dataManager.Employee.GetEmployeeEntry(EmployeeType.Technician).state.count;
        int availWorkers = Mathf.Max(0, _dataManager.Employee.GetAvailableEmployeeCount(EmployeeType.Worker));
        int availTechs = Mathf.Max(0, _dataManager.Employee.GetAvailableEmployeeCount(EmployeeType.Technician));

        // 1. 텍스트 업데이트
        UpdateEmployeeTexts(requiredTotal, currentWorkers, currentTechs, hiredWorkers, hiredTechs);

        // 2. 슬라이더 업데이트 (Logic Max 계산 포함)
        UpdateSliderState(_workerSlider, currentWorkers, availWorkers, requiredTotal);
        UpdateSliderState(_technicianSlider, currentTechs, availTechs, requiredTotal);
    }

    private void UpdateEmployeeTexts(int max, int currW, int currT, int hiredW, int hiredT)
    {
        if (_maxWorkersText) _maxWorkersText.text = $"Max: {max}";
        if (_maxTechniciansText) _maxTechniciansText.text = $"Max: {max}";

        if (_currentWorkersText) _currentWorkersText.text = hiredW.ToString("N0");
        if (_currentTechniciansText) _currentTechniciansText.text = hiredT.ToString("N0");

        if (_assignedWorkersText) _assignedWorkersText.text = currW.ToString("N0");
        if (_assignedTechniciansText) _assignedTechniciansText.text = currT.ToString("N0");
    }

    /// <summary>
    /// 슬라이더의 최대값과 현재값을 안전하게 설정합니다.
    /// <para>슬라이더의 Max값은 (현재 할당된 인원 + 가용 인원)과 (시설 최대 인원) 중 작은 값입니다.</para>
    /// </summary>
    private void UpdateSliderState(Slider slider, int currentAssigned, int availableGlobal, int maxRequired)
    {
        if (slider == null) return;

        int logicMax = Mathf.Min(maxRequired, currentAssigned + availableGlobal);
        slider.SetValueWithoutNotify(Mathf.Clamp(currentAssigned, 0, logicMax));
        slider.maxValue = logicMax;
        slider.SetValueWithoutNotify(Mathf.Clamp(currentAssigned, 0, logicMax));
    }

    private void UpdateProductionStatus()
    {
        float efficiency = _currentThreadState.currentProductionEfficiency;
        float progress = _currentThreadState.currentProductionProgress;

        if (_productionEfficiencySlider) _productionEfficiencySlider.value = efficiency;
        if (_productionProgressSlider) _productionProgressSlider.value = progress;

        if (_productionEfficiencyText) _productionEfficiencyText.text = $"{Mathf.RoundToInt(efficiency * 100)}%";
        if (_productionProgressText) _productionProgressText.text = $"{Mathf.RoundToInt(progress * 100)}%";
    }

    private void LoadPreviewImage()
    {
        if (_previewImage == null) return;

        bool hasImage = !string.IsNullOrEmpty(_currentThreadState.previewImagePath);
        if (hasImage)
        {
            Sprite sprite = SpriteUtils.LoadSpriteFromFile(_currentThreadState.previewImagePath);
            if (sprite != null)
            {
                _previewImage.sprite = sprite;
                _previewImage.enabled = true;
                return;
            }
        }

        // 이미지 경로가 없거나 로드 실패 시 숨김
        _previewImage.enabled = false;
    }

    /// <summary>
    /// Worker 슬라이더 값 변경 시 호출됩니다.
    /// </summary>
    public void OnWorkerSliderChanged()
    {
        ProcessEmployeeAssignment(EmployeeType.Worker, _workerSlider,
                    ref _currentThreadState.currentWorkers, _currentThreadState.currentTechnicians);
    }

    /// <summary>
    /// Technician 슬라이더 값 변경 시 호출됩니다.
    /// </summary>
    public void OnTechnicianSliderChanged()
    {
        ProcessEmployeeAssignment(EmployeeType.Technician, _technicianSlider,
                    ref _currentThreadState.currentTechnicians, _currentThreadState.currentWorkers);
    }

    /// <summary>
    /// 실제 직원 할당/해제 로직을 처리합니다.
    /// </summary>
    /// <param name="type">직원 타입</param>
    /// <param name="slider">조작된 슬라이더</param>
    /// <param name="currentCount">변경할 현재 할당 수 (ref)</param>
    /// <param name="otherCount">다른 타입 직원의 현재 할당 수 (최대 인원 체크용)</param>
    private void ProcessEmployeeAssignment(EmployeeType type, Slider slider, ref int currentCount, int otherCount)
    {
        if (_dataManager == null || slider == null) return;

        int targetCount = Mathf.RoundToInt(slider.value);
        int delta = targetCount - currentCount;

        if (delta == 0) return;

        bool isSuccess = false;

        if (delta > 0) // 고용 (Assign)
        {
            int remainingSlots = Mathf.Max(0, _currentThreadState.requiredEmployees - (currentCount + otherCount));
            int availableGlobal = _dataManager.Employee.GetAvailableEmployeeCount(type);
            int addAmount = Mathf.Min(delta, availableGlobal, remainingSlots);

            if (addAmount > 0 && _dataManager.Employee.TryAssignEmployee(type, addAmount))
            {
                currentCount += addAmount;
                isSuccess = true;
            }
        }
        else // 해고 (Unassign)
        {
            int removeAmount = Mathf.Min(-delta, currentCount);

            if (removeAmount > 0 && _dataManager.Employee.TryUnassignEmployee(type, removeAmount))
            {
                currentCount -= removeAmount;
                isSuccess = true;
            }
        }

        if (isSuccess)
        {
            SyncAndRefresh();
        }
        else
        {
            slider.SetValueWithoutNotify(currentCount);
        }
    }

    private void SyncAndRefresh()
    {
        // 데이터 매니저와 동기화 후 UI 갱신
        _dataManager?.Employee.SyncAssignedCountsFromThreads(_dataManager.ThreadPlacement);
        RefreshAllUI();
    }

    private string GetCategoryName(string categoryId)
    {
        if (string.IsNullOrEmpty(categoryId) || _dataManager == null) return "None";
        var category = _dataManager.Thread.GetCategory(categoryId);
        return category != null ? category.categoryName : "None";
    }
}