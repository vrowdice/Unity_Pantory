using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 건물 오브젝트를 나타내는 컴포넌트
/// 건물의 마커(Input/Output)를 관리합니다.
/// </summary>
public class BuildingObject : MonoBehaviour
{
    // ==================== Properties ====================
    [SerializeField] private float _productionIconContentOffset;

    private BuildingData _buildingData;
    private BuildingState _buildingState;

    private GameObject _inputMarker;
    private GameObject _outputMarker;
    private GameObject _inputProductionContainer;
    private GameObject _outputProductionContainer;
    
    // ==================== Public Properties ====================
    public BuildingData BuildingData => _buildingData;
    public BuildingState BuildingState => _buildingState;
    
    // ==================== Initialization ====================
    
    /// <summary>
    /// 건물 오브젝트를 초기화합니다.
    /// </summary>
    public void Initialize(BuildingData buildingData, BuildingState buildingState, GameObject inputMarkerPrefab, GameObject outputMarkerPrefab, BuildingGridHandler gridHandler)
    {
        _buildingData = buildingData;
        _buildingState = buildingState;
        
        // Input/Output 마커 생성
        CreateMarkers(inputMarkerPrefab, outputMarkerPrefab, gridHandler);
    }
    
    /// <summary>
    /// 프리뷰용으로 초기화합니다 (BuildingState 없이).
    /// </summary>
    public void InitializePreview(BuildingData buildingData, GameObject inputMarkerPrefab, GameObject outputMarkerPrefab)
    {
        _buildingData = buildingData;
        _buildingState = null;
        
        // 프리뷰용 마커 생성 (위치는 UpdatePreviewMarkers에서 설정)
        if (buildingData.InputPosition != Vector2Int.zero && inputMarkerPrefab != null)
        {
            _inputMarker = CreateMarkerAsChild("PreviewInput", inputMarkerPrefab);
        }
        
        if (buildingData.OutputPosition != Vector2Int.zero && outputMarkerPrefab != null)
        {
            _outputMarker = CreateMarkerAsChild("PreviewOutput", outputMarkerPrefab);
        }
    }
    
    // ==================== Marker Management ====================
    
    /// <summary>
    /// Input/Output 마커를 생성합니다.
    /// </summary>
    private void CreateMarkers(GameObject inputMarkerPrefab, GameObject outputMarkerPrefab, BuildingGridHandler gridHandler)
    {
        if (_buildingData == null || _buildingState == null || gridHandler == null)
            return;
        
        // Input 마커 생성
        if (_buildingData.InputPosition != Vector2Int.zero && inputMarkerPrefab != null)
        {
            _inputMarker = CreateMarkerAsChild("Input", inputMarkerPrefab);
            UpdateMarkerPosition(_inputMarker, _buildingState.inputPosition, gridHandler);
        }
        
        // Output 마커 생성
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
        if (prefab == null)
            return null;
        
        GameObject marker = Instantiate(prefab, transform);
        marker.name = $"IOMarker_{name}";
        
        // 부모의 스케일 영향을 제거하여 마커가 원래 크기로 보이도록 함
        GameObjectUtils.CompensateParentScale(marker.transform, transform);
        
        return marker;
    }
    
    /// <summary>
    /// 마커의 위치를 업데이트합니다.
    /// </summary>
    private void UpdateMarkerPosition(GameObject marker, Vector2Int gridPos, BuildingGridHandler gridHandler)
    {
        if (marker == null || gridHandler == null)
            return;
        
        Vector3 worldPos = gridHandler.GridToWorldPosition(gridPos, Vector2Int.one);
        marker.transform.position = new Vector3(worldPos.x, worldPos.y, 1.0f);
    }
    
    /// <summary>
    /// 프리뷰 마커의 위치를 업데이트합니다 (그리드 핸들러 필요).
    /// </summary>
    public void UpdatePreviewMarkers(Vector2Int buildingGridPos, BuildingGridHandler gridHandler, int rotation = 0)
    {
        if (_buildingData == null || gridHandler == null)
            return;
        
        // 회전된 Input/Output 상대 위치 계산 (건물 크기 고려)
        Vector2Int rotatedInputPos = RotatePositionAroundCenter(_buildingData.InputPosition, rotation, _buildingData.size);
        Vector2Int rotatedOutputPos = RotatePositionAroundCenter(_buildingData.OutputPosition, rotation, _buildingData.size);
        
        // Input/Output 절대 좌표 계산
        Vector2Int inputPos = buildingGridPos + rotatedInputPos;
        Vector2Int outputPos = buildingGridPos + rotatedOutputPos;
        
        // Input 마커 위치 업데이트
        if (_inputMarker != null && _buildingData.InputPosition != Vector2Int.zero)
        {
            Vector3 inputWorldPos = gridHandler.GridToWorldPosition(inputPos, Vector2Int.one);
            _inputMarker.transform.position = new Vector3(inputWorldPos.x, inputWorldPos.y, -0.5f);
        }
        
        // Output 마커 위치 업데이트
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
        
        // 건물 중심 계산
        float centerX = (buildingSize.x - 1) / 2f;
        float centerY = (buildingSize.y - 1) / 2f;
        
        // 중심 기준으로 변환
        float relX = pos.x - centerX;
        float relY = pos.y - centerY;
        
        // 회전 적용
        float rotatedX, rotatedY;
        switch (rotation)
        {
            case 1: // 90도 시계방향: (x, y) -> (-y, x)
                rotatedX = -relY;
                rotatedY = relX;
                break;
            case 2: // 180도: (x, y) -> (-x, -y)
                rotatedX = -relX;
                rotatedY = -relY;
                break;
            case 3: // 270도 시계방향: (x, y) -> (y, -x)
                rotatedX = relY;
                rotatedY = -relX;
                break;
            default:
                rotatedX = relX;
                rotatedY = relY;
                break;
        }
        
        // 다시 절대 좌표로 변환
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
        if (_inputMarker != null)
            _inputMarker.SetActive(active);
        
        if (_outputMarker != null)
            _outputMarker.SetActive(active);
    }
    
