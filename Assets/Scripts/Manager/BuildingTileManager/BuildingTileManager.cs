using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 건물 타일 시스템의 메인 매니저 (조율자).
/// - 그리드, 배치/제거 모드, 임시 데이터 및 최종 저장 로직을 관리합니다.
/// </summary>
public class BuildingTileManager : MonoBehaviour, IGameSceneManager
{
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

    [Header("Animation Settings")]
    [SerializeField] private GameObject _resourceItemPrefab;

    private string _currentThreadId = "";
    private BoxCollider2D _cameraCollider;
    private Camera _mainCamera;
    private MainCameraController _mainCameraController;
    private dataManager _dataManager;
    private GameManager _gameManager;

    private BuildingGridHandler _gridGenHandler;
    private BuildingPlacementHandler _placementHandler;
    private BuildingRemovalHandler _removalHandler;
    private BuildingCalculateHandler _calculateHandler;
    private BuildingCaptureHandler _captureHandler;

    private GameObject _sharedProductionIconCanvas;

    private List<BuildingState> _tempBuildingStates = new List<BuildingState>();
    private bool _isTempDataDirty = false;
    private bool _handlersInitialized = false;
    private bool _isInitialized = false;

    public dataManager DataManager => _dataManager;
    public BuildingGridHandler GridGenHandler => _gridGenHandler;
    public BuildingPlacementHandler PlacementHandler => _placementHandler;
    public BuildingRemovalHandler RemovalHandler => _removalHandler;
    public BuildingCalculateHandler CalculateHandler => _calculateHandler;
    public BuildingCaptureHandler CaptureHandler => _captureHandler;
    public DesignUiManager DesignUiManager => _designUiManager;
    public Camera MainCamera => _mainCamera;
    public MainCameraController MainCameraController => _mainCameraController;

    public string CurrentThreadId => _currentThreadId;
    public bool IsPlacementMode => _placementHandler != null && _placementHandler.IsActive;
    public bool IsRemovalMode => _removalHandler != null && _removalHandler.IsActive;

    public GameObject GetInputMarkerPrefab() => _inputMarkerPrefab;
    public GameObject GetOutputMarkerPrefab() => _outputMarkerPrefab;

    void Update()
    {
        bool canUpdate = !string.IsNullOrEmpty(_currentThreadId) || IsPlacementMode || IsRemovalMode;
        
        if (!canUpdate)
            return;

        _placementHandler?.Update();
        
        if (!string.IsNullOrEmpty(_currentThreadId))
        {
            _removalHandler?.Update(_currentThreadId);
        }

        if (!IsPlacementMode && !IsRemovalMode && !string.IsNullOrEmpty(_currentThreadId))
        {
            HandleBuildingClick();
        }
    }

    public void OnInitialize(GameManager gameManager, dataManager dataManager)
    {
        _gameManager = gameManager;
        _dataManager = dataManager;
        _mainCamera = Camera.main;

        if (!_handlersInitialized)
        {
            InitializeHandlers();
            _handlersInitialized = true;
        }

        _currentThreadId = _gameManager.CurrentThreadId;

        if (!_isInitialized)
        {
            CreateGrid(_gridWidth, _gridHeight);
            SetPositionCenter();
            SetCameraCollider();
            CreateSharedProductionIconCanvas();
            LoadInitialThreadState();
            _isInitialized = true;
        }
        else
        {
            RefreshBuildings();
        }
    }

    private void InitializeHandlers()
    {
        _gridGenHandler = new BuildingGridHandler(this, _buildingTilePrefab, _buildingObjectPrefab, _inputMarkerPrefab, _outputMarkerPrefab, _gridWidth, _gridHeight);
        _placementHandler = new BuildingPlacementHandler(this, _buildingObjectPrefab);
        _removalHandler = new BuildingRemovalHandler(this);
        _calculateHandler = new BuildingCalculateHandler(this);
        _captureHandler = new BuildingCaptureHandler(this);
    }

