using UnityEngine;

/// <summary>
/// 건물 타일 시스템의 메인 매니저 (조율자)
/// </summary>
public class BuildingTileManager : MonoBehaviour
{
    // ==================== Inspector 설정 ====================
    [Header("UI Manager")]
    [SerializeField] private DesignUiManager _designUiManager;
    
    [Header("Prefab")]
    [SerializeField] private GameObject _buildingTilePrefab;
    [SerializeField] private GameObject _buildingObjectPrefab;
    [SerializeField] private GameObject _inputMarkerPrefab;
    [SerializeField] private GameObject _outputMarkerPrefab;
    
    [Header("Grid Settings")]
    [SerializeField] private int _gridWidth = 10;
    [SerializeField] private int _gridHeight = 10;

    // ==================== Private 변수 ====================
    private string _currentThreadId = "";
    private BoxCollider2D _cameraCollider;
    private Camera _mainCamera;
    private GameDataManager _dataManager;
    
    // 핸들러들
    private MainCameraController _mainCameraController;
    private BuildingGridHandler _gridGenHandler;
    private BuildingPlacementHandler _placementHandler;
    private BuildingRemovalHandler _removalHandler;
    private BuildingCalculateHandler _calculateHandler;
    private VisualManager _visualManager;
    
    // ==================== Public 프로퍼티 ====================
    public GameDataManager DataManager => _dataManager;
    public MainCameraController MainCameraController => _mainCameraController;
    public BuildingGridHandler GridGenHandler => _gridGenHandler;
    public BuildingPlacementHandler PlacementHandler => _placementHandler;
    public BuildingRemovalHandler RemovalHandler => _removalHandler;
    public DesignUiManager DesignUiManager => _designUiManager;
    
    public string CurrentThreadId => _currentThreadId;
    public bool IsPlacementMode => _placementHandler != null && _placementHandler.IsActive;
    public bool IsRemovalMode => _removalHandler != null && _removalHandler.IsActive;

    // ==================== Unity Lifecicle ====================
    void Awake()
    {
        InitializeReferences();
        InitializeHandlers();
        InitializeThread();
    }

    void Start()
    {
        CreateGrid(_gridWidth, _gridHeight);
        SetPositionCenter();
        SetCameraCollider();
    }

    void Update()
    {
        if (string.IsNullOrEmpty(_currentThreadId))
            return;

        _placementHandler?.Update();
        _removalHandler?.Update(_currentThreadId);
        
        // 건물 클릭 감지 (배치 모드나 제거 모드가 아닐 때만)
        if (!IsPlacementMode && !IsRemovalMode)
        {
            HandleBuildingClick();
        }
    }

    // ================== 그리드 관리 ==================

    public void SetPositionCenter()
    {
        transform.position = new Vector3(-_gridWidth / 2, _gridHeight / 2, 11);
    }

    public void SetCameraCollider()
    {
        _cameraCollider = GetComponent<BoxCollider2D>();
        _cameraCollider.offset = new Vector2(_gridWidth / 2, -_gridHeight / 2);
        _cameraCollider.size = new Vector2(_gridWidth, _gridHeight);
    }

    /// <summary>
    /// 그리드를 생성합니다.
    /// </summary>
    public void CreateGrid(int width, int height)
    {
        _gridWidth = width;
        _gridHeight = height;
        _gridGenHandler?.CreateGrid(width, height);
    }

    /// <summary>
    /// 그리드를 확장합니다.
    /// </summary>
    public void ExpandGrid(int newWidth, int newHeight)
    {
        _gridWidth = newWidth;
        _gridHeight = newHeight;
        _gridGenHandler?.ExpandGrid(newWidth, newHeight);
        SetPositionCenter();
        SetCameraCollider();
    }

    /// <summary>
    /// 특정 좌표의 타일을 반환합니다.
    /// </summary>
    public GameObject GetThreadTile(Vector2Int position)
    {
        return _gridGenHandler?.GetTile(position);
    }

    /// <summary>
    /// 타일이 존재하는지 확인합니다.
    /// </summary>
    public bool HasThreadTile(Vector2Int position)
    {
        return _gridGenHandler?.HasTile(position) ?? false;
    }

    // ================== 건물 배치 모드 ==================

    /// <summary>
    /// 건물 배치 모드를 시작합니다.
    /// </summary>
    public void StartPlacementMode(BuildingData buildingData)
    {
        if (string.IsNullOrEmpty(_currentThreadId))
        {
            Debug.LogWarning("[BuildingTileManager] Cannot start placement mode: Thread ID not set");
            return;
        }

        // 제거 모드가 활성화되어 있으면 취소
        if (IsRemovalMode)
        {
            _removalHandler?.CancelRemoval();
        }

        _placementHandler?.StartPlacement(buildingData);
    }

