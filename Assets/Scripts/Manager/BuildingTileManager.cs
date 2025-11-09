using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 건물 타일 시스템의 메인 매니저 (조율자).
/// - 그리드, 배치/제거 모드, 임시 데이터 및 최종 저장 로직을 관리합니다.
/// </summary>
public class BuildingTileManager : MonoBehaviour
{
    #region 인스펙터 설정

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

    #endregion

    #region Private 변수 및 핸들러

    private string _currentThreadId = "";
    private BoxCollider2D _cameraCollider;
    private Camera _mainCamera;
    private GameDataManager _dataManager;
    private VisualManager _visualManager;

    // 핸들러들
    private BuildingGridHandler _gridGenHandler;
    private BuildingPlacementHandler _placementHandler;
    private BuildingRemovalHandler _removalHandler;
    private BuildingCalculateHandler _calculateHandler;

    // 공용 World Space Canvas
    private GameObject _sharedProductionIconCanvas;

    // 임시 저장 상태
    private List<BuildingState> _tempBuildingStates = new List<BuildingState>();
    private bool _isTempDataDirty = false; // 임시 데이터 변경 여부

    #endregion

    #region Public 프로퍼티

    public GameDataManager DataManager => _dataManager;
    public BuildingGridHandler GridGenHandler => _gridGenHandler;
    public BuildingPlacementHandler PlacementHandler => _placementHandler;
    public BuildingRemovalHandler RemovalHandler => _removalHandler;
    public DesignUiManager DesignUiManager => _designUiManager;
    public Transform SharedProductionIconCanvas => _sharedProductionIconCanvas?.transform;
    public Camera MainCamera => _mainCamera;

    public string CurrentThreadId => _currentThreadId;
    public bool IsPlacementMode => _placementHandler != null && _placementHandler.IsActive;
    public bool IsRemovalMode => _removalHandler != null && _removalHandler.IsActive;

    #endregion

    //---------------------------------------------------------

    #region Unity 생명주기

    void Start()
    {
        InitializeReferences();
        InitializeHandlers();
        InitializeThread();

        CreateGrid(_gridWidth, _gridHeight);
        SetPositionCenter();
        SetCameraCollider();
        CreateSharedProductionIconCanvas();

        // 초기 스레드 로드
        if (!string.IsNullOrEmpty(_currentThreadId))
        {
            var buildingStates = _dataManager.GetBuildingStates(_currentThreadId);
            if (buildingStates != null)
            {
                _tempBuildingStates = new List<BuildingState>(buildingStates);
            }
            RefreshBuildings();
            Debug.Log($"[BuildingTileManager] Initial thread data loaded and refreshed: {_currentThreadId} ({_tempBuildingStates.Count} buildings)");
        }
    }

    void Update()
    {
        if (string.IsNullOrEmpty(_currentThreadId))
            return;

        _placementHandler?.Update();
        _removalHandler?.Update(_currentThreadId);

        // 일반 클릭 처리
        if (!IsPlacementMode && !IsRemovalMode)
        {
            HandleBuildingClick();
        }
    }

    #endregion

    //---------------------------------------------------------

    #region 초기화 메서드

    private void InitializeReferences()
    {
        if (Camera.main == null)
        {
            Debug.LogError("[BuildingTileManager] Main camera not found.");
            return;
        }

        _mainCamera = Camera.main;
        _mainCamera.TryGetComponent(out MainCameraController mainCameraController);
        _dataManager = GameDataManager.Instance;
        _visualManager = VisualManager.Instance;
    }

    private void InitializeHandlers()
    {
        _gridGenHandler = new BuildingGridHandler(this, _buildingTilePrefab, _buildingObjectPrefab, _inputMarkerPrefab, _outputMarkerPrefab, _gridWidth, _gridHeight);
        _placementHandler = new BuildingPlacementHandler(this, _buildingObjectPrefab);
        _removalHandler = new BuildingRemovalHandler(this);
        _calculateHandler = new BuildingCalculateHandler(this);

        if (_visualManager != null)
        {
            _placementHandler.SetColors(_visualManager.ValidColor, _visualManager.InvalidColor);
            _removalHandler.SetColor(_visualManager.ValidColor, _visualManager.InvalidColor);
        }
    }

