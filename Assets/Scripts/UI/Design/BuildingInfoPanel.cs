using System.Collections.Generic;
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
    [SerializeField] private GameObject _changeProductionBtn;
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
        // 생산 정보 초기화 (UpdateInputProductionImages와 UpdateOutputProductionImages에서 각각 처리)
        GameObjectUtils.ClearChildren(_productionExplainTextContentTransform);

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
            if (_currentBuildingData.AllowedResourceTypes != null && _currentBuildingData.AllowedResourceTypes.Count > 0)
            {
                string resourceTypes = "Allowed Resources:";
                foreach (var resourceType in _currentBuildingData.AllowedResourceTypes)
                {
                    resourceTypes += $" {resourceType}";
                }
                _resourceTypesText.text = resourceTypes;
                _changeProductionBtn.SetActive(true);
            }
            else
            {
                _changeProductionBtn.SetActive(false);
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
        // 기존 내용 지우기
        GameObjectUtils.ClearChildren(_inputGridContentTransform);
        
        Dictionary<string, int> inputCounts = AggregateResourceCounts(_currentBuildingState.inputProductionIds);
        if (inputCounts.Count == 0)
        {
            return;
        }

        foreach (var kvp in inputCounts)
        {
            ResourceEntry resourceEntry = _gameDataManager.GetResourceEntry(kvp.Key);

            if (resourceEntry != null)
            {
                Instantiate(_designUiManager.ProductionInfoImage, _inputGridContentTransform).
                GetComponent<ProductionInfoImage>().OnInitialize(resourceEntry, kvp.Value);

                Instantiate(_productionExplainTextPrefab, _productionExplainTextContentTransform).
                GetComponent<TextMeshProUGUI>().text =
                 $"Input: {resourceEntry.resourceData.displayName}\nConsumption: {kvp.Value}\nPrice: {resourceEntry.resourceState.currentValue}";
            }
        }
    }

    private void UpdateOutputProductionImages()
    {
        // 기존 내용 지우기
        GameObjectUtils.ClearChildren(_outputGridContentTransform);
        
        Dictionary<string, int> outputCounts = AggregateResourceCounts(_currentBuildingState.outputProductionIds);
        if (outputCounts.Count == 0)
        {
            return;
        }

        foreach (var kvp in outputCounts)
        {
            ResourceEntry resourceEntry = _gameDataManager.GetResourceEntry(kvp.Key);
            
            if (resourceEntry != null)
            {
                Instantiate(_designUiManager.ProductionInfoImage, _outputGridContentTransform).
                GetComponent<ProductionInfoImage>().OnInitialize(resourceEntry, kvp.Value);

                Instantiate(_productionExplainTextPrefab, _productionExplainTextContentTransform).
                GetComponent<TextMeshProUGUI>().text =
                 $"Output: {resourceEntry.resourceData.displayName}\nProduction: {kvp.Value}\nPrice: {resourceEntry.resourceState.currentValue}";
            }
        }
    }


    /// <summary>
    /// 출력 자원 선택 패널을 표시합니다.
    /// </summary>
    public void ShowOutputResourceSelection()
    {
        if (_currentBuildingData?.AllowedResourceTypes == null || _currentBuildingData.AllowedResourceTypes.Count == 0)
        {
            Debug.LogWarning("[BuildingInfoPanel] No allowed resource types for output selection.");
            return;
        }

        if (_designUiManager.GameManager != null)
        {
            _designUiManager.GameManager.ShowSelectResourcePanel(_currentBuildingData.AllowedResourceTypes, _onOutputResourceSelected);
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

            // 생산 정보 텍스트 초기화
            GameObjectUtils.ClearChildren(_productionExplainTextContentTransform);
            
            // UI 업데이트
            UpdateOutputProductionImages();
            UpdateInputProductionImages();
            
            // 실제 게임 화면의 건물 오브젝트도 업데이트
            RefreshBuildingObjectIcons();

            gameObject.SetActive(false);
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
                int requiredCount = Mathf.Max(1, requirement.count);
                for (int i = 0; i < requiredCount; i++)
                {
                    _currentBuildingState.inputProductionIds.Add(requirement.resource.id);
                }
                Debug.Log($"[BuildingInfoPanel] Required input resource added: {requirement.resource.displayName} (count: {requiredCount})");
            }
        }
    }

    /// <summary>
    /// 실제 게임 화면의 건물 오브젝트 아이콘을 갱신합니다.
    /// </summary>
    private void RefreshBuildingObjectIcons()
    {
        if (_currentBuildingState == null)
            return;
        
        // BuildingTileManager 찾기
        BuildingTileManager buildingTileManager = FindFirstObjectByType<BuildingTileManager>();
        if (buildingTileManager != null)
        {
            // 건물 새로고침 (모든 건물 오브젝트를 다시 생성)
            buildingTileManager.RefreshBuildings();
            Debug.Log("[BuildingInfoPanel] Building object icons refreshed.");
        }
        else
        {
            Debug.LogWarning("[BuildingInfoPanel] BuildingTileManager not found.");
        }
    }

    /// <summary>
    /// 패널을 숨깁니다.
    /// </summary>
    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private Dictionary<string, int> AggregateResourceCounts(List<string> resourceIds)
    {
        Dictionary<string, int> counts = new Dictionary<string, int>();

        if (resourceIds == null)
            return counts;

        foreach (var resourceId in resourceIds)
        {
            if (string.IsNullOrEmpty(resourceId))
                continue;

            if (counts.ContainsKey(resourceId))
            {
                counts[resourceId]++;
            }
            else
            {
                counts[resourceId] = 1;
            }
        }

        return counts;
    }
}
