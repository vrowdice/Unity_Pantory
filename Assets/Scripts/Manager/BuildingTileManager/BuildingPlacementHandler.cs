using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 건물 배치 모드를 처리하는 핸들러
/// </summary>
public class BuildingPlacementHandler
{
    private readonly BuildingTileManager _buildingTileManager;
    private readonly BuildingGridHandler _gridManager;
    private readonly GameDataManager _dataManager;
    private readonly Camera _mainCamera;

    private bool _isActive = false;
    private BuildingData _selectedBuilding = null;
    private GameObject _previewObject = null;
    private SpriteRenderer _previewRenderer = null;
    private GameObject _previewInputMarker = null;
    private GameObject _previewOutputMarker = null;
    private Vector2Int _currentGridPos;
    private bool _canPlace = false;

    // Preview Settings
    private Color _validColor = new Color(0, 1, 0, 0.5f);
    private Color _invalidColor = new Color(1, 0, 0, 0.5f);
    
    // Input/Output Marker Prefabs
    private GameObject _inputMarkerPrefab;
    private GameObject _outputMarkerPrefab;

    public bool IsActive => _isActive;
    public BuildingData SelectedBuilding => _selectedBuilding;

    public BuildingPlacementHandler(BuildingGridHandler gridManager, GameDataManager dataManager, Camera mainCamera, BuildingTileManager buildingTileManager)
    {
        _gridManager = gridManager;
        _dataManager = dataManager;
        _buildingTileManager = buildingTileManager;
        _mainCamera = mainCamera;
        
        // BuildingTileManager에서 Input/Output 마커 프리팹 가져오기
        _inputMarkerPrefab = _buildingTileManager.GetInputMarkerPrefab();
        _outputMarkerPrefab = _buildingTileManager.GetOutputMarkerPrefab();
    }

    /// <summary>
    /// Preview 색상을 설정합니다.
    /// </summary>
    public void SetColors(Color validColor, Color invalidColor)
    {
        _validColor = validColor;
        _invalidColor = invalidColor;
    }

    /// <summary>
    /// 배치 모드를 시작합니다.
    /// </summary>
    public void StartPlacement(BuildingData buildingData)
    {
        _isActive = true;
        _selectedBuilding = buildingData;
        
        CreatePreviewObject();
        _gridManager.SetAllTilesOutline(true, _validColor);  // 타일 윤곽선 표시
        Debug.Log($"[BuildingPlacementHandler] Placement mode started: {buildingData.displayName}");
    }

    /// <summary>
    /// 배치 모드를 취소합니다.
    /// </summary>
    public void CancelPlacement()
    {
        _isActive = false;
        _selectedBuilding = null;
        
        DestroyPreviewObject();
        _gridManager.SetAllTilesOutline(false);  // 타일 윤곽선 숨김
        Debug.Log("[BuildingPlacementHandler] Placement mode cancelled");
    }

    /// <summary>
    /// 배치 모드를 업데이트합니다 (매 프레임 호출).
    /// </summary>
    public void Update()
    {
        if (!_isActive)
            return;

        UpdatePreview();
        HandleInput();
    }

    /// <summary>
    /// 프리뷰를 업데이트합니다.
    /// </summary>
    private void UpdatePreview()
    {
        if (_previewObject == null || _selectedBuilding == null)
            return;

        // 마우스가 UI 위에 있는지 확인
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            // UI 위에 있으면 프리뷰 숨기기
            _previewRenderer.enabled = false;
            if (_previewInputMarker != null)
                _previewInputMarker.SetActive(false);
            if (_previewOutputMarker != null)
                _previewOutputMarker.SetActive(false);
            _canPlace = false;
            return;
        }

        // 프리뷰 다시 보이기
        _previewRenderer.enabled = true;
        if (_previewInputMarker != null)
            _previewInputMarker.SetActive(true);
        if (_previewOutputMarker != null)
            _previewOutputMarker.SetActive(true);

        // 마우스 위치를 월드 좌표로 변환
        Vector3 mouseWorldPos = _mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0;

        // 그리드 좌표로 변환
        Vector2Int gridPos = _gridManager.WorldToGridPosition(mouseWorldPos);
        _currentGridPos = gridPos;

        // 배치 가능 여부 체크
        _canPlace = _gridManager.CanPlaceBuilding(gridPos, _selectedBuilding.size);

        // 프리뷰 위치 및 색상 업데이트
        Vector3 worldPos = _gridManager.GridToWorldPosition(gridPos, _selectedBuilding.size);
        _previewObject.transform.position = worldPos;
        _previewRenderer.color = _canPlace ? _validColor : _invalidColor;
        
