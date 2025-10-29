using UnityEngine;

/// <summary>
/// 건물 오브젝트를 나타내는 컴포넌트
/// 건물의 마커(Input/Output)를 관리합니다.
/// </summary>
public class BuildingObject : MonoBehaviour
{
    // ==================== Properties ====================
    [SerializeField] private BuildingData _buildingData;
    [SerializeField] private BuildingState _buildingState;
    
    private GameObject _inputMarker;
    private GameObject _outputMarker;
    
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
        if (buildingData.inputPosition != Vector2Int.zero && inputMarkerPrefab != null)
        {
            _inputMarker = CreateMarkerAsChild("PreviewInput", inputMarkerPrefab);
        }
        
        if (buildingData.outputPosition != Vector2Int.zero && outputMarkerPrefab != null)
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
        if (_buildingData.inputPosition != Vector2Int.zero && inputMarkerPrefab != null)
        {
            _inputMarker = CreateMarkerAsChild("Input", inputMarkerPrefab);
            UpdateMarkerPosition(_inputMarker, _buildingState.inputPosition, gridHandler);
        }
        
        // Output 마커 생성
        if (_buildingData.outputPosition != Vector2Int.zero && outputMarkerPrefab != null)
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
        Vector3 parentScale = transform.localScale;
        marker.transform.localScale = new Vector3(
            1f / parentScale.x,
            1f / parentScale.y,
            1f / parentScale.z
        );
        
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
        Vector2Int rotatedInputPos = RotatePositionAroundCenter(_buildingData.inputPosition, rotation, _buildingData.size);
        Vector2Int rotatedOutputPos = RotatePositionAroundCenter(_buildingData.outputPosition, rotation, _buildingData.size);
        
        // Input/Output 절대 좌표 계산
        Vector2Int inputPos = buildingGridPos + rotatedInputPos;
        Vector2Int outputPos = buildingGridPos + rotatedOutputPos;
        
        // Input 마커 위치 업데이트
        if (_inputMarker != null && _buildingData.inputPosition != Vector2Int.zero)
        {
            Vector3 inputWorldPos = gridHandler.GridToWorldPosition(inputPos, Vector2Int.one);
            _inputMarker.transform.position = new Vector3(inputWorldPos.x, inputWorldPos.y, -0.5f);
        }
        
        // Output 마커 위치 업데이트
        if (_outputMarker != null && _buildingData.outputPosition != Vector2Int.zero)
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
    
    // ==================== Cleanup ====================
    
    void OnDestroy()
    {
        // 마커들은 자식 오브젝트이므로 자동으로 삭제됨
    }
}
