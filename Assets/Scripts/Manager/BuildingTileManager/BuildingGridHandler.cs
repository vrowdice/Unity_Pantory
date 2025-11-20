using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 건물 배치용 그리드 및 타일을 관리하는 클래스
/// </summary>
public class BuildingGridHandler
{
    private readonly Transform _parentTransform;
    private readonly GameObject _buildingTilePrefab;
    private readonly GameObject _buildingObjectPrefab;
    private readonly GameObject _inputMarkerPrefab;
    private readonly GameObject _outputMarkerPrefab;
    private readonly GameDataManager _dataManager;
    private readonly BuildingTileManager _buildingTileManager;

    private int _gridWidth;
    private int _gridHeight;

    // 타일을 좌표로 저장하는 Dictionary
    private readonly Dictionary<Vector2Int, GameObject> _buildingTiles = new Dictionary<Vector2Int, GameObject>();

    // 배치된 건물 오브젝트를 저장하는 Dictionary
    private readonly Dictionary<Vector2Int, GameObject> _placedBuildings = new Dictionary<Vector2Int, GameObject>();

    #region Public Properties

    public int GridWidth => _gridWidth;
    public int GridHeight => _gridHeight;
    public Dictionary<Vector2Int, GameObject> BuildingTiles => _buildingTiles;
    public Dictionary<Vector2Int, GameObject> PlacedBuildings => _placedBuildings;

    #endregion

    #region Constructor

    public BuildingGridHandler(BuildingTileManager buildingTileManager, GameObject buildingTilePrefab, GameObject buildingObjectPrefab, GameObject inputMarkerPrefab, GameObject outputMarkerPrefab, int gridWidth, int gridHeight)
    {
        _buildingTilePrefab = buildingTilePrefab;
        _buildingObjectPrefab = buildingObjectPrefab;
        _inputMarkerPrefab = inputMarkerPrefab;
        _outputMarkerPrefab = outputMarkerPrefab;
        _dataManager = buildingTileManager.DataManager;
        _buildingTileManager = buildingTileManager;
        _gridWidth = gridWidth;
        _gridHeight = gridHeight;

        _parentTransform = _buildingTileManager.transform;
    }

    #endregion

    #region Grid Initialization and Clearing

    /// <summary>
    /// 그리드를 생성합니다.
    /// </summary>
    public void CreateGrid(int width, int height)
    {
        ClearGrid();

        _gridWidth = width;
        _gridHeight = height;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector2Int position = new Vector2Int(x, y);
                // Unity의 World Space에서 Y축은 위로 갈수록 증가하지만, 그리드 Y좌표(세로 인덱스)는 아래로 갈수록 증가하므로 -y 사용
                GameObject tile = Object.Instantiate(_buildingTilePrefab, new Vector3(x, -y, 10), Quaternion.identity, _parentTransform);
                _buildingTiles[position] = tile;

                // BuildingTile 컴포넌트에 좌표 전달
                var buildingTile = tile.GetComponent<BuildingTile>();
                if (buildingTile != null)
                {
                    buildingTile.Initialize(position, null);
                }
            }
        }
        Debug.Log($"[BuildingGridHandler] Grid created successfully: {width}x{height}");
    }

    /// <summary>
    /// 그리드를 확장합니다. (현재는 단순히 새 그리드를 생성하는 방식으로 구현됨)
    /// </summary>
    public void ExpandGrid(int newWidth, int newHeight)
    {
        // 최적화: 기존 타일을 유지하고 확장된 부분만 추가하는 로직이 필요할 수 있으나, 현재는 Clear 후 전체 재생성
        CreateGrid(newWidth, newHeight);
        Debug.Log($"[BuildingGridHandler] Grid expanded and rebuilt to: {newWidth}x{newHeight}");
    }

    /// <summary>
    /// 그리드를 완전히 초기화하고 모든 타일 및 건물을 제거합니다.
    /// </summary>
    public void ClearGrid()
    {
        ClearAllOccupiedTiles();
        ClearAllPlacedBuildings();

        foreach (var tile in _buildingTiles.Values)
        {
            if (tile != null)
                Object.Destroy(tile);
        }
        _buildingTiles.Clear();
        Debug.Log("[BuildingGridHandler] Grid tiles cleared.");
    }

    /// <summary>
    /// 모든 배치된 건물 오브젝트를 파괴하고 딕셔너리를 비웁니다.
    /// (BuildingTileManager의 RefreshBuildings()에서 요청한 기능)
    /// </summary>
    public void ClearAllPlacedBuildings()
    {
        foreach (var building in _placedBuildings.Values)
        {
            if (building != null)
                Object.Destroy(building);
        }
        _placedBuildings.Clear();
        Debug.Log("[BuildingGridHandler] All placed building objects cleared.");
    }

    /// <summary>
    /// 모든 타일의 점유 상태를 '해제됨 (false)'로 설정합니다.
    /// (BuildingTileManager의 RefreshBuildings()에서 요청한 기능)
    /// </summary>
    public void ClearAllOccupiedTiles()
    {
        foreach (var tile in _buildingTiles.Values)
        {
            var buildingTile = tile.GetComponent<BuildingTile>();
            if (buildingTile != null)
            {
                buildingTile.SetOccupied(false);
            }
        }
        Debug.Log("[BuildingGridHandler] All tiles marked as unoccupied.");
    }

    #endregion

    #region Tile and Occupation Status

    /// <summary>
    /// 특정 좌표의 타일을 반환합니다.
    /// </summary>
    public GameObject GetTile(Vector2Int position)
    {
        return _buildingTiles.ContainsKey(position) ? _buildingTiles[position] : null;
    }

    /// <summary>
    /// 타일이 존재하는지 확인합니다.
    /// </summary>
    public bool HasTile(Vector2Int position)
    {
        return _buildingTiles.ContainsKey(position);
    }

    /// <summary>
    /// 모든 타일의 윤곽선 표시 여부를 설정합니다.
    /// </summary>
    public void SetAllTilesOutline(bool visible, Color color = default)
    {
        foreach (var tile in _buildingTiles.Values)
        {
            if (tile != null)
            {
                var buildingTile = tile.GetComponent<BuildingTile>();
                if (buildingTile != null)
                {
                    buildingTile.SetOutlineVisible(visible, color);
                }
            }
        }
    }

    /// <summary>
    /// 지정된 영역의 타일을 차지된 것으로 표시하거나 해제합니다.
    /// </summary>
    public void MarkTilesAsOccupied(Vector2Int startPos, Vector2Int size, bool occupied = true)
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
                        buildingTile.SetOccupied(occupied);
                    }
                }
            }
        }
    }

    #endregion

    #region Building Object Management

    /// <summary>
    /// 건물을 배치할 수 있는지 확인합니다.
    /// </summary>
    public bool CanPlaceBuilding(Vector2Int gridPos, Vector2Int size)
    {
        // 그리드 범위 체크
        if (gridPos.x < 0 || gridPos.y < 0)
            return false;

        if (gridPos.x + size.x > _gridWidth || gridPos.y + size.y > _gridHeight)
            return false;

        // 겹침 체크 (차지된 타일 확인)
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
                    return false; // 타일이 존재하지 않음 (범위 밖)
                }
            }
        }

        return true;
    }

    /// <summary>
    /// 건물 오브젝트를 생성하고 표시합니다.
    /// </summary>
    /// <returns>생성된 건물 GameObject</returns>
    public GameObject CreateBuildingObject(Vector2Int gridPos, BuildingData buildingData, BuildingState buildingState = null)
    {
        if (buildingData.buildingSprite == null)
            return null;

        GameObject buildingObj;

        // Prefab 인스턴스화 또는 새 GameObject 생성
        if (_buildingObjectPrefab != null)
        {
            buildingObj = Object.Instantiate(_buildingObjectPrefab, _parentTransform);
            buildingObj.name = $"Building_{buildingData.id}_{gridPos}";
        }
        else
        {
            buildingObj = new GameObject($"Building_{buildingData.id}_{gridPos}");
            buildingObj.transform.SetParent(_parentTransform);
        }

        // 1. 회전 및 크기 계산
        int rotation = buildingState != null ? buildingState.rotation : 0;
        Vector2Int displaySize = GetRotatedSize(buildingData.size, rotation);

        // 2. 월드 위치 및 회전 적용
        Vector3 worldPos = GridToWorldPosition(gridPos, displaySize);
        buildingObj.transform.position = worldPos;

        float angle = rotation * 90f;
        buildingObj.transform.rotation = Quaternion.Euler(0, 0, -angle); // Z축 회전 적용

        // 3. Sprite Renderer 및 크기 조정
        SpriteRenderer renderer = buildingObj.GetComponent<SpriteRenderer>();
        if (renderer == null)
            renderer = buildingObj.AddComponent<SpriteRenderer>();

        renderer.sprite = buildingData.buildingSprite;
        renderer.sortingOrder = 0;

        // 건물 크기를 타일 크기에 맞춤 (1타일 = 1유닛, 회전 전 원본 크기 사용)
        Vector3 scale = _buildingTileManager.CalculateSpriteScale(buildingData.buildingSprite, buildingData.size);
        buildingObj.transform.localScale = scale;

        // 4. Collider 추가 및 크기 조정
        BoxCollider2D collider = buildingObj.GetComponent<BoxCollider2D>();
        if (collider == null)
            collider = buildingObj.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(displaySize.x, displaySize.y);

        // 5. BuildingObject 컴포넌트 초기화
        BuildingObject buildingComponent = buildingObj.GetComponent<BuildingObject>();
        if (buildingComponent == null)
            buildingComponent = buildingObj.AddComponent<BuildingObject>();

        // BuildingState가 있으면 일반 초기화 (마커 생성), 없으면 프리뷰 초기화
        if (buildingState != null)
        {
            buildingComponent.Initialize(buildingData, buildingState, _inputMarkerPrefab, _outputMarkerPrefab, this);

            // 입출력 자원 아이콘 표시 (공용 Canvas 전달)
            Transform sharedCanvas = _buildingTileManager.SharedProductionIconCanvas;
            buildingComponent.SetupProductionIcons(_dataManager, sharedCanvas);
        }

        _placedBuildings[gridPos] = buildingObj;
        return buildingObj;
    }

    /// <summary>
    /// 건물 오브젝트를 제거합니다.
    /// </summary>
    public void RemoveBuildingObject(Vector2Int gridPos)
    {
        if (_placedBuildings.TryGetValue(gridPos, out GameObject buildingObj))
        {
            Object.Destroy(buildingObj);
            _placedBuildings.Remove(gridPos);
            // 점유 해제는 BuildingRemovalHandler 또는 BuildingTileManager에서 별도로 처리해야 함 (MarkTilesAsOccupied 호출)
        }
    }

    /// <summary>
    /// 특정 위치에 있는 건물을 찾습니다. (클릭 처리 로직 보강)
    /// </summary>
    public GameObject GetBuildingAtPosition(Vector2Int gridPos, string currentThreadId)
    {
        // 배치된 모든 건물의 점유 영역을 확인하여 클릭된 건물을 찾습니다.

        // BuildingTileManager의 임시 데이터(BuildingState)를 가져와서 크기와 회전 정보를 활용합니다.
        List<BuildingState> buildingStates = _buildingTileManager.GetCurrentBuildingStates();

        if (buildingStates != null)
        {
            foreach (var buildingState in buildingStates)
            {
                BuildingData buildingData = _dataManager.Building.GetBuildingData(buildingState.buildingId);

                // BuildingState의 위치가 곧 건물이 차지하는 영역의 시작점입니다.
                Vector2Int buildingPos = new Vector2Int(buildingState.positionX, buildingState.positionY);

                if (buildingData != null && _placedBuildings.TryGetValue(buildingPos, out GameObject placedBuilding))
                {
                    Vector2Int rotatedSize = GetRotatedSize(buildingData.size, buildingState.rotation);

                    // 클릭된 위치(gridPos)가 이 건물의 영역 내에 있는지 확인
                    if (gridPos.x >= buildingPos.x && gridPos.x < buildingPos.x + rotatedSize.x &&
                        gridPos.y >= buildingPos.y && gridPos.y < buildingPos.y + rotatedSize.y)
                    {
                        return placedBuilding;
                    }
                }
            }
        }

        return null;
    }

    #endregion

    #region 좌표 변환 유틸리티

    /// <summary>
    /// 월드 좌표를 그리드 좌표로 변환합니다.
    /// </summary>
    public Vector2Int WorldToGridPosition(Vector3 worldPos)
    {
        Vector3 localPos = worldPos - _parentTransform.position;
        // 타일 중심을 기준으로 가장 가까운 그리드 좌표 계산
        // X: Local X + 0.5f (반올림)
        // Y: -Local Y + 0.5f (반올림)
        int x = Mathf.FloorToInt(localPos.x + 0.5f);
        int y = Mathf.FloorToInt(-localPos.y + 0.5f);
        return new Vector2Int(x, y);
    }

    /// <summary>
    /// 그리드 좌표를 월드 좌표로 변환합니다 (건물 중심 기준).
    /// </summary>
    public Vector3 GridToWorldPosition(Vector2Int gridPos, Vector2Int size)
    {
        // 건물 크기를 고려하여 중심 위치 계산
        // X축: 시작 위치 + (크기 - 1) / 2
        // Y축: 시작 위치 - (크기 - 1) / 2 (Unity World Y)
        float centerX = gridPos.x + (size.x - 1) * 0.5f;
        float centerY = -gridPos.y - (size.y - 1) * 0.5f;

        // Z축을 9로 설정하여 타일(10)보다 앞으로 나오게 함
        return _parentTransform.position + new Vector3(centerX, centerY, 9);
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

    #endregion
}