    private void LoadInitialThreadState()
    {
        if (string.IsNullOrEmpty(_currentThreadId))
        {
            _tempBuildingStates = new List<BuildingState>();
            return;
        }

        if (_dataManager == null)
        {
            Debug.LogWarning("[BuildingTileManager] Cannot load initial thread state: DataManager is null.");
            _tempBuildingStates = new List<BuildingState>();
            return;
        }

        if (_dataManager.Thread == null)
        {
            Debug.LogWarning("[BuildingTileManager] Cannot load initial thread state: Thread handler is null.");
            _tempBuildingStates = new List<BuildingState>();
            return;
        }

        var buildingStates = _dataManager.Thread.GetBuildingStates(_currentThreadId);
        if (buildingStates != null)
        {
            _tempBuildingStates = new List<BuildingState>(buildingStates);
            RefreshBuildings();
            Debug.Log($"[BuildingTileManager] Initial thread data loaded and refreshed: {_currentThreadId} ({_tempBuildingStates.Count} buildings)");
        }
        else
        {
            // 스레드가 존재하지 않으면 빈 리스트로 초기화 (새 스레드)
            _tempBuildingStates = new List<BuildingState>();
            Debug.Log($"[BuildingTileManager] Thread '{_currentThreadId}' not found. Starting with empty building list.");
        }
    }

    private void CreateSharedProductionIconCanvas()
    {
        if (_sharedProductionIconCanvas != null)
            return;

        if (_gameManager == null)
        {
            Debug.LogWarning("[BuildingTileManager] GameManager is null. Cannot create shared production icon canvas.");
            return;
        }

        Camera targetCamera = _mainCamera ?? Camera.main;
        RectTransform canvasRect = _gameManager.GetWorldCanvas();

        if (canvasRect == null)
        {
            Debug.LogWarning("[BuildingTileManager] Failed to acquire shared production icon canvas from GameManager.");
            return;
        }

        _sharedProductionIconCanvas = canvasRect.gameObject;
    }

    public void SetPositionCenter()
    {
        transform.position = new Vector3(-_gridWidth / 2f, _gridHeight / 2f, 11);
    }

    public void SetCameraCollider()
    {
        _cameraCollider = GetComponent<BoxCollider2D>();
        if (_cameraCollider != null)
        {
            _cameraCollider.offset = new Vector2(_gridWidth / 2f, -_gridHeight / 2f);
            _cameraCollider.size = new Vector2(_gridWidth, _gridHeight);
        }
    }

    /// <summary> 그리드를 생성합니다. </summary>
    public void CreateGrid(int width, int height)
    {
        _gridWidth = width;
        _gridHeight = height;
        _gridGenHandler?.CreateGrid(width, height);
    }

    /// <summary> 그리드를 확장합니다. </summary>
    public void ExpandGrid(int newWidth, int newHeight)
    {
        _gridWidth = newWidth;
        _gridHeight = newHeight;
        _gridGenHandler?.ExpandGrid(newWidth, newHeight);
        SetPositionCenter();
        SetCameraCollider();
    }

    /// <summary> 건물 배치 모드를 시작합니다. </summary>
    public void StartPlacementMode(BuildingData buildingData)
    {
        if (IsRemovalMode) _removalHandler?.CancelRemoval();
        _placementHandler?.StartPlacement(buildingData);
    }

    /// <summary> 현재 편집 중인 Thread ID를 설정하고 데이터를 로드합니다. </summary>
    public void SetCurrentThread(string threadId)
    {
        if (string.IsNullOrEmpty(threadId)) return;
        if (_currentThreadId == threadId) return;

        // 이전 임시 데이터 버림
        if (_isTempDataDirty) Debug.Log($"[BuildingTileManager] Discarding temporary building data for thread: {_currentThreadId}");

        _currentThreadId = threadId;
        _isTempDataDirty = false;
        _tempBuildingStates.Clear();

        // GameDataManager에서 건물 상태를 임시 저장소로 복사
        var buildingStates = _dataManager.Thread.GetBuildingStates(threadId);
        if (buildingStates != null)
        {
            _tempBuildingStates = new List<BuildingState>(buildingStates);
        }

        RefreshBuildings();
    }