    // ==================== Production Icons ====================
    
    /// <summary>
    /// 건물 위에 입출력 자원 아이콘을 표시합니다.
    /// </summary>
    /// <param name="dataManager">게임 데이터 매니저</param>
    /// <param name="sharedCanvas">공용 World Space Canvas (성능 최적화)</param>
    public void SetupProductionIcons(GameDataManager dataManager, Transform sharedCanvas = null)
    {
        if (_buildingState == null || dataManager == null)
            return;
        
        // 기존 컨테이너 제거
        if (_inputProductionContainer != null)
        {
            Destroy(_inputProductionContainer);
            _inputProductionContainer = null;
        }
        
        if (_outputProductionContainer != null)
        {
            Destroy(_outputProductionContainer);
            _outputProductionContainer = null;
        }
        
        // GameManager 확인
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[BuildingObject] GameManager.Instance is null.");
            return;
        }
        
        if (sharedCanvas == null)
        {
            Debug.LogWarning("[BuildingObject] Shared canvas is null. Production icons will not be displayed.");
            return;
        }

        Dictionary<string, int> inputCounts = AggregateResourceCounts(_buildingState.inputProductionIds);
        Dictionary<string, int> outputCounts = AggregateResourceCounts(_buildingState.outputProductionIds);
        
        // 건물 크기 계산 (회전 고려)
        Vector2Int rotatedSize = GetRotatedSize(_buildingData.size, _buildingState.rotation);
        float buildingHeight = rotatedSize.y;
        
        // Input 자원 표시 (건물 중간 위)
        if (inputCounts.Count > 0)
        {
            // 위치 계산
            float yOffset = buildingHeight * _productionIconContentOffset;
            
            Vector3 worldPosition = transform.position + new Vector3(0, yOffset, -1);
            _inputProductionContainer = GameManager.Instance.CreateProductionIconContainerWithoutCanvas(
                sharedCanvas,
                $"InputIcons_{gameObject.name}",
                worldPosition
            );
            
            if (_inputProductionContainer != null)
            {
                // 아이콘들 생성 (GameManager 헬퍼 사용)
                GameManager.Instance.CreateProductionIcons(
                    _inputProductionContainer.transform, 
                    inputCounts, 
                    dataManager
                );
            }
        }
        
        // Output 자원 표시 (건물 중간 아래)
        if (outputCounts.Count > 0)
        {
            // 위치 계산
            float yOffset = -buildingHeight * _productionIconContentOffset;
            
            Vector3 worldPosition = transform.position + new Vector3(0, yOffset, -1);
            _outputProductionContainer = GameManager.Instance.CreateProductionIconContainerWithoutCanvas(
                sharedCanvas,
                $"OutputIcons_{gameObject.name}",
                worldPosition
            );
            
            if (_outputProductionContainer != null)
            {
                // 아이콘들 생성 (GameManager 헬퍼 사용)
                GameManager.Instance.CreateProductionIcons(
                    _outputProductionContainer.transform, 
                    outputCounts, 
                    dataManager
                );
            }
        }
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
    
    /// <summary>
    /// 회전에 따라 건물 크기를 계산합니다.
    /// </summary>
    private Vector2Int GetRotatedSize(Vector2Int size, int rotation)
    {
        rotation = rotation % 4;
        // 90도 또는 270도 회전 시 가로/세로 바뀜
        if (rotation == 1 || rotation == 3)
        {
            return new Vector2Int(size.y, size.x);
        }
        return size;
    }
    
    // ==================== Cleanup ====================
    
    void OnDestroy()
    {
        // 마커들과 컨테이너들은 자식 오브젝트이므로 자동으로 삭제됨
        if (_inputProductionContainer != null)
            Destroy(_inputProductionContainer);
        
        if (_outputProductionContainer != null)
            Destroy(_outputProductionContainer);
    }
}
