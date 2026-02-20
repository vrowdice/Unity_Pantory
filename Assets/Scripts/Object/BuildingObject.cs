using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 건물 오브젝트를 나타내는 컴포넌트
/// 건물의 마커(Input/Output)를 관리합니다.
/// </summary>
public class BuildingObject : MonoBehaviour
{
    [SerializeField] private float _productionIconContentOffset;
    [SerializeField] private float _productionIconScale;

    private GameManager _gameManager;

    private BuildingData _buildingData;
    private BuildingState _buildingState;

    private GameObject _inputMarker;
    private GameObject _outputMarker;
    private GameObject _inputProductionContainer;
    private GameObject _outputProductionContainer;
    private GameObject _roadResourceContainer;  // 도로 자원 표시용 컨테이너

    public BuildingData BuildingData => _buildingData;
    public BuildingState BuildingState => _buildingState;
    
    /// <summary>
    /// 건물 오브젝트를 초기화합니다.
    /// </summary>
    public void Initialize(BuildingData buildingData, BuildingState buildingState, GameObject inputMarkerPrefab, GameObject outputMarkerPrefab, DesignRunnerGridHandler gridHandler)
    {
        _gameManager = GameManager.Instance;

        _buildingData = buildingData;
        _buildingState = buildingState;

        CreateMarkers(inputMarkerPrefab, outputMarkerPrefab, gridHandler);
    }
    
    /// <summary>
    /// 프리뷰용으로 초기화합니다 (BuildingState 없이).
    /// </summary>
    public void InitializePreview(BuildingData buildingData, GameObject inputMarkerPrefab, GameObject outputMarkerPrefab)
    {
        _buildingData = buildingData;
        _buildingState = null;

        if (buildingData.InputPosition != Vector2Int.zero && inputMarkerPrefab != null)
        {
            _inputMarker = CreateMarkerAsChild("PreviewInput", inputMarkerPrefab);
        }
        
        if (buildingData.OutputPosition != Vector2Int.zero && outputMarkerPrefab != null)
        {
            _outputMarker = CreateMarkerAsChild("PreviewOutput", outputMarkerPrefab);
        }
    }
    
    /// <summary>
    /// Input/Output 마커를 생성합니다.
    /// </summary>
    private void CreateMarkers(GameObject inputMarkerPrefab, GameObject outputMarkerPrefab, DesignRunnerGridHandler gridHandler)
    {
        if (_buildingData.InputPosition != Vector2Int.zero && inputMarkerPrefab != null)
        {
            _inputMarker = CreateMarkerAsChild("Input", inputMarkerPrefab);
            UpdateMarkerPosition(_inputMarker, _buildingState.inputPosition, gridHandler);
        }
        if (_buildingData.OutputPosition != Vector2Int.zero && outputMarkerPrefab != null)
        {
            _outputMarker = CreateMarkerAsChild("Output", outputMarkerPrefab);
            UpdateMarkerPosition(_outputMarker, _buildingState.outputPosition, gridHandler);
        }
    }
    
    /// <summary>
    /// 마커를 자식으로 생성합니다.
    /// </summary>
    private GameObject CreateMarkerAsChild(string name, GameObject prefab)
    {
        GameObject marker = Instantiate(prefab, transform);
        marker.name = $"IOMarker_{name}";
        GameObjectUtils.CompensateParentScale(marker.transform, transform);
        return marker;
    }
    
    /// <summary>
    /// 마커의 위치를 업데이트합니다.
    /// </summary>
    private void UpdateMarkerPosition(GameObject marker, Vector2Int gridPos, DesignRunnerGridHandler gridHandler)
    {
        Vector3 worldPos = gridHandler.GridToWorldPosition(gridPos, Vector2Int.one);
        marker.transform.position = new Vector3(worldPos.x, worldPos.y, 1.0f);
    }
    
    /// <summary>
    /// 프리뷰 마커의 위치를 업데이트합니다 (그리드 핸들러 필요).
    /// </summary>
    public void UpdatePreviewMarkers(Vector2Int buildingGridPos, DesignRunnerGridHandler gridHandler, int rotation = 0)
    {
        Vector2Int rotatedInputPos = RotatePositionAroundCenter(_buildingData.InputPosition, rotation, _buildingData.size);
        Vector2Int rotatedOutputPos = RotatePositionAroundCenter(_buildingData.OutputPosition, rotation, _buildingData.size);
        Vector2Int inputPos = buildingGridPos + rotatedInputPos;
        Vector2Int outputPos = buildingGridPos + rotatedOutputPos;

        if (_inputMarker != null && _buildingData.InputPosition != Vector2Int.zero)
        {
            Vector3 inputWorldPos = gridHandler.GridToWorldPosition(inputPos, Vector2Int.one);
            _inputMarker.transform.position = new Vector3(inputWorldPos.x, inputWorldPos.y, -0.5f);
        }
        if (_outputMarker != null && _buildingData.OutputPosition != Vector2Int.zero)
        {
            Vector3 outputWorldPos = gridHandler.GridToWorldPosition(outputPos, Vector2Int.one);
            _outputMarker.transform.position = new Vector3(outputWorldPos.x, outputWorldPos.y, -0.5f);
        }
    }
    