    /// <summary> 임시 저장된 건물 데이터를 DataManager에 반영합니다. </summary>
    public void ApplyTempBuildingDataToDataManager()
    {
        if (string.IsNullOrEmpty(_currentThreadId) || _dataManager == null || _dataManager.Thread == null) 
        {
            Debug.LogWarning($"[BuildingTileManager] Cannot apply temp building data: Thread ID or DataManager is null.");
            return;
        }

        // 스레드가 존재하는지 확인 (빈 스레드 저장 방지)
        if (!_dataManager.Thread.HasThread(_currentThreadId))
        {
            Debug.LogWarning($"[BuildingTileManager] Cannot apply temp building data: Thread '{_currentThreadId}' does not exist. Create thread first.");
            return;
        }

        if (!_isTempDataDirty && _tempBuildingStates.Count == (_dataManager.Thread.GetBuildingStates(_currentThreadId)?.Count ?? 0))
        {
            Debug.Log($"[BuildingTileManager] No changes to apply for thread: {_currentThreadId}");
            return;
        }

        _dataManager.Thread.OverwriteBuildings(_currentThreadId, _tempBuildingStates);

        _isTempDataDirty = false;
        Debug.Log($"[BuildingTileManager] Applied {_tempBuildingStates.Count} buildings to GameDataManager for thread: {_currentThreadId}");
    }

    /// <summary> 최종 저장 로직을 통합 관리합니다. (UI에서 호출) </summary>
    public void SaveThreadChanges(string threadName, string categoryId)
    {
        // 1. Thread ID 결정 및 생성/업데이트
        string newThreadId = _designUiManager.GetThreadIdFromTitle(threadName);
        if (_tempBuildingStates == null || _tempBuildingStates.Count == 0)
        {
            Debug.LogWarning($"[BuildingTileManager] Cannot save thread '{threadName}': No buildings to save. Thread will not be created.");
            return;
        }

        _dataManager.Thread.CreateThread(newThreadId, threadName);

        // 2. 임시 데이터를 DataManager에 반영
        string oldThreadId = _currentThreadId;
        _currentThreadId = newThreadId;
        ApplyTempBuildingDataToDataManager();
        _currentThreadId = newThreadId;

        // 3. 카테고리 적용
        if (!string.IsNullOrEmpty(categoryId))
        {
            _dataManager.Thread.AddThreadToCategory(categoryId, newThreadId);
        }

        // 4. 레이아웃 캡처 및 이미지 경로 업데이트
        string imagePath = _captureHandler?.CaptureThreadLayout(newThreadId);
        
        // 5. 유지비 계산 및 저장
        int totalMaintenance = CalculateTotalMaintenanceCost(newThreadId);
        
        // 6. 직원 요구사항 계산 및 저장
        CalculateAndSetEmployeeRequirements(newThreadId);
        
        ThreadState thread = _dataManager.Thread.GetThread(newThreadId);
        if (thread != null)
        {
            if (!string.IsNullOrEmpty(imagePath))
            {
                thread.previewImagePath = imagePath;
            }
            thread.totalMaintenanceCost = totalMaintenance;
            _dataManager.Thread.Save();
        }

        SetCurrentThread(newThreadId);

        Debug.Log($"[BuildingTileManager] Save operation completed for Thread: {newThreadId}");
    }

    /// <summary> 현재 스레드의 건물 상태를 반환합니다 (임시 저장소 우선). </summary>
    public List<BuildingState> GetCurrentBuildingStates()
    {
        if (string.IsNullOrEmpty(_currentThreadId))
        {
            return _tempBuildingStates ?? new List<BuildingState>();
        }

        if (_isTempDataDirty && _tempBuildingStates != null) return _tempBuildingStates;
        
        if (_dataManager?.Thread == null)
        {
            return _tempBuildingStates ?? new List<BuildingState>();
        }

        return _dataManager.Thread.GetBuildingStates(_currentThreadId) ?? new List<BuildingState>();
    }

    /// <summary> 임시 저장소에 건물을 추가합니다. </summary>
    internal void AddBuildingToTemp(BuildingState buildingState)
    {
        if (buildingState == null) return;
        _tempBuildingStates.RemoveAll(b => b.positionX == buildingState.positionX && b.positionY == buildingState.positionY);
        _tempBuildingStates.Add(buildingState);
        _isTempDataDirty = true;
    }

