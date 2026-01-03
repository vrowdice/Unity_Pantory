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
    private DataManager _dataManager;
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
        _dataManager = designUiManager.GameDataManager;

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
        GameObjectUtils.ClearChildren(_productionExplainTextContentTransform);

        _nameText.text = _currentBuildingData.displayName;
        _typeText.text = $"Type: {_currentBuildingData.buildingType}";
        _descriptionText.text = _currentBuildingData.description;
        _costText.text = $"Cost: {_currentBuildingData.baseCost:N0}";
        _maintenanceText.text = $"Maintenance: {_currentBuildingData.baseMaintenanceCost:N0}/month";

        if (_resourceTypesText != null)
        {
            var btn = _changeProductionBtn != null ? _changeProductionBtn.GetComponent<Button>() : null;

            if (_currentBuildingData.IsProductionBuilding)
            {
                if (btn != null) btn.interactable = true;
                _changeProductionBtn?.SetActive(true);

                if (_currentBuildingData.AllowedResourceTypes != null && _currentBuildingData.AllowedResourceTypes.Count > 0)
                {
                    string resourceTypes = "Allowed Resources:";
                    foreach (var resourceType in _currentBuildingData.AllowedResourceTypes)
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
            else
            {
                _changeProductionBtn?.SetActive(true);
                if (btn != null) btn.interactable = false;

                if (_currentBuildingData.HandlingResource != null)
                {
                    _resourceTypesText.text = $"Handling: {_currentBuildingData.HandlingResource.displayName}";
                }
                else
                {
                    _resourceTypesText.text = "Handling: None";
                }
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
        
        Dictionary<string, int> inputCounts = GameObjectUtils.AggregateResourceCounts(_currentBuildingState.inputProductionIds);
        if (inputCounts.Count == 0)
        {
            return;
        }

        foreach (var kvp in inputCounts)
        {
            ResourceEntry resourceEntry = _dataManager.Resource.GetResourceEntry(kvp.Key);

            if (resourceEntry != null)
            {
                Instantiate(_designUiManager.ProductionInfoImage, _inputGridContentTransform).
                GetComponent<ProductionInfoImage>().OnInitialize(resourceEntry, kvp.Value);

                Instantiate(_productionExplainTextPrefab, _productionExplainTextContentTransform).
                GetComponent<TextMeshProUGUI>().text =
                 $"Input: {resourceEntry.data.displayName}\nConsumption: {kvp.Value}\nPrice: {resourceEntry.state.value}";
            }
        }
    }

    private void UpdateOutputProductionImages()
    {
        GameObjectUtils.ClearChildren(_outputGridContentTransform);

        // 비생산 건물(road/load/unload 등)은 handlingResource 또는 runtime 자원을 표시
        if (!_currentBuildingData.IsProductionBuilding)
        {
            GameObjectUtils.ClearChildren(_productionExplainTextContentTransform);

            string handlingId = null;
            string handlingNameFallback = null;

            if (_currentBuildingData.HandlingResource != null)
            {
                handlingId = _currentBuildingData.HandlingResource.id;
                handlingNameFallback = _currentBuildingData.HandlingResource.displayName;
            }
            else if (_currentBuildingState != null && !string.IsNullOrEmpty(_currentBuildingState.currentResourceId))
            {
                // ResourceAnimHandler 등에서 runtime으로 채워주는 currentResourceId가 있으면 우선 사용
                handlingId = _currentBuildingState.currentResourceId;
            }

            if (!string.IsNullOrEmpty(handlingId))
            {
                ResourceEntry resourceEntry = _dataManager.Resource.GetResourceEntry(handlingId);

                if (resourceEntry != null)
                {
                    Instantiate(_designUiManager.ProductionInfoImage, _outputGridContentTransform)
                        .GetComponent<ProductionInfoImage>()
                        .OnInitialize(resourceEntry, 1);

                    Instantiate(_productionExplainTextPrefab, _productionExplainTextContentTransform)
                        .GetComponent<TextMeshProUGUI>().text =
                        $"Handling: {resourceEntry.data.displayName}\nPrice: {resourceEntry.state.value}";
                }
                else
                {
                    Instantiate(_productionExplainTextPrefab, _productionExplainTextContentTransform)
                        .GetComponent<TextMeshProUGUI>().text =
                        $"Handling: {handlingNameFallback ?? handlingId}";
                }
            }
            else
            {
                Instantiate(_productionExplainTextPrefab, _productionExplainTextContentTransform)
                    .GetComponent<TextMeshProUGUI>().text = "Handling: None";
            }
            return;
        }
        
        Dictionary<string, int> outputCounts = GameObjectUtils.AggregateResourceCounts(_currentBuildingState.outputProductionIds);
        if (outputCounts.Count == 0)
        {
            return;
        }

        foreach (var kvp in outputCounts)
        {
            ResourceEntry resourceEntry = _dataManager.Resource.GetResourceEntry(kvp.Key);
            
            if (resourceEntry != null)
            {
                Instantiate(_designUiManager.ProductionInfoImage, _outputGridContentTransform).
                GetComponent<ProductionInfoImage>().OnInitialize(resourceEntry, kvp.Value);

                string requireText = BuildRequirementText(resourceEntry.data);

                Instantiate(_productionExplainTextPrefab, _productionExplainTextContentTransform).
                GetComponent<TextMeshProUGUI>().text =
                 $"Output: {resourceEntry.data.displayName}\nProduction: {kvp.Value}\nPrice: {resourceEntry.state.value}{requireText}";
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
            // 생산 가능한 자원 목록 가져오기
            List<ResourceData> producibleResources = null;
            
            // ProductionBuildingData인 경우 생산 가능한 자원 목록 사용
            if (_currentBuildingData is ProductionBuildingData productionData)
            {
                if (productionData.ProducibleResources != null && productionData.ProducibleResources.Count > 0)
                {
                    producibleResources = new List<ResourceData>(productionData.ProducibleResources);
                }
            }
            // RawMaterialFactoryData인 경우 생산 가능한 원자재 목록 사용
            else if (_currentBuildingData is RawMaterialFactoryData rawMaterialData)
            {
                if (rawMaterialData.ProducibleRawResources != null && rawMaterialData.ProducibleRawResources.Count > 0)
                {
                    producibleResources = new List<ResourceData>(rawMaterialData.ProducibleRawResources);
                }
            }
            
            _designUiManager.GameManager.ShowSelectResourcePanel(_currentBuildingData.AllowedResourceTypes, _onOutputResourceSelected, producibleResources);
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

            _currentBuildingState.outputProductionIds.Add(selectedResource.data.id);
            Debug.Log($"[BuildingInfoPanel] Output resource added: {selectedResource.data.displayName}");

            // 선택된 출력 자원의 제조 요구사항을 확인하고 필요한 입력 자원들을 자동으로 추가
            AddRequiredInputResources(selectedResource.data);

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


    private string BuildRequirementText(ResourceData data)
    {
        if (data == null || data.requirements == null || data.requirements.Count == 0)
            return "";

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.Append("\nRequires: ");

        bool first = true;
        foreach (var req in data.requirements)
        {
            if (req?.resource == null) continue;
            if (!first) sb.Append(", ");
            sb.Append($"{req.resource.displayName} x{Mathf.Max(1, req.count)}");
            first = false;
        }

        return sb.ToString();
    }
}
