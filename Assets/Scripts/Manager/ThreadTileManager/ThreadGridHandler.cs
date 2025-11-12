using System.Collections.Generic;
using UnityEngine;

namespace Pantory.Managers
{
    public class ThreadGridHandler
    {
        private readonly global::ThreadTileManager _manager;
    private readonly Transform _parentTransform;
    private readonly GameObject _threadTilePrefab;
    private readonly GameObject _threadObjectPrefab;

    private int _gridWidth;
    private int _gridHeight;

    private readonly Dictionary<Vector2Int, ThreadTile> _threadTiles = new Dictionary<Vector2Int, ThreadTile>();
    private readonly Dictionary<Vector2Int, ThreadObject> _threadObjects = new Dictionary<Vector2Int, ThreadObject>();

        public ThreadGridHandler(global::ThreadTileManager manager, GameObject threadTilePrefab, GameObject threadObjectPrefab, int width, int height)
        {
            _manager = manager;
            _parentTransform = manager.transform;
            _threadTilePrefab = threadTilePrefab;
            _threadObjectPrefab = threadObjectPrefab;
            _gridWidth = width;
            _gridHeight = height;
        }

    public void CreateGrid(int width, int height)
    {
        ClearGrid();

        _gridWidth = width;
        _gridHeight = height;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector2Int gridPos = new Vector2Int(x, y);
                GameObject tileObj = Object.Instantiate(_threadTilePrefab, _parentTransform);
                tileObj.transform.localPosition = new Vector3(x, -y, 10f);
                tileObj.name = $"ThreadTile_{x}_{y}";

                ThreadTile tile = tileObj.GetComponent<ThreadTile>();
                if (tile == null)
                {
                    tile = tileObj.AddComponent<ThreadTile>();
                }
                tile.Initialize(gridPos);

                _threadTiles[gridPos] = tile;
            }
        }
    }

    public void ClearGrid()
    {
        ClearAllThreadObjects();

        foreach (var tile in _threadTiles.Values)
        {
            if (tile != null)
            {
                Object.Destroy(tile.gameObject);
            }
        }

        _threadTiles.Clear();
    }

    public bool CanPlaceThread(Vector2Int gridPos)
    {
        if (gridPos.x < 0 || gridPos.y < 0)
            return false;

        if (gridPos.x >= _gridWidth || gridPos.y >= _gridHeight)
            return false;

        return !_threadObjects.ContainsKey(gridPos);
    }

    public void SetTileOccupied(Vector2Int gridPos, bool occupied)
    {
        if (_threadTiles.TryGetValue(gridPos, out ThreadTile tile))
        {
            tile.SetOccupied(occupied);
        }
    }

    public ThreadObject CreateThreadObject(Vector2Int gridPos, ThreadState threadState)
    {
        if (_threadObjectPrefab == null)
            return null;

        if (_threadObjects.TryGetValue(gridPos, out ThreadObject existing))
        {
            Object.Destroy(existing.gameObject);
            _threadObjects.Remove(gridPos);
        }

        GameObject obj = Object.Instantiate(_threadObjectPrefab, _parentTransform);
        obj.transform.position = GridToWorldPosition(gridPos);
        obj.name = $"ThreadObject_{threadState?.threadId ?? "Unknown"}_{gridPos.x}_{gridPos.y}";

        ThreadObject threadObject = obj.GetComponent<ThreadObject>();
        if (threadObject == null)
        {
            threadObject = obj.AddComponent<ThreadObject>();
        }

        threadObject.Initialize(threadState, _manager.SharedThreadLabelCanvas);
        threadObject.SetGridPosition(gridPos);
        _threadObjects[gridPos] = threadObject;
        return threadObject;
    }

    public void RemoveThreadObject(Vector2Int gridPos)
    {
        if (_threadObjects.TryGetValue(gridPos, out ThreadObject threadObject))
        {
            if (threadObject != null)
            {
                Object.Destroy(threadObject.gameObject);
            }

            _threadObjects.Remove(gridPos);
        }

        SetTileOccupied(gridPos, false);
    }

    public ThreadObject GetThreadObjectAt(Vector2Int gridPos)
    {
        _threadObjects.TryGetValue(gridPos, out ThreadObject threadObject);
        return threadObject;
    }

    public void ClearAllThreadObjects()
    {
        foreach (var obj in _threadObjects.Values)
        {
            if (obj != null)
            {
                Object.Destroy(obj.gameObject);
            }
        }

        _threadObjects.Clear();

        foreach (var tile in _threadTiles.Values)
        {
            tile?.SetOccupied(false);
        }
    }

    public void SetAllTilesOutline(bool visible, Color color = default)
    {
        foreach (var tile in _threadTiles.Values)
        {
            tile?.SetOutlineVisible(visible, color);
        }
    }

    public Vector2Int WorldToGridPosition(Vector3 worldPosition)
    {
        Vector3 localPos = worldPosition - _parentTransform.position;
        int x = Mathf.FloorToInt(localPos.x + 0.5f);
        int y = Mathf.FloorToInt(-localPos.y + 0.5f);
        return new Vector2Int(x, y);
    }

    public Vector3 GridToWorldPosition(Vector2Int gridPos)
    {
        return _parentTransform.position + new Vector3(gridPos.x, -gridPos.y, 9f);
    }

    public ThreadTile GetTile(Vector2Int gridPos)
    {
        _threadTiles.TryGetValue(gridPos, out ThreadTile tile);
        return tile;
    }

    public bool HasTile(Vector2Int gridPos)
    {
        return _threadTiles.ContainsKey(gridPos);
    }
    }
}

