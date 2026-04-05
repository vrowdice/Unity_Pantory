using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// 메인에서 사용하는 건물 그리드/점유/오브젝트 생성 핸들러.
/// </summary>
public class MainBuildingGridHandler
{
    private readonly MainRunner _runner;
    private readonly DataManager _dataManager;

    private readonly Transform _tileParent;
    private readonly Transform _buildingParent;
    private readonly Transform _roadParent;

    private readonly Dictionary<string, BuildingObject> _buildingObjDict = new();
    private readonly Dictionary<string, RoadObject> _roadObjDict = new();
    private readonly Dictionary<Vector2Int, string> _occupiedAsObjectDict = new();
    private readonly Dictionary<Vector2Int, BuildingTile> _tileDict = new();

    private const float TileZ = 10f;
    private const float BuildingZ = 9f;

    public MainBuildingGridHandler(MainRunner runner)
    {
        _runner = runner;
        _dataManager = DataManager.Instance;

        GameObject tileParentObj = new GameObject("Tiles");
        tileParentObj.transform.SetParent(runner.transform, worldPositionStays: false);
        _tileParent = tileParentObj.transform;

        GameObject buildingParentObj = new GameObject("Buildings");
        buildingParentObj.transform.SetParent(runner.transform, worldPositionStays: false);
        _buildingParent = buildingParentObj.transform;

        GameObject roadParentObj = new GameObject("Roads");
        roadParentObj.transform.SetParent(runner.transform, worldPositionStays: false);
        _roadParent = roadParentObj.transform;
    }

