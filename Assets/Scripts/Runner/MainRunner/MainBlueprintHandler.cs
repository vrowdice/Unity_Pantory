using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class MainBlueprintHandler
{
    private const float MinDragWorldSqr = 0.0004f;
    private const float SelectionBoxZ = 8f;
    private const int SelectionSortOrder = 950;

    private readonly MainRunner _runner;

    private bool _isBlueprintMode;
    private bool _pointerDownForSelection;
    private Vector3 _selectionWorldStart;

    private GameObject _selectionBoxObj;
    private SpriteRenderer _selectionRenderer;
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

        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
        {
            _runner.SetBlueprintMode(false);
            return;
        }

        bool pointerOverUi = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

        if (Input.GetMouseButtonDown(0) && !pointerOverUi)
        {
            _pointerDownForSelection = true;
            _selectionWorldStart = ScreenToWorldOnPlane(cam, Input.mousePosition);
            EnsureSelectionVisual();
        }

        if (_pointerDownForSelection)
        {
            if (Input.GetMouseButton(0))
            {
                Vector3 current = ScreenToWorldOnPlane(cam, Input.mousePosition);
                UpdateSelectionVisual(_selectionWorldStart, current);
            }

            if (Input.GetMouseButtonUp(0))
            {
                Vector3 end = ScreenToWorldOnPlane(cam, Input.mousePosition);
                _pointerDownForSelection = false;
                DestroySelectionVisual();
                TryCommitSelection(_selectionWorldStart, end);
            }
        }
    }

    private static Vector3 ScreenToWorldOnPlane(Camera camera, Vector3 screenPosition)
    {
        Vector3 w = camera.ScreenToWorldPoint(screenPosition);
        w.z = 0f;
        return w;
    }

    private void TryCommitSelection(Vector3 worldA, Vector3 worldB)
    {
        if ((worldB - worldA).sqrMagnitude < MinDragWorldSqr)
            return;

        MainBuildingGridHandler grid = _runner.GridHandler;
        Vector2Int cellMin = new Vector2Int(int.MaxValue, int.MaxValue);
        Vector2Int cellMax = new Vector2Int(int.MinValue, int.MinValue);

        void ExpandCell(Vector3 world)
        {
            Vector2Int g = grid.WorldToGridPosition(world);
            cellMin.x = Mathf.Min(cellMin.x, g.x);
            cellMin.y = Mathf.Min(cellMin.y, g.y);
            cellMax.x = Mathf.Max(cellMax.x, g.x);
            cellMax.y = Mathf.Max(cellMax.y, g.y);
        }

        float minX = Mathf.Min(worldA.x, worldB.x);
        float maxX = Mathf.Max(worldA.x, worldB.x);
        float minY = Mathf.Min(worldA.y, worldB.y);
        float maxY = Mathf.Max(worldA.y, worldB.y);

        ExpandCell(new Vector3(minX, minY, 0f));
        ExpandCell(new Vector3(maxX, minY, 0f));
        ExpandCell(new Vector3(maxX, maxY, 0f));
        ExpandCell(new Vector3(minX, maxY, 0f));

        List<PlacedBuildingSaveData> captured = grid.ExportBuildingsIntersectingGridRect(cellMin, cellMax);
        if (captured.Count == 0)
            return;

        MainCanvas canvas = _runner.MainCanvas;
        if (canvas != null)
            canvas.AddBlueprintSavedEntryBeforeAddButton(captured);
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
        if (_selectionRenderer == null) return;

        float minX = Mathf.Min(worldA.x, worldB.x);
        float maxX = Mathf.Max(worldA.x, worldB.x);
        float minY = Mathf.Min(worldA.y, worldB.y);
        float maxY = Mathf.Max(worldA.y, worldB.y);

        float w = Mathf.Max(maxX - minX, 0.05f);
        float h = Mathf.Max(maxY - minY, 0.05f);
        Vector3 center = new Vector3((minX + maxX) * 0.5f, (minY + maxY) * 0.5f, SelectionBoxZ);

        _selectionBoxObj.transform.position = center;
        _selectionBoxObj.transform.localScale = new Vector3(w, h, 1f);
    }

    private void DestroySelectionVisual()
    {
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
