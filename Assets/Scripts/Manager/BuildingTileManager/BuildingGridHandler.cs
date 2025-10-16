using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 건물 배치용 그리드 및 타일을 관리하는 클래스
/// </summary>
public class BuildingGridHandler
{
    private readonly Transform _parentTransform;
    private readonly GameObject _buildingTilePrefab;
    private readonly GameDataManager _dataManager;
    private readonly BuildingTileManager _buildingTileManager;
    
    private int _gridWidth;
    private int _gridHeight;
    
    // 타일을 좌표로 저장하는 Dictionary
    private Dictionary<Vector2Int, GameObject> _buildingTiles = new Dictionary<Vector2Int, GameObject>();
    
    // 배치된 건물 오브젝트를 저장하는 Dictionary
    private Dictionary<Vector2Int, GameObject> _placedBuildings = new Dictionary<Vector2Int, GameObject>();

    public int GridWidth => _gridWidth;
    public int GridHeight => _gridHeight;
    public Dictionary<Vector2Int, GameObject> BuildingTiles => _buildingTiles;
    public Dictionary<Vector2Int, GameObject> PlacedBuildings => _placedBuildings;

    public BuildingGridHandler(Transform parentTransform, GameObject buildingTilePrefab, GameDataManager dataManager, BuildingTileManager buildingTileManager, int gridWidth, int gridHeight)
    {
        _parentTransform = parentTransform;
        _buildingTilePrefab = buildingTilePrefab;
        _dataManager = dataManager;
        _buildingTileManager = buildingTileManager;
        _gridWidth = gridWidth;
        _gridHeight = gridHeight;
    }

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
    }

    /// <summary>
    /// 그리드를 확장합니다.
    /// </summary>
    public void ExpandGrid(int newWidth, int newHeight)
    {
        CreateGrid(newWidth, newHeight);
    }

    /// <summary>
    /// 그리드를 초기화합니다.
    /// </summary>
    public void ClearGrid()
    {
        foreach (var tile in _buildingTiles.Values)
        {
            if (tile != null)
                Object.Destroy(tile);
        }
        _buildingTiles.Clear();

        foreach (var building in _placedBuildings.Values)
        {
            if (building != null)
                Object.Destroy(building);
        }
        _placedBuildings.Clear();
    }

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
    /// 타일을 차지된 것으로 표시하거나 해제합니다.
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

    /// <summary>
    /// 건물 오브젝트를 생성하고 표시합니다.
    /// </summary>
    /// <returns>생성된 건물 GameObject</returns>
    public GameObject CreateBuildingObject(Vector2Int gridPos, BuildingData buildingData)
    {
        if (buildingData.buildingSprite == null)
            return null;

        GameObject buildingObj = new GameObject($"Building_{buildingData.id}_{gridPos}");
        buildingObj.transform.SetParent(_parentTransform);
        
        Vector3 worldPos = GridToWorldPosition(gridPos, buildingData.size);
        buildingObj.transform.position = worldPos;

        SpriteRenderer renderer = buildingObj.AddComponent<SpriteRenderer>();
        renderer.sprite = buildingData.buildingSprite;
        renderer.sortingOrder = 0; // 타일 위에 표시

        // 건물 크기를 타일 크기에 맞춤 (1타일 = 1유닛)
        Vector3 scale = _buildingTileManager.CalculateSpriteScale(buildingData.buildingSprite, buildingData.size);
        buildingObj.transform.localScale = scale;

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
        }
    }

    /// <summary>
    /// 특정 위치에 있는 건물을 찾습니다.
    /// </summary>
    public GameObject GetBuildingAtPosition(Vector2Int gridPos, string currentThreadId)
    {
        // 먼저 정확한 위치에 건물이 있는지 확인
        if (_placedBuildings.TryGetValue(gridPos, out GameObject building))
        {
            return building;
        }

        // 큰 건물의 경우 다른 타일을 차지할 수 있으므로 모든 건물을 확인
        foreach (var kvp in _placedBuildings)
        {
            Vector2Int buildingPos = kvp.Key;
            
            // 건물 데이터에서 크기 정보 가져오기
            BuildingState buildingState = _dataManager.GetBuildingStates(currentThreadId)
                ?.Find(b => b.position == buildingPos);
            
            if (buildingState != null)
            {
                BuildingData buildingData = _dataManager.GetBuildingData(buildingState.buildingId);
                if (buildingData != null)
                {
                    // 건물이 차지하는 영역 확인
                    if (gridPos.x >= buildingPos.x && gridPos.x < buildingPos.x + buildingData.size.x &&
                        gridPos.y >= buildingPos.y && gridPos.y < buildingPos.y + buildingData.size.y)
                    {
                        return kvp.Value;
                    }
                }
            }
        }

        return null;
    }

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

    // ================== 좌표 변환 ==================

    /// <summary>
    /// 월드 좌표를 그리드 좌표로 변환합니다.
    /// </summary>
    public Vector2Int WorldToGridPosition(Vector3 worldPos)
    {
        Vector3 localPos = worldPos - _parentTransform.position;
        int x = Mathf.FloorToInt(localPos.x);
        int y = Mathf.FloorToInt(-localPos.y);
        return new Vector2Int(x, y);
    }

    /// <summary>
    /// 그리드 좌표를 월드 좌표로 변환합니다 (건물 중심 기준).
    /// </summary>
    public Vector3 GridToWorldPosition(Vector2Int gridPos, Vector2Int size)
    {
        // 건물 크기를 고려하여 중심 위치 계산
        float centerX = gridPos.x + (size.x - 1) * 0.5f;
        float centerY = -gridPos.y - (size.y - 1) * 0.5f;
        
        return _parentTransform.position + new Vector3(centerX, centerY, 9);
    }
}