    /// <summary>
    /// 건물 크기를 고려하여 중심을 기준으로 위치를 회전합니다.
    /// </summary>
    private Vector2Int RotatePositionAroundCenter(Vector2Int pos, int rotation, Vector2Int buildingSize)
    {
        rotation = rotation % 4;
        if (rotation == 0)
            return pos;

        float centerX = (buildingSize.x - 1) / 2f;
        float centerY = (buildingSize.y - 1) / 2f;
        float relX = pos.x - centerX;
        float relY = pos.y - centerY;
        float rotatedX, rotatedY;
        switch (rotation)
        {
            case 1:
                rotatedX = -relY;
                rotatedY = relX;
                break;
            case 2:
                rotatedX = -relX;
                rotatedY = -relY;
                break;
            case 3:
                rotatedX = relY;
                rotatedY = -relX;
                break;
            default:
                rotatedX = relX;
                rotatedY = relY;
                break;
        }

        return new Vector2Int(
            Mathf.RoundToInt(rotatedX + centerX),
            Mathf.RoundToInt(rotatedY + centerY)
        );
    }
    
    /// <summary>
    /// 마커의 표시/숨김을 설정합니다.
    /// </summary>
    public void SetMarkersActive(bool active)
    {
        _inputMarker?.SetActive(active);
        _outputMarker?.SetActive(active);
    }
    
    /// <summary>
    /// 건물 위에 입출력 자원 아이콘을 표시합니다.
    /// </summary>
    public void SetupProductionIcons()
    {
        ClearProductionIconContainers();
        
        Transform sharedCanvas = _gameManager.GetWorldCanvas();
        if (sharedCanvas == null) return;

        Dictionary<string, int> inputCounts = GameObjectUtils.AggregateResourceCounts(_buildingState.inputProductionIds);
        Dictionary<string, int> outputCounts = GameObjectUtils.AggregateResourceCounts(_buildingState.outputProductionIds);

        Vector2Int rotatedSize = GetRotatedSize(_buildingData.size, _buildingState.rotation);
        float buildingHeight = rotatedSize.y;

        if (inputCounts.Count > 0)
        {
            float yOffset = buildingHeight * _productionIconContentOffset;
            Vector3 worldPosition = transform.position + new Vector3(0, yOffset, -1);
            
            _inputProductionContainer = _gameManager.CreateProductionIconContainer(
                sharedCanvas,
                $"InputIcons_{gameObject.name}",
                worldPosition,
                _productionIconScale,
                inputCounts
            );
        }

        if (outputCounts.Count > 0)
        {
            float yOffset = -buildingHeight * _productionIconContentOffset;
            Vector3 worldPosition = transform.position + new Vector3(0, yOffset, -1);
            
            _outputProductionContainer = _gameManager.CreateProductionIconContainer(
                sharedCanvas,
                $"OutputIcons_{gameObject.name}",
                worldPosition,
                _productionIconScale,
                outputCounts
            );
        }
    }

    /// <summary>
    /// Production Icon 컨테이너들을 정리합니다.
    /// </summary>
    private void ClearProductionIconContainers()
    {
        if (_inputProductionContainer != null)
        {
            PoolingManager.Instance?.ClearChildrenToPool(_inputProductionContainer.transform);
            Destroy(_inputProductionContainer);
            _inputProductionContainer = null;
        }
        
        if (_outputProductionContainer != null)
        {
            PoolingManager.Instance?.ClearChildrenToPool(_outputProductionContainer.transform);
            Destroy(_outputProductionContainer);
            _outputProductionContainer = null;
        }
    }

    
    /// <summary>
    /// 회전에 따라 건물 크기를 계산합니다.
    /// </summary>
    private Vector2Int GetRotatedSize(Vector2Int size, int rotation)
    {
        rotation = rotation % 4;
        if (rotation == 1 || rotation == 3)
        {
            return new Vector2Int(size.y, size.x);
        }
        return size;
    }
    
    /// <summary>
    /// 도로 건물에 전파된 자원들을 표시합니다.
    /// </summary>
    public void SetupRoadResources(DesignRunnerRoadHandler roadHandler)
    {
        if (!_buildingData.IsRoad) return;
        
        ClearRoadResourceContainer();
        
        Transform sharedCanvas = _gameManager.GetWorldCanvas();
        if (sharedCanvas == null) return;
        
        Vector2Int roadPos = new Vector2Int(_buildingState.positionX, _buildingState.positionY);
        HashSet<string> resources = roadHandler.GetResourcesAtRoad(roadPos);
        
        if (resources.Count == 0) return;
        
        Dictionary<string, int> resourceCounts = new Dictionary<string, int>();
        foreach (string resourceId in resources)
        {
            resourceCounts[resourceId] = 1;
        }
        
        Vector3 worldPosition = transform.position + new Vector3(0, 0, -1);
        float roadIconScale = _productionIconScale * 0.7f; // 도로 자원 아이콘을 70% 크기로
        
        _roadResourceContainer = _gameManager.CreateProductionIconContainer(
            sharedCanvas,
            $"RoadResources_{gameObject.name}",
            worldPosition,
            roadIconScale,
            resourceCounts
        );
    }
    
    /// <summary>
    /// 도로 자원 컨테이너를 정리합니다.
    /// </summary>
    private void ClearRoadResourceContainer()
    {
        if (_roadResourceContainer != null)
        {
            PoolingManager.Instance?.ClearChildrenToPool(_roadResourceContainer.transform);
            Destroy(_roadResourceContainer);
            _roadResourceContainer = null;
        }
    }
    
    void OnDestroy()
    {
        ClearProductionIconContainers();
        ClearRoadResourceContainer();
    }
}
