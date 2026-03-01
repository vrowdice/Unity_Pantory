using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// 그리드 타일의 생성, 건물 오브젝트의 배치 및 좌표 변환을 관리하는 핸들러입니다.
/// 배치, 제거, 계산 기능을 모두 포함합니다.
/// </summary>
public class DesignRunnerGridHandler
{
    private readonly DesignRunner _manager;
    private readonly Transform _parentTransform;

    private readonly Dictionary<Vector2Int, GameObject> _tileObjectMap = new Dictionary<Vector2Int, GameObject>();
    private readonly Dictionary<Vector2Int, GameObject> _buildingOriginMap = new Dictionary<Vector2Int, GameObject>();
    private readonly Dictionary<Vector2Int, GameObject> _occupancyMap = new Dictionary<Vector2Int, GameObject>();


    private const float TileZDepth = 10f;
    private const float BuildingZDepth = 9f;

    public int Width => _manager.GridWidth;
    public int Height => _manager.GridHeight;
    private DataManager DataManager => _manager.DataManager;

    public DesignRunnerGridHandler(DesignRunner manager)
    {
        _manager = manager;
        _parentTransform = manager.transform;
    }

    public void CreateGrid(int width, int height)
    {
        ClearGrid();

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector2Int gridPosition = new Vector2Int(x, y);
                GameObject tileObject = Object.Instantiate(_manager.BuildingTilePrefab, new Vector3(x, -y, TileZDepth), Quaternion.identity, _parentTransform);
                tileObject.name = $"Tile_{x}_{y}";

                if (tileObject.TryGetComponent(out BuildingTile tileComponent))
                {
                    tileComponent.Initialize(gridPosition, null);
                }

                _tileObjectMap[gridPosition] = tileObject;
            }
        }
    }

    public void ExpandGrid(int newWidth, int newHeight) => CreateGrid(newWidth, newHeight);

    public void ClearGrid()
    {
        ClearAllPlacedBuildings();
        foreach (GameObject tile in _tileObjectMap.Values)
        {
            if (tile != null) Object.Destroy(tile);
        }
        _tileObjectMap.Clear();
    }

    public void ClearAllPlacedBuildings()
    {
        foreach (GameObject building in _buildingOriginMap.Values)
        {
            if (building != null) Object.Destroy(building);
        }
        _buildingOriginMap.Clear();
        _occupancyMap.Clear();
        ClearAllTileOccupancyStatus();
    }

    private void ClearAllTileOccupancyStatus()
    {
        foreach (GameObject tileObject in _tileObjectMap.Values)
        {
            if (tileObject.TryGetComponent(out BuildingTile tile))
            {
                tile.SetOccupied(false);
            }
        }
    }

    public GameObject CreateBuildingObject(Vector2Int gridPosition, BuildingData buildingData, BuildingState buildingState = null, bool playPlaceAnimation = false)
    {
        if (buildingData?.buildingSprite == null) return null;

        GameObject buildingObject;
        if (_manager.BuildingObjectPrefab != null)
            buildingObject = Object.Instantiate(_manager.BuildingObjectPrefab, _parentTransform);
        else
            buildingObject = new GameObject($"Building_{buildingData.id}");

        buildingObject.name = $"Building_{buildingData.id}_{gridPosition}";
        
        int rotation = buildingState?.rotation ?? 0;
        Vector2Int rotatedSize = GridMathUtils.GetRotatedSize(buildingData.size, rotation);
        
        buildingObject.transform.position = GridMathUtils.GetGridToWorldPos(_parentTransform, gridPosition, rotatedSize, BuildingZDepth);
        buildingObject.transform.rotation = Quaternion.Euler(0, 0, -rotation * 90f);

        SpriteRenderer renderer = buildingObject.GetComponent<SpriteRenderer>();
        renderer.sprite = buildingData.buildingSprite;
        Vector3 targetScale = GameObjectUtils.CalculateSpriteScale(buildingData.buildingSprite, buildingData.size);
        buildingObject.transform.localScale = targetScale;

        renderer.color = Color.white;

        BuildingObject buildingComponent = buildingObject.GetComponent<BuildingObject>();
        BoxCollider2D boxCollider = buildingObject.GetComponent<BoxCollider2D>();
        boxCollider.size = new Vector2(buildingData.size.x, buildingData.size.y);

        if (buildingState != null)
        {
            buildingComponent.Initialize(buildingData, buildingState, _manager.InputMarkerPrefab, _manager.OutputMarkerPrefab, this);
            buildingComponent.SetupProductionIcons();
            
            if (buildingData.IsRoad)
            {
                buildingComponent.SetupRoadResources(_manager.RoadHandler);
            }
        }

        RegisterBuildingToMaps(gridPosition, rotatedSize, buildingObject);
        
        if (playPlaceAnimation)
        {
            Vector3 originalScale = buildingObject.transform.localScale;
            buildingObject.transform.localScale = Vector3.zero;
            buildingObject.transform.DOScale(originalScale, 0.2f)
                .SetEase(Ease.OutBack)
                .SetUpdate(true)
                .SetLink(buildingObject);
        }

        return buildingObject;
    }

    private void RegisterBuildingToMaps(Vector2Int origin, Vector2Int size, GameObject buildingObj)
    {
        _buildingOriginMap[origin] = buildingObj;
        
        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                Vector2Int pos = new Vector2Int(origin.x + x, origin.y + y);
                _occupancyMap[pos] = buildingObj;
                SetTileOccupied(pos, true);
            }
        }
    }

    public void RemoveBuildingObject(Vector2Int origin)
    {
        if (_buildingOriginMap.TryGetValue(origin, out GameObject buildingObj))
        {
            if (buildingObj.TryGetComponent(out BuildingObject comp) && comp.BuildingData != null)
            {
                Vector2Int rotatedSize = GridMathUtils.GetRotatedSize(comp.BuildingData.size, comp.BuildingState.rotation);
                
                for (int x = 0; x < rotatedSize.x; x++)
                {
                    for (int y = 0; y < rotatedSize.y; y++)
                    {
                        Vector2Int pos = new Vector2Int(origin.x + x, origin.y + y);
                        _occupancyMap.Remove(pos);
                        SetTileOccupied(pos, false);
                    }
                }
            }

            Object.Destroy(buildingObj);
            _buildingOriginMap.Remove(origin);
        }
    }

    public GameObject GetBuildingAtPosition(Vector2Int gridPosition)
    {
        return _occupancyMap.TryGetValue(gridPosition, out GameObject obj) ? obj : null;
    }
    
    /// <summary>
    /// 특정 origin 위치의 건물 오브젝트를 가져옵니다.
    /// </summary>
    public GameObject GetBuildingAtOrigin(Vector2Int originPosition)
    {
        return _buildingOriginMap.TryGetValue(originPosition, out GameObject obj) ? obj : null;
    }

    /// <summary>
    /// 특정 그리드 위치의 타일 오브젝트를 가져옵니다.
    /// </summary>
    public GameObject GetTileAtPosition(Vector2Int gridPosition)
    {
        return _tileObjectMap.TryGetValue(gridPosition, out GameObject tile) ? tile : null;
    }
    
    /// <summary>
    /// 모든 타일의 위치를 가져옵니다.
    /// </summary>
    public Dictionary<Vector2Int, GameObject> GetAllTiles()
    {
        return new Dictionary<Vector2Int, GameObject>(_tileObjectMap);
    }
    
    /// <summary>
    /// 특정 위치가 도로인지 확인합니다.
    /// </summary>
    public bool IsRoadAtPosition(Vector2Int gridPosition, Dictionary<Vector2Int, BuildingState> gridMap = null)
    {
        if (gridMap != null && gridMap.TryGetValue(gridPosition, out BuildingState state))
        {
            BuildingData data = DataManager.Building.GetBuildingData(state.Id);
            return data != null && data.IsRoad;
        }
        return false;
    }

    private void SetTileOccupied(Vector2Int pos, bool occupied)
    {
        if (_tileObjectMap.TryGetValue(pos, out GameObject tileObj) && 
            tileObj.TryGetComponent(out BuildingTile tile))
        {
            tile.SetOccupied(occupied);
        }
    }

    public void MarkTilesAsOccupied(Vector2Int startPosition, Vector2Int size, bool occupied = true)
    {
        for (int y = 0; y < size.y; y++)
        {
            for (int x = 0; x < size.x; x++)
            {
                Vector2Int currentPosition = new Vector2Int(startPosition.x + x, startPosition.y + y);
                SetTileOccupied(currentPosition, occupied);
            }
        }
    }

    public bool CanPlaceBuilding(Vector2Int startGridPos, Vector2Int rotatedSize)
    {
        if (!IsWithinBounds(startGridPos, rotatedSize)) return false;

        for (int y = 0; y < rotatedSize.y; y++)
        {
            for (int x = 0; x < rotatedSize.x; x++)
            {
                if (_occupancyMap.ContainsKey(new Vector2Int(startGridPos.x + x, startGridPos.y + y)))
                    return false;
            }
        }
        return true;
    }
    
    private bool IsWithinBounds(Vector2Int gridPos, Vector2Int size)
    {
        return gridPos.x >= 0 && gridPos.y >= 0 &&
               gridPos.x + size.x <= _manager.GridWidth && 
               gridPos.y + size.y <= _manager.GridHeight;
    }

    /// <summary>
    /// 건물 상태 리스트로부터 그리드 맵을 생성합니다.
    /// </summary>
    public Dictionary<Vector2Int, BuildingState> BuildGridMap(List<BuildingState> states)
    {
        Dictionary<Vector2Int, BuildingState> map = new Dictionary<Vector2Int, BuildingState>();
        
        if (states == null) return map;
        
        foreach (BuildingState state in states)
        {
            BuildingData data = DataManager.Building.GetBuildingData(state.Id);
            if (data == null) continue;

            Vector2Int size = GridMathUtils.GetRotatedSize(data.size, state.rotation);
            for (int x = 0; x < size.x; x++)
            {
                for (int y = 0; y < size.y; y++)
                {
                    Vector2Int pos = new Vector2Int(state.positionX + x, state.positionY + y);
                    map[pos] = state;
                }
            }
        }
        
        return map;
    }

    public Vector2Int WorldToGridPosition(Vector3 worldPosition)
    {
        return GridMathUtils.GetWorldToGridPos(_parentTransform, worldPosition);
    }

    public Vector3 GridToWorldPosition(Vector2Int gridPosition, Vector2Int size)
    {
        return GridMathUtils.GetGridToWorldPos(_parentTransform, gridPosition, size, BuildingZDepth);
    }
}
