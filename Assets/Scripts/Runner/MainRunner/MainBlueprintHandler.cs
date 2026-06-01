using System.Collections.Generic;
using UnityEngine;

public class MainBlueprintHandler
{
    private const float MinDragWorldSqr = 0.0004f;
    private const float SelectionBoxZ = 8f;
    private const int SelectionSortOrder = 950;
    private const int HighlightSortOrder = 949;

    private readonly MainRunner _runner;

    private bool _isBlueprintMode;
    private bool _pointerDownForSelection;
    private Vector3 _selectionWorldStart;

    private GameObject _selectionBoxObj;
    private SpriteRenderer _selectionRenderer;
    private GameObject _highlightRoot;
    private readonly List<GameObject> _highlightObjects = new List<GameObject>();
    private readonly List<Vector2Int> _scratchOrigins = new List<Vector2Int>();
    private readonly List<Vector2Int> _scratchSizes = new List<Vector2Int>();
    private static Sprite _unitSquareSprite;

    public bool IsBlueprintMode => _isBlueprintMode;
    public bool IsBlockingCameraDrag => _isBlueprintMode && _pointerDownForSelection;

    public MainBlueprintHandler(MainRunner runner)
    {
        _runner = runner;
    }

    public void SetBlueprintMode(bool active)
    {
        if (!active)
        {
            _pointerDownForSelection = false;
            DestroySelectionVisual();
        }

        _isBlueprintMode = active;
    }

    public void Update(Camera cam)
    {
        if (!_isBlueprintMode || cam == null) return;
        if (UIManager.Instance != null && UIManager.Instance.IsTypingInTextInput()) return;

        if (PointerInput.WasCancelPressed())
        {
            _runner.SetBlueprintMode(false);
            return;
        }

        if (PointerInput.IsMultiTouch)
            return;

        bool pointerOverUi = PointerInput.IsPointerOverUi();

        if (PointerInput.GetPrimaryPointerDown() && !pointerOverUi)
        {
            _pointerDownForSelection = true;
            _selectionWorldStart = ScreenToWorldOnPlane(cam, PointerInput.PrimaryScreenPosition);
            EnsureSelectionVisual();
        }

        if (_pointerDownForSelection)
        {
            if (PointerInput.GetPrimaryPointerHeld())
            {
                Vector3 current = ScreenToWorldOnPlane(cam, PointerInput.PrimaryScreenPosition);
                UpdateSelectionVisual(_selectionWorldStart, current);
            }

            if (PointerInput.GetPrimaryPointerUp())
            {
                Vector3 end = ScreenToWorldOnPlane(cam, PointerInput.PrimaryScreenPosition);
                _pointerDownForSelection = false;
                DestroySelectionVisual();
                TryCommitSelection(_selectionWorldStart, end);
            }
        }
    }

    private static Vector3 ScreenToWorldOnPlane(Camera camera, Vector2 screenPosition)
    {
        return PointerInput.ScreenToWorldOnPlane(camera, screenPosition);
    }

    private void TryCommitSelection(Vector3 worldA, Vector3 worldB)
    {
        if ((worldB - worldA).sqrMagnitude < MinDragWorldSqr)
            return;

        if (!TryGetGridRectFromWorldDrag(worldA, worldB, out Vector2Int cellMin, out Vector2Int cellMax))
            return;

        MainBuildingGridHandler grid = _runner.GridHandler;
        List<PlacedBuildingSaveData> captured = grid.ExportBuildingsIntersectingGridRect(cellMin, cellMax);
        List<PlacedRoadSaveData> capturedRoads = grid.ExportRoadsIntersectingGridRect(cellMin, cellMax);
        if (captured.Count == 0 && capturedRoads.Count == 0)
            return;

        _runner.CommitBlueprintSelection(captured, capturedRoads);
    }

    private bool TryGetGridRectFromWorldDrag(Vector3 worldA, Vector3 worldB, out Vector2Int cellMin, out Vector2Int cellMax)
    {
        cellMin = default;
        cellMax = default;

        MainBuildingGridHandler grid = _runner.GridHandler;
        if (grid == null)
            return false;

        float minX = Mathf.Min(worldA.x, worldB.x);
        float maxX = Mathf.Max(worldA.x, worldB.x);
        float minY = Mathf.Min(worldA.y, worldB.y);
        float maxY = Mathf.Max(worldA.y, worldB.y);

        Vector2Int g0 = grid.WorldToGridPosition(new Vector3(minX, minY, 0f));
        Vector2Int g1 = grid.WorldToGridPosition(new Vector3(maxX, minY, 0f));
        Vector2Int g2 = grid.WorldToGridPosition(new Vector3(maxX, maxY, 0f));
        Vector2Int g3 = grid.WorldToGridPosition(new Vector3(minX, maxY, 0f));

        int x0 = Mathf.Min(Mathf.Min(g0.x, g1.x), Mathf.Min(g2.x, g3.x));
        int y0 = Mathf.Min(Mathf.Min(g0.y, g1.y), Mathf.Min(g2.y, g3.y));
        int x1 = Mathf.Max(Mathf.Max(g0.x, g1.x), Mathf.Max(g2.x, g3.x));
        int y1 = Mathf.Max(Mathf.Max(g0.y, g1.y), Mathf.Max(g2.y, g3.y));

        cellMin = new Vector2Int(x0, y0);
        cellMax = new Vector2Int(x1, y1);
        return true;
    }

