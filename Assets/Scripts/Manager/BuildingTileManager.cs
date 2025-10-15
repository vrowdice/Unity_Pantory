using UnityEngine;
using System.Collections.Generic;

public class BuildingTileManager : MonoBehaviour
{
    [SerializeField] private GameObject _buildingTilePrefab;
    [SerializeField] private GameObject _buildingObjectPrefab;  // 건물 표시용 프리팹

    [SerializeField] private int _gridWidth = 10;
    [SerializeField] private int _gridHeight = 10;
    [SerializeField] private string _currentThreadId = "thread_main";  // 현재 편집 중인 Thread ID

    // 타일을 좌표로 저장하는 Dictionary
    private Dictionary<Vector2Int, GameObject> _buildingTiles = new Dictionary<Vector2Int, GameObject>();
    
    // 배치된 건물 오브젝트를 저장하는 Dictionary
    private Dictionary<Vector2Int, GameObject> _placedBuildings = new Dictionary<Vector2Int, GameObject>();

    private BoxCollider2D _cameraCollider;
    private Camera _mainCamera;
    private GameDataManager _dataManager;

    // 건물 배치 모드 관련
    private bool _isPlacementMode = false;
    private BuildingData _selectedBuilding = null;
    private GameObject _previewObject = null;
    private SpriteRenderer _previewRenderer = null;
    private Vector2Int _currentGridPos;
    private bool _canPlace = false;

    [Header("Preview Settings")]
    [SerializeField] private Color _validColor = new Color(0, 1, 0, 0.5f);    // 배치 가능 (초록)
    [SerializeField] private Color _invalidColor = new Color(1, 0, 0, 0.5f);  // 배치 불가 (빨강)

    void Awake()
    {
        _mainCamera = Camera.main;
        _dataManager = GameDataManager.Instance;
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
        if (_isPlacementMode)
        {
            UpdatePlacementPreview();
            HandlePlacementInput();
        }
    }

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