    /// <summary>
    /// 건물 배치 모드를 취소합니다.
    /// </summary>
    public void CancelPlacementMode()
    {
        _placementHandler?.CancelPlacement();
    }

    /// <summary>
    /// 건물을 왼쪽으로 회전합니다 (반시계방향).
    /// </summary>
    public void RotateBuildingLeft()
    {
        if (_placementHandler != null && _placementHandler.IsActive)
        {
            _placementHandler.RotateLeft();
        }
    }

    /// <summary>
    /// 건물을 오른쪽으로 회전합니다 (시계방향).
    /// </summary>
    public void RotateBuildingRight()
    {
        if (_placementHandler != null && _placementHandler.IsActive)
        {
            _placementHandler.RotateRight();
        }
    }

    // ================== 건물 제거 모드 ==================

    /// <summary>
    /// 건물 제거 모드를 시작합니다.
    /// </summary>
    public void StartRemovalMode()
    {
        // 배치 모드가 활성화되어 있으면 취소
        if (IsPlacementMode)
        {
            _placementHandler?.CancelPlacement();
        }

        _removalHandler?.StartRemoval();
    }

    /// <summary>
    /// 건물 제거 모드를 취소합니다.
    /// </summary>
    public void CancelRemovalMode()
    {
        _removalHandler?.CancelRemoval();
    }

    // ================== Thread 관리 ==================
    
    /// <summary>
    /// 현재 편집 중인 Thread ID를 설정합니다.
    /// </summary>
    public void SetCurrentThread(string threadId)
    {
        if (string.IsNullOrEmpty(threadId))
        {
            Debug.LogWarning("[BuildingTileManager] Cannot set empty thread ID");
            return;
        }

        _currentThreadId = threadId;
        Debug.Log($"[BuildingTileManager] Current thread set to: {threadId}");

        RefreshBuildings();
    }

    /// <summary>
    /// 현재 Thread ID를 반환합니다.
    /// </summary>
    public string GetCurrentThreadId()
    {
        return _currentThreadId;
    }

    // ================== 초기화 메서드 ====================
    
    /// <summary>
    /// 참조들을 초기화합니다.
    /// </summary>
    private void InitializeReferences()
    {
        _mainCameraController = Camera.main.GetComponent<MainCameraController>();
        _mainCamera = _mainCameraController.Camera;
        _dataManager = GameDataManager.Instance;
        _visualManager = VisualManager.Instance;
    }

    /// <summary>
    /// 핸들러들을 초기화합니다.
    /// </summary>
    private void InitializeHandlers()
    {
        _gridGenHandler = new BuildingGridHandler(this, _buildingTilePrefab, _buildingObjectPrefab, _inputMarkerPrefab, _outputMarkerPrefab, _gridWidth, _gridHeight);
        _placementHandler = new BuildingPlacementHandler(this, _buildingObjectPrefab);
        _removalHandler = new BuildingRemovalHandler(this);
        _calculateHandler = new BuildingCalculateHandler(this);

        // 색상 설정
        _placementHandler.SetColors(_visualManager.ValidColor, _visualManager.InvalidColor);
        _removalHandler.SetColor(_visualManager.ValidColor, _visualManager.InvalidColor);
    }

    /// <summary>
    /// Thread를 초기화합니다.
    /// </summary>
    private void InitializeThread()
    {
        if (GameManager.Instance != null)
        {
            string threadId = GameManager.Instance.CurrentThreadId;

            if (!string.IsNullOrEmpty(threadId))
            {
                // Thread가 없으면 생성
                if (_dataManager != null && !_dataManager.HasThread(threadId))
                {
                    string threadTitle = ExtractThreadTitle(threadId);
                    _dataManager.CreateThread(threadId, threadTitle, "생산부");
                    Debug.Log($"[BuildingTileManager] Created thread: {threadId} ({threadTitle})");
                }

                _currentThreadId = threadId;
                Debug.Log($"[BuildingTileManager] Initialized with thread: {threadId}");
            }
            else
            {
                Debug.LogWarning("[BuildingTileManager] GameManager.CurrentThreadId is empty");
            }
        }
        else
        {
            Debug.LogWarning("[BuildingTileManager] GameManager.Instance is null");
        }
    }

    /// <summary>
    /// Thread ID에서 제목을 추출합니다.
    /// </summary>
    private string ExtractThreadTitle(string threadId)
    {
        // "thread_" 접두사 제거
        if (threadId.StartsWith("thread_"))
        {
            string title = threadId.Substring(7); // "thread_" 길이만큼 제거
            // 언더스코어를 공백으로 변환
            title = title.Replace("_", " ");
            return title;
        }

        return threadId;
    }

    // ================== 건물 렌더링 ====================
    
