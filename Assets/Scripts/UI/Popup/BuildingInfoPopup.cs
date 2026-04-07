using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BuildingInfoPopup : PopupBase
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private Toggle _IsNeededExpertToggle;
    [SerializeField] private TextMeshProUGUI _buildCostText;
    [SerializeField] private TextMeshProUGUI _maintenanceText;

    [Header("Employee Assignment (Global Stats)")]
    [SerializeField] private TextMeshProUGUI _maxEmployeeText;
    [SerializeField] private TextMeshProUGUI _requiredTechnicianText;

    [Header("Employee Assignment (Local Stats)")]
    [SerializeField] private TextMeshProUGUI _currentWorkersText;
    [SerializeField] private TextMeshProUGUI _maxWorkersText;
    [SerializeField] private TextMeshProUGUI _assignedWorkersText;
    [SerializeField] private GameObject _workerSliderContainer;
    [SerializeField] private Slider _workerSlider;

    [SerializeField] private TextMeshProUGUI _currentTechniciansText;
    [SerializeField] private TextMeshProUGUI _maxTechniciansText;
    [SerializeField] private TextMeshProUGUI _assignedTechniciansText;
    [SerializeField] private GameObject _technicianSliderContainer;
    [SerializeField] private Slider _technicianSlider;

    [Header("Input Output Resource")]
    [SerializeField] private Transform _inputResourceContent;
    [SerializeField] private Transform _outputResourceContent;

    [Header("Change Production Btn")]
    [SerializeField] private Button _changeProductionBtn;
    [SerializeField] private Transform _inputGridTransform;
    [SerializeField] private Transform _outputGridTransform;

    [Header("Production Stats")]
    [SerializeField] private Slider _productionProgressSlider;
    [SerializeField] private TextMeshProUGUI _productionProgressText;
    [SerializeField] private Slider _productionEfficiencySlider;
    [SerializeField] private TextMeshProUGUI _productionEfficiencyText;

    private BuildingData _currentData;
    private DataManager _dataManager;
    private BuildingObject _buildingObject;

    private readonly List<string> _recipeInputIds = new List<string>();
    private readonly List<string> _recipeOutputIds = new List<string>();
    private string _recipeCurrentResourceId;

    public void ShowBuildingInfo(BuildingObject buildingObject)
    {
        if (buildingObject == null || buildingObject.BuildingData == null)
        {
            return;
        }

        base.Init();

        UnsubscribeTimeEvents();
        _buildingObject = buildingObject;
        _currentData = buildingObject.BuildingData;
        _dataManager = DataManager.Instance;

        if (_dataManager != null && _dataManager.Time != null)
        {
            _dataManager.Time.OnDayChanged -= OnTimeRefreshFromBuilding;
            _dataManager.Time.OnDayChanged += OnTimeRefreshFromBuilding;
            _dataManager.Time.OnHourChanged -= OnTimeRefreshFromBuilding;
            _dataManager.Time.OnHourChanged += OnTimeRefreshFromBuilding;
        }

        UpdateUI();
        Show();
    }

    private void OnDisable()
    {
        UnsubscribeTimeEvents();
    }

    private void UnsubscribeTimeEvents()
    {
        if (_dataManager != null && _dataManager.Time != null)
        {
            _dataManager.Time.OnDayChanged -= OnTimeRefreshFromBuilding;
            _dataManager.Time.OnHourChanged -= OnTimeRefreshFromBuilding;
        }
    }

    private void OnTimeRefreshFromBuilding()
    {
        if (!gameObject.activeSelf || _buildingObject == null) return;

        RefreshResourceGrids();
        RefreshAllRuntimePanels();
    }

    private void UpdateUI()
    {
        _nameText.text = _currentData.id.Localize(LocalizationUtils.TABLE_BUILDING);
        _buildCostText.text = $"{ReplaceUtils.FormatNumberWithCommas(_currentData.buildCost)}";
        _maintenanceText.text = $"{ReplaceUtils.FormatNumberWithCommas(_currentData.maintenanceCost)} / day";
        _IsNeededExpertToggle.isOn = _currentData.isProfessional;

        UpdateProductionContext();
        RefreshResourceGrids();
        RefreshAllRuntimePanels();
    }

    private void RefreshAllRuntimePanels()
    {
        if (_buildingObject == null || _dataManager == null)
        {
            if (_workerSliderContainer != null) _workerSliderContainer.SetActive(false);
            if (_technicianSliderContainer != null) _technicianSliderContainer.SetActive(false);
            return;
        }

        UpdateEmployeeStatusFromBuilding();
        UpdateProductionStatusFromBuilding();
    }

    private void UpdateEmployeeStatusFromBuilding()
    {
        BuildingObject b = _buildingObject;
        int requiredTotal = b.RequiredEmployeeSlots;
        int minTech = b.RequiredTechnicianMinimum;
        int currentWorkers = b.AssignedWorkers;
        int currentTechs = b.AssignedTechnicians;

        int availWorkers = Mathf.Max(0, _dataManager.Employee.GetAvailableEmployeeCount(EmployeeType.Worker));
        int availTechs = Mathf.Max(0, _dataManager.Employee.GetAvailableEmployeeCount(EmployeeType.Technician));

        if (_currentWorkersText != null)
        {
            EmployeeEntry ew = _dataManager.Employee.GetEmployeeEntry(EmployeeType.Worker);
            _currentWorkersText.text = ew != null ? $"{ew.state.count:N0}" : "0";
        }

        if (_currentTechniciansText != null)
        {
            EmployeeEntry et = _dataManager.Employee.GetEmployeeEntry(EmployeeType.Technician);
            _currentTechniciansText.text = et != null ? $"{et.state.count:N0}" : "0";
        }

        if (_maxEmployeeText != null) _maxEmployeeText.text = $"{requiredTotal:N0}";
        if (_requiredTechnicianText != null) _requiredTechnicianText.text = $"{minTech:N0}";
        if (_maxWorkersText != null) _maxWorkersText.text = $"MAX {requiredTotal - minTech}";
        if (_maxTechniciansText != null) _maxTechniciansText.text = $"MAX {requiredTotal}";

        if (_assignedWorkersText != null) _assignedWorkersText.text = currentWorkers.ToString("N0");
        if (_assignedTechniciansText != null) _assignedTechniciansText.text = currentTechs.ToString("N0");

        if (_workerSliderContainer != null) _workerSliderContainer.SetActive(requiredTotal > 0);
        if (_technicianSliderContainer != null) _technicianSliderContainer.SetActive(minTech > 0);

        int maxWorkerSlots = b.MaxWorkerSlots;
        UpdateSliderState(_workerSlider, currentWorkers, availWorkers, maxWorkerSlots);
        UpdateSliderState(_technicianSlider, currentTechs, availTechs, requiredTotal);
    }

    private static void UpdateSliderState(Slider slider, int currentAssigned, int availableGlobal, int maxRequired)
    {
        if (slider == null) return;

        int logicMax = Mathf.Min(maxRequired, currentAssigned + availableGlobal);
        int clamped = Mathf.Clamp(currentAssigned, 0, logicMax);
        slider.minValue = 0;
        slider.maxValue = Mathf.Max(0, logicMax);
        slider.SetValueWithoutNotify(clamped);
    }

    private void UpdateProductionStatusFromBuilding()
    {
        BuildingObject b = _buildingObject;
        float efficiency = b.GetAverageAssignedEfficiencyNormalized(_dataManager);
        float progress = b.GetProductionProgressNormalized();
        float deltaTick = b.GetWorkProgressDeltaPerTick(_dataManager);

        if (_productionEfficiencySlider != null) _productionEfficiencySlider.value = efficiency;
        if (_productionProgressSlider != null) _productionProgressSlider.value = progress;

        if (_productionEfficiencyText != null) _productionEfficiencyText.text = $"{Mathf.RoundToInt(efficiency * 100)}%";
        if (_productionProgressText != null)
            _productionProgressText.text = $"{Mathf.RoundToInt(progress * 100)}% (+{deltaTick * 100f:0.#}%/h)";
    }

    public void OnWorkerSliderChanged()
    {
        if (_buildingObject == null || _workerSlider == null || _dataManager == null) return;

        int targetCount = Mathf.RoundToInt(_workerSlider.value);
        int delta = targetCount - _buildingObject.AssignedWorkers;
        if (delta == 0) return;

        if (_buildingObject.TryApplyEmployeeDelta(EmployeeType.Worker, delta))
        {
            UpdateEmployeeStatusFromBuilding();
            UpdateProductionStatusFromBuilding();
        }
        else
        {
            UpdateSliderState(_workerSlider, _buildingObject.AssignedWorkers,
                Mathf.Max(0, _dataManager.Employee.GetAvailableEmployeeCount(EmployeeType.Worker)),
                _buildingObject.MaxWorkerSlots);
        }
    }

    public void OnTechnicianSliderChanged()
    {
        if (_buildingObject == null || _technicianSlider == null || _dataManager == null) return;

        int targetCount = Mathf.RoundToInt(_technicianSlider.value);
        int delta = targetCount - _buildingObject.AssignedTechnicians;
        if (delta == 0) return;

        if (_buildingObject.TryApplyEmployeeDelta(EmployeeType.Technician, delta))
        {
            UpdateEmployeeStatusFromBuilding();
            UpdateProductionStatusFromBuilding();
        }
        else
        {
            UpdateSliderState(_technicianSlider, _buildingObject.AssignedTechnicians,
                Mathf.Max(0, _dataManager.Employee.GetAvailableEmployeeCount(EmployeeType.Technician)),
                _buildingObject.RequiredEmployeeSlots);
        }
    }

    private void UpdateProductionContext()
    {
        bool isProd = _currentData.IsProductionBuilding;
        bool isUnloadStation = _currentData.IsUnloadStation;
        _changeProductionBtn.gameObject.SetActive(true);
        _changeProductionBtn.interactable = isProd || isUnloadStation;
    }

    /// <summary>
    /// _inputGridTransform / _outputGridTransform: 선택 생산·하역의 레시피(필요 입력 / 산출 품목).
    /// _inputResourceContent / _outputResourceContent: 메인 건물의 실제 입력·출력 큐.
    /// </summary>
    private void RefreshResourceGrids()
    {
        if (_buildingObject == null) return;
        if (_currentData.IsLoadStation)
        {
            _changeProductionBtn.gameObject.SetActive(false);
        }
        else
        {
            _changeProductionBtn.gameObject.SetActive(true);
        }

            _buildingObject.GetRecipeDisplayData(_recipeInputIds, _recipeOutputIds, out _recipeCurrentResourceId);
        RefreshRecipeGrids();

        if (_currentData.IsLoadStation)
        {
            UpdateResourceGridFromCounts(_buildingObject.GetRuntimeInputResourceCounts(), _inputResourceContent);
            ClearRuntimeOutputContents();
        }
        else if (_currentData.IsProductionBuilding || _currentData.IsUnloadStation)
        {
            UpdateResourceGridFromCounts(_buildingObject.GetRuntimeInputResourceCounts(), _inputResourceContent);
            UpdateResourceGridFromCounts(_buildingObject.GetRuntimeOutputResourceCounts(), _outputResourceContent);
        }
        else
            ClearRuntimeQueueContents();
    }

    private void RefreshRecipeGrids()
    {
        if (!_currentData.IsUnloadStation)
            UpdateResourceGrid(_recipeInputIds, _inputGridTransform);

        if (_currentData.IsUnloadStation)
            UpdateResourceGrid(_recipeOutputIds, _outputGridTransform);
        else if (!_currentData.IsProductionBuilding)
            UpdateNonProductionOutput();
        else
            UpdateResourceGrid(_recipeOutputIds, _outputGridTransform);
    }

    private void ClearRuntimeQueueContents()
    {
        if (_inputResourceContent != null) GameObjectUtils.ClearChildren(_inputResourceContent);
        ClearRuntimeOutputContents();
    }

    private void ClearRuntimeOutputContents()
    {
        if (_outputResourceContent != null) GameObjectUtils.ClearChildren(_outputResourceContent);
    }

    private GameObject GetProductionInfoImagePrefab()
    {
        return UIManager.Instance != null ? UIManager.Instance.ProductionInfoImagePrefab : null;
    }

    private void UpdateResourceGrid(List<string> resourceIds, Transform container)
    {
        Dictionary<string, int> counts = GameObjectUtils.AggregateResourceCounts(resourceIds);
        UpdateResourceGridFromCounts(counts, container);
    }

    private void UpdateResourceGridFromCounts(Dictionary<string, int> counts, Transform container)
    {
        if (container == null || _dataManager == null) return;

        GameObjectUtils.ClearChildren(container);
        GameObject prefab = GetProductionInfoImagePrefab();
        if (prefab == null) return;

        if (counts == null || counts.Count == 0) return;

        foreach (KeyValuePair<string, int> kvp in counts)
        {
            ResourceEntry entry = _dataManager.Resource.GetResourceEntry(kvp.Key);
            if (entry == null) continue;

            Instantiate(prefab, container)
                .GetComponent<ProductionInfoImage>().Init(entry, kvp.Value);
        }
    }

    private void UpdateNonProductionOutput()
    {
        GameObjectUtils.ClearChildren(_outputGridTransform);

        string handlingId = _recipeCurrentResourceId;
        if (string.IsNullOrEmpty(handlingId)) return;

        GameObject prefab = GetProductionInfoImagePrefab();
        if (prefab == null) return;

        ResourceEntry entry = _dataManager.Resource.GetResourceEntry(handlingId);
        if (entry != null)
        {
            Instantiate(prefab, _outputGridTransform)
                .GetComponent<ProductionInfoImage>().Init(entry, 1);
        }
    }

    public void ShowOutputResourceSelection()
    {
        if (_currentData.IsUnloadStation)
        {
            UIManager.Instance.ShowSelectResourcePopup(
                new List<ResourceType>((ResourceType[])Enum.GetValues(typeof(ResourceType))),
                OnUnloadStationResourceSelected,
                null);
            return;
        }

        if (_currentData.AllowedResourceTypes == null || _currentData.AllowedResourceTypes.Count == 0) return;

        List<ResourceData> producible = GetProducibleList();
        UIManager.Instance.ShowSelectResourcePopup(
            _currentData.AllowedResourceTypes,
            OnOutputResourceSelected,
            producible
        );
    }
    
    /// <summary>
    /// 하역소에서 자원이 선택되었을 때 호출됩니다.
    /// </summary>
    private void OnUnloadStationResourceSelected(ResourceEntry selected)
    {
        if (selected == null) return;

        if (_buildingObject == null) return;

        _buildingObject.TrySetSelectedResource(selected.data);

        UpdateUI();
        RefreshWorldIcons();

        Close();
    }

    private List<ResourceData> GetProducibleList()
    {
        if (_currentData is ProductionBuildingData p) return p.ProducibleResources;
        if (_currentData is RawMaterialFactoryData r) return r.ProducibleRawResources;
        return null;
    }

    private void OnOutputResourceSelected(ResourceEntry selected)
    {
        if (selected == null) return;

        if (_buildingObject == null) return;

        if (!_buildingObject.TrySetSelectedResource(selected.data))
            return;

        UpdateUI();
        RefreshWorldIcons();

        Close();
    }

    private void RefreshWorldIcons()
    {
        _buildingObject?.RefreshOutgoingResourceIcons();
    }
}