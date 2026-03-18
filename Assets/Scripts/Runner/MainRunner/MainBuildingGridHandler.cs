using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// 메인에서 사용하는 건물 그리드/점유/오브젝트 생성 핸들러.
/// </summary>
public class MainBuildingGridHandler
{
    private readonly Transform _parent;
    private readonly GameObject _tilePrefab;
    private readonly GameObject _buildingPrefab;

    private int _width;
    private int _height;

    private readonly Dictionary<Vector2Int, BuildingTile> _tiles = new Dictionary<Vector2Int, BuildingTile>();
    private readonly Dictionary<Vector2Int, GameObject> _buildingOrigins = new Dictionary<Vector2Int, GameObject>();
    private readonly Dictionary<Vector2Int, Vector2Int> _occupiedToOrigin = new Dictionary<Vector2Int, Vector2Int>();

    private const float TileZ = 10f;
    private const float BuildingZ = 9f;

    public int Width => _width;
    public int Height => _height;

    public MainBuildingGridHandler(Transform parent, GameObject tilePrefab, GameObject buildingPrefab, int width, int height)
    {
        _parent = parent;
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
                GameObject tileObj = Object.Instantiate(_tilePrefab, _parent);
                tileObj.transform.localPosition = new Vector3(x, -y, TileZ);

                if (!tileObj.TryGetComponent(out BuildingTile tile))
                {
                    tile = tileObj.AddComponent<BuildingTile>();
                }

                tile.Initialize(p);
                _tiles[p] = tile;
            }
        }
    }

    public void ClearGrid()
    {
        ClearAllBuildings();

        foreach (BuildingTile t in _tiles.Values)
        {
            if (t != null) Object.Destroy(t.gameObject);
        }
        _tiles.Clear();
    }

    public void ClearAllBuildings()
    {
        foreach (GameObject b in _buildingOrigins.Values)
        {
            if (b != null) Object.Destroy(b);
        }
        _buildingOrigins.Clear();
        _occupiedToOrigin.Clear();

        foreach (BuildingTile t in _tiles.Values)
        {
            t?.SetOccupied(false);
        }
    }

    public Vector2Int WorldToGridPosition(Vector3 worldPos)
    {
        Vector3 lp = worldPos - _parent.position;
        return new Vector2Int(Mathf.RoundToInt(lp.x), Mathf.RoundToInt(-lp.y));
    }

    public Vector3 GridToWorldPosition(Vector2Int gridPos, Vector2Int size)
    {
        float cx = gridPos.x + (size.x - 1) * 0.5f;
        float cy = -gridPos.y - (size.y - 1) * 0.5f;
        return _parent.position + new Vector3(cx, cy, BuildingZ);
    }

    public static Vector2Int GetRotatedSize(Vector2Int size, int rotation)
    {
        rotation %= 4;
        return (rotation == 1 || rotation == 3) ? new Vector2Int(size.y, size.x) : size;
    }

    public bool IsWithinBounds(Vector2Int origin, Vector2Int size)
    {
        return origin.x >= 0 && origin.y >= 0 &&
               origin.x + size.x <= _width &&
               origin.y + size.y <= _height;
    }

    public bool CanPlace(Vector2Int origin, Vector2Int size)
    {
        if (!IsWithinBounds(origin, size)) return false;

        for (int dx = 0; dx < size.x; dx++)
        {
            for (int dy = 0; dy < size.y; dy++)
            {
                if (_occupiedToOrigin.ContainsKey(new Vector2Int(origin.x + dx, origin.y + dy)))
                    return false;
            }
        }
        return true;
    }

    public bool TryPlaceBuilding(BuildingData data, Vector2Int origin, int rotation, out GameObject placed)
    {
        placed = null;
        if (data == null || data.buildingSprite == null) return false;

        Vector2Int rotatedSize = GetRotatedSize(data.size, rotation);
        if (!CanPlace(origin, rotatedSize)) return false;

        GameObject obj = _buildingPrefab != null ? Object.Instantiate(_buildingPrefab, _parent) : new GameObject($"Building_{data.id}");
        obj.name = $"Building_{data.id}_{origin.x}_{origin.y}";

        obj.transform.position = GridToWorldPosition(origin, rotatedSize);
        BuildingObject placedComp = obj.GetComponent<BuildingObject>();
        if (placedComp == null) placedComp = obj.AddComponent<BuildingObject>();
        placedComp.Init(data, origin, rotatedSize, rotation);

        RegisterOccupancy(origin, rotatedSize);
        _buildingOrigins[origin] = obj;

        Vector3 s = obj.transform.localScale;
        obj.transform.localScale = Vector3.zero;
        obj.transform.DOScale(s, 0.18f).SetEase(Ease.OutBack).SetUpdate(true).SetLink(obj);

        placed = obj;
        return true;
    }

    public bool TryRemoveAt(Vector2Int anyOccupiedCell)
    {
        if (!_occupiedToOrigin.TryGetValue(anyOccupiedCell, out Vector2Int origin))
            return false;

        if (!_buildingOrigins.TryGetValue(origin, out GameObject obj) || obj == null)
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
        _buildingOrigins.Remove(origin);
        Object.Destroy(obj);
        return true;
    }

    private void RegisterOccupancy(Vector2Int origin, Vector2Int size)
    {
        for (int dx = 0; dx < size.x; dx++)
        {
            for (int dy = 0; dy < size.y; dy++)
            {
                Vector2Int p = new Vector2Int(origin.x + dx, origin.y + dy);
                _occupiedToOrigin[p] = origin;
                if (_tiles.TryGetValue(p, out BuildingTile t)) t.SetOccupied(true);
            }
        }
    }

    private void UnregisterOccupancy(Vector2Int origin, Vector2Int size)
    {
        for (int dx = 0; dx < size.x; dx++)
        {
            for (int dy = 0; dy < size.y; dy++)
            {
                Vector2Int p = new Vector2Int(origin.x + dx, origin.y + dy);
                _occupiedToOrigin.Remove(p);
                if (_tiles.TryGetValue(p, out BuildingTile t)) t.SetOccupied(false);
            }
        }
    }
}

