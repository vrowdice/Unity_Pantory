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
    [SerializeField] private GameObject _buildingInfoPopupResourceBtnPrefab;
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
        if (_buildingObject == null || _dataManager == null) return;

        UpdateEmployeeStatusFromBuilding();
        UpdateProductionStatusFromBuilding();
    }

    private void UpdateEmployeeStatusFromBuilding()
    {
        BuildingObject b = _buildingObject;
        int requiredTotal = b.RequiredEmployeeSlots;
        bool professionalOnly = _currentData.isProfessional;
        int currentWorkers = b.AssignedWorkers;
        int currentTechs = b.AssignedTechnicians;

        int availWorkers = Mathf.Max(0, _dataManager.Employee.GetAvailableEmployeeCount(EmployeeType.Worker));
        int availTechs = Mathf.Max(0, _dataManager.Employee.GetAvailableEmployeeCount(EmployeeType.Technician));

        EmployeeEntry ew = _dataManager.Employee.GetEmployeeEntry(EmployeeType.Worker);
        _currentWorkersText.text = ew != null ? $"{ew.state.count:N0}" : "0";

        EmployeeEntry et = _dataManager.Employee.GetEmployeeEntry(EmployeeType.Technician);
        _currentTechniciansText.text = et != null ? $"{et.state.count:N0}" : "0";

        _maxEmployeeText.text = $"{requiredTotal:N0}";
        _requiredTechnicianText.text = professionalOnly ? $"{requiredTotal:N0}" : "—";
        _maxWorkersText.text = professionalOnly ? "—" : $"MAX {requiredTotal}";
        _maxTechniciansText.text = $"MAX {requiredTotal}";

        _assignedWorkersText.text = currentWorkers.ToString("N0");
        _assignedTechniciansText.text = currentTechs.ToString("N0");

        _workerSliderContainer.SetActive(requiredTotal > 0 && !professionalOnly);
        _technicianSliderContainer.SetActive(requiredTotal > 0);

        int maxWorkerSlots = b.MaxWorkerSlots;
        int maxTechSlots = b.MaxTechnicianSlots;
        UpdateSliderState(_workerSlider, currentWorkers, availWorkers, maxWorkerSlots);
        UpdateSliderState(_technicianSlider, currentTechs, availTechs, maxTechSlots);
    }

    private static void UpdateSliderState(Slider slider, int currentAssigned, int availableGlobal, int maxRequired)
    {
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

        _productionEfficiencySlider.value = efficiency;
        _productionProgressSlider.value = progress;

        _productionEfficiencyText.text = $"{Mathf.RoundToInt(efficiency * 100)}%";
        _productionProgressText.text = $"{Mathf.RoundToInt(progress * 100)}% (+{deltaTick * 100f:0.#}%/h)";
    }

    public void OnWorkerSliderChanged()
    {
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
                _buildingObject.MaxTechnicianSlots);
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
            _changeProductionBtn.gameObject.SetActive(false);
        else
            _changeProductionBtn.gameObject.SetActive(true);

        RefreshRecipeGrids();

        RepopulateRuntimeResourceButton(_inputResourceContent, _buildingObject.GetRuntimeInputResourceCounts(), true);
        RepopulateRuntimeResourceButton(_outputResourceContent, _buildingObject.GetRuntimeOutputResourceCounts(), false);
    }

    public void RefreshAfterRuntimeBufferChanged()
    {
        RefreshResourceGrids();
        RefreshAllRuntimePanels();
    }

    private void RepopulateRuntimeResourceButton(Transform container, Dictionary<string, int> counts, bool fromInputBuffer)
    {
        PoolingManager pool = GameManager.Instance.PoolingManager;
        pool.ClearChildrenToPool(container);

        foreach (KeyValuePair<string, int> kvp in counts)
        {
            ResourceEntry entry = _dataManager.Resource.GetResourceEntry(kvp.Key);
            GameObject btnObj = pool.GetPooledObject(_buildingInfoPopupResourceBtnPrefab);
            btnObj.transform.SetParent(container, false);
            btnObj.GetComponent<BuildingInfoPopupResourceBtn>()
                .Init(_buildingObject, kvp.Key, fromInputBuffer, entry, kvp.Value, this);
        }
    }

    private void RefreshRecipeGrids()
    {
        _buildingObject.GetRecipeDisplayData(_recipeInputIds, _recipeOutputIds, out _recipeCurrentResourceId);

        if (_currentData.IsUnloadStation)
        {
            UIManager.Instance.RepopulateProductionInfoImages(_outputGridTransform, GameObjectUtils.AggregateResourceCounts(_recipeOutputIds));
            UIManager.Instance.RepopulateProductionInfoImages(_inputGridTransform, null);
        }
        else
        {
            UIManager.Instance.RepopulateProductionInfoImages(_inputGridTransform, GameObjectUtils.AggregateResourceCounts(_recipeInputIds));
            UIManager.Instance.RepopulateProductionInfoImages(_outputGridTransform, GameObjectUtils.AggregateResourceCounts(_recipeOutputIds));
        }
    }

    /// <summary>
    /// 생산 변경: 건물 입·출력 큐를 플레이어 자원으로 돌린 뒤 생산품 선택 팝업을 띄웁니다.
    /// </summary>
    public void OnChangeProductionButtonClicked()
    {
        if (_buildingObject == null || _dataManager == null) return;
        if (!_currentData.IsProductionBuilding && !_currentData.IsUnloadStation) return;

        _buildingObject.ReturnAllRuntimeBuffersToDataManager(_dataManager);
        RefreshResourceGrids();
        RefreshAllRuntimePanels();
        ShowOutputResourceSelection();
    }

    public void ShowOutputResourceSelection()
    {
        if (_currentData.AllowedResourceTypes == null || _currentData.AllowedResourceTypes.Count == 0) return;

        List<ResourceData> producible = GetProducibleList();
        UIManager.Instance.ShowSelectResourcePopup(
            _currentData.AllowedResourceTypes,
            OnOutputResourceSelected,
            producible
        );
    }

    private List<ResourceData> GetProducibleList()
    {
        if (_currentData is ProductionBuildingData p) return p.ProducibleResources;
        return null;
    }

    private void OnOutputResourceSelected(ResourceEntry selected)
    {
        if (!_buildingObject.TrySetSelectedResource(selected.data))
            return;

        UpdateUI();
        RefreshWorldIcons();
        Close();
    }

    private void RefreshWorldIcons()
    {
        _buildingObject.RefreshOutgoingResourceIcons();
    }
}