using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BuildingInfoPanel : MonoBehaviour
{
    [SerializeField] private GameObject _productionExplainTextPrefab;

    [Header("UI References")]
    [SerializeField] private Button _changeProductionBtn;
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private TextMeshProUGUI _typeText;
    [SerializeField] private TextMeshProUGUI _descriptionText;
    [SerializeField] private TextMeshProUGUI _costText;
    [SerializeField] private TextMeshProUGUI _maintenanceText;
    [SerializeField] private TextMeshProUGUI _resourceTypesText;
    [SerializeField] private Image _buildingImage;

    [Header("Containers")]
    [SerializeField] private Transform _inputGridTransform;
    [SerializeField] private Transform _outputGridTransform;
    [SerializeField] private Transform _explainContentTransform;

    private BuildingData _currentData;
    private BuildingState _currentState;
    private DesignCanvas _designCanvas;
    private DataManager _dataManager;

    // 캐싱을 위한 컴포넌트 참조
    private DesignRunner _designRunner;

    public void ShowBuildingInfo(BuildingData data, BuildingState state, DesignCanvas canvas)
    {
        if (data == null) return;

        _currentData = data;
        _currentState = state;
        _designCanvas = canvas;
        _dataManager = DataManager.Instance; // 또는 주입받은 참조 사용

        UpdateUI();
    }

    private void UpdateUI()
    {
        _nameText.text = _currentData.displayName;
        _typeText.text = $"Type: {_currentData.buildingType}";
        _descriptionText.text = _currentData.description;
        _costText.text = $"Cost: {ReplaceUtils.FormatNumberWithCommas(_currentData.baseCost)}";
        _maintenanceText.text = $"Maint: {ReplaceUtils.FormatNumberWithCommas(_currentData.baseMaintenanceCost)}/mo";

        if (_buildingImage != null)
        {
            _buildingImage.sprite = _currentData.buildingSprite;
            _buildingImage.enabled = _currentData.buildingSprite != null;
        }

        UpdateProductionContext();
        RefreshResourceGrids();
    }

    private void UpdateProductionContext()
    {
        bool isProd = _currentData.IsProductionBuilding;
        _changeProductionBtn.gameObject.SetActive(true);
        _changeProductionBtn.interactable = isProd;

        if (isProd)
        {
            var allowed = _currentData.AllowedResourceTypes;
            string types = (allowed != null && allowed.Count > 0)
                ? string.Join(", ", allowed)
                : "None";
            _resourceTypesText.text = $"Allowed: {types}";
        }
        else
        {
            string handling = _currentData.HandlingResource?.displayName ?? "None";
            _resourceTypesText.text = $"Handling: {handling}";
        }
    }

    private void RefreshResourceGrids()
    {
        GameObjectUtils.ClearChildren(_explainContentTransform);

        // Input 리스트 갱신
        UpdateResourceGrid(_currentState.inputProductionIds, _inputGridTransform, "Input");

        // Output 리스트 갱신 (생산 건물이 아닐 경우 별도 처리)
        if (!_currentData.IsProductionBuilding)
            UpdateNonProductionOutput();
        else
            UpdateResourceGrid(_currentState.outputProductionIds, _outputGridTransform, "Output");
    }

    private void UpdateResourceGrid(List<string> resourceIds, Transform container, string label)
    {
        GameObjectUtils.ClearChildren(container);
        var counts = GameObjectUtils.AggregateResourceCounts(resourceIds);

        foreach (var kvp in counts)
        {
            var entry = _dataManager.Resource.GetResourceEntry(kvp.Key);
            if (entry == null) continue;

            // 아이콘 생성
            Instantiate(_designCanvas.ProductionInfoImage, container)
                .GetComponent<ProductionInfoImage>().Init(entry, kvp.Value);

            // 설명 텍스트 생성
            string reqs = (label == "Output") ? BuildRequirementText(entry.data) : "";
            string info = $"{label}: {entry.data.displayName}\nAmount: {kvp.Value}\nPrice: {ReplaceUtils.FormatNumber(entry.state.currentValue)}{reqs}";

            CreateExplainText(info);
        }
    }

    private void UpdateNonProductionOutput()
    {
        GameObjectUtils.ClearChildren(_outputGridTransform);

        string handlingId = _currentData.HandlingResource?.id ?? _currentState.currentResourceId;
        if (string.IsNullOrEmpty(handlingId)) return;

        var entry = _dataManager.Resource.GetResourceEntry(handlingId);
        if (entry != null)
        {
            Instantiate(_designCanvas.ProductionInfoImage, _outputGridTransform)
                .GetComponent<ProductionInfoImage>().Init(entry, 1);
            CreateExplainText($"Handling: {entry.data.displayName}\nPrice: {ReplaceUtils.FormatNumber(entry.state.currentValue)}");
        }
    }

    private void CreateExplainText(string content)
    {
        var textObj = Instantiate(_productionExplainTextPrefab, _explainContentTransform);
        textObj.GetComponent<TextMeshProUGUI>().text = content;
    }

    public void ShowOutputResourceSelection()
    {
        if (_currentData.AllowedResourceTypes == null || _currentData.AllowedResourceTypes.Count == 0) return;

        List<ResourceData> producible = GetProducibleList();
        _designCanvas.GameManager.ShowSelectResourcePanel(
            _currentData.AllowedResourceTypes,
            OnOutputResourceSelected,
            producible
        );
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

        // 데이터 갱신
        _currentState.outputProductionIds.Clear();
        _currentState.outputProductionIds.Add(selected.data.id);

        // 요구사항 자동 추가
        SyncInputToRequirements(selected.data);

        // UI 및 월드 아이콘 갱신
        UpdateUI();
        RefreshWorldIcons();

        gameObject.SetActive(false);
    }

    private void SyncInputToRequirements(ResourceData outputData)
    {
        _currentState.inputProductionIds.Clear();
        if (outputData.requirements == null) return;

        foreach (var req in outputData.requirements)
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

        var sb = new StringBuilder();
        sb.Append("\nRequires: ");
        for (int i = 0; i < data.requirements.Count; i++)
        {
            var req = data.requirements[i];
            if (req.resource == null) continue;
            sb.Append($"{req.resource.displayName} x{Mathf.Max(1, req.count)}");
            if (i < data.requirements.Count - 1) sb.Append(", ");
        }
        return sb.ToString();
    }
}