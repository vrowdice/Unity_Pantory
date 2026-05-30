using System;
using System.Collections;
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

    [Header("Input buffer (runtime)")]
    [SerializeField] private GameObject _buildingInfoPopupResourceBtnPrefab;
    [SerializeField] private Transform _inputResourceContent;

    [Header("Change Production Btn")]
    [SerializeField] private GameObject _changeProductionBtn;
    [SerializeField] private Transform _inputGridTransform;
    [SerializeField] private Transform _outputGridTransform;

    [Header("Production Stats")]
    [SerializeField] private Slider _productionProgressSlider;
    [SerializeField] private TextMeshProUGUI _productionProgressText;
    [SerializeField] private Slider _productionEfficiencySlider;
    [SerializeField] private TextMeshProUGUI _productionEfficiencyText;

    private BuildingData _currentData;
    private BuildingObject _buildingObject;
    private RawBuildingObject _rawBuildingObject;

    /// <summary>
    /// UI Slider의 min/max/value를 코드로 바꿀 때 onValueChanged가 호출되는 경우가 있어,
    /// 그때는 직원 할당 콜백을 무시합니다.
    /// </summary>
    private bool _suppressEmployeeSliderCallbacks;

    private readonly List<string> _recipeInputIds = new List<string>();
    private readonly List<string> _recipeOutputIds = new List<string>();
    private string _recipeCurrentResourceId;
    private Coroutine _resourceGridCoroutine;

    public void ShowBuildingInfo(BuildingObject buildingObject)
    {
        if (buildingObject == null || buildingObject.BuildingData == null)
        {
            return;
        }

        base.Init();

        _buildingObject = buildingObject;
        _rawBuildingObject = null;
        _currentData = buildingObject.BuildingData;
        SubscribeHourEvents();

        UpdateUI();
        Show();
    }

    public void ShowRawBuildingInfo(RawBuildingObject rawBuildingObject)
    {
        if (rawBuildingObject == null || rawBuildingObject.BuildingData == null)
            return;

        base.Init();

        _rawBuildingObject = rawBuildingObject;
        _buildingObject = null;
        _currentData = rawBuildingObject.BuildingData;
        SubscribeHourEvents();

        UpdateUI();
        Show();
    }

    public override void Close()
    {
        StaggeredSpawnUtils.Stop(this, ref _resourceGridCoroutine);
        UnsubscribeHourEvents();
        base.Close();
    }

    protected override void HandleDayChanged()
    {
        OnTimeRefreshFromBuilding();
    }

    protected override void HandleHourChanged()
    {
        OnTimeRefreshFromBuilding();
    }

    private void OnTimeRefreshFromBuilding()
    {
        if (!gameObject.activeSelf || (_buildingObject == null && _rawBuildingObject == null))
            return;

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
        if (_buildingObject == null && _rawBuildingObject == null || _dataManager == null)
            return;

        UpdateEmployeeStatusFromBuilding();
        UpdateProductionStatusFromBuilding();
    }

    private int GetAssignedWorkers()
    {
        if (_buildingObject != null)
            return _buildingObject.AssignedWorkers;
        return _rawBuildingObject != null ? _rawBuildingObject.AssignedWorkers : 0;
    }

    private int GetAssignedTechnicians()
    {
        if (_buildingObject != null)
            return _buildingObject.AssignedTechnicians;
        return _rawBuildingObject != null ? _rawBuildingObject.AssignedTechnicians : 0;
    }

    private int GetRequiredEmployeeSlots()
    {
        if (_buildingObject != null)
            return _buildingObject.RequiredEmployeeSlots;
        return _rawBuildingObject != null ? _rawBuildingObject.RequiredEmployeeSlots : 0;
    }

    private int GetMaxWorkerSlots()
    {
        if (_buildingObject != null)
            return _buildingObject.MaxWorkerSlots;
        return _rawBuildingObject != null ? _rawBuildingObject.MaxWorkerSlots : 0;
    }

    private int GetMaxTechnicianSlots()
    {
        if (_buildingObject != null)
            return _buildingObject.MaxTechnicianSlots;
        return _rawBuildingObject != null ? _rawBuildingObject.MaxTechnicianSlots : 0;
    }

    private bool TryApplyEmployeeDelta(EmployeeType type, int delta)
    {
        if (_buildingObject != null)
            return _buildingObject.TryApplyEmployeeDelta(type, delta);
        if (_rawBuildingObject != null)
            return _rawBuildingObject.TryApplyEmployeeDelta(type, delta);
        return false;
    }

    private float GetAverageAssignedEfficiencyNormalized()
    {
        if (_buildingObject != null)
            return _buildingObject.GetAverageAssignedEfficiencyNormalized(_dataManager);
        if (_rawBuildingObject != null)
            return _rawBuildingObject.GetAverageAssignedEfficiencyNormalized(_dataManager);
        return 0f;
    }

    private float GetProductionProgressNormalized()
    {
        if (_buildingObject != null)
            return _buildingObject.GetProductionProgressNormalized();
        if (_rawBuildingObject != null)
            return _rawBuildingObject.GetProductionProgressNormalized();
        return 0f;
    }

    private float GetWorkProgressDeltaPerTick()
    {
        if (_buildingObject != null)
            return _buildingObject.GetWorkProgressDeltaPerTick(_dataManager);
        if (_rawBuildingObject != null)
            return _rawBuildingObject.GetWorkProgressDeltaPerTick(_dataManager);
        return 0f;
    }

    private void GetRecipeDisplayData(List<string> inputIds, List<string> outputIds, out string currentResourceId)
    {
        if (_buildingObject != null)
        {
            _buildingObject.GetRecipeDisplayData(inputIds, outputIds, out currentResourceId);
            return;
        }

        if (_rawBuildingObject != null)
        {
            _rawBuildingObject.GetRecipeDisplayData(inputIds, outputIds, out currentResourceId);
            return;
        }

        inputIds.Clear();
        outputIds.Clear();
        currentResourceId = null;
    }

    private void FlushPlacedLayoutIfNeeded()
    {
        if (_rawBuildingObject == null)
            return;

        BuildingSceneRunnerBase runner = GameManager.Instance?.CurrentRunner as BuildingSceneRunnerBase;
        runner?.FlushPlacedLayoutToDataManager();
    }

    private void UpdateEmployeeStatusFromBuilding()
    {
        int requiredTotal = GetRequiredEmployeeSlots();
        bool professionalOnly = _currentData.isProfessional;
        int currentWorkers = GetAssignedWorkers();
        int currentTechs = GetAssignedTechnicians();

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

        int maxWorkerSlots = GetMaxWorkerSlots();
        int maxTechSlots = GetMaxTechnicianSlots();
        _suppressEmployeeSliderCallbacks = true;
        try
        {
            UpdateSliderState(_workerSlider, currentWorkers, availWorkers, maxWorkerSlots);
            UpdateSliderState(_technicianSlider, currentTechs, availTechs, maxTechSlots);
        }
        finally
        {
            _suppressEmployeeSliderCallbacks = false;
        }
    }

    private static void UpdateSliderState(Slider slider, int currentAssigned, int availableGlobal, int maxRequired)
    {
        if (slider == null)
        {
            return;
        }

        int logicMax = Mathf.Min(maxRequired, currentAssigned + availableGlobal);
        int clamped = Mathf.Clamp(currentAssigned, 0, logicMax);
        slider.minValue = 0;
        slider.maxValue = Mathf.Max(0, logicMax);
        slider.SetValueWithoutNotify(clamped);
    }

    private void UpdateProductionStatusFromBuilding()
    {
        float efficiency = GetAverageAssignedEfficiencyNormalized();
        float progress = GetProductionProgressNormalized();
        float deltaTick = GetWorkProgressDeltaPerTick();

        _productionEfficiencySlider.value = efficiency;
        _productionProgressSlider.value = progress;

        _productionEfficiencyText.text = $"{Mathf.RoundToInt(efficiency * 100)}%";
        _productionProgressText.text = $"{Mathf.RoundToInt(progress * 100)}% (+{deltaTick * 100f:0.#}%/h)";
    }

    public void OnWorkerSliderChanged()
    {
        if (_suppressEmployeeSliderCallbacks || (_buildingObject == null && _rawBuildingObject == null))
        {
            return;
        }

        int targetCount = Mathf.RoundToInt(_workerSlider.value);
        int delta = targetCount - GetAssignedWorkers();
        if (delta == 0) return;

        if (TryApplyEmployeeDelta(EmployeeType.Worker, delta))
        {
            UpdateEmployeeStatusFromBuilding();
            UpdateProductionStatusFromBuilding();
            FlushPlacedLayoutIfNeeded();
        }
        else
        {
            _suppressEmployeeSliderCallbacks = true;
            try
            {
                UpdateSliderState(_workerSlider, GetAssignedWorkers(),
                    Mathf.Max(0, _dataManager.Employee.GetAvailableEmployeeCount(EmployeeType.Worker)),
                    GetMaxWorkerSlots());
            }
            finally
            {
                _suppressEmployeeSliderCallbacks = false;
            }
        }
    }

    public void OnTechnicianSliderChanged()
    {
        if (_suppressEmployeeSliderCallbacks || (_buildingObject == null && _rawBuildingObject == null))
        {
            return;
        }

        int targetCount = Mathf.RoundToInt(_technicianSlider.value);
        int delta = targetCount - GetAssignedTechnicians();
        if (delta == 0) return;

        if (TryApplyEmployeeDelta(EmployeeType.Technician, delta))
        {
            UpdateEmployeeStatusFromBuilding();
            UpdateProductionStatusFromBuilding();
            FlushPlacedLayoutIfNeeded();
        }
        else
        {
            _suppressEmployeeSliderCallbacks = true;
            try
            {
                UpdateSliderState(_technicianSlider, GetAssignedTechnicians(),
                    Mathf.Max(0, _dataManager.Employee.GetAvailableEmployeeCount(EmployeeType.Technician)),
                    GetMaxTechnicianSlots());
            }
            finally
            {
                _suppressEmployeeSliderCallbacks = false;
            }
        }
    }

    private void UpdateProductionContext()
    {
        bool isProd = _currentData.IsProductionBuilding;
        bool isUnloadStation = _currentData.IsUnloadStation;
        _changeProductionBtn.gameObject.SetActive(true);
    }

    /// <summary>
    /// _inputGridTransform / _outputGridTransform: 선택 생산·하역의 레시피(필요 입력 / 산출 품목).
    /// _inputResourceContent: 건물 입력 버퍼에 쌓인 자원(클릭 시 창고로 반환).
    /// </summary>
    private void RefreshResourceGrids()
    {
        if (_buildingObject == null && _rawBuildingObject == null)
            return;

        if (_inputResourceContent != null)
            _inputResourceContent.gameObject.SetActive(_buildingObject != null);

        if (_currentData.IsLoadStation)
            _changeProductionBtn.gameObject.SetActive(false);
        else
            _changeProductionBtn.gameObject.SetActive(true);

        RefreshRecipeGrids();
        if (_buildingObject != null)
            StaggeredSpawnUtils.Restart(this, ref _resourceGridCoroutine, RefreshRuntimeResourceGridsRoutine());
        else if (_inputResourceContent != null)
            GameManager.Instance.PoolingManager.ClearChildrenToPool(_inputResourceContent);
    }

    private IEnumerator RefreshRuntimeResourceGridsRoutine()
    {
        yield return RepopulateRuntimeResourceButtonsRoutine(
            _inputResourceContent,
            _buildingObject.GetRuntimeInputResourceCounts());
    }

    public void RefreshAfterRuntimeInputBufferChanged()
    {
        RefreshResourceGrids();
        RefreshAllRuntimePanels();
    }

    private IEnumerator RepopulateRuntimeResourceButtonsRoutine(Transform container, Dictionary<string, int> counts)
    {
        PoolingManager pool = GameManager.Instance.PoolingManager;
        pool.ClearChildrenToPool(container);

        List<KeyValuePair<string, int>> resourceCounts = new List<KeyValuePair<string, int>>(counts);

        yield return StaggeredSpawnUtils.ForEachFrame(resourceCounts.Count, i =>
        {
            KeyValuePair<string, int> kvp = resourceCounts[i];
            ResourceEntry entry = _dataManager.Resource.GetResourceEntry(kvp.Key);
            GameObject btnObj = pool.GetPooledObject(_buildingInfoPopupResourceBtnPrefab);
            btnObj.transform.SetParent(container, false);
            btnObj.GetComponent<BuildingInfoPopupResourceBtn>()
                .Init(_buildingObject, kvp.Key, entry, kvp.Value, this);
        });
    }

    private void RefreshRecipeGrids()
    {
        GetRecipeDisplayData(_recipeInputIds, _recipeOutputIds, out _recipeCurrentResourceId);

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
    /// 생산 변경: 입력 버퍼를 창고로 돌린 뒤 생산품 선택 팝업을 띄웁니다.
    /// </summary>
    public void OnChangeProductionButtonClicked()
    {
        if (_buildingObject == null && _rawBuildingObject == null || _dataManager == null)
            return;
        if (!_currentData.IsProductionBuilding && !_currentData.IsUnloadStation)
            return;

        if (_buildingObject != null)
        {
            _buildingObject.ReturnAllRuntimeBuffersToDataManager(_dataManager);
            RefreshResourceGrids();
            RefreshAllRuntimePanels();
        }

        ShowOutputResourceSelection();
    }

    public void ShowOutputResourceSelection()
    {
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
        if (_rawBuildingObject != null)
        {
            if (!_rawBuildingObject.TrySetSelectedResource(selected.data))
                return;

            string buildingId = _rawBuildingObject.BuildingData != null ? _rawBuildingObject.BuildingData.id : null;
            UpdateUI();
            FlushPlacedLayoutIfNeeded();
            Close();

            if (!string.IsNullOrEmpty(buildingId))
                TutorialDirector.Instance?.NotifyBuildingResourceAssigned(buildingId);
            return;
        }

        if (!_buildingObject.TrySetSelectedResource(selected.data))
            return;

        string gridBuildingId = _buildingObject.BuildingData != null ? _buildingObject.BuildingData.id : null;
        UpdateUI();
        RefreshWorldIcons();
        Close();

        if (!string.IsNullOrEmpty(gridBuildingId))
            TutorialDirector.Instance?.NotifyBuildingResourceAssigned(gridBuildingId);
    }

    private void RefreshWorldIcons()
    {
        _buildingObject.RefreshOutgoingResourceIcons();
    }
}