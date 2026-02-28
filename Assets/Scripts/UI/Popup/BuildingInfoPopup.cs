using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BuildingInfoPopup : PopupBase
{
    [Header("UI References")]
    [SerializeField] private Button _changeProductionBtn;
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private TextMeshProUGUI _typeText;
    [SerializeField] private TextMeshProUGUI _descriptionText;
    [SerializeField] private TextMeshProUGUI _buildCostText;
    [SerializeField] private TextMeshProUGUI _maintenanceText;
    [SerializeField] private TextMeshProUGUI _requiredEmployeeText;
    [SerializeField] private Image _buildingImage;
    [SerializeField] private Toggle _IsNeededExpertToggle;

    [SerializeField] private TextMeshProUGUI _materialPurchaseText;
    [SerializeField] private TextMeshProUGUI _productPriceText;
    [SerializeField] private TextMeshProUGUI _expectedCostText;

    [Header("Containers")]
    [SerializeField] private Transform _inputGridTransform;
    [SerializeField] private Transform _outputGridTransform;

    private BuildingData _currentData;
    private BuildingState _currentState;
    private DesignCanvas _designCanvas;
    private DataManager _dataManager;
    private DesignRunner _designRunner;

    public void ShowBuildingInfo(BuildingData data, BuildingState state, DesignCanvas canvas)
    {
        if (data == null) return;

        base.Init();
        
        _currentData = data;
        _currentState = state;
        _designCanvas = canvas;
        _dataManager = DataManager.Instance;

        UpdateUI();
        Show();
    }

    private void UpdateUI()
    {
        _nameText.text = _currentData.id.Localize(LocalizationUtils.TABLE_BUILDING);
        _typeText.text = $"{_currentData.buildingType.Localize(LocalizationUtils.TABLE_BUILDING_TYPE)}";
        _descriptionText.text = _currentData.id.Localize(LocalizationUtils.TABLE_BUILDING_DESCRIPTION);
        _buildCostText.text = $"{ReplaceUtils.FormatNumberWithCommas(_currentData.buildCost)}";
        _maintenanceText.text = $"{ReplaceUtils.FormatNumberWithCommas(_currentData.maintenanceCost)} / day";
        _requiredEmployeeText.text = $"{_currentData.requiredEmployees}";
        _IsNeededExpertToggle.isOn = _currentData.isProfessional;

        if (_buildingImage != null)
        {
            _buildingImage.sprite = _currentData.buildingSprite;
            _buildingImage.enabled = _currentData.buildingSprite != null;
        }

        UpdateProductionContext();
        RefreshResourceGrids();
        UpdateFinancialInfo();
    }

    private void UpdateFinancialInfo()
    {
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

        if (_materialPurchaseText) _materialPurchaseText.text = $"{ReplaceUtils.FormatNumberWithCommas(inputCost)}";
        if (_productPriceText) _productPriceText.text = $"{ReplaceUtils.FormatNumberWithCommas(outputPrice)}";
        if (_expectedCostText)
        {
            _expectedCostText.text = $"{ReplaceUtils.FormatNumberWithCommas(profit)}";
            _expectedCostText.color = VisualManager.Instance.GetDeltaColor(profit);
        }
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
        UpdateResourceGrid(_currentState.inputProductionIds, _inputGridTransform, "Input");
        
        // 하역소인 경우 출력 자원 표시
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

    private void UpdateResourceGrid(List<string> resourceIds, Transform container, string label)
    {
        GameObjectUtils.ClearChildren(container);
        Dictionary<string, int> counts = GameObjectUtils.AggregateResourceCounts(resourceIds);

        foreach (KeyValuePair<string, int> kvp in counts)
        {
            ResourceEntry entry = _dataManager.Resource.GetResourceEntry(kvp.Key);
            if (entry == null) continue;

            Instantiate(_designCanvas.ProductionInfoImage, container)
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

        ResourceEntry entry = _dataManager.Resource.GetResourceEntry(handlingId);
        if (entry != null)
        {
            Instantiate(_designCanvas.ProductionInfoImage, _outputGridTransform)
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
                null  // 하역소는 모든 자원 선택 가능
            );
            return;
        }
        
        // 생산 건물인 경우 기존 로직 사용
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
        if (_currentState == null || selected == null) return;

        _currentState.outputProductionIds.Clear();
        _currentState.outputProductionIds.Add(selected.data.id);

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
        if (_currentState == null || selected == null) return;

        _currentState.outputProductionIds.Clear();
        _currentState.outputProductionIds.Add(selected.data.id);

        SyncInputToRequirements(selected.data);
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
        if (_designRunner == null) _designRunner = FindFirstObjectByType<DesignRunner>();
        _designRunner?.RefreshBuildings();
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