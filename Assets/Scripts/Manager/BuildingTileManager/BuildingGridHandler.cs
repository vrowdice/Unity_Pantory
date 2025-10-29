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
    private Dictionary<Vector2Int, GameObject> _buildingTiles = new Dictionary<Vector2Int, GameObject>();
    
    // 배치된 건물 오브젝트를 저장하는 Dictionary
    private Dictionary<Vector2Int, GameObject> _placedBuildings = new Dictionary<Vector2Int, GameObject>();

    public int GridWidth => _gridWidth;
    public int GridHeight => _gridHeight;
    public Dictionary<Vector2Int, GameObject> BuildingTiles => _buildingTiles;
    public Dictionary<Vector2Int, GameObject> PlacedBuildings => _placedBuildings;

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
    public GameObject CreateBuildingObject(Vector2Int gridPos, BuildingData buildingData, BuildingState buildingState = null)
    {
        if (buildingData.buildingSprite == null)
            return null;

        GameObject buildingObj;
        
        // Prefab이 있으면 Instantiate, 없으면 새로 생성
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
        
        // 회전 정보 가져오기
        int rotation = buildingState != null ? buildingState.rotation : 0;
        Vector2Int displaySize = GetRotatedSize(buildingData.size, rotation);
        
        Vector3 worldPos = GridToWorldPosition(gridPos, displaySize);
        buildingObj.transform.position = worldPos;
        
        // 회전 적용
        float angle = rotation * 90f;
        buildingObj.transform.rotation = Quaternion.Euler(0, 0, -angle);

        // SpriteRenderer가 없으면 추가
        SpriteRenderer renderer = buildingObj.GetComponent<SpriteRenderer>();
        if (renderer == null)
            renderer = buildingObj.AddComponent<SpriteRenderer>();
            
        renderer.sprite = buildingData.buildingSprite;
        renderer.sortingOrder = 0; // 타일 위에 표시

        // 건물 크기를 타일 크기에 맞춤 (1타일 = 1유닛, 회전 전 원본 크기 사용)
        Vector3 scale = _buildingTileManager.CalculateSpriteScale(buildingData.buildingSprite, buildingData.size);
        buildingObj.transform.localScale = scale;

        // BoxCollider2D 추가 (클릭 감지용, 회전된 크기 사용)
        BoxCollider2D collider = buildingObj.GetComponent<BoxCollider2D>();
        if (collider == null)
            collider = buildingObj.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(displaySize.x, displaySize.y);

        // BuildingObject 컴포넌트 추가 및 초기화
        BuildingObject buildingComponent = buildingObj.GetComponent<BuildingObject>();
        if (buildingComponent == null)
            buildingComponent = buildingObj.AddComponent<BuildingObject>();
        
        // BuildingState가 있으면 일반 초기화, 없으면 프리뷰 초기화
        if (buildingState != null)
        {
            buildingComponent.Initialize(buildingData, buildingState, _inputMarkerPrefab, _outputMarkerPrefab, this);
        }

        _placedBuildings[gridPos] = buildingObj;
        return buildingObj;
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
                    // 회전된 크기 계산
                    Vector2Int rotatedSize = GetRotatedSize(buildingData.size, buildingState.rotation);
                    
                    // 건물이 차지하는 영역 확인
                    if (gridPos.x >= buildingPos.x && gridPos.x < buildingPos.x + rotatedSize.x &&
                        gridPos.y >= buildingPos.y && gridPos.y < buildingPos.y + rotatedSize.y)
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
        // 타일 중심을 기준으로 가장 가까운 그리드 좌표 계산
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
        float centerX = gridPos.x + (size.x - 1) * 0.5f;
        float centerY = -gridPos.y - (size.y - 1) * 0.5f;
        
        return _parentTransform.position + new Vector3(centerX, centerY, 9);
    }
}