    /// <summary> 임시 저장소에서 건물을 제거합니다. </summary>
    internal bool RemoveBuildingFromTemp(Vector2Int position)
    {
        int removedCount = _tempBuildingStates.RemoveAll(b => b.positionX == position.x && b.positionY == position.y);
        if (removedCount > 0)
        {
            _isTempDataDirty = true;
            return true;
        }
        return false;
    }

    /// <summary> Thread의 건물들을 다시 로드하여 표시합니다. </summary>
    public void RefreshBuildings()
    {
        if (_gridGenHandler == null || _dataManager == null)
        {
            Debug.LogWarning("[BuildingTileManager] Cannot refresh buildings: GridGenHandler or DataManager is null.");
            return;
        }

        if (_dataManager.Building == null)
        {
            Debug.LogWarning("[BuildingTileManager] Cannot refresh buildings: Building handler is null.");
            return;
        }

        _gridGenHandler.ClearAllOccupiedTiles();
        _gridGenHandler.ClearAllPlacedBuildings();

        List<BuildingState> buildingStates = GetCurrentBuildingStates();

        if (buildingStates != null)
        {
            foreach (var buildingState in buildingStates)
            {
                if (buildingState == null || string.IsNullOrEmpty(buildingState.buildingId))
                {
                    Debug.LogWarning("[BuildingTileManager] Invalid building state found. Skipping.");
                    continue;
                }

                BuildingData buildingData = _dataManager.Building.GetBuildingData(buildingState.buildingId);
                if (buildingData != null)
                {
                    _gridGenHandler.CreateBuildingObject(new Vector2Int(buildingState.positionX, buildingState.positionY), buildingData, buildingState);
                    Vector2Int rotatedSize = GetRotatedSize(buildingData.size, buildingState.rotation);
                    _gridGenHandler.MarkTilesAsOccupied(new Vector2Int(buildingState.positionX, buildingState.positionY), rotatedSize);
                }
                else
                {
                    Debug.LogWarning($"[BuildingTileManager] Building data not found for ID: {buildingState.buildingId}. This building may have been removed.");
                }
            }
        }
    }

    /// <summary> 건물 클릭을 처리합니다. </summary>
    private void HandleBuildingClick()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mouseWorldPos = _mainCamera.ScreenToWorldPoint(Input.mousePosition);
            Vector2Int gridPos = _gridGenHandler.WorldToGridPosition(mouseWorldPos);
            GameObject clickedBuilding = _gridGenHandler.GetBuildingAtPosition(gridPos, _currentThreadId);