    // 그리드 생성
    public void CreateGrid(int width, int height)
    {
        ClearGrid();

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector2Int position = new Vector2Int(x, y);
                GameObject tile = Instantiate(_buildingTilePrefab, new Vector3(x, -y, 10), Quaternion.identity, transform);
                _buildingTiles[position] = tile;
                
                // BuildingTile 컴포넌트에 좌표 전달
                var buildingTile = tile.GetComponent<BuildingTile>();
                if (buildingTile != null)
                {
                    buildingTile.Initialize(position, this);
                }
            }
        }
    }

    // ================== 건물 배치 모드 ==================

    /// <summary>
    /// 건물 배치 모드를 시작합니다.
    /// </summary>
    public void StartPlacementMode(BuildingData buildingData)
    {
        _isPlacementMode = true;
        _selectedBuilding = buildingData;
        
        CreatePreviewObject();
        Debug.Log($"[BuildingTileManager] Placement mode started: {buildingData.displayName}");
    }

    /// <summary>
    /// 건물 배치 모드를 취소합니다.
    /// </summary>
    public void CancelPlacementMode()
    {
        _isPlacementMode = false;
        _selectedBuilding = null;
        
        DestroyPreviewObject();
        Debug.Log("[BuildingTileManager] Placement mode cancelled");
    }

    /// <summary>
    /// 프리뷰 오브젝트를 생성합니다.
    /// </summary>
    private void CreatePreviewObject()
    {
        if (_selectedBuilding == null || _selectedBuilding.buildingSprite == null)
        {
            Debug.LogWarning("[BuildingTileManager] Cannot create preview object");
            return;
        }

        _previewObject = new GameObject("BuildingPreview");
        _previewObject.transform.SetParent(transform);
        _previewRenderer = _previewObject.AddComponent<SpriteRenderer>();
        _previewRenderer.sprite = _selectedBuilding.buildingSprite;
        _previewRenderer.sortingOrder = 100; // 가장 위에 표시
        
        // 프리뷰 크기를 타일 크기에 맞춤 (1타일 = 1유닛)
        Vector3 scale = CalculateSpriteScale(_selectedBuilding.buildingSprite, _selectedBuilding.size);
        _previewObject.transform.localScale = scale;
    }

    /// <summary>
    /// 프리뷰 오브젝트를 삭제합니다.
    /// </summary>
    private void DestroyPreviewObject()
    {
        if (_previewObject != null)
        {
            Destroy(_previewObject);
            _previewObject = null;
            _previewRenderer = null;
        }
    }

    /// <summary>
    /// 배치 프리뷰를 업데이트합니다.
    /// </summary>
    private void UpdatePlacementPreview()
    {
        if (_previewObject == null || _selectedBuilding == null)
            return;

        // 마우스 위치를 월드 좌표로 변환
        Vector3 mouseWorldPos = _mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0;

        // 그리드 좌표로 변환
        Vector2Int gridPos = WorldToGridPosition(mouseWorldPos);
        _currentGridPos = gridPos;

        // 배치 가능 여부 체크
        _canPlace = CanPlaceBuilding(gridPos, _selectedBuilding.size);

        // 프리뷰 위치 및 색상 업데이트
        Vector3 worldPos = GridToWorldPosition(gridPos, _selectedBuilding.size);
        _previewObject.transform.position = worldPos;
        _previewRenderer.color = _canPlace ? _validColor : _invalidColor;
    }

    /// <summary>
    /// 배치 입력을 처리합니다.
    /// </summary>
    private void HandlePlacementInput()
    {
        // 왼쪽 클릭 - 건물 배치
        if (Input.GetMouseButtonDown(0) && _canPlace)
        {
            PlaceBuilding(_currentGridPos, _selectedBuilding);
        }

        // 오른쪽 클릭 또는 ESC - 취소
        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
        {
            CancelPlacementMode();
        }
    }

    // ================== 건물 배치 로직 ==================

    /// <summary>
    /// 건물을 배치합니다.
    /// </summary>
    private void PlaceBuilding(Vector2Int gridPos, BuildingData buildingData)
    {
        if (!CanPlaceBuilding(gridPos, buildingData.size))
        {
            Debug.LogWarning("[BuildingTileManager] Cannot place building at this position");
            return;
        }

        // BuildingState 생성 및 ThreadService에 추가
        BuildingState buildingState = new BuildingState(buildingData.id, gridPos);
        if (_dataManager.AddBuildingToThread(_currentThreadId, buildingState))
        {
            // 건물 오브젝트 생성
            CreateBuildingObject(gridPos, buildingData);
            
            // 배치된 타일 차지 표시
            MarkTilesAsOccupied(gridPos, buildingData.size);
            
            Debug.Log($"[BuildingTileManager] Building placed: {buildingData.displayName} at {gridPos}");
        }
        
        // 배치 모드 유지 (연속 배치 가능)
        // 취소하려면 CancelPlacementMode();
    }

    /// <summary>
    /// 건물 오브젝트를 생성하고 표시합니다.
    /// </summary>
    private void CreateBuildingObject(Vector2Int gridPos, BuildingData buildingData)
    {
        if (buildingData.buildingSprite == null)
            return;

        GameObject buildingObj = new GameObject($"Building_{buildingData.id}_{gridPos}");
        buildingObj.transform.SetParent(transform);
        
        Vector3 worldPos = GridToWorldPosition(gridPos, buildingData.size);
        buildingObj.transform.position = worldPos;

        SpriteRenderer renderer = buildingObj.AddComponent<SpriteRenderer>();
        renderer.sprite = buildingData.buildingSprite;
        renderer.sortingOrder = 0; // 타일 위에 표시

        // 건물 크기를 타일 크기에 맞춤 (1타일 = 1유닛)
        Vector3 scale = CalculateSpriteScale(buildingData.buildingSprite, buildingData.size);
        buildingObj.transform.localScale = scale;

        _placedBuildings[gridPos] = buildingObj;
    }

    /// <summary>
    /// 타일을 차지된 것으로 표시합니다.
    /// </summary>
    private void MarkTilesAsOccupied(Vector2Int startPos, Vector2Int size)
    {
        for (int y = 0; y < size.y; y++)
        {
            for (int x = 0; x < size.x; x++)
            {
                Vector2Int pos = new Vector2Int(startPos.x + x, startPos.y + y);
                if (_buildingTiles.TryGetValue(pos, out GameObject tile))
                {
                    var buildingTile = tile.GetComponent<BuildingTile>();
                    if (buildingTile != null)
                    {
                        buildingTile.SetOccupied(true);
                    }
                }
            }
        }
    }

    /// <summary>
    /// 건물을 배치할 수 있는지 확인합니다.
    /// </summary>
    private bool CanPlaceBuilding(Vector2Int gridPos, Vector2Int size)
    {
        // 그리드 범위 체크
        if (gridPos.x < 0 || gridPos.y < 0)
            return false;
        
        if (gridPos.x + size.x > _gridWidth || gridPos.y + size.y > _gridHeight)
            return false;

        // 겹침 체크
        for (int y = 0; y < size.y; y++)
        {
            for (int x = 0; x < size.x; x++)
            {
                Vector2Int checkPos = new Vector2Int(gridPos.x + x, gridPos.y + y);
                
                if (_buildingTiles.TryGetValue(checkPos, out GameObject tile))
                {
                    var buildingTile = tile.GetComponent<BuildingTile>();
                    if (buildingTile != null && buildingTile.IsOccupied)
                    {
                        return false; // 이미 차지된 타일
                    }
                }
                else
                {
                    return false; // 타일이 존재하지 않음
                }
            }
        }

        return true;
    }

    /// <summary>
    /// 스프라이트를 타일 크기에 맞게 스케일을 계산합니다.
    /// </summary>
    /// <param name="sprite">스프라이트</param>
    /// <param name="targetSize">목표 크기 (타일 단위)</param>
    /// <returns>적용할 스케일</returns>
    private Vector3 CalculateSpriteScale(Sprite sprite, Vector2Int targetSize)
    {
        if (sprite == null)
            return Vector3.one;

        // 스프라이트의 실제 크기 (유닛 단위)
        float spriteWidth = sprite.bounds.size.x;
        float spriteHeight = sprite.bounds.size.y;

        // 목표 크기 (타일 단위, 1타일 = 1유닛)
        float targetWidth = targetSize.x;
        float targetHeight = targetSize.y;

        // 스케일 계산
        float scaleX = targetWidth / spriteWidth;
        float scaleY = targetHeight / spriteHeight;

        return new Vector3(scaleX, scaleY, 1f);
    }

    // ================== 좌표 변환 ==================

    /// <summary>
    /// 월드 좌표를 그리드 좌표로 변환합니다.
    /// </summary>
    private Vector2Int WorldToGridPosition(Vector3 worldPos)
    {
        Vector3 localPos = worldPos - transform.position;
        int x = Mathf.FloorToInt(localPos.x);
        int y = Mathf.FloorToInt(-localPos.y);
        return new Vector2Int(x, y);
    }

    /// <summary>
    /// 그리드 좌표를 월드 좌표로 변환합니다 (건물 중심 기준).
    /// </summary>
    private Vector3 GridToWorldPosition(Vector2Int gridPos, Vector2Int size)
    {
        // 건물 크기를 고려하여 중심 위치 계산
        float centerX = gridPos.x + (size.x - 1) * 0.5f;
        float centerY = -gridPos.y - (size.y - 1) * 0.5f;
        
        return transform.position + new Vector3(centerX, centerY, 9);
    }

    // ================== 기타 메서드 ==================

    /// <summary>
    /// 특정 좌표의 타일을 반환합니다.
    /// </summary>
    public GameObject GetThreadTile(Vector2Int position)
    {
        return _buildingTiles.ContainsKey(position) ? _buildingTiles[position] : null;
    }

    /// <summary>
    /// 타일이 존재하는지 확인합니다.
    /// </summary>
    public bool HasThreadTile(Vector2Int position)
    {
        return _buildingTiles.ContainsKey(position);
    }

    /// <summary>
    /// 그리드를 확장합니다 (런타임에 크기 변경 시).
    /// </summary>
    public void ExpandGrid(int newWidth, int newHeight)
    {
        _gridWidth = newWidth;
        _gridHeight = newHeight;
        CreateGrid(newWidth, newHeight);
        SetPositionCenter();
        SetCameraCollider();
    }

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
        // 기존 건물 오브젝트 제거
        foreach (var building in _placedBuildings.Values)
        {
            if (building != null)
                Destroy(building);
        }
        _placedBuildings.Clear();

        // 모든 타일 점유 상태 초기화
        foreach (var tile in _buildingTiles.Values)
        {
            var buildingTile = tile.GetComponent<BuildingTile>();
            if (buildingTile != null)
            {
                buildingTile.SetOccupied(false);
            }
        }

        // ThreadService에서 건물 데이터 가져와서 표시
        if (_dataManager != null)
        {
            var buildingStates = _dataManager.GetBuildingStates(_currentThreadId);
            if (buildingStates != null)
            {
                foreach (var buildingState in buildingStates)
                {
                    BuildingData buildingData = _dataManager.GetBuildingData(buildingState.buildingId);
                    if (buildingData != null)
                    {
                        CreateBuildingObject(buildingState.position, buildingData);
                        MarkTilesAsOccupied(buildingState.position, buildingData.size);
                    }
                }
            }
        }
    }

    /// <summary>
    /// 그리드를 초기화합니다.
    /// </summary>
    private void ClearGrid()
    {
        foreach (var tile in _buildingTiles.Values)
        {
            Destroy(tile);
        }
        _buildingTiles.Clear();

        foreach (var building in _placedBuildings.Values)
        {
            Destroy(building);
        }
        _placedBuildings.Clear();
    }
}
