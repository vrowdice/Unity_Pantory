using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// 메인에서 사용하는 건물 그리드/점유/오브젝트 생성 핸들러.
/// </summary>
public class MainBuildingGridHandler
{
    private readonly Transform _tileParent;
    private readonly Transform _buildingParent;
    private readonly Transform _roadParent;

    private readonly GameObject _tilePrefab;
    private readonly GameObject _roadPrefab;
    private readonly GameObject _buildingPrefab;

    private int _width;
    private int _height;

    private readonly Dictionary<Vector2Int, BuildingTile> _tileList = new Dictionary<Vector2Int, BuildingTile>();
    private readonly Dictionary<Vector2Int, GameObject> _buildingObjectList = new Dictionary<Vector2Int, GameObject>();
    private readonly Dictionary<Vector2Int, GameObject> _roadObjectList = new Dictionary<Vector2Int, GameObject>();
    private readonly Dictionary<Vector2Int, Vector2Int> _occupiedAsObject = new Dictionary<Vector2Int, Vector2Int>();

    private const float TileZ = 10f;
    private const float BuildingZ = 9f;

    public MainBuildingGridHandler(Transform parent, GameObject tilePrefab, GameObject buildingPrefab, int width, int height)
    {
        GameObject tileParentObj = new GameObject("Tiles");
        tileParentObj.transform.SetParent(parent, worldPositionStays: false);
        _tileParent = tileParentObj.transform;

        GameObject buildingParentObj = new GameObject("Buildings");
        buildingParentObj.transform.SetParent(parent, worldPositionStays: false);
        _buildingParent = buildingParentObj.transform;

        GameObject roadParentObj = new GameObject("Roads");
        roadParentObj.transform.SetParent(parent, worldPositionStays: false);
        _roadParent = roadParentObj.transform;

        _tilePrefab = tilePrefab;
        _buildingPrefab = buildingPrefab;
        _width = width;
        _height = height;
    }

    public void CreateGrid(int width, int height)
    {
        ClearGrid();
        _width = width;
        _height = height;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector2Int p = new Vector2Int(x, y);
                GameObject tileObj = Object.Instantiate(_tilePrefab, _tileParent);
                tileObj.transform.localPosition = new Vector3(x, -y, TileZ);

                if (!tileObj.TryGetComponent(out BuildingTile tile))
                {
                    tile = tileObj.AddComponent<BuildingTile>();
                }

                tile.Initialize(p);
                _tileList[p] = tile;
            }
        }
    }

    public void ClearGrid()
    {
        ClearAllBuildings();

        foreach (BuildingTile tile in _tileList.Values)
        {
            if (tile != null) Object.Destroy(tile.gameObject);
        }
        _tileList.Clear();
    }

    public void ClearAllBuildings()
    {
        foreach (GameObject building in _buildingObjectList.Values)
        {
            if (building != null) Object.Destroy(building);
        }
        _buildingObjectList.Clear();
        _occupiedAsObject.Clear();

        foreach (BuildingTile tile in _tileList.Values)
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
               position.x + size.x <= _width &&
               position.y + size.y <= _height;
    }

    public bool CanPlace(Vector2Int position, Vector2Int size)
    {
        if (!IsWithinBounds(position, size)) return false;

        for (int dx = 0; dx < size.x; dx++)
        {
            for (int dy = 0; dy < size.y; dy++)
            {
                if (_occupiedAsObject.ContainsKey(new Vector2Int(position.x + dx, position.y + dy)))
                    return false;
            }
        }
        return true;
    }

    public bool TryPlaceRoad(Vector2Int position, out GameObject placed)
    {
        placed = null;
        if (!IsWithinBounds(position, Vector2Int.one) || _occupiedAsObject.ContainsKey(position)) return false;

        GameObject obj = _buildingPrefab != null ? Object.Instantiate(_buildingPrefab, _roadParent) : new GameObject($"Road_{position.x}_{position.y}");
        obj.name = $"Road_{position.x}_{position.y}";

        obj.transform.position = GridToWorldPosition(position, Vector2Int.one);
        _roadObjectList[position] = obj;
        Vector3 s = obj.transform.localScale;

        obj.transform.localScale = Vector3.zero;
        obj.transform.DOScale(s, 0.18f).SetEase(Ease.OutBack).SetUpdate(true).SetLink(obj);
        _occupiedAsObject[position] = position;
        if (_tileList.TryGetValue(position, out BuildingTile t)) t.SetOccupied(true);

        placed = obj;
        return true;
    }

    public bool TryPlaceBuilding(BuildingData data, Vector2Int position, int rotation, out GameObject placed)
    {
        placed = null;
        if (data == null || data.buildingSprite == null) return false;

        Vector2Int rotatedSize = GetRotatedSize(data.size, rotation);
        if (!CanPlace(position, rotatedSize)) return false;

        GameObject obj = Object.Instantiate(_buildingPrefab, _buildingParent);
        obj.name = $"Building_{data.id}_{position.x}_{position.y}";

        obj.transform.position = GridToWorldPosition(position, rotatedSize);
        BuildingObject placedComp = obj.GetComponent<BuildingObject>();
        placedComp.Init(data, position, rotatedSize, rotation);

        RegisterOccupancy(position, rotatedSize);
        _buildingObjectList[position] = obj;

        Vector3 s = obj.transform.localScale;
        obj.transform.localScale = Vector3.zero;
        obj.transform.DOScale(s, 0.18f).SetEase(Ease.OutBack).SetUpdate(true).SetLink(obj);

        placed = obj;
        return true;
    }

    public bool TryRemoveAt(Vector2Int anyOccupiedCell)
    {
        if (!_occupiedAsObject.TryGetValue(anyOccupiedCell, out Vector2Int origin))
            return false;

        if (!_buildingObjectList.TryGetValue(origin, out GameObject obj) || obj == null)
            return false;

        Vector2Int size = Vector2Int.one;
        if (obj.TryGetComponent(out BuildingObject placed))
        {
            size = placed.Size;
        }
        else if (obj.TryGetComponent(out BoxCollider2D bc))
        {
            size = new Vector2Int(Mathf.RoundToInt(bc.size.x), Mathf.RoundToInt(bc.size.y));
        }

        UnregisterOccupancy(origin, size);
        _buildingObjectList.Remove(origin);
        Object.Destroy(obj);
        return true;
    }

    private void RegisterOccupancy(Vector2Int position, Vector2Int size)
    {
        for (int dx = 0; dx < size.x; dx++)
        {
            for (int dy = 0; dy < size.y; dy++)
            {
                Vector2Int p = new Vector2Int(position.x + dx, position.y + dy);
                _occupiedAsObject[p] = position;
                if (_tileList.TryGetValue(p, out BuildingTile t)) t.SetOccupied(true);
            }
        }
    }

    private void UnregisterOccupancy(Vector2Int position, Vector2Int size)
    {
        for (int dx = 0; dx < size.x; dx++)
        {
            for (int dy = 0; dy < size.y; dy++)
            {
                Vector2Int p = new Vector2Int(position.x + dx, position.y + dy);
                _occupiedAsObject.Remove(p);
                if (_tileList.TryGetValue(p, out BuildingTile t)) t.SetOccupied(false);
            }
        }
    }
}