    /// <summary>
    /// Thread의 건물들을 다시 로드하여 표시합니다.
    /// </summary>
    public void RefreshBuildings()
    {
        if (_gridGenHandler == null || _dataManager == null)
            return;

        // 모든 타일 점유 상태 초기화
        foreach (var tile in _gridGenHandler.BuildingTiles.Values)
        {
            var buildingTile = tile.GetComponent<BuildingTile>();
            if (buildingTile != null)
            {
                buildingTile.SetOccupied(false);
            }
        }

        // 기존 건물 오브젝트 제거 (마커도 자식으로 함께 제거됨)
        foreach (var building in _gridGenHandler.PlacedBuildings.Values)
        {
            if (building != null)
                Destroy(building);
        }
        _gridGenHandler.PlacedBuildings.Clear();

        // ThreadService에서 건물 데이터 가져와서 표시
        var buildingStates = _dataManager.GetBuildingStates(_currentThreadId);
        if (buildingStates != null)
        {
            foreach (var buildingState in buildingStates)
            {
                BuildingData buildingData = _dataManager.GetBuildingData(buildingState.buildingId);
                if (buildingData != null)
                {
                    // BuildingObject가 마커를 자동으로 생성하도록 buildingState 전달
                    GameObject buildingObj = _gridGenHandler.CreateBuildingObject(buildingState.position, buildingData, buildingState);
                    
                    // 회전된 크기로 타일 점유 처리
                    Vector2Int rotatedSize = GetRotatedSize(buildingData.size, buildingState.rotation);
                    _gridGenHandler.MarkTilesAsOccupied(buildingState.position, rotatedSize);
                }
            }
        }
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

    // ================== 유틸리티 ====================
    
    /// <summary>
    /// Input/Output 마커 프리팹을 반환합니다.
    /// </summary>
    public GameObject GetInputMarkerPrefab() => _inputMarkerPrefab;
    public GameObject GetOutputMarkerPrefab() => _outputMarkerPrefab;

    /// <summary>
    /// 스프라이트를 타일 크기에 맞게 스케일을 계산합니다.
    /// </summary>
    public Vector3 CalculateSpriteScale(Sprite sprite, Vector2Int targetSize)
    {
        if (sprite == null)
            return Vector3.one;

        float spriteWidth = sprite.bounds.size.x;
        float spriteHeight = sprite.bounds.size.y;

        float targetWidth = targetSize.x;
        float targetHeight = targetSize.y;

        float scaleX = targetWidth / spriteWidth;
        float scaleY = targetHeight / spriteHeight;

        return new Vector3(scaleX, scaleY, 1f);
    }

    // ================== 건물 클릭 처리 ====================
    
    /// <summary>
    /// 건물 클릭을 처리합니다.
    /// </summary>
    private void HandleBuildingClick()
    {
        // UI 위에 마우스가 있으면 무시
        if (UnityEngine.EventSystems.EventSystem.current != null && 
            UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }
        
        // 왼쪽 클릭 시
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mouseWorldPos = _mainCamera.ScreenToWorldPoint(Input.mousePosition);
            mouseWorldPos.z = 0;
            
            Vector2Int gridPos = _gridGenHandler.WorldToGridPosition(mouseWorldPos);
            GameObject clickedBuilding = _gridGenHandler.GetBuildingAtPosition(gridPos, _currentThreadId);
            
            if (clickedBuilding != null)
            {
                OnBuildingClicked(gridPos);
            }
        }
    }
    
    /// <summary>
    /// 건물이 클릭되었을 때 호출됩니다.
    /// </summary>
    private void OnBuildingClicked(Vector2Int gridPos)
    {
        // 해당 위치의 건물 데이터 찾기
        var buildingStates = _dataManager.GetBuildingStates(_currentThreadId);
        if (buildingStates != null)
        {
            BuildingState clickedState = buildingStates.Find(b => b.position == gridPos);
            if (clickedState == null)
            {
                // 정확한 위치가 아니면 건물이 차지하는 영역 내에서 찾기
                foreach (var state in buildingStates)
                {
                    BuildingData data = _dataManager.GetBuildingData(state.buildingId);
                    if (data != null)
                    {
                        // 회전된 크기 고려
                        Vector2Int rotatedSize = GetRotatedSize(data.size, state.rotation);
                        
                        if (gridPos.x >= state.position.x && gridPos.x < state.position.x + rotatedSize.x &&
                            gridPos.y >= state.position.y && gridPos.y < state.position.y + rotatedSize.y)
                        {
                            clickedState = state;
                            break;
                        }
                    }
                }
            }
            
            if (clickedState != null)
            {
                BuildingData buildingData = _dataManager.GetBuildingData(clickedState.buildingId);
                if (buildingData != null)
                {
                    _designUiManager?.ShowBuildingInfo(buildingData, clickedState);
                }
            }
        }
    }
}