    public void CreateGrid(int width, int height)
    {
        ClearGrid();

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector2Int p = new Vector2Int(x, y);
                GameObject tileObj = Object.Instantiate(_runner.TilePrefab, _tileParent);
                tileObj.transform.localPosition = new Vector3(x, -y, TileZ);

                if (!tileObj.TryGetComponent(out BuildingTile tile))
                {
                    tile = tileObj.AddComponent<BuildingTile>();
                }

                tile.Initialize(p);
                _tileDict[p] = tile;
            }
        }
    }

    public void ClearGrid()
    {
        ClearAllBuildings();

        foreach (BuildingTile tile in _tileDict.Values)
        {
            if (tile != null) Object.Destroy(tile.gameObject);
        }
        _tileDict.Clear();
    }

    public void ClearAllBuildings()
    {
        foreach (BuildingObject building in _buildingObjDict.Values)
        {
            if (building != null) Object.Destroy(building.gameObject);
        }
        _buildingObjDict.Clear();

        foreach (RoadObject road in _roadObjDict.Values)
        {
            if (road != null) Object.Destroy(road.gameObject);
        }
        _roadObjDict.Clear();

        _occupiedAsObjectDict.Clear();

        foreach (BuildingTile tile in _tileDict.Values)
        {
            tile?.SetOccupied(false);
        }
    }

    public Vector2Int WorldToGridPosition(Vector3 worldPos)
    {
        Vector3 lp = worldPos - _tileParent.position;
        return new Vector2Int(Mathf.RoundToInt(lp.x), Mathf.RoundToInt(-lp.y));
    }

    public Vector3 GridToWorldPosition(Vector2Int gridPos, Vector2Int size)
    {
        float cx = gridPos.x + (size.x - 1) * 0.5f;
        float cy = -gridPos.y - (size.y - 1) * 0.5f;
        return _tileParent.position + new Vector3(cx, cy, BuildingZ);
    }

    public static Vector2Int GetRotatedSize(Vector2Int size, int rotation)
    {
        rotation %= 4;
        return (rotation == 1 || rotation == 3) ? new Vector2Int(size.y, size.x) : size;
    }

    public bool IsWithinBounds(Vector2Int position, Vector2Int size)
    {
        return position.x >= 0 && position.y >= 0 &&
               position.x + size.x <= _runner.GridWidth &&
               position.y + size.y <= _runner.GridHeight;
    }

    public bool CanPlace(Vector2Int position, Vector2Int size)
    {
        if (!IsWithinBounds(position, size)) return false;

        for (int dx = 0; dx < size.x; dx++)
        {
            for (int dy = 0; dy < size.y; dy++)
            {
                if (_occupiedAsObjectDict.ContainsKey(new Vector2Int(position.x + dx, position.y + dy)))
                    return false;
            }
        }
        return true;
    }

    public bool TryPlaceRoad(Vector2Int position, int rotation, out GameObject placed)
    {
        placed = null;
        if (!IsWithinBounds(position, Vector2Int.one) || _occupiedAsObjectDict.ContainsKey(position)) return false;

        GameObject obj = Object.Instantiate(_runner.RoadObjectPrefab, _roadParent);
        obj.name = $"Road_{position.x}_{position.y}";

        obj.transform.position = GridToWorldPosition(position, Vector2Int.one);
        Vector3 s = obj.transform.localScale;

        obj.transform.localScale = Vector3.zero;
        obj.transform.DOScale(s, 0.18f).SetEase(Ease.OutBack).SetUpdate(true).SetLink(obj);

        RoadObject roadObject = obj.GetComponent<RoadObject>();
        if (roadObject == null)
        {
            Object.Destroy(obj);
            return false;
        }

        roadObject.Init(position, rotation);
        string key = RoadGridKey(position);
        _roadObjDict[key] = roadObject;
        _occupiedAsObjectDict[position] = key;
        if (_tileDict.TryGetValue(position, out BuildingTile t)) t.SetOccupied(true);

        placed = obj;
        return true;
    }

    public bool TryPlaceBuilding(BuildingData data, Vector2Int position, int rotation, out GameObject placed)
    {
        placed = null;
        if (data == null || data.buildingSprite == null) return false;

        Vector2Int rotatedSize = GetRotatedSize(data.size, rotation);
        if (!CanPlace(position, rotatedSize)) return false;

        GameObject obj = Object.Instantiate(_runner.BuildingObjectPrefab, _buildingParent);
        obj.name = $"Building_{data.id}_{position.x}_{position.y}";
        obj.transform.position = GridToWorldPosition(position, rotatedSize);

        BuildingObject building = obj.GetComponent<BuildingObject>();
        building.Init(_runner, data, position, rotatedSize, rotation);

        string key = BuildingGridKey(position);
        RegisterBuildingOccupancy(position, rotatedSize, key);
        _buildingObjDict[key] = building;

        Vector3 s = obj.transform.localScale;
        obj.transform.localScale = Vector3.zero;
        obj.transform.DOScale(s, 0.18f).SetEase(Ease.OutBack).SetUpdate(true).SetLink(obj);

        placed = obj;
        return true;
    }

    public bool TryRemoveAt(Vector2Int anyOccupiedCell)
    {
        if (!_occupiedAsObjectDict.TryGetValue(anyOccupiedCell, out string key)) return false;

        if (_roadObjDict.TryGetValue(key, out RoadObject road))
        {
            Vector2Int pos = road.GridPosition;
            _occupiedAsObjectDict.Remove(pos);
            _roadObjDict.Remove(key);
            if (_tileDict.TryGetValue(pos, out BuildingTile t)) t.SetOccupied(false);
            Object.Destroy(road.gameObject);
            return true;
        }

        if (_buildingObjDict.TryGetValue(key, out BuildingObject building))
        {
            Vector2Int origin = building.Origin;
            Vector2Int size = building.Size;
            UnregisterOccupancy(origin, size);
            _buildingObjDict.Remove(key);
            Object.Destroy(building.gameObject);
            return true;
        }

        return false;
    }

    /// <summary>
    /// 시간 틱마다 건물 시뮬레이션(생산/하역 진행·적재) 후 도로/건물 간 패킷 이동.
    /// </summary>
    public void OnMainHourChanged()
    {
        if (_dataManager != null)
        {
            foreach (BuildingObject building in _buildingObjDict.Values)
            {
                if (building == null) continue;
                building.TickSimulation(_dataManager);
            }
        }

        TickResourceFlow();
    }

    public void TickResourceFlow()
    {
        foreach (RoadObject road in _roadObjDict.Values)
        {
            road.ResetRoadForwardGatesForQueuedPackets();
        }

        foreach (RoadObject road in _roadObjDict.Values)
        {
            if (road == null || road.IsEmpty) continue;

            foreach (Vector2Int outCell in road.OutputGridPositions)
            {
                if (!TryGetResourceNodeAtCell(outCell, out IResourceNode destNode)) continue;
                if (ReferenceEquals(road, destNode)) continue;
                road.TryForwardTo(destNode);
                break;
            }
        }

        foreach (BuildingObject building in _buildingObjDict.Values)
        {
            if (building == null) continue;
            foreach (Vector2Int outCell in building.OutputGridPositions)
            {
                if (!TryGetResourceNodeAtCell(outCell, out IResourceNode destNode)) continue;
                if (ReferenceEquals(building, destNode)) continue;
                building.TryForwardTo(destNode);
                break;
            }
        }
    }

    private static string BuildingGridKey(Vector2Int origin)
    {
        return $"b:{origin.x}_{origin.y}";
    }

    private static string RoadGridKey(Vector2Int pos)
    {
        return $"r:{pos.x}_{pos.y}";
    }

    private bool TryGetResourceNodeAtCell(Vector2Int pos, out IResourceNode node)
    {
        node = null;
        if (!_occupiedAsObjectDict.TryGetValue(pos, out string key)) return false;
        if (_roadObjDict.TryGetValue(key, out RoadObject road))
        {
            node = road;
            return true;
        }
        if (_buildingObjDict.TryGetValue(key, out BuildingObject building))
        {
            node = building;
            return true;
        }
        return false;
    }

    private void RegisterBuildingOccupancy(Vector2Int origin, Vector2Int size, string instanceKey)
    {
        for (int sizeX = 0; sizeX < size.x; sizeX++)
        {
            for (int sizeY = 0; sizeY < size.y; sizeY++)
            {
                Vector2Int pos = new Vector2Int(origin.x + sizeX, origin.y + sizeY);
                _occupiedAsObjectDict[pos] = instanceKey;
                if (_tileDict.TryGetValue(pos, out BuildingTile t)) t.SetOccupied(true);
            }
        }
    }

    private void UnregisterOccupancy(Vector2Int position, Vector2Int size)
    {
        for (int sizeX = 0; sizeX < size.x; sizeX++)
        {
            for (int sizey = 0; sizey < size.y; sizey++)
            {
                Vector2Int pos = new Vector2Int(position.x + sizeX, position.y + sizey);
                _occupiedAsObjectDict.Remove(pos);
                if (_tileDict.TryGetValue(pos, out BuildingTile t)) t.SetOccupied(false);
            }
        }
    }
}