        // Input/Output 프리뷰 마커 위치 업데이트
        UpdatePreviewIOMarkers(gridPos);
    }

    /// <summary>
    /// 입력을 처리합니다.
    /// </summary>
    private void HandleInput()
    {
        // 왼쪽 클릭 - 건물 배치
        if (Input.GetMouseButtonDown(0) && _canPlace)
        {
            PlaceBuildingWithCurrentThread(_currentGridPos, _selectedBuilding);
        }

        // 오른쪽 클릭 또는 ESC - 취소
        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
        {
            CancelPlacement();
        }
    }

    /// <summary>
    /// 건물을 배치합니다 (현재 Thread에).
    /// </summary>
    private void PlaceBuildingWithCurrentThread(Vector2Int gridPos, BuildingData buildingData)
    {
        if (string.IsNullOrEmpty(_buildingTileManager.CurrentThreadId))
        {
            Debug.LogWarning("[BuildingPlacementHandler] Cannot place building: Thread ID not set");
            return;
        }

        PlaceBuildingWithData(gridPos, buildingData, _buildingTileManager.CurrentThreadId);
    }

    /// <summary>
    /// 건물을 배치하고 데이터에 추가합니다.
    /// </summary>
    public void PlaceBuildingWithData(Vector2Int gridPos, BuildingData buildingData, string currentThreadId)
    {
        if (!_gridManager.CanPlaceBuilding(gridPos, buildingData.size))
        {
            Debug.LogWarning("[BuildingPlacementHandler] Cannot place building at this position");
            return;
        }

        // BuildingState 생성 및 ThreadService에 추가 (BuildingData를 전달하여 절대 좌표 계산)
        BuildingState buildingState = new BuildingState(buildingData.id, gridPos, buildingData);
        if (_dataManager.AddBuildingToThread(currentThreadId, buildingState))
        {
            // 데이터만 추가하고, 실제 오브젝트는 RefreshBuildings로 생성
            _buildingTileManager.RefreshBuildings();
            
            Debug.Log($"[BuildingPlacementHandler] Building placed: {buildingData.displayName} at {gridPos}, Input: {buildingState.inputPosition}, Output: {buildingState.outputPosition}");
        }
    }

    /// <summary>
    /// 프리뷰 오브젝트를 생성합니다.
    /// </summary>
    private void CreatePreviewObject()
    {
        if (_selectedBuilding == null || _selectedBuilding.buildingSprite == null)
        {
            Debug.LogWarning("[BuildingPlacementHandler] Cannot create preview object");
            return;
        }

        _previewObject = new GameObject("BuildingPreview");
        _previewObject.transform.SetParent(_buildingTileManager.transform);
        _previewRenderer = _previewObject.AddComponent<SpriteRenderer>();
        _previewRenderer.sprite = _selectedBuilding.buildingSprite;
        _previewRenderer.sortingOrder = 0;
        
        // 프리뷰 크기를 타일 크기에 맞춤 (1타일 = 1유닛)
        Vector3 scale = _buildingTileManager.CalculateSpriteScale(_selectedBuilding.buildingSprite, _selectedBuilding.size);
        _previewObject.transform.localScale = scale;

        if(_selectedBuilding.inputPosition != Vector2Int.zero && _inputMarkerPrefab != null)
        {
            _previewInputMarker = CreatePreviewIOMarker("PreviewInput", _inputMarkerPrefab);
        }
        if(_selectedBuilding.outputPosition != Vector2Int.zero && _outputMarkerPrefab != null)
        {
            _previewOutputMarker = CreatePreviewIOMarker("PreviewOutput", _outputMarkerPrefab);
        }
    }

    /// <summary>
    /// 프리뷰 오브젝트를 삭제합니다.
    /// </summary>
    private void DestroyPreviewObject()
    {
        // 프리뷰 오브젝트만 삭제하면 자식인 마커도 함께 삭제됨
        if (_previewObject != null)
        {
            Object.Destroy(_previewObject);
            _previewObject = null;
            _previewRenderer = null;
            _previewInputMarker = null;
            _previewOutputMarker = null;
        }
    }

    /// <summary>
    /// 프리뷰용 Input/Output 마커를 생성합니다.
    /// </summary>
    private GameObject CreatePreviewIOMarker(string name, GameObject prefab)
    {
        GameObject marker = Object.Instantiate(prefab, _previewObject.transform);
        marker.name = name;
        
                // 부모의 스케일 영향을 제거하여 마커가 원래 크기로 보이도록 함
        Vector3 parentScale = _previewObject.transform.localScale;
        marker.transform.localScale = new Vector3(
            1f / parentScale.x,
            1f / parentScale.y,
            1f / parentScale.z
        );
        

        return marker;
    }

    /// <summary>
    /// 프리뷰 Input/Output 마커 위치를 업데이트합니다.
    /// </summary>
    private void UpdatePreviewIOMarkers(Vector2Int buildingGridPos)
    {
        if (_selectedBuilding == null)
            return;
            
        // Input/Output 절대 좌표 계산
        Vector2Int inputPos = buildingGridPos + _selectedBuilding.inputPosition;
        Vector2Int outputPos = buildingGridPos + _selectedBuilding.outputPosition;
        
        // Input 마커 위치 업데이트
        if (_previewInputMarker != null && _selectedBuilding.inputPosition != Vector2Int.zero)
        {
            Vector3 inputWorldPos = _gridManager.GridToWorldPosition(inputPos, Vector2Int.one);
            _previewInputMarker.transform.position = new Vector3(inputWorldPos.x, inputWorldPos.y, -0.5f);
        }
        
        // Output 마커 위치 업데이트
        if (_previewOutputMarker != null && _selectedBuilding.outputPosition != Vector2Int.zero)
        {
            Vector3 outputWorldPos = _gridManager.GridToWorldPosition(outputPos, Vector2Int.one);
            _previewOutputMarker.transform.position = new Vector3(outputWorldPos.x, outputWorldPos.y, -0.5f);
        }
    }
}

