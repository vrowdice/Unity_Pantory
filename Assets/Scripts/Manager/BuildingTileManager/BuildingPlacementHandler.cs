using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 건물 배치 모드를 처리하는 핸들러
/// </summary>
public class BuildingPlacementHandler
{
    // ==================== References ====================
    private readonly BuildingTileManager _buildingTileManager;
    private readonly BuildingGridHandler _gridHandler;
    private readonly GameDataManager _dataManager;
    private readonly MainCameraController _mainCameraController;
    private readonly DesignUiManager _designUiManager;

    // ==================== State ====================
    private bool _isActive = false;
    private bool _isDragging = false;
    private bool _canPlace = false;
    
    // ==================== Building Data ====================
    private BuildingData _selectedBuilding = null;
    private Vector2Int _currentGridPos;
    private Vector2Int _lastPlacedGridPos = new Vector2Int(int.MinValue, int.MinValue);
    private int _currentRotation = 0; // 0=0도, 1=90도, 2=180도, 3=270도
    
    // ==================== Preview Objects ====================
    private GameObject _previewObject = null;
    private SpriteRenderer _previewRenderer = null;
    private BuildingObject _previewBuildingComponent = null;
    
    // ==================== Prefabs ====================
    private GameObject _buildingObjectPrefab;
    private GameObject _inputMarkerPrefab;
    private GameObject _outputMarkerPrefab;
    
    // ==================== Colors ====================
    private Color _validColor = new Color(0, 1, 0, 0.5f);
    private Color _invalidColor = new Color(1, 0, 0, 0.5f);

    // ==================== Properties ====================
    public bool IsActive => _isActive;
    public BuildingData SelectedBuilding => _selectedBuilding;
    public int CurrentRotation => _currentRotation;

    // ==================== Constructor ====================
    public BuildingPlacementHandler(BuildingTileManager buildingTileManager, GameObject buildingObjectPrefab)
    {
        _buildingTileManager = buildingTileManager;
        _gridHandler = buildingTileManager.GridGenHandler;
        _dataManager = buildingTileManager.DataManager;
        _mainCameraController = buildingTileManager.MainCamera.GetComponent<MainCameraController>();
        _designUiManager = buildingTileManager.DesignUiManager;
        
        _buildingObjectPrefab = buildingObjectPrefab;
        _inputMarkerPrefab = _buildingTileManager.GetInputMarkerPrefab();
        _outputMarkerPrefab = _buildingTileManager.GetOutputMarkerPrefab();
    }

    // ==================== Public Methods ====================
    
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
        DestroyPreviewObject();

        _isActive = true;
        _selectedBuilding = buildingData;
        _currentRotation = 0; // 회전 초기화
        
        CreatePreviewObject();
        _gridHandler.SetAllTilesOutline(true, _validColor);  // 타일 윤곽선 표시
        