    private void InitializeThread()
    {
        if (GameManager.Instance != null && !string.IsNullOrEmpty(GameManager.Instance.CurrentThreadId))
        {
            _currentThreadId = GameManager.Instance.CurrentThreadId;
        }
        else
        {
            Debug.LogWarning("[BuildingTileManager] GameManager.Instance or CurrentThreadId is null/empty. Starting with an empty thread.");
        }
    }

    private void CreateSharedProductionIconCanvas()
    {
        _sharedProductionIconCanvas = new GameObject("SharedProductionIconCanvas");
        _sharedProductionIconCanvas.transform.SetParent(transform);
        // ... Canvas 설정 로직 (World Space, Raycast 무시)
        Canvas canvas = _sharedProductionIconCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 1;
        _sharedProductionIconCanvas.AddComponent<CanvasScaler>().dynamicPixelsPerUnit = 100;
        CanvasGroup canvasGroup = _sharedProductionIconCanvas.AddComponent<CanvasGroup>();
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        RectTransform rectTransform = _sharedProductionIconCanvas.GetComponent<RectTransform>();
        if (rectTransform != null) rectTransform.sizeDelta = new Vector2(1000, 1000);

        Debug.Log("[BuildingTileManager] Shared Production Icon Canvas created (raycast ignored).");
    }

    #endregion

    //---------------------------------------------------------

    #region 그리드 및 카메라 관리

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

    /// <summary> 특정 좌표의 타일을 반환합니다. </summary>
    public GameObject GetThreadTile(Vector2Int position)
    {
        return _gridGenHandler?.GetTile(position);
    }

    /// <summary> 타일이 존재하는지 확인합니다. </summary>
    public bool HasThreadTile(Vector2Int position)
    {
        return _gridGenHandler?.HasTile(position) ?? false;
    }

    #endregion

    //---------------------------------------------------------

    #region 건물 배치 및 제거 모드 제어

    /// <summary> 건물 배치 모드를 시작합니다. </summary>
    public void StartPlacementMode(BuildingData buildingData)
    {
        if (string.IsNullOrEmpty(_currentThreadId)) return;
        if (IsRemovalMode) _removalHandler?.CancelRemoval();
        _placementHandler?.StartPlacement(buildingData);
    }

    /// <summary> 건물 배치 모드를 취소합니다. </summary>
    public void CancelPlacementMode()
    {
        _placementHandler?.CancelPlacement();
    }

    /// <summary> 건물을 왼쪽으로 회전합니다. </summary>
    public void RotateBuildingLeft()
    {
        if (IsPlacementMode) _placementHandler.RotateLeft();
    }

    /// <summary> 건물을 오른쪽으로 회전합니다. </summary>
    public void RotateBuildingRight()
    {
        if (IsPlacementMode) _placementHandler.RotateRight();
    }

    /// <summary> 건물 제거 모드를 시작합니다. </summary>
    public void StartRemovalMode()
    {
        if (IsPlacementMode) _placementHandler?.CancelPlacement();
        _removalHandler?.StartRemoval();
    }

    /// <summary> 건물 제거 모드를 취소합니다. </summary>
    public void CancelRemovalMode()
    {
        _removalHandler?.CancelRemoval();
    }

    #endregion

    //---------------------------------------------------------

    #region 스레드 및 임시 데이터 관리 (Temp-First Logic)

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
        var buildingStates = _dataManager?.GetBuildingStates(threadId);
        if (buildingStates != null)
        {
            _tempBuildingStates = new List<BuildingState>(buildingStates);
        }

