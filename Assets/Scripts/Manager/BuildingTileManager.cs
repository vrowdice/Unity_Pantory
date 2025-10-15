using UnityEngine;

/// <summary>
/// 건물 타일 시스템의 메인 매니저 (조율자)
/// </summary>
public class BuildingTileManager : MonoBehaviour
{
    [SerializeField] private GameObject _buildingTilePrefab;
    [SerializeField] private GameObject _buildingObjectPrefab;  // 건물 표시용 프리팹

    [SerializeField] private int _gridWidth = 10;
    [SerializeField] private int _gridHeight = 10;
    [SerializeField] private string _currentThreadId = "thread_main";  // 현재 편집 중인 Thread ID

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

    [Header("Preview Settings")]
    [SerializeField] private Color _validColor = new Color(0, 1, 0, 0.5f);    // 배치 가능 (초록)
    [SerializeField] private Color _invalidColor = new Color(1, 0, 0, 0.5f);  // 배치 불가 (빨강)
    [SerializeField] private Color _removalHighlightColor = new Color(1, 0, 0, 0.7f);  // 제거 하이라이트 (빨강)

    void Awake()
    {
        _mainCamera = Camera.main;
        _dataManager = GameDataManager.Instance;

        // 핸들러 초기화
        _gridGenHandler = new BuildingGridHandler(transform, _buildingTilePrefab, _dataManager, _gridWidth, _gridHeight);
        _placementHandler = new BuildingPlacementHandler(_gridGenHandler, _dataManager, transform, _mainCamera);
        _removalHandler = new BuildingRemovalHandler(_gridGenHandler, _dataManager, _mainCamera);

        // 색상 설정
        _placementHandler.SetPreviewColors(_validColor, _invalidColor);
        _removalHandler.SetHighlightColor(_removalHighlightColor);
    }

    void Start()
    {
        CreateGrid(_gridWidth, _gridHeight);
        SetPositionCenter();
        SetCameraCollider();
        
        // 테스트용 Thread 생성
        if (_dataManager != null && !_dataManager.HasThread(_currentThreadId))
        {
            _dataManager.CreateThread(_currentThreadId, "메인 라인", "생산부");
        }
    }

    void Update()
    {
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
        _currentThreadId = threadId;
        RefreshBuildings();
    }

    /// <summary>
    /// Thread의 건물들을 다시 로드하여 표시합니다.
    /// </summary>
    private void RefreshBuildings()
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

        // 기존 건물 오브젝트 제거
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
                    _gridGenHandler.CreateBuildingObject(buildingState.position, buildingData);
                    _gridGenHandler.MarkTilesAsOccupied(buildingState.position, buildingData.size);
                }
            }
        }
    }
}
