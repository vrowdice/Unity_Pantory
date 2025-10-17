using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// 건물 정보를 표시하는 패널
/// </summary>
public class BuildingInfoPanel : MonoBehaviour
{
    [SerializeField] private GameObject _productionExplainTextPrefab;

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private TextMeshProUGUI _typeText;
    [SerializeField] private TextMeshProUGUI _descriptionText;
    [SerializeField] private TextMeshProUGUI _costText;
    [SerializeField] private TextMeshProUGUI _maintenanceText;
    [SerializeField] private TextMeshProUGUI _resourceTypesText;
    [SerializeField] private Image _buildingImage;

    [SerializeField] private Transform _inputGridContentTransform;
    [SerializeField] private Transform _outputGridContentTransform;
    [SerializeField] private Transform _productionExplainTextContentTransform;

    private BuildingData _currentBuildingData;
    private BuildingState _currentBuildingState;

    private DesignUiManager _designUiManager;
    private GameDataManager _gameDataManager;

    /// <summary>
    /// 건물 정보를 표시합니다.
    /// </summary>
    public void ShowBuildingInfo(BuildingData buildingData, BuildingState buildingState, DesignUiManager designUiManager)
    {
        if (buildingData == null)
        {
            Debug.LogWarning("[BuildingInfoPanel] BuildingData is null!");
            return;
        }

        _currentBuildingData = buildingData;
        _currentBuildingState = buildingState;
        _designUiManager = designUiManager;
        _gameDataManager = designUiManager.DataManager;
        // UI 업데이트
        UpdateUI();
    }

    /// <summary>
    /// UI를 업데이트합니다.
    /// </summary>
    private void UpdateUI()
    {
        if (_nameText != null)
            _nameText.text = _currentBuildingData.displayName;

        if (_typeText != null)
            _typeText.text = $"Type: {_currentBuildingData.buildingType}";

        if (_descriptionText != null)
            _descriptionText.text = _currentBuildingData.description;

        if (_costText != null)
            _costText.text = $"Cost: {_currentBuildingData.baseCost:N0}";

        if (_maintenanceText != null)
            _maintenanceText.text = $"Maintenance: {_currentBuildingData.baseMaintenanceCost:N0}/month";

        if (_resourceTypesText != null)
        {
            if (_currentBuildingData.allowedResourceTypes != null && _currentBuildingData.allowedResourceTypes.Count > 0)
            {
                string resourceTypes = "Allowed Resources:\n";
                foreach (var resourceType in _currentBuildingData.allowedResourceTypes)
                {
                    resourceTypes += $"- {resourceType}\n";
                }
                _resourceTypesText.text = resourceTypes;
            }
            else
            {
                _resourceTypesText.text = "Allowed Resources: None";
            }
        }

        if (_buildingImage != null && _currentBuildingData.buildingSprite != null)
        {
            _buildingImage.sprite = _currentBuildingData.buildingSprite;
            _buildingImage.enabled = true;
        }
        else if (_buildingImage != null)
        {
            _buildingImage.enabled = false;
        }

        UpdateInputProductionImages();
        UpdateOutputProductionImages();


    }

    private void UpdateInputProductionImages()
    {
        GameObjectUtils.ClearChildren(_inputGridContentTransform);
        foreach (var productionId in _currentBuildingState.inputProductionIds)
        {
            ResourceEntry resourceEntry = _gameDataManager.GetResourceEntry(productionId);

            Instantiate(_designUiManager.ProductionInfoImage, _inputGridContentTransform).
            GetComponent<ProductionInfoImage>().OnInitialize(resourceEntry);

            Instantiate(_productionExplainTextPrefab, _productionExplainTextContentTransform).
            GetComponent<TextMeshProUGUI>().text =
             $"Input: {resourceEntry.resourceData.description}\nPrice: {resourceEntry.resourceState.currentValue}";
        }
    }

    private void UpdateOutputProductionImages()
    {
        GameObjectUtils.ClearChildren(_outputGridContentTransform);
        foreach (var productionId in _currentBuildingState.outputProductionIds)
        {
            Instantiate(_designUiManager.ProductionInfoImage, _outputGridContentTransform).
            GetComponent<ProductionInfoImage>().OnInitialize(_gameDataManager.GetResourceEntry(productionId));

            Instantiate(_productionExplainTextPrefab, _productionExplainTextContentTransform).
            GetComponent<TextMeshProUGUI>().text =
             $"Output: {_gameDataManager.GetResourceEntry(productionId).resourceData.description}\nPrice: {_gameDataManager.GetResourceEntry(productionId).resourceState.currentValue}";
        }
    }

    /// <summary>
    /// 패널을 숨깁니다.
    /// </summary>
    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