            if (clickedBuilding != null)
            {
                OnBuildingClicked(gridPos);
            }
        }
    }

    /// <summary> 건물이 클릭되었을 때 정보를 표시합니다. </summary>
    private void OnBuildingClicked(Vector2Int gridPos)
    {
        var buildingStates = GetCurrentBuildingStates();
        BuildingState clickedState = null;

        // 클릭된 영역 내의 건물 찾기
        foreach (var state in buildingStates)
        {
            BuildingData data = _dataManager.Building.GetBuildingData(state.buildingId);
            if (data != null)
            {
                Vector2Int rotatedSize = GetRotatedSize(data.size, state.rotation);
                if (gridPos.x >= state.positionX && gridPos.x < state.positionX + rotatedSize.x &&
                    gridPos.y >= state.positionY && gridPos.y < state.positionY + rotatedSize.y)
                {
                    clickedState = state;
                    break;
                }
            }
        }

        if (clickedState != null)
        {
            BuildingData buildingData = _dataManager.Building.GetBuildingData(clickedState.buildingId);
            if (buildingData != null)
            {
                _designUiManager?.ShowBuildingInfo(buildingData, clickedState);
            }
        }
    }

    /// <summary> 회전에 따라 건물 크기를 계산합니다. </summary>
    private Vector2Int GetRotatedSize(Vector2Int size, int rotation)
    {
        rotation %= 4;
        if (rotation == 1 || rotation == 3) return new Vector2Int(size.y, size.x);
        return size;
    }


    /// <summary> 유효한 생산 건물의 수를 계산합니다. </summary>
    /// <remarks>경로 탐색이 필요하므로 BuildingCalculateHandler 사용</remarks>
    public int CalculateCurrentThreadOutputs()
    {
        if (string.IsNullOrEmpty(_currentThreadId) || _calculateHandler == null) return 0;
        return _calculateHandler.CalculateThreadOutputs(_currentThreadId, GetCurrentBuildingStates());
    }

    /// <summary> Thread의 총 유지비를 계산합니다. </summary>
    public int CalculateTotalMaintenanceCost(string threadId)
    {
        if (_dataManager?.ThreadCalculate == null) return 0;
        var buildingStates = (threadId == _currentThreadId) ? GetCurrentBuildingStates() : _dataManager.Thread.GetBuildingStates(threadId);
        return _dataManager.ThreadCalculate.CalculateTotalMaintenanceCost(threadId, buildingStates);
    }

    /// <summary> Thread의 직원 요구사항을 계산하고 ThreadState에 저장합니다. </summary>
    public void CalculateAndSetEmployeeRequirements(string threadId)
    {
        ThreadState thread = _dataManager.Thread.GetThread(threadId);
        if (thread == null)
        {
            Debug.LogWarning($"[BuildingTileManager] Thread not found: {threadId}");
            return;
        }

        List<BuildingState> buildingStates = (threadId == _currentThreadId) ? GetCurrentBuildingStates() : _dataManager.Thread.GetBuildingStates(threadId);
        int totalEmployees = _dataManager.ThreadCalculate.CalculateRequiredEmployees(threadId, buildingStates);
        thread.requiredEmployees = totalEmployees;

        Debug.Log($"[BuildingTileManager] Employee requirements calculated for thread '{threadId}': Total Employees={totalEmployees}");
    }

    /// <summary> Thread의 입력 자원 ID 목록을 수집합니다. </summary>
    public List<string> CollectInputProductionIds(string threadId)
    {
        if (_dataManager?.ThreadCalculate == null) return new List<string>();
        var buildingStates = (threadId == _currentThreadId) ? GetCurrentBuildingStates() : _dataManager.Thread.GetBuildingStates(threadId);
        return _dataManager.ThreadCalculate.CollectInputProductionIds(threadId, buildingStates);
    }

    /// <summary> Thread의 출력 자원 ID 목록을 수집합니다. </summary>
    public List<string> CollectOutputProductionIds(string threadId)
    {
        if (_dataManager?.ThreadCalculate == null) return new List<string>();
        var buildingStates = (threadId == _currentThreadId) ? GetCurrentBuildingStates() : _dataManager.Thread.GetBuildingStates(threadId);
        return _dataManager.ThreadCalculate.CollectOutputProductionIds(threadId, buildingStates);
    }

    /// <summary> 생산 체인 연결 정보를 계산합니다. </summary>
    public void CalculateProductionChain(string threadId, out List<string> inputResourceIds, out Dictionary<string, int> inputResourceCounts, out List<string> outputResourceIds, out Dictionary<string, int> outputResourceCounts)
    {
        inputResourceIds = new List<string>();
        inputResourceCounts = new Dictionary<string, int>();
        outputResourceIds = new List<string>();
        outputResourceCounts = new Dictionary<string, int>();
        
        if (_dataManager?.ThreadCalculate == null) return;

        List<BuildingState> buildingStatesToUse = (threadId == _currentThreadId) ? GetCurrentBuildingStates() : _dataManager.Thread.GetBuildingStates(threadId);

        _dataManager.ThreadCalculate.CalculateProductionChain(threadId, buildingStatesToUse, out inputResourceIds, out inputResourceCounts, out outputResourceIds, out outputResourceCounts);
    }

    /// <summary> 스프라이트의 스케일을 타일 크기에 맞게 계산합니다. </summary>
    public Vector3 CalculateSpriteScale(Sprite sprite, Vector2Int targetSize)
    {
        if (sprite == null) return Vector3.one;
        float scaleX = targetSize.x / sprite.bounds.size.x;
        float scaleY = targetSize.y / sprite.bounds.size.y;
        return new Vector3(scaleX, scaleY, 1f);
    }
}