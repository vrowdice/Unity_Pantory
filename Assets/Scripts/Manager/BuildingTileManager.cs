using UnityEngine;

/// <summary>
/// 건물 타일 시스템의 메인 매니저 (조율자)
/// </summary>
public class BuildingTileManager : MonoBehaviour
{
    [SerializeField] private GameObject _inputMarkerPrefab;
    [SerializeField] private GameObject _outputMarkerPrefab;

    [SerializeField] private GameObject _buildingTilePrefab;
    [SerializeField] private GameObject _buildingObjectPrefab;  // 건물 표시용 프리팹

    [SerializeField] private int _gridWidth = 10;
    [SerializeField] private int _gridHeight = 10;
    private string _currentThreadId = "";  // 현재 편집 중인 Thread ID (DesignUiManager에서 설정됨)

    private BoxCollider2D _cameraCollider;
    private Camera _mainCamera;
    private GameDataManager _dataManager;

    // 핸들러들
    private BuildingGridHandler _gridGenHandler;
    private BuildingPlacementHandler _placementHandler;
    private BuildingRemovalHandler _removalHandler;

    // 모드 상태 프로퍼티
    public bool IsPlacementMode => _placementHandler != null && _placementHandler.IsActive;
    public bool IsRemovalMode => _removalHandler != null && _removalHandler.IsActive;
    public string CurrentThreadId => _currentThreadId;

    [Header("Preview Settings")]
    [SerializeField] private Color _validColor = new Color(0, 1, 0, 0.2f);    // 배치 가능 (초록)
    [SerializeField] private Color _invalidColor = new Color(1, 0, 0, 0.2f);  // 배치 불가 (빨강)

    void Awake()
    {
        _mainCamera = Camera.main;
        _dataManager = GameDataManager.Instance;

        // 핸들러 초기화
        _gridGenHandler = new BuildingGridHandler(transform, _buildingTilePrefab, _dataManager, this, _gridWidth, _gridHeight);
        _placementHandler = new BuildingPlacementHandler(_gridGenHandler, _dataManager, _mainCamera, this);
        _removalHandler = new BuildingRemovalHandler(_gridGenHandler, _dataManager, _mainCamera, this);

        // 색상 설정
        _placementHandler.SetColors(_validColor, _invalidColor);
        _removalHandler.SetColor(_validColor, _invalidColor);

        // GameManager에서 현재 Thread ID 가져오기
        if (GameManager.Instance != null)
        {
            string threadId = GameManager.Instance.CurrentThreadId;

            if (!string.IsNullOrEmpty(threadId))
            {
                // Thread가 없으면 생성
                if (_dataManager != null && !_dataManager.HasThread(threadId))
                {
                    // Thread ID에서 제목 추출 (예: "thread_메인_라인" -> "메인 라인")
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

    void Start()
    {
        CreateGrid(_gridWidth, _gridHeight);
        SetPositionCenter();
        SetCameraCollider();

        // 초기 Thread는 DesignUiManager에서 생성됨
    }

    void Update()
    {
        // Thread ID가 설정되지 않았으면 업데이트하지 않음
        if (string.IsNullOrEmpty(_currentThreadId))
            return;

        // 각 핸들러 업데이트
        _placementHandler?.Update();
        _removalHandler?.Update(_currentThreadId);
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
                    GameObject buildingObj = _gridGenHandler.CreateBuildingObject(buildingState.position, buildingData);
                    _gridGenHandler.MarkTilesAsOccupied(buildingState.position, buildingData.size);

                    // Input/Output 마커 표시 (BuildingData의 상대 좌표가 (0,0)이 아닐 때만)
                    // 마커를 건물 오브젝트의 자식으로 생성
                    if (buildingObj != null)
                    {
                        if (buildingData.inputPosition != Vector2Int.zero)
                        {
                            CreateIOMarkerAsChild(buildingState.inputPosition, _inputMarkerPrefab, "Input", buildingObj.transform);
                        }
                        if (buildingData.outputPosition != Vector2Int.zero)
                        {
                            CreateIOMarkerAsChild(buildingState.outputPosition, _outputMarkerPrefab, "Output", buildingObj.transform);
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Input/Output 마커를 건물 오브젝트의 자식으로 생성합니다.
    /// </summary>
    private void CreateIOMarkerAsChild(Vector2Int gridPos, GameObject prefab, string label, Transform parent)
    {
        if (prefab == null)
        {
            Debug.LogWarning($"[BuildingTileManager] {label} marker prefab is not assigned");
            return;
        }

        GameObject marker = Instantiate(prefab, parent);
        marker.name = $"IOMarker_{label}";

        // 월드 좌표로 변환
        Vector3 worldPos = _gridGenHandler.GridToWorldPosition(gridPos, Vector2Int.one);
        marker.transform.position = new Vector3(worldPos.x, worldPos.y, -0.5f);

        // 부모의 스케일 영향을 제거하여 마커가 원래 크기로 보이도록 함
        Vector3 parentScale = parent.transform.localScale;
        marker.transform.localScale = new Vector3(
            1f / parentScale.x,
            1f / parentScale.y,
            1f / parentScale.z
        );

    }

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
}