        _designUiManager.UpdateModeBtnImages(true, false);
        Debug.Log($"[BuildingPlacementHandler] Placement mode started: {buildingData.displayName}");
    }

    /// <summary>
    /// 배치 모드를 취소합니다.
    /// </summary>
    public void CancelPlacement()
    {
        _isActive = false;
        _selectedBuilding = null;
        _isDragging = false;
        _lastPlacedGridPos = new Vector2Int(int.MinValue, int.MinValue);
        _currentRotation = 0;
        
        DestroyPreviewObject();
        _gridHandler.SetAllTilesOutline(false);  // 타일 윤곽선 숨김
        
        _designUiManager.UpdateModeBtnImages(false, false);
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
    /// 건물을 왼쪽으로 회전합니다 (반시계방향, -90도).
    /// </summary>
    public void RotateLeft()
    {
        if (!_isActive || _selectedBuilding == null)
            return;

        _currentRotation = (_currentRotation + 3) % 4; // -1과 동일 (반시계)
        UpdatePreviewRotation();
        Debug.Log($"[BuildingPlacementHandler] Rotated left: {_currentRotation * 90} degrees");
    }

    /// <summary>
    /// 건물을 오른쪽으로 회전합니다 (시계방향, +90도).
    /// </summary>
    public void RotateRight()
    {
        if (!_isActive || _selectedBuilding == null)
            return;

        _currentRotation = (_currentRotation + 1) % 4;
        UpdatePreviewRotation();
        Debug.Log($"[BuildingPlacementHandler] Rotated right: {_currentRotation * 90} degrees");
    }

    // ==================== Private Methods ====================
    
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

    /// <summary>
    /// 프리뷰 회전을 업데이트합니다.
    /// </summary>
    private void UpdatePreviewRotation()
    {
        if (_previewObject == null || _selectedBuilding == null)
            return;

        // 회전 적용
        float angle = _currentRotation * 90f;
        _previewObject.transform.rotation = Quaternion.Euler(0, 0, -angle); // Unity는 반시계방향이 양수
        
        // 크기 재계산 (스프라이트 스케일은 회전에 영향 받지 않음)
        Vector2Int rotatedSize = GetRotatedSize(_selectedBuilding.size, _currentRotation);
        Vector3 scale = _buildingTileManager.CalculateSpriteScale(_selectedBuilding.buildingSprite, _selectedBuilding.size);
        _previewObject.transform.localScale = scale;
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
            if (_previewBuildingComponent != null)
                _previewBuildingComponent.SetMarkersActive(false);
            _canPlace = false;
            return;
        }

        // 프리뷰 다시 보이기
        _previewRenderer.enabled = true;
        if (_previewBuildingComponent != null)
            _previewBuildingComponent.SetMarkersActive(true);

        // 마우스 위치를 월드 좌표로 변환
        Vector3 mouseWorldPos = _mainCameraController.Camera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0;

        // 그리드 좌표로 변환
        Vector2Int gridPos = _gridHandler.WorldToGridPosition(mouseWorldPos);
        _currentGridPos = gridPos;

        // 회전된 크기로 배치 가능 여부 체크
        Vector2Int rotatedSize = GetRotatedSize(_selectedBuilding.size, _currentRotation);
        _canPlace = _gridHandler.CanPlaceBuilding(gridPos, rotatedSize);

        // 프리뷰 위치 및 색상 업데이트
        Vector3 worldPos = _gridHandler.GridToWorldPosition(gridPos, rotatedSize);
        _previewObject.transform.position = worldPos;
        _previewRenderer.color = _canPlace ? _validColor : _invalidColor;
        
        // Input/Output 프리뷰 마커 위치 업데이트 (BuildingObject를 통해)
        if (_previewBuildingComponent != null)
        {
            _previewBuildingComponent.UpdatePreviewMarkers(gridPos, _gridHandler, _currentRotation);
        }
    }

    /// <summary>
    /// 입력을 처리합니다.
    /// </summary>
    private void HandleInput()
    {
        // 마우스 버튼을 누르기 시작할 때
        if (Input.GetMouseButtonDown(0))
        {
            _isDragging = true;
            _lastPlacedGridPos = new Vector2Int(int.MinValue, int.MinValue); // 드래그 시작 시 초기화
        }

        // 마우스 버튼을 떼었을 때
        if (Input.GetMouseButtonUp(0))
        {
            _isDragging = false;
        }

        // 드래그 중이거나 클릭 시 건물 배치
        if (_isDragging && _canPlace)
        {
            // 현재 위치가 이전에 배치한 위치와 다를 때만 배치
            if (_currentGridPos != _lastPlacedGridPos)
            {
                PlaceBuildingWithCurrentThread(_currentGridPos, _selectedBuilding);
                _lastPlacedGridPos = _currentGridPos;
            }
        }

        // Q 키 - 왼쪽 회전 (반시계방향)
        if (Input.GetKeyDown(KeyCode.Q))
        {
            RotateLeft();
        }

        // E 키 - 오른쪽 회전 (시계방향)
        if (Input.GetKeyDown(KeyCode.E))
        {
            RotateRight();
        }

        // ESC - 취소
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CancelPlacement();
        }
    }

    /// <summary>
    /// 건물을 배치합니다 (현재 Thread에).
    /// </summary>
    private void PlaceBuildingWithCurrentThread(Vector2Int gridPos, BuildingData buildingData)
    {
        // 스레드 ID가 없으면 자동으로 설정 시도
        if (string.IsNullOrEmpty(_buildingTileManager.CurrentThreadId))
        {
            _buildingTileManager.EnsureThreadId();
        }

        string threadId = _buildingTileManager.CurrentThreadId;
        if (string.IsNullOrEmpty(threadId))
        {
            Debug.LogWarning("[BuildingPlacementHandler] Cannot place building: Thread ID could not be set");
            return;
        }

        PlaceBuildingWithData(gridPos, buildingData, threadId, _currentRotation);
    }

    /// <summary>
    /// 건물을 배치하고 임시 저장소에 추가합니다.
    /// </summary>
    public void PlaceBuildingWithData(Vector2Int gridPos, BuildingData buildingData, string currentThreadId, int rotation = 0)
    {
        Vector2Int rotatedSize = GetRotatedSize(buildingData.size, rotation);
        
        if (!_gridHandler.CanPlaceBuilding(gridPos, rotatedSize))
        {
            Debug.LogWarning("[BuildingPlacementHandler] Cannot place building at this position");
            return;
        }

        // BuildingState 생성 및 임시 저장소에 추가 (회전 정보 포함)
        BuildingState buildingState = new BuildingState(buildingData.id, gridPos, buildingData, rotation);
        _buildingTileManager.AddBuildingToTemp(buildingState);
        
        // 데이터만 추가하고, 실제 오브젝트는 RefreshBuildings로 생성
        _buildingTileManager.RefreshBuildings();
        
        Debug.Log($"[BuildingPlacementHandler] Building placed (temp): {buildingData.displayName} at {gridPos}, Rotation: {rotation * 90}°, Input: {buildingState.inputPosition}, Output: {buildingState.outputPosition}");
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

        // Prefab이 있으면 사용, 없으면 새로 생성
        if (_buildingObjectPrefab != null)
        {
            _previewObject = Object.Instantiate(_buildingObjectPrefab, _buildingTileManager.transform);
            _previewObject.name = "BuildingPreview";
        }
        else
        {
            _previewObject = new GameObject("BuildingPreview");
            _previewObject.transform.SetParent(_buildingTileManager.transform);
        }
        
        // SpriteRenderer 설정
        _previewRenderer = _previewObject.GetComponent<SpriteRenderer>();
        if (_previewRenderer == null)
            _previewRenderer = _previewObject.AddComponent<SpriteRenderer>();
            
        _previewRenderer.sprite = _selectedBuilding.buildingSprite;
        _previewRenderer.sortingOrder = 0;
        
        // 프리뷰 크기를 타일 크기에 맞춤 (1타일 = 1유닛)
        Vector3 scale = _buildingTileManager.CalculateSpriteScale(_selectedBuilding.buildingSprite, _selectedBuilding.size);
        _previewObject.transform.localScale = scale;

        // BuildingObject 컴포넌트 추가 및 프리뷰 초기화
        _previewBuildingComponent = _previewObject.GetComponent<BuildingObject>();
        if (_previewBuildingComponent == null)
            _previewBuildingComponent = _previewObject.AddComponent<BuildingObject>();
        
        _previewBuildingComponent.InitializePreview(_selectedBuilding, _inputMarkerPrefab, _outputMarkerPrefab);
        
        // 초기 회전 적용
        UpdatePreviewRotation();
    }

    /// <summary>
    /// 프리뷰 오브젝트를 삭제합니다.
    /// </summary>
    private void DestroyPreviewObject()
    {
        // 프리뷰 오브젝트만 삭제하면 BuildingObject의 자식인 마커도 함께 삭제됨
        if (_previewObject != null)
        {
            Object.Destroy(_previewObject);
            _previewObject = null;
            _previewRenderer = null;
            _previewBuildingComponent = null;
        }
    }
}