    private void EnsureSelectionVisual()
    {
        if (_selectionBoxObj != null)
            return;

        _selectionBoxObj = new GameObject("BlueprintSelectionBox");
        _selectionBoxObj.transform.SetParent(_runner.transform, worldPositionStays: true);

        _selectionRenderer = _selectionBoxObj.AddComponent<SpriteRenderer>();
        _selectionRenderer.sprite = GetUnitSquareSprite();
        _selectionRenderer.sortingOrder = SelectionSortOrder;
        Color c = VisualManager.Instance.ValidColor;
        _selectionRenderer.color = new Color(c.r, c.g, c.b, 0.35f);
    }

    private void UpdateSelectionVisual(Vector3 worldA, Vector3 worldB)
    {
        if (_selectionRenderer == null)
            return;

        float minX = Mathf.Min(worldA.x, worldB.x);
        float maxX = Mathf.Max(worldA.x, worldB.x);
        float minY = Mathf.Min(worldA.y, worldB.y);
        float maxY = Mathf.Max(worldA.y, worldB.y);

        float w = Mathf.Max(maxX - minX, 0.05f);
        float h = Mathf.Max(maxY - minY, 0.05f);
        Vector3 center = new Vector3((minX + maxX) * 0.5f, (minY + maxY) * 0.5f, SelectionBoxZ);

        _selectionBoxObj.transform.position = center;
        _selectionBoxObj.transform.localScale = new Vector3(w, h, 1f);

        UpdateSelectionHighlights(worldA, worldB);
    }

    private void UpdateSelectionHighlights(Vector3 worldA, Vector3 worldB)
    {
        ClearHighlightOverlays();

        if (!TryGetGridRectFromWorldDrag(worldA, worldB, out Vector2Int cellMin, out Vector2Int cellMax))
            return;

        MainBuildingGridHandler grid = _runner.GridHandler;
        if (grid == null)
            return;

        grid.CollectFootprintsIntersectingGridRect(cellMin, cellMax, _scratchOrigins, _scratchSizes);
        if (_scratchOrigins.Count == 0)
            return;

        EnsureHighlightRoot();
        Color highlightColor = GetHighlightColor();

        for (int i = 0; i < _scratchOrigins.Count; i++)
        {
            Vector2Int origin = _scratchOrigins[i];
            Vector2Int size = _scratchSizes[i];
            GameObject highlight = new GameObject("BlueprintSelectionHighlight");
            highlight.transform.SetParent(_highlightRoot.transform, worldPositionStays: true);

            SpriteRenderer renderer = highlight.AddComponent<SpriteRenderer>();
            renderer.sprite = GetUnitSquareSprite();
            renderer.sortingOrder = HighlightSortOrder;
            renderer.color = highlightColor;

            Vector3 position = grid.GridToWorldPosition(origin, size);
            position.z = SelectionBoxZ - 0.05f;
            highlight.transform.position = position;
            highlight.transform.localScale = new Vector3(size.x, size.y, 1f);

            _highlightObjects.Add(highlight);
        }
    }

    private static Color GetHighlightColor()
    {
        Color c = VisualManager.Instance.ValidColor;
        return new Color(c.r, c.g, c.b, Mathf.Clamp01(c.a + 0.25f));
    }

    private void EnsureHighlightRoot()
    {
        if (_highlightRoot != null)
            return;

        _highlightRoot = new GameObject("BlueprintSelectionHighlights");
        _highlightRoot.transform.SetParent(_runner.transform, worldPositionStays: true);
    }

    private void ClearHighlightOverlays()
    {
        for (int i = 0; i < _highlightObjects.Count; i++)
        {
            if (_highlightObjects[i] != null)
                Object.Destroy(_highlightObjects[i]);
        }

        _highlightObjects.Clear();
    }

    private void DestroySelectionVisual()
    {
        ClearHighlightOverlays();

        if (_highlightRoot != null)
        {
            Object.Destroy(_highlightRoot);
            _highlightRoot = null;
        }

        if (_selectionBoxObj != null)
        {
            Object.Destroy(_selectionBoxObj);
            _selectionBoxObj = null;
            _selectionRenderer = null;
        }
    }

    private static Sprite GetUnitSquareSprite()
    {
        if (_unitSquareSprite != null)
            return _unitSquareSprite;

        Texture2D tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        _unitSquareSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        return _unitSquareSprite;
    }
}