        Debug.Log($"[BuildingTileManager] Current thread set to: {threadId} (loaded {_tempBuildingStates.Count} buildings)");
        RefreshBuildings();
    }

    /// <summary> 임시 저장된 건물 데이터를 DataManager에 반영합니다. </summary>
    public void ApplyTempBuildingDataToDataManager()
    {
        if (string.IsNullOrEmpty(_currentThreadId) || _dataManager == null) return;

        if (!_isTempDataDirty && _tempBuildingStates.Count == (_dataManager.GetBuildingStates(_currentThreadId)?.Count ?? 0))
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
        if (_dataManager == null) return;

        // 1. Thread ID 결정 및 생성/업데이트
        string newThreadId = _designUiManager.GetThreadIdFromTitle(threadName);
        _dataManager.CreateThread(newThreadId, threadName);

        // 2. 임시 데이터를 DataManager에 반영
        string oldThreadId = _currentThreadId;
        _currentThreadId = newThreadId; // 저장 대상을 새 ID로 변경
        ApplyTempBuildingDataToDataManager(); // 임시 데이터가 newThreadId에 저장됨
        _currentThreadId = newThreadId; // CurrentThreadId 확정

        // 3. 카테고리 적용
        if (!string.IsNullOrEmpty(categoryId))
        {
            _dataManager.AddThreadToCategory(categoryId, newThreadId);
        }

        // 4. 레이아웃 캡처 및 이미지 경로 업데이트
        string imagePath = CaptureThreadLayout(newThreadId);
        if (!string.IsNullOrEmpty(imagePath))
        {
            ThreadState thread = _dataManager.GetThread(newThreadId);
            if (thread != null)
            {
                // ThreadState의 필드를 직접 수정하는 것은 ThreadDataHandler의 캡슐화를 위반할 수 있으므로,
                // ThreadDataHandler에 해당 필드를 업데이트하는 메서드가 있어야 하지만,
                // 현재는 편의상 직접 접근하거나, ThreadDataHandler가 저장을 트리거하는 것으로 가정합니다.
                thread.previewImagePath = imagePath;
            }
        }

        // 5. GameManager 및 화면 갱신
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetCurrentThreadId(newThreadId);
        }
        SetCurrentThread(newThreadId); // 임시 데이터 초기화 및 로드/갱신

        Debug.Log($"[BuildingTileManager] Save operation completed for Thread: {newThreadId}");
    }

    /// <summary> 현재 스레드의 건물 상태를 반환합니다 (임시 저장소 우선). </summary>
    public List<BuildingState> GetCurrentBuildingStates()
    {
        if (_isTempDataDirty && _tempBuildingStates != null) return _tempBuildingStates;
        return _dataManager.GetBuildingStates(_currentThreadId) ?? new List<BuildingState>();
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

    #endregion

    //---------------------------------------------------------

    #region 건물 렌더링 및 클릭 이벤트

    /// <summary> Thread의 건물들을 다시 로드하여 표시합니다. </summary>
    public void RefreshBuildings()
    {
        if (_gridGenHandler == null || _dataManager == null) return;

        _gridGenHandler.ClearAllOccupiedTiles();
        _gridGenHandler.ClearAllPlacedBuildings();

        List<BuildingState> buildingStates = GetCurrentBuildingStates();

        if (buildingStates != null)
        {
            foreach (var buildingState in buildingStates)
            {
                BuildingData buildingData = _dataManager.GetBuildingData(buildingState.buildingId);
                if (buildingData != null)
                {
                    _gridGenHandler.CreateBuildingObject(new Vector2Int(buildingState.positionX, buildingState.positionY), buildingData, buildingState);
                    Vector2Int rotatedSize = GetRotatedSize(buildingData.size, buildingState.rotation);
                    _gridGenHandler.MarkTilesAsOccupied(new Vector2Int(buildingState.positionX, buildingState.positionY), rotatedSize);
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
            BuildingData data = _dataManager?.GetBuildingData(state.buildingId);
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
            BuildingData buildingData = _dataManager.GetBuildingData(clickedState.buildingId);
            if (buildingData != null)
            {
                _designUiManager?.ShowBuildingInfo(buildingData, clickedState);
            }
        }
    }

    #endregion

    //---------------------------------------------------------

    #region 계산 및 유틸리티 메서드

    /// <summary> 회전에 따라 건물 크기를 계산합니다. </summary>
    private Vector2Int GetRotatedSize(Vector2Int size, int rotation)
    {
        rotation %= 4;
        if (rotation == 1 || rotation == 3) return new Vector2Int(size.y, size.x);
        return size;
    }

    /// <summary> Input 마커 프리팹을 반환합니다. </summary>
    public GameObject GetInputMarkerPrefab() => _inputMarkerPrefab;

    /// <summary> Output 마커 프리팹을 반환합니다. </summary>
    public GameObject GetOutputMarkerPrefab() => _outputMarkerPrefab;

    /// <summary> 현재 Thread의 건물 레이아웃을 이미지로 캡처합니다. </summary>
    public string CaptureThreadLayout(string threadId)
    {
        if (string.IsNullOrEmpty(threadId) || _mainCamera == null) return null;

        // 경계 계산에 사용할 건물 목록 로드
        List<BuildingState> buildingStates = (threadId == _currentThreadId) ? GetCurrentBuildingStates() : _dataManager?.GetBuildingStates(threadId);
        if (buildingStates == null || buildingStates.Count == 0) return null;

        // 경계 계산
        int minX = int.MaxValue, minY = int.MaxValue;
        int maxX = int.MinValue, maxY = int.MinValue;
        foreach (var buildingState in buildingStates)
        {
            BuildingData buildingData = _dataManager.GetBuildingData(buildingState.buildingId);
            if (buildingData != null)
            {
                Vector2Int rotatedSize = GetRotatedSize(buildingData.size, buildingState.rotation);
                minX = Mathf.Min(minX, buildingState.positionX);
                minY = Mathf.Min(minY, buildingState.positionY);
                maxX = Mathf.Max(maxX, buildingState.positionX + rotatedSize.x);
                maxY = Mathf.Max(maxY, buildingState.positionY + rotatedSize.y);
            }
        }
        
        // 유효한 경계가 없는 경우 (모든 건물이 1x1이고 겹칠 때 min/max가 같을 수 있음)를 대비
        if (minX == int.MaxValue) return null;

        // 경계 및 패딩 설정
        int padding = 2;
        int gridWidth = (maxX + padding) - (minX - padding);
        int gridHeight = (maxY + padding) - (minY - padding);
        
        // 중앙 계산
        float centerX = (minX + maxX) / 2f;
        float centerY = (minY + maxY) / 2f;

        // 캡처 설정
        // 렌더 텍스처 설정: 알파 채널 포함 (RGBA)
        int width = Mathf.Max(512, gridWidth * 64);
        int height = Mathf.Max(512, gridHeight * 64);
        RenderTexture renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);

        // --- 카메라 상태 백업 ---
        RenderTexture originalRT = _mainCamera.targetTexture;
        CameraClearFlags originalClearFlags = _mainCamera.clearFlags;
        Color originalBackgroundColor = _mainCamera.backgroundColor;
        float originalOrthographicSize = _mainCamera.orthographicSize;
        Vector3 originalPosition = _mainCamera.transform.position;
        
        try
        {
            // --- 카메라 캡처 설정 ---
            _mainCamera.targetTexture = renderTexture;
            _mainCamera.clearFlags = CameraClearFlags.Color; // SolidColor 대신 Color 사용 (Unity 5.x 이후)
            _mainCamera.backgroundColor = new Color(0, 0, 0, 0); // 투명한 배경 설정
            
            // NOTE: 투명도 캡처를 위해 Layer나 DepthOnly를 사용해야 할 수 있으나, 
            // 가장 간단한 방법은 ClearFlags를 사용하고 ARGB32 포맷을 사용하는 것입니다.

            // 카메라 위치 및 줌 조정
            _mainCamera.transform.position = new Vector3(centerX, -centerY, originalPosition.z); // Z 위치 유지
            _mainCamera.orthographicSize = Mathf.Max(gridWidth / _mainCamera.aspect, gridHeight) / 2f + padding;

            _mainCamera.Render();

            // --- 캡처 및 파일 저장 ---
            RenderTexture.active = renderTexture;
            // TextureFormat.ARGB32 또는 RGBA32 사용 (알파 채널 지원)
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false); 
            texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            texture.Apply();
            
            byte[] imageBytes = texture.EncodeToPNG(); // PNG는 알파 채널 지원
            string filePath = System.IO.Path.Combine(Application.persistentDataPath, $"ThreadPreview_{threadId}.png");
            System.IO.File.WriteAllBytes(filePath, imageBytes);

            Debug.Log($"[BuildingTileManager] Thread layout captured: {filePath} (Size: {gridWidth}x{gridHeight})");
            return filePath;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[BuildingTileManager] Failed to capture thread layout: {e.Message}");
            return null;
        }
        finally
        {
            // --- 카메라 상태 복원 및 리소스 정리 ---
            _mainCamera.targetTexture = originalRT;
            _mainCamera.clearFlags = originalClearFlags;
            _mainCamera.backgroundColor = originalBackgroundColor;
            _mainCamera.orthographicSize = originalOrthographicSize;
            _mainCamera.transform.position = originalPosition;
            RenderTexture.active = null;

            if (renderTexture != null)
            {
                renderTexture.Release();
                Destroy(renderTexture);
            }
        }
    }

    /// <summary> 유효한 생산 건물의 수를 계산합니다. </summary>
    public int CalculateCurrentThreadOutputs()
    {
        if (string.IsNullOrEmpty(_currentThreadId) || _calculateHandler == null) return 0;
        return _calculateHandler.CalculateThreadOutputs(_currentThreadId, GetCurrentBuildingStates());
    }

    /// <summary> Thread의 총 유지비를 계산합니다. </summary>
    public int CalculateTotalMaintenanceCost(string threadId)
    {
        return _calculateHandler?.CalculateTotalMaintenanceCost(threadId) ?? 0;
    }

    /// <summary> Thread의 입력 자원 ID 목록을 수집합니다. </summary>
    public List<string> CollectInputProductionIds(string threadId)
    {
        return _calculateHandler?.CollectInputProductionIds(threadId) ?? new List<string>();
    }

    /// <summary> Thread의 출력 자원 ID 목록을 수집합니다. </summary>
    public List<string> CollectOutputProductionIds(string threadId)
    {
        return _calculateHandler?.CollectOutputProductionIds(threadId) ?? new List<string>();
    }

    /// <summary> 생산 체인 연결 정보를 계산합니다. </summary>
    public void CalculateProductionChain(string threadId, out List<string> inputResourceIds, out List<string> outputResourceIds, out Dictionary<string, int> outputResourceCounts)
    {
        inputResourceIds = new List<string>(); outputResourceIds = new List<string>(); outputResourceCounts = new Dictionary<string, int>();
        if (_calculateHandler == null) return;

        List<BuildingState> buildingStatesToUse = (threadId == _currentThreadId) ? GetCurrentBuildingStates() : _dataManager?.GetBuildingStates(threadId);

        _calculateHandler.CalculateProductionChain(threadId, buildingStatesToUse, out inputResourceIds, out outputResourceIds, out outputResourceCounts);
    }

    /// <summary> 스프라이트의 스케일을 타일 크기에 맞게 계산합니다. </summary>
    public Vector3 CalculateSpriteScale(Sprite sprite, Vector2Int targetSize)
    {
        if (sprite == null) return Vector3.one;

        float scaleX = targetSize.x / sprite.bounds.size.x;
        float scaleY = targetSize.y / sprite.bounds.size.y;

        return new Vector3(scaleX, scaleY, 1f);
    }

    #endregion
}