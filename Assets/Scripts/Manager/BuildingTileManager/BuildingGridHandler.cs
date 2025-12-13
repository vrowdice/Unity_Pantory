using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 건물 배치용 그리드 및 타일을 관리하는 클래스
/// (모든 건물은 정사각형이며, 본체 회전 없이 입출력 포트 위치만 변경됨)
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

    private readonly Dictionary<Vector2Int, GameObject> _buildingTiles = new Dictionary<Vector2Int, GameObject>();
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
                // 타일 생성 (Z축 정렬을 위해 y값 활용)
                GameObject tile = Object.Instantiate(_buildingTilePrefab, new Vector3(x, -y, 10), Quaternion.identity, _parentTransform);
                _buildingTiles[position] = tile;

                var buildingTile = tile.GetComponent<BuildingTile>();
                if (buildingTile != null)
                {
                    buildingTile.Initialize(position, null);
                }
            }
        }
    }

    public void ExpandGrid(int newWidth, int newHeight)
    {
        CreateGrid(newWidth, newHeight);
    }

    public void ClearGrid()
    {
        ClearAllOccupiedTiles();
        ClearAllPlacedBuildings();

        foreach (var tile in _buildingTiles.Values)
        {
            if (tile != null) Object.Destroy(tile);
        }
        _buildingTiles.Clear();
    }

    public void ClearAllPlacedBuildings()
    {
        foreach (var building in _placedBuildings.Values)
        {
            if (building != null) Object.Destroy(building);
        }
        _placedBuildings.Clear();
    }

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
    }

    #endregion

    #region Tile and Occupation Status

    public GameObject GetTile(Vector2Int position)
    {
        return _buildingTiles.ContainsKey(position) ? _buildingTiles[position] : null;
    }

    public bool HasTile(Vector2Int position)
    {
        return _buildingTiles.ContainsKey(position);
    }

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

    public bool CanPlaceBuilding(Vector2Int gridPos, Vector2Int size)
    {
        if (gridPos.x < 0 || gridPos.y < 0) return false;
        if (gridPos.x + size.x > _gridWidth || gridPos.y + size.y > _gridHeight) return false;

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
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
        }

        return true;
    }

    /// <summary>
    /// 건물 오브젝트를 생성하고 표시합니다.
    /// (정사각형 건물이므로 회전 로직은 제거되고, 마커 위치 계산을 위해 데이터만 전달합니다)
    /// </summary>
    public GameObject CreateBuildingObject(Vector2Int gridPos, BuildingData buildingData, BuildingState buildingState = null)
    {
        if (buildingData.buildingSprite == null) return null;

        GameObject buildingObj;

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

        // 1. 크기 확인 (정사각형이므로 회전 계산 불필요)
        Vector2Int displaySize = buildingData.size;

        // 2. 월드 위치 적용 (회전 없음)
        Vector3 worldPos = GridToWorldPosition(gridPos, displaySize);
        buildingObj.transform.position = worldPos;
        buildingObj.transform.rotation = Quaternion.identity; // 회전 초기화 (항상 0도)

        // 3. Sprite Renderer 설정
        SpriteRenderer renderer = buildingObj.GetComponent<SpriteRenderer>();
        if (renderer == null) renderer = buildingObj.AddComponent<SpriteRenderer>();

        renderer.sprite = buildingData.buildingSprite;
        renderer.sortingOrder = 0;

        // 건물 크기 조정
        Vector3 scale = _buildingTileManager.CalculateSpriteScale(buildingData.buildingSprite, displaySize);
        buildingObj.transform.localScale = scale;

        // 4. Collider 설정
        BoxCollider2D collider = buildingObj.GetComponent<BoxCollider2D>();
        if (collider == null) collider = buildingObj.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(displaySize.x, displaySize.y);

        // 5. BuildingObject 컴포넌트 초기화
        BuildingObject buildingComponent = buildingObj.GetComponent<BuildingObject>();
        if (buildingComponent == null) buildingComponent = buildingObj.AddComponent<BuildingObject>();

        if (buildingState != null)
        {
            // BuildingObject 내부에서 rotation 값을 이용해 Input/Output 마커 위치만 변경함
            buildingComponent.Initialize(buildingData, buildingState, _inputMarkerPrefab, _outputMarkerPrefab, this);
            buildingComponent.SetupProductionIcons();
        }

        _placedBuildings[gridPos] = buildingObj;
        return buildingObj;
    }

    public void RemoveBuildingObject(Vector2Int gridPos)
    {
        if (_placedBuildings.TryGetValue(gridPos, out GameObject buildingObj))
        {
            Object.Destroy(buildingObj);
            _placedBuildings.Remove(gridPos);
        }
    }

    public GameObject GetBuildingAtPosition(Vector2Int gridPos, string currentThreadId)
    {
        List<BuildingState> buildingStates = _buildingTileManager.GetCurrentBuildingStates();

        if (buildingStates != null)
        {
            foreach (var buildingState in buildingStates)
            {
                BuildingData buildingData = _dataManager.Building.GetBuildingData(buildingState.buildingId);
                Vector2Int buildingPos = new Vector2Int(buildingState.positionX, buildingState.positionY);

                if (buildingData != null && _placedBuildings.TryGetValue(buildingPos, out GameObject placedBuilding))
                {
                    // 정사각형이므로 단순히 size만 체크
                    if (gridPos.x >= buildingPos.x && gridPos.x < buildingPos.x + buildingData.size.x &&
                        gridPos.y >= buildingPos.y && gridPos.y < buildingPos.y + buildingData.size.y)
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

    public Vector2Int WorldToGridPosition(Vector3 worldPos)
    {
        Vector3 localPos = worldPos - _parentTransform.position;
        int x = Mathf.FloorToInt(localPos.x + 0.5f);
        int y = Mathf.FloorToInt(-localPos.y + 0.5f);
        return new Vector2Int(x, y);
    }

    public Vector3 GridToWorldPosition(Vector2Int gridPos, Vector2Int size)
    {
        float centerX = gridPos.x + (size.x - 1) * 0.5f;
        float centerY = -gridPos.y - (size.y - 1) * 0.5f;
        return _parentTransform.position + new Vector3(centerX, centerY, 9);
    }

    #endregion
}