using System.Collections.Generic;
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
    
    // 공용 World Space Canvas
    private GameObject _sharedProductionIconCanvas;
    
    // 임시 저장: 현재 편집 중인 스레드의 건물 상태를 임시로 저장 (실제 저장 시 GameDataManager에 반영)
    private List<BuildingState> _tempBuildingStates = new List<BuildingState>();
    private bool _isTempDataDirty = false; // 임시 데이터가 변경되었는지 여부
    
    // ==================== Public 프로퍼티 ====================
    public GameDataManager DataManager => _dataManager;
    public MainCameraController MainCameraController => _mainCameraController;
    public BuildingGridHandler GridGenHandler => _gridGenHandler;
    public BuildingPlacementHandler PlacementHandler => _placementHandler;
    public BuildingRemovalHandler RemovalHandler => _removalHandler;
    public DesignUiManager DesignUiManager => _designUiManager;
    public Transform SharedProductionIconCanvas => _sharedProductionIconCanvas?.transform;
    
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
        CreateSharedProductionIconCanvas();
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

        // 같은 스레드로 설정하는 경우는 건너뜀 (임시 데이터 유지)
        if (_currentThreadId == threadId)
        {
            Debug.Log($"[BuildingTileManager] Thread ID unchanged: {threadId}");
            return;
        }

        // 이전 스레드의 임시 데이터가 있으면 저장하지 않고 버림 (편집 취소)
        if (_isTempDataDirty)
        {
            Debug.Log($"[BuildingTileManager] Discarding temporary building data for thread: {_currentThreadId}");
        }

        _currentThreadId = threadId;
        _isTempDataDirty = false;
        _tempBuildingStates.Clear();

        // GameDataManager에서 현재 스레드의 건물 상태를 임시 저장소로 복사 (스레드가 없으면 빈 리스트)
        var buildingStates = _dataManager.GetBuildingStates(threadId);
        if (buildingStates != null)
        {
            _tempBuildingStates = new List<BuildingState>(buildingStates);
        }
        else
        {
            // 스레드가 없으면 빈 리스트로 시작 (저장 시 생성됨)
            _tempBuildingStates = new List<BuildingState>();
        }

        Debug.Log($"[BuildingTileManager] Current thread set to: {threadId} (loaded {_tempBuildingStates.Count} buildings)");

        RefreshBuildings();
    }

    /// <summary>
    /// 현재 Thread ID를 반환합니다.
    /// </summary>
    public string GetCurrentThreadId()
    {
        return _currentThreadId;
    }

    /// <summary>
    /// 임시 저장된 건물 데이터를 GameDataManager에 반영합니다.
    /// 저장 시 호출됩니다.
    /// </summary>
    public void ApplyTempBuildingDataToDataManager()
    {
        if (string.IsNullOrEmpty(_currentThreadId) || _dataManager == null)
        {
            Debug.LogWarning("[BuildingTileManager] Cannot apply temp data: Thread ID is empty or DataManager is null");
            return;
        }

        // 임시 데이터가 없으면 저장할 필요 없음
        if (_tempBuildingStates == null || _tempBuildingStates.Count == 0)
        {
            if (_isTempDataDirty)
            {
                // 임시 데이터가 비어있고 dirty 상태면 모든 건물을 제거
                var existingBuildingsToClear = _dataManager.GetBuildingStates(_currentThreadId);
                if (existingBuildingsToClear != null)
                {
                    foreach (var building in existingBuildingsToClear)
                    {
                        _dataManager.RemoveBuildingFromThread(_currentThreadId, building.position);
                    }
                }
                _isTempDataDirty = false;
                Debug.Log($"[BuildingTileManager] Cleared all buildings for thread: {_currentThreadId}");
            }
            return;
        }

        // GameDataManager의 기존 건물 상태를 모두 제거
        var existingBuildings = _dataManager.GetBuildingStates(_currentThreadId);
        if (existingBuildings != null)
        {
            foreach (var building in existingBuildings)
            {
                _dataManager.RemoveBuildingFromThread(_currentThreadId, building.position);
            }
        }

        // 임시 저장된 건물 상태를 GameDataManager에 추가
        foreach (var buildingState in _tempBuildingStates)
        {
            _dataManager.AddBuildingToThread(_currentThreadId, buildingState);
        }

        _isTempDataDirty = false;
        Debug.Log($"[BuildingTileManager] Applied {_tempBuildingStates.Count} buildings to GameDataManager for thread: {_currentThreadId}");
    }

    /// <summary>
    /// 임시 저장소에 건물을 추가합니다.
    /// </summary>
    internal void AddBuildingToTemp(BuildingState buildingState)
    {
        if (buildingState == null)
            return;

        // 중복 확인 (같은 위치에 건물이 있으면 제거)
        _tempBuildingStates.RemoveAll(b => b.position == buildingState.position);
        
        _tempBuildingStates.Add(buildingState);
        _isTempDataDirty = true;
    }

    /// <summary>
    /// 임시 저장소에서 건물을 제거합니다.
    /// </summary>
    internal bool RemoveBuildingFromTemp(Vector2Int position)
    {
        int removedCount = _tempBuildingStates.RemoveAll(b => b.position == position);
        if (removedCount > 0)
        {
            _isTempDataDirty = true;
            return true;
        }
        return false;
    }

    /// <summary>
    /// 임시 저장소의 건물 상태 리스트를 반환합니다.
    /// </summary>
    internal List<BuildingState> GetTempBuildingStates()
    {
        return _tempBuildingStates;
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
    /// 모든 건물이 공유할 World Space Canvas를 생성합니다.
    /// </summary>
    private void CreateSharedProductionIconCanvas()
    {
        _sharedProductionIconCanvas = new GameObject("SharedProductionIconCanvas");
        _sharedProductionIconCanvas.transform.SetParent(transform);
        _sharedProductionIconCanvas.transform.localPosition = Vector3.zero;
        _sharedProductionIconCanvas.transform.localRotation = Quaternion.identity;
        _sharedProductionIconCanvas.transform.localScale = Vector3.one;

        // Canvas 설정 (World Space)
        Canvas canvas = _sharedProductionIconCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 1; // 건물 위에 표시

        // CanvasScaler 추가
        UnityEngine.UI.CanvasScaler scaler = _sharedProductionIconCanvas.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 100;

        // CanvasGroup 추가 - 클릭 무시 설정
        UnityEngine.CanvasGroup canvasGroup = _sharedProductionIconCanvas.AddComponent<UnityEngine.CanvasGroup>();
        canvasGroup.interactable = false;      // 상호작용 불가
        canvasGroup.blocksRaycasts = false;    // 레이캐스트 차단 안 함

        // RectTransform 설정
        RectTransform rectTransform = _sharedProductionIconCanvas.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.sizeDelta = new Vector2(1000, 1000); // 큰 크기로 설정
        }

        Debug.Log("[BuildingTileManager] Shared Production Icon Canvas created (raycast ignored).");
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
                // Thread ID만 설정 (스레드는 저장 시 생성됨)
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
    /// 임시 저장된 데이터가 있으면 임시 데이터를 사용하고, 없으면 GameDataManager에서 로드합니다.
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

        // 임시 저장된 데이터가 있으면 임시 데이터 사용, 없으면 GameDataManager에서 로드
        List<BuildingState> buildingStates = null;
        if (_isTempDataDirty && _tempBuildingStates != null && _tempBuildingStates.Count > 0)
        {
            buildingStates = _tempBuildingStates;
        }
        else
        {
            buildingStates = _dataManager.GetBuildingStates(_currentThreadId);
        }

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
    /// 현재 Thread의 건물 레이아웃을 이미지로 캡처합니다.
    /// </summary>
    /// <param name="threadId">캡처할 Thread ID</param>
    /// <returns>이미지 파일 경로 (실패 시 null)</returns>
    public string CaptureThreadLayout(string threadId)
    {
        if (string.IsNullOrEmpty(threadId))
        {
            Debug.LogWarning("[BuildingTileManager] Cannot capture layout: Thread ID is empty");
            return null;
        }

        if (_mainCamera == null)
        {
            Debug.LogWarning("[BuildingTileManager] Cannot capture layout: Main camera is null");
            return null;
        }

        try
        {
            // 건물이 있는 영역 계산 (임시 저장소 우선 사용)
            List<BuildingState> buildingStates = null;
            if (_isTempDataDirty && _tempBuildingStates != null && _tempBuildingStates.Count > 0 && threadId == _currentThreadId)
            {
                buildingStates = _tempBuildingStates;
            }
            else
            {
                buildingStates = _dataManager?.GetBuildingStates(threadId);
            }

            if (buildingStates == null || buildingStates.Count == 0)
            {
                Debug.LogWarning("[BuildingTileManager] No buildings to capture");
                return null;
            }

            // 건물들의 경계 계산
            int minX = int.MaxValue, minY = int.MaxValue;
            int maxX = int.MinValue, maxY = int.MinValue;

            foreach (var buildingState in buildingStates)
            {
                BuildingData buildingData = _dataManager.GetBuildingData(buildingState.buildingId);
                if (buildingData != null)
                {
                    Vector2Int rotatedSize = GetRotatedSize(buildingData.size, buildingState.rotation);
                    minX = Mathf.Min(minX, buildingState.position.x);
                    minY = Mathf.Min(minY, buildingState.position.y);
                    maxX = Mathf.Max(maxX, buildingState.position.x + rotatedSize.x);
                    maxY = Mathf.Max(maxY, buildingState.position.y + rotatedSize.y);
                }
            }

            // 경계에 여유 공간 추가
            int padding = 2;
            minX -= padding;
            minY -= padding;
            maxX += padding;
            maxY += padding;

            int gridWidth = maxX - minX;
            int gridHeight = maxY - minY;

            // RenderTexture 생성 (그리드 크기에 맞춤, 최소 512x512)
            int width = Mathf.Max(512, gridWidth * 64); // 타일당 64픽셀
            int height = Mathf.Max(512, gridHeight * 64);
            RenderTexture renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            
            // 카메라의 원래 설정 백업
            RenderTexture originalRT = _mainCamera.targetTexture;
            CameraClearFlags originalClearFlags = _mainCamera.clearFlags;
            Color originalBackgroundColor = _mainCamera.backgroundColor;
            float originalOrthographicSize = _mainCamera.orthographicSize;
            Vector3 originalPosition = _mainCamera.transform.position;

            // 카메라 설정 변경 - 그리드 전체가 보이도록
            _mainCamera.targetTexture = renderTexture;
            _mainCamera.clearFlags = CameraClearFlags.SolidColor;
            _mainCamera.backgroundColor = Color.white;
            
            // 그리드 중심 계산
            float centerX = (minX + maxX) / 2f;
            float centerY = (minY + maxY) / 2f;
            
            // 카메라 위치 조정 (그리드 중심으로)
            _mainCamera.transform.position = new Vector3(centerX, -centerY, originalPosition.z);
            
            // Orthographic 크기 조정 (전체 그리드가 보이도록)
            float requiredSize = Mathf.Max(gridWidth, gridHeight) / 2f + padding;
            _mainCamera.orthographicSize = requiredSize;

            // 렌더링
            _mainCamera.Render();

            // RenderTexture에서 Texture2D로 변환
            RenderTexture.active = renderTexture;
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            texture.Apply();

            // 카메라 설정 복원
            _mainCamera.targetTexture = originalRT;
            _mainCamera.clearFlags = originalClearFlags;
            _mainCamera.backgroundColor = originalBackgroundColor;
            _mainCamera.orthographicSize = originalOrthographicSize;
            _mainCamera.transform.position = originalPosition;
            RenderTexture.active = null;

            // 이미지를 PNG로 저장
            byte[] imageBytes = texture.EncodeToPNG();
            string fileName = $"ThreadPreview_{threadId}.png";
            string filePath = System.IO.Path.Combine(Application.persistentDataPath, fileName);
            
            System.IO.File.WriteAllBytes(filePath, imageBytes);

            // 리소스 정리
            Destroy(texture);
            renderTexture.Release();
            Destroy(renderTexture);

            Debug.Log($"[BuildingTileManager] Thread layout captured: {filePath} (Size: {gridWidth}x{gridHeight})");
            return filePath;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[BuildingTileManager] Failed to capture thread layout: {e.Message}");
            return null;
        }
    }

    /// <summary>
    /// 현재 스레드의 유효한 생산 건물 수(간이 산출량)를 계산합니다.
    /// </summary>
    public int CalculateCurrentThreadOutputs()
    {
        if (string.IsNullOrEmpty(_currentThreadId) || _calculateHandler == null)
            return 0;

        // 임시 저장소의 건물 상태를 사용 (임시 저장소가 있으면 우선 사용)
        List<BuildingState> buildingStatesToUse = null;
        if (_isTempDataDirty && _tempBuildingStates != null && _tempBuildingStates.Count > 0)
        {
            buildingStatesToUse = _tempBuildingStates;
        }
        else
        {
            buildingStatesToUse = _dataManager.GetBuildingStates(_currentThreadId);
        }

        return _calculateHandler.CalculateThreadOutputs(_currentThreadId, buildingStatesToUse);
    }

    /// <summary>
    /// Thread의 총 유지비를 계산합니다.
    /// </summary>
    public int CalculateTotalMaintenanceCost(string threadId)
    {
        if (_calculateHandler == null)
            return 0;

        return _calculateHandler.CalculateTotalMaintenanceCost(threadId);
    }

    /// <summary>
    /// Thread의 입력 생산 자원 리스트를 수집합니다.
    /// </summary>
    public List<string> CollectInputProductionIds(string threadId)
    {
        if (_calculateHandler == null)
            return new List<string>();

        return _calculateHandler.CollectInputProductionIds(threadId);
    }

    /// <summary>
    /// Thread의 출력 생산 자원 리스트를 수집합니다.
    /// </summary>
    public List<string> CollectOutputProductionIds(string threadId)
    {
        if (_calculateHandler == null)
            return new List<string>();

        return _calculateHandler.CollectOutputProductionIds(threadId);
    }

    /// <summary>
    /// 생산 체인을 추적하여 하역소에서 시작하는 입력 자원과 상역소까지 연결된 최종 출력 자원을 계산합니다.
    /// </summary>
    public void CalculateProductionChain(string threadId, out List<string> inputResourceIds, out List<string> outputResourceIds, out Dictionary<string, int> outputResourceCounts)
    {
        inputResourceIds = new List<string>();
        outputResourceIds = new List<string>();
        outputResourceCounts = new Dictionary<string, int>();

        if (_calculateHandler == null)
            return;

        // 임시 저장소의 건물 상태를 사용 (임시 저장소가 있으면 우선 사용)
        List<BuildingState> buildingStatesToUse = null;
        if (_isTempDataDirty && _tempBuildingStates != null && _tempBuildingStates.Count > 0 && threadId == _currentThreadId)
        {
            buildingStatesToUse = _tempBuildingStates;
        }
        else
        {
            buildingStatesToUse = _dataManager.GetBuildingStates(threadId);
        }

        _calculateHandler.CalculateProductionChain(threadId, buildingStatesToUse, out inputResourceIds, out outputResourceIds, out outputResourceCounts);
    }

    /// <summary>
    /// 현재 스레드의 건물 상태를 반환합니다 (임시 저장소 우선).
    /// </summary>
    public List<BuildingState> GetCurrentBuildingStates()
    {
        if (_isTempDataDirty && _tempBuildingStates != null && _tempBuildingStates.Count > 0)
        {
            return _tempBuildingStates;
        }
        return _dataManager.GetBuildingStates(_currentThreadId);
    }

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

