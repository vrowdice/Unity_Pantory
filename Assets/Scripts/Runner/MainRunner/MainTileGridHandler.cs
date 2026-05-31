using System;
using System.Collections.Generic;
using UnityEngine;

public class MainTileGridHandler
{
    private readonly MainRunner _mainRunner;
    private readonly Transform _tileParent;
    private readonly Dictionary<Vector2Int, BuildingTile> _tileDict = new();

    private const float TileZ = 10f;
    private const float TileViewportCullMargin = 0.75f;
    private const int TileViewportCullPaddingCells = 2;

    private Vector2Int _lastVisibleCellMin;
    private Vector2Int _lastVisibleCellMax;
    private bool _tileViewportCullInitialized;
    private bool _tileOverviewModeActive;
    private float _lastTileZoomOrthoSize = -1f;

    private GameObject _gridOverviewObj;
    private SpriteRenderer _gridOverviewRenderer;

    public bool TileOverviewModeActive => _tileOverviewModeActive;
    public Dictionary<Vector2Int, BuildingTile> TileDict => _tileDict;

    public MainTileGridHandler(MainRunner runner, Transform tileParent)
    {
        _mainRunner = runner;
        _tileParent = tileParent;
    }

    public void CreateGrid(int width, int height, Action initializeRawBuildingsCallback)
    {
        ClearGrid();

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector2Int p = new Vector2Int(x, y);
                GameObject tileObj = MonoBehaviour.Instantiate(_mainRunner.TilePrefab, _tileParent);
                tileObj.transform.localPosition = new Vector3(x, -y, TileZ);
                tileObj.name = $"Tile_{x}_{y}";

                if (!tileObj.TryGetComponent(out BuildingTile tile))
                    tile = tileObj.AddComponent<BuildingTile>();

                tile.Initialize(p);
                _tileDict[p] = tile;
            }
        }

        _tileViewportCullInitialized = false;
        EnsureGridOverview(width, height);

        initializeRawBuildingsCallback?.Invoke();
    }

    public void ClearGrid()
    {
        foreach (BuildingTile tile in _tileDict.Values)
            MonoBehaviour.Destroy(tile.gameObject);
        _tileDict.Clear();

        _tileViewportCullInitialized = false;
        _tileOverviewModeActive = false;
        _lastTileZoomOrthoSize = -1f;
        DestroyGridOverview();
    }

    public void RefreshTileZoomVisuals(Camera camera)
    {
        if (camera == null || _tileDict.Count == 0)
            return;

        if (!camera.orthographic)
        {
            ApplyTileDetailMode(camera, forceDetail: true);
            return;
        }

        float orthoSize = camera.orthographicSize;
        if (_lastTileZoomOrthoSize >= 0f &&
            Mathf.Approximately(orthoSize, _lastTileZoomOrthoSize) &&
            _tileOverviewModeActive == (orthoSize >= _mainRunner.TileOverviewOrthographicSizeThreshold))
        {
            if (!_tileOverviewModeActive)
                RefreshTileViewportCulling(camera);
            return;
        }

        _lastTileZoomOrthoSize = orthoSize;
        bool useOverview = orthoSize >= _mainRunner.TileOverviewOrthographicSizeThreshold;
        if (useOverview)
            ApplyTileOverviewMode();
        else
            ApplyTileDetailMode(camera, forceDetail: false);
    }

    private void ApplyTileOverviewMode()
    {
        if (_tileOverviewModeActive)
            return;

        _tileOverviewModeActive = true;
        _tileViewportCullInitialized = false;

        if (_gridOverviewRenderer != null)
            _gridOverviewRenderer.enabled = true;

        foreach (BuildingTile tile in _tileDict.Values)
            tile.SetDetailRenderingEnabled(false);
    }

    private void ApplyTileDetailMode(Camera camera, bool forceDetail)
    {
        bool wasOverview = _tileOverviewModeActive;
        _tileOverviewModeActive = false;

        if (_gridOverviewRenderer != null)
            _gridOverviewRenderer.enabled = false;

        foreach (BuildingTile tile in _tileDict.Values)
            tile.SetDetailRenderingEnabled(true);

        if (wasOverview || forceDetail)
            _tileViewportCullInitialized = false;

        RefreshTileViewportCulling(camera);
    }

    public void RefreshTileViewportCulling(Camera camera)
    {
        if (camera == null || _tileDict.Count == 0 || _tileOverviewModeActive)
            return;

        if (!TryGetVisibleGridCellRange(camera, out Vector2Int cellMin, out Vector2Int cellMax))
            return;

        if (_tileViewportCullInitialized &&
            cellMin == _lastVisibleCellMin &&
            cellMax == _lastVisibleCellMax)
            return;

        _lastVisibleCellMin = cellMin;
        _lastVisibleCellMax = cellMax;
        _tileViewportCullInitialized = true;

        foreach (KeyValuePair<Vector2Int, BuildingTile> pair in _tileDict)
        {
            Vector2Int gridPos = pair.Key;
            bool inView = gridPos.x >= cellMin.x && gridPos.x <= cellMax.x &&
                          gridPos.y >= cellMin.y && gridPos.y <= cellMax.y;
            pair.Value.SetViewportCulled(!inView);
        }
    }

    private bool TryGetVisibleGridCellRange(Camera camera, out Vector2Int cellMin, out Vector2Int cellMax)
    {
        cellMin = default;
        cellMax = default;

        if (!camera.orthographic)
            return false;

        float halfHeight = camera.orthographicSize + TileViewportCullMargin;
        float halfWidth = halfHeight * camera.aspect;
        Vector3 center = camera.transform.position;

        Vector3 worldMin = new Vector3(center.x - halfWidth, center.y - halfHeight, 0f);
        Vector3 worldMax = new Vector3(center.x + halfWidth, center.y + halfHeight, 0f);

        Vector2Int g0 = WorldToGridPosition(worldMin);
        Vector2Int g1 = WorldToGridPosition(worldMax);
        Vector2Int g2 = WorldToGridPosition(new Vector3(worldMin.x, worldMax.y, 0f));
        Vector2Int g3 = WorldToGridPosition(new Vector3(worldMax.x, worldMin.y, 0f));

        int minX = Mathf.Min(g0.x, g1.x, g2.x, g3.x);
        int maxX = Mathf.Max(g0.x, g1.x, g2.x, g3.x);
        int minY = Mathf.Min(g0.y, g1.y, g2.y, g3.y);
        int maxY = Mathf.Max(g0.y, g1.y, g2.y, g3.y);

        int gridMaxX = _mainRunner.GridWidth - 1;
        int gridMaxY = _mainRunner.GridHeight - 1;

        cellMin = new Vector2Int(
            Mathf.Clamp(minX - TileViewportCullPaddingCells, 0, gridMaxX),
            Mathf.Clamp(minY - TileViewportCullPaddingCells, 0, gridMaxY));
        cellMax = new Vector2Int(
            Mathf.Clamp(maxX + TileViewportCullPaddingCells, 0, gridMaxX),
            Mathf.Clamp(maxY + TileViewportCullPaddingCells, 0, gridMaxY));

        return true;
    }

    private Vector2Int WorldToGridPosition(Vector3 worldPos)
    {
        Vector3 lp = worldPos - _tileParent.position;
        return new Vector2Int(Mathf.RoundToInt(lp.x), Mathf.RoundToInt(-lp.y));
    }

    private void EnsureGridOverview(int width, int height)
    {
        DestroyGridOverview();

        GameObject tilePrefab = _mainRunner.TilePrefab;
        if (tilePrefab == null)
            return;

        SpriteRenderer referenceRenderer = tilePrefab.GetComponentInChildren<SpriteRenderer>();
        if (referenceRenderer == null || referenceRenderer.sprite == null)
            return;

        _gridOverviewObj = new GameObject("GridTileOverview");
        _gridOverviewObj.transform.SetParent(_tileParent, worldPositionStays: false);
        _gridOverviewObj.transform.localPosition = new Vector3((width - 1) * 0.5f, -(height - 1) * 0.5f, TileZ);

        _gridOverviewRenderer = _gridOverviewObj.AddComponent<SpriteRenderer>();
        _gridOverviewRenderer.sprite = referenceRenderer.sprite;
        _gridOverviewRenderer.color = referenceRenderer.color;
        _gridOverviewRenderer.sortingLayerID = referenceRenderer.sortingLayerID;
        _gridOverviewRenderer.sortingOrder = referenceRenderer.sortingOrder;
        _gridOverviewRenderer.drawMode = SpriteDrawMode.Tiled;
        _gridOverviewRenderer.size = new Vector2(width, height);
        ApplyGridOverviewPatternDensity(referenceRenderer, width, height);
        _gridOverviewRenderer.enabled = false;
    }

    private void ApplyGridOverviewPatternDensity(SpriteRenderer referenceRenderer, int width, int height)
    {
        if (_gridOverviewRenderer == null || referenceRenderer == null || referenceRenderer.sprite == null)
            return;

        float density = _mainRunner.TileOverviewPatternDensity;
        Vector2 spriteBounds = referenceRenderer.sprite.bounds.size;

        if (Mathf.Approximately(density, 1f))
        {
            _gridOverviewObj.transform.localScale = Vector3.one;
            _gridOverviewRenderer.size = new Vector2(width, height);
            return;
        }

        float period = 1f / density;
        float scaleX = period / Mathf.Max(0.001f, spriteBounds.x);
        float scaleY = period / Mathf.Max(0.001f, spriteBounds.y);

        _gridOverviewObj.transform.localScale = new Vector3(scaleX, scaleY, 1f);
        _gridOverviewRenderer.size = new Vector2(
            width / Mathf.Max(0.001f, scaleX),
            height / Mathf.Max(0.001f, scaleY));
    }

    private void DestroyGridOverview()
    {
        if (_gridOverviewObj != null)
        {
            MonoBehaviour.Destroy(_gridOverviewObj);
            _gridOverviewObj = null;
            _gridOverviewRenderer = null;
        }
    }
}
