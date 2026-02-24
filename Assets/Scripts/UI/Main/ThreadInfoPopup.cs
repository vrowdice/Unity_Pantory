using Evo;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 스레드(생산 시설)의 정보를 표시하고 직원을 할당/해제하는 UI 패널을 관리합니다.
/// <para>슬라이더를 통해 직원 수를 제어하며, 생산 효율 및 리소스 소비/생산 현황을 시각화합니다.</para>
/// </summary>
public class ThreadInfoPopup : BasePopup
{
    [Header("Resource Visualization")]
    [SerializeField] private Transform _provideContentTransform;
    [SerializeField] private Transform _consumeContentTransform;

    [Header("Basic Info")]
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private TextMeshProUGUI _buildCostText;
    [SerializeField] private TextMeshProUGUI _maintenanceText;
    [SerializeField] private TextMeshProUGUI _categoryText;
    [SerializeField] private Image _previewImage;

    [Header("Production Stats")]
    [SerializeField] private Slider _productionEfficiencySlider;
    [SerializeField] private TextMeshProUGUI _productionEfficiencyText;
    [SerializeField] private Slider _productionProgressSlider;
    [SerializeField] private TextMeshProUGUI _productionProgressText;

    [Header("Employee Assignment (Global Stats)")]
    [SerializeField] private TextMeshProUGUI _maxEmployeeText;
    [SerializeField] private TextMeshProUGUI _requiredTechnicianText;
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
    private MainCanvas _mainUiManager;

    /// <summary>
    /// 패널을 초기화하고 데이터를 연결합니다.
    /// </summary>
    public void Init(ThreadState threadState, MainCanvas mainUiManager)
    {
        base.Init();
        
        _currentThreadState = threadState;
        _mainUiManager = mainUiManager;
        _dataManager = DataManager.Instance;

        _dataManager.Time.OnDayChanged -= OnDayChanged;
        _dataManager.Time.OnDayChanged += OnDayChanged;

        RefreshAllUI();

        Show();
    }

    private void OnDisable()
    {
        if (_dataManager != null)
        {
            _dataManager.Time.OnDayChanged -= OnDayChanged;
        }
    }

    private void OnDayChanged()
    {
        if (gameObject.activeSelf) RefreshAllUI();
    }

    /// <summary>
    /// 모든 UI 요소를 현재 데이터 기반으로 갱신합니다.
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
        _nameText.text = _currentThreadState.threadName;
        _categoryText.text = GetCategoryName(_currentThreadState.categoryId);
        _buildCostText.text = $"{_currentThreadState.requiredBuildCost:N0}";
        _maintenanceText.text = $"{_currentThreadState.totalMaintenanceCost:N0}/day";

        LoadPreviewImage();
    }

    private void UpdateResourceIcons()
    {
        if (PoolingManager.Instance != null)
        {
            PoolingManager.Instance.ClearChildrenToPool(_provideContentTransform);
            PoolingManager.Instance.ClearChildrenToPool(_consumeContentTransform);
        }
        else
        {
            GameObjectUtils.ClearChildren(_provideContentTransform);
            GameObjectUtils.ClearChildren(_consumeContentTransform);
        }

        if (_currentThreadState.TryGetAggregatedResourceCounts(out var consumption, out var production))
        {
            SpawnIcons(consumption, _provideContentTransform);
            SpawnIcons(production, _consumeContentTransform);
        }
    }

    private void SpawnIcons(Dictionary<string, int> resources, Transform parent)
    {
        GameManager gameManager = GameManager.Instance;
        if (gameManager == null) return;

        foreach (KeyValuePair<string, int> kvp in resources)
        {
            ResourceEntry entry = _dataManager.Resource.GetResourceEntry(kvp.Key);
            if (entry != null)
            {
                gameManager.CreateProductionIcon(parent, entry, kvp.Value);
            }
        }
    }

    private void UpdateEmployeeStatus()
    {
        int requiredTotal = _currentThreadState.requiredEmployees;
        int currentWorkers = _currentThreadState.currentWorkers;
        int currentTechs = _currentThreadState.currentTechnicians;

        int availWorkers = Mathf.Max(0, _dataManager.Employee.GetAvailableEmployeeCount(EmployeeType.Worker));
        int availTechs = Mathf.Max(0, _dataManager.Employee.GetAvailableEmployeeCount(EmployeeType.Technician));

        _currentWorkersText.text = $"{_dataManager.Employee.GetEmployeeEntry(EmployeeType.Worker).state.count:N0}";
        _currentTechniciansText.text = $"{_dataManager.Employee.GetEmployeeEntry(EmployeeType.Technician).state.count:N0}";

        _maxEmployeeText.text = $"{requiredTotal:N0}";
        _requiredTechnicianText.text = $"{_currentThreadState.requiredTechnicians:N0}";
        _maxWorkersText.text = $"MAX {requiredTotal - _currentThreadState.requiredTechnicians}";
        _maxTechniciansText.text = $"MAX {requiredTotal}";

        _assignedWorkersText.text = _currentThreadState.currentWorkers.ToString("N0");
        _assignedTechniciansText.text = _currentThreadState.currentTechnicians.ToString("N0");

        UpdateSliderState(_workerSlider, currentWorkers, availWorkers, requiredTotal - _currentThreadState.requiredTechnicians);
        UpdateSliderState(_technicianSlider, currentTechs, availTechs, requiredTotal);
    }

    /// <summary>
    /// 슬라이더의 최대값과 현재값을 설정합니다.
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

        if (delta > 0)
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
        else
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