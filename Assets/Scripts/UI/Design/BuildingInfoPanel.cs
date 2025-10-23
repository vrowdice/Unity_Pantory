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
    private System.Action<ResourceEntry> _onOutputResourceSelected;

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
        _gameDataManager = designUiManager.GameDataManager;

        // 자원 선택 콜백 설정
        _onOutputResourceSelected = OnOutputResourceSelected;

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
                string resourceTypes = "Allowed Resources:";
                foreach (var resourceType in _currentBuildingData.allowedResourceTypes)
                {
                    resourceTypes += $" {resourceType}";
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

        if (_currentBuildingState.inputProductionIds == null)
        {
            return;
        }

        foreach (var productionId in _currentBuildingState.inputProductionIds)
        {
            ResourceEntry resourceEntry = _gameDataManager.GetResourceEntry(productionId);

            Instantiate(_designUiManager.ProductionInfoImage, _inputGridContentTransform).
            GetComponent<ProductionInfoImage>().OnInitialize(resourceEntry);

            Instantiate(_productionExplainTextPrefab, _productionExplainTextContentTransform).
            GetComponent<TextMeshProUGUI>().text =
             $"Input: {resourceEntry.resourceData.displayName}\nPrice: {resourceEntry.resourceState.currentValue}";
        }
    }

    private void UpdateOutputProductionImages()
    {
        GameObjectUtils.ClearChildren(_outputGridContentTransform);
        GameObjectUtils.ClearChildren(_productionExplainTextContentTransform);

        if (_currentBuildingState.inputProductionIds == null)
        {
            return;
        }
    
        foreach (var productionId in _currentBuildingState.outputProductionIds)
        {
            Instantiate(_designUiManager.ProductionInfoImage, _outputGridContentTransform).
            GetComponent<ProductionInfoImage>().OnInitialize(_gameDataManager.GetResourceEntry(productionId));

            Instantiate(_productionExplainTextPrefab, _productionExplainTextContentTransform).
            GetComponent<TextMeshProUGUI>().text =
             $"Output: {_gameDataManager.GetResourceEntry(productionId).resourceData.displayName}\nPrice: {_gameDataManager.GetResourceEntry(productionId).resourceState.currentValue}";
        }
    }


    /// <summary>
    /// 출력 자원 선택 패널을 표시합니다.
    /// </summary>
    public void ShowOutputResourceSelection()
    {
        if (_currentBuildingData?.allowedResourceTypes == null || _currentBuildingData.allowedResourceTypes.Count == 0)
        {
            Debug.LogWarning("[BuildingInfoPanel] No allowed resource types for output selection.");
            return;
        }

        if (_designUiManager.GameManager != null)
        {
            _designUiManager.GameManager.ShowSelectResourcePanel(_currentBuildingData.allowedResourceTypes, _onOutputResourceSelected);
        }
        else
        {
            Debug.LogError("[BuildingInfoPanel] GameManager.Instance is null.");
        }
    }


    /// <summary>
    /// 출력 자원이 선택되었을 때 호출되는 콜백
    /// </summary>
    /// <param name="selectedResource">선택된 자원</param>
    private void OnOutputResourceSelected(ResourceEntry selectedResource)
    {
        if (_currentBuildingState != null && selectedResource != null)
        {
            _currentBuildingState.outputProductionIds.Clear();

            _currentBuildingState.outputProductionIds.Add(selectedResource.resourceData.id);
            Debug.Log($"[BuildingInfoPanel] Output resource added: {selectedResource.resourceData.displayName}");

            // 선택된 출력 자원의 제조 요구사항을 확인하고 필요한 입력 자원들을 자동으로 추가
            AddRequiredInputResources(selectedResource.resourceData);

            UpdateOutputProductionImages();
            UpdateInputProductionImages();
        }
    }

    /// <summary>
    /// 선택된 출력 자원의 제조 요구사항을 확인하고 필요한 입력 자원들을 자동으로 추가합니다.
    /// </summary>
    /// <param name="outputResourceData">출력 자원 데이터</param>
    private void AddRequiredInputResources(ResourceData outputResourceData)
    {
        // 기존 입력 자원들을 모두 초기화
        _currentBuildingState.inputProductionIds.Clear();
        Debug.Log($"[BuildingInfoPanel] Input resources cleared for new output: {outputResourceData.displayName}");

        if (outputResourceData.requirements == null || outputResourceData.requirements.Count == 0)
        {
            Debug.Log($"[BuildingInfoPanel] No requirements found for {outputResourceData.displayName}");
            return;
        }

        foreach (var requirement in outputResourceData.requirements)
        {
            if (requirement.resource != null)
            {
                _currentBuildingState.inputProductionIds.Add(requirement.resource.id);
                Debug.Log($"[BuildingInfoPanel] Required input resource added: {requirement.resource.displayName} (count: {requirement.count})");
            }
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
