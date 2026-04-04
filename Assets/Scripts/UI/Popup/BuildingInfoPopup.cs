using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BuildingInfoPopup : PopupBase
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private Image _buildingImage;
    [SerializeField] private Toggle _IsNeededExpertToggle;
    [SerializeField] private TextMeshProUGUI _descriptionText;
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
    private BuildingState _currentState;
    private DataManager _dataManager;
    private BuildingObject _buildingObject;

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
        _currentState = buildingObject.CreateStateSnapshot();
        _dataManager = DataManager.Instance;

        if (_dataManager != null && _dataManager.Time != null)
        {
            _dataManager.Time.OnDayChanged -= OnDayChangedRefresh;
            _dataManager.Time.OnDayChanged += OnDayChangedRefresh;
        }

        UpdateUI();
        Show();
    }

    public void ShowBuildingInfo(BuildingData data, BuildingState state)
    {
        if (data == null) return;

        base.Init();

        UnsubscribeTimeEvents();
        _buildingObject = null;
        _currentData = data;
        _currentState = state;
        _dataManager = DataManager.Instance;

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
            _dataManager.Time.OnDayChanged -= OnDayChangedRefresh;
        }
    }

    private void OnDayChangedRefresh()
    {
        if (!gameObject.activeSelf || _buildingObject == null)
        {
            return;
        }

        _currentState = _buildingObject.CreateStateSnapshot();
        RefreshAllRuntimePanels();
    }

    private void UpdateUI()
    {
        _nameText.text = _currentData.id.Localize(LocalizationUtils.TABLE_BUILDING);
        _descriptionText.text = (_currentData.id + LocalizationUtils.KEY_SUFFIX_DESC).Localize(LocalizationUtils.TABLE_BUILDING);
        _buildCostText.text = $"{ReplaceUtils.FormatNumberWithCommas(_currentData.buildCost)}";
        _maintenanceText.text = $"{ReplaceUtils.FormatNumberWithCommas(_currentData.maintenanceCost)} / day";
        _IsNeededExpertToggle.isOn = _currentData.isProfessional;

        if (_buildingImage != null)
        {
            _buildingImage.sprite = _currentData.buildingSprite;
            _buildingImage.enabled = _currentData.buildingSprite != null;
        }

        UpdateProductionContext();
        RefreshResourceGrids();
        UpdateFinancialInfo();
        RefreshAllRuntimePanels();
    }

    /// <summary>직원 슬라이더·생산 진행/효율 (메인 건물 오브젝트 연동 시).</summary>
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

        if (_productionEfficiencySlider != null) _productionEfficiencySlider.value = efficiency;
        if (_productionProgressSlider != null) _productionProgressSlider.value = progress;

        if (_productionEfficiencyText != null) _productionEfficiencyText.text = $"{Mathf.RoundToInt(efficiency * 100)}%";
        if (_productionProgressText != null) _productionProgressText.text = $"{Mathf.RoundToInt(progress * 100)}%";
    }

    /// <summary>Worker 슬라이더 (인스펙터 OnValueChanged → ThreadInfoPopup과 동일)</summary>
    public void OnWorkerSliderChanged()
    {
        if (_buildingObject == null || _workerSlider == null || _dataManager == null) return;

        int targetCount = Mathf.RoundToInt(_workerSlider.value);
        int delta = targetCount - _buildingObject.AssignedWorkers;
        if (delta == 0) return;

        if (_buildingObject.TryApplyEmployeeDelta(EmployeeType.Worker, delta))
        {
            _currentState = _buildingObject.CreateStateSnapshot();
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

    /// <summary>Technician 슬라이더</summary>
    public void OnTechnicianSliderChanged()
    {
        if (_buildingObject == null || _technicianSlider == null || _dataManager == null) return;

        int targetCount = Mathf.RoundToInt(_technicianSlider.value);
        int delta = targetCount - _buildingObject.AssignedTechnicians;
        if (delta == 0) return;

        if (_buildingObject.TryApplyEmployeeDelta(EmployeeType.Technician, delta))
        {
            _currentState = _buildingObject.CreateStateSnapshot();
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

    private void UpdateFinancialInfo()
    {
        if (_currentState == null)
        {
            return;
        }

        long inputCost = 0;
        long outputPrice = 0;

        if (_currentState.inputProductionIds != null)
        {
            foreach (string id in _currentState.inputProductionIds)
            {
                ResourceEntry entry = _dataManager.Resource.GetResourceEntry(id);
                if (entry != null) inputCost += (long)entry.state.currentValue;
            }
        }

        if (_currentState.outputProductionIds != null)
        {
            foreach (string id in _currentState.outputProductionIds)
            {
                ResourceEntry entry = _dataManager.Resource.GetResourceEntry(id);
                if (entry != null) outputPrice += (long)entry.state.currentValue;
            }
        }

        long profit = outputPrice - inputCost;

        //if (_materialPurchaseText) _materialPurchaseText.text = $"{ReplaceUtils.FormatNumberWithCommas(inputCost)}";
        //if (_productPriceText) _productPriceText.text = $"{ReplaceUtils.FormatNumberWithCommas(outputPrice)}";
        //if (_expectedCostText)
        //{
        //    _expectedCostText.text = $"{ReplaceUtils.FormatNumberWithCommas(profit)}";
        //    _expectedCostText.color = VisualManager.Instance.GetDeltaColor(profit);
        //}
    }

    private void UpdateProductionContext()
    {
        bool isProd = _currentData.IsProductionBuilding;
        bool isUnloadStation = _currentData.IsUnloadStation;
        
        // 생산 건물이거나 하역소인 경우 자원 선택 버튼 활성화
        _changeProductionBtn.gameObject.SetActive(true);
        _changeProductionBtn.interactable = isProd || isUnloadStation;
    }

    private void RefreshResourceGrids()
    {
        if (_currentState == null)
        {
            return;
        }

        UpdateResourceGrid(_currentState.inputProductionIds, _inputGridTransform, "Input");

        if (_currentData.IsUnloadStation)
        {
            UpdateResourceGrid(_currentState.outputProductionIds, _outputGridTransform, "Output");
        }
        else if (!_currentData.IsProductionBuilding)
        {
            UpdateNonProductionOutput();
        }
        else
        {
            UpdateResourceGrid(_currentState.outputProductionIds, _outputGridTransform, "Output");
        }
    }

    private GameObject GetProductionInfoImagePrefab()
    {
        return UIManager.Instance != null ? UIManager.Instance.ProductionInfoImagePrefab : null;
    }

    private void UpdateResourceGrid(List<string> resourceIds, Transform container, string label)
    {
        GameObjectUtils.ClearChildren(container);
        GameObject prefab = GetProductionInfoImagePrefab();
        if (prefab == null) return;

        Dictionary<string, int> counts = GameObjectUtils.AggregateResourceCounts(resourceIds);

        foreach (KeyValuePair<string, int> kvp in counts)
        {
            ResourceEntry entry = _dataManager.Resource.GetResourceEntry(kvp.Key);
            if (entry == null) continue;

            Instantiate(prefab, container)
                .GetComponent<ProductionInfoImage>().Init(entry, kvp.Value);

            string reqs = (label == "Output") ? BuildRequirementText(entry.data) : "";
            string info = $"{label}: {entry.data.displayName}\nAmount: {kvp.Value}\nPrice: {ReplaceUtils.FormatNumber(entry.state.currentValue)}{reqs}";
        }
    }

    private void UpdateNonProductionOutput()
    {
        GameObjectUtils.ClearChildren(_outputGridTransform);

        string handlingId = _currentState.currentResourceId;
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
        // 하역소인 경우 모든 자원 타입 허용
        if (_currentData.IsUnloadStation)
        {
            List<ResourceType> allResourceTypes = new List<ResourceType>();
            foreach (ResourceType type in System.Enum.GetValues(typeof(ResourceType)))
            {
                allResourceTypes.Add(type);
            }
            
            UIManager.Instance.ShowSelectResourcePopup(
                allResourceTypes,
                OnUnloadStationResourceSelected,
                null
            );
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

        if (_buildingObject != null)
        {
            _buildingObject.TrySetSelectedResource(selected.data);
            _currentState = _buildingObject.CreateStateSnapshot();
        }
        else if (_currentState != null)
        {
            _currentState.outputProductionIds.Clear();
            _currentState.outputProductionIds.Add(selected.data.id);
        }
        else
        {
            return;
        }

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

        if (_buildingObject != null)
        {
            if (!_buildingObject.TrySetSelectedResource(selected.data))
            {
                return;
            }

            _currentState = _buildingObject.CreateStateSnapshot();
        }
        else if (_currentState != null)
        {
            _currentState.outputProductionIds.Clear();
            _currentState.outputProductionIds.Add(selected.data.id);
            SyncInputToRequirements(selected.data);
        }
        else
        {
            return;
        }

        UpdateUI();
        RefreshWorldIcons();

        Close();
    }

    private void SyncInputToRequirements(ResourceData outputData)
    {
        _currentState.inputProductionIds.Clear();
        if (outputData.requirements == null) return;

        foreach (ResourceRequirement req in outputData.requirements)
        {
            if (req.resource == null) continue;
            int count = Mathf.Max(1, req.count);
            for (int i = 0; i < count; i++)
                _currentState.inputProductionIds.Add(req.resource.id);
        }
    }

    private void RefreshWorldIcons()
    {
        // DesignRunner/Design 씬 제거: 월드 아이콘 새로고침은 추후 메인 건물 시스템 기준으로 재구현
    }

    private string BuildRequirementText(ResourceData data)
    {
        if (data.requirements == null || data.requirements.Count == 0) return "";

        StringBuilder sb = new StringBuilder();
        sb.Append("\nRequires: ");
        for (int i = 0; i < data.requirements.Count; i++)
        {
            ResourceRequirement req = data.requirements[i];
            if (req.resource == null) continue;
            sb.Append($"{req.resource.displayName} x{Mathf.Max(1, req.count)}");
            if (i < data.requirements.Count - 1) sb.Append(", ");
        }
        return sb.ToString();
    }
}