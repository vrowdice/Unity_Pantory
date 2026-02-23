using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 그리드 타일 생성, 스레드 객체 배치 및 제거 모드를 관리하는 핸들러 클래스입니다.
/// </summary>
public class MainRunnerThreadGridHandler
{
    private readonly Transform _parentTransform;
    private readonly GameObject _threadTilePrefab;
    private readonly GameObject _threadObjectPrefab;

    private int _gridWidth;
    private int _gridHeight;

    private readonly Dictionary<Vector2Int, ThreadTile> _threadTiles = new Dictionary<Vector2Int, ThreadTile>();
    private readonly Dictionary<Vector2Int, ThreadObject> _threadObjects = new Dictionary<Vector2Int, ThreadObject>();

    private ThreadState _selectedThread;
    private GameObject _previewObject;
    private ThreadObject _previewComponent;
    private Vector2Int _currentGridPos;
    private ThreadObject _hoveredThread;

    private bool _canPlace;
    private bool _isPlacementActive;
    private bool _isRemovalActive;

    public bool IsPlacementActive => _isPlacementActive;
    public bool IsRemovalActive => _isRemovalActive;
    public ThreadState SelectedThread => _selectedThread;

    public bool IsPointerOverUI => EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    public bool CanPlaceThread(Vector2Int pos) => pos.x >= 0 && pos.x < _gridWidth && pos.y >= 0 && pos.y < _gridHeight && !_threadObjects.ContainsKey(pos);
    public void SetTileOccupied(Vector2Int pos, bool occ) { if (_threadTiles.TryGetValue(pos, out ThreadTile t)) t.SetOccupied(occ); }
    public void SetAllTilesOutline(bool vis, Color col = default) { foreach (ThreadTile t in _threadTiles.Values) t?.SetOutlineVisible(vis, col); }
    public Vector3 GridToWorldPosition(Vector2Int p) => _parentTransform.position + new Vector3((float)p.x, (float)-p.y, 9f);
    public ThreadObject GetThreadObjectAt(Vector2Int p) => _threadObjects.TryGetValue(p, out ThreadObject o) ? o : null;
    public Vector2Int WorldToGridPosition(Vector3 wPos)
    {
        Vector3 lp = wPos - _parentTransform.position;
        return new Vector2Int(Mathf.RoundToInt(lp.x), Mathf.RoundToInt(-lp.y));
    }

    public MainRunnerThreadGridHandler(Transform parentTransform, GameObject tilePrefab, GameObject objPrefab, int width, int height)
    {
        _parentTransform = parentTransform;
        _threadTilePrefab = tilePrefab;
        _threadObjectPrefab = objPrefab;
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
                tileObj.transform.localPosition = new Vector3((float)x, (float)-y, 10f);

                if (!tileObj.TryGetComponent<ThreadTile>(out ThreadTile tile)) tile = tileObj.AddComponent<ThreadTile>();
                tile.Initialize(gridPos);
                _threadTiles[gridPos] = tile;
            }
        }
    }

    public void ClearGrid()
    {
        ClearAllThreadObjects();
        foreach (ThreadTile tile in _threadTiles.Values)
        {
            if (tile != null) Object.Destroy(tile.gameObject);
        }
        _threadTiles.Clear();
    }

    public void StartPlacement(ThreadState threadState)
    {
        CancelPlacement();
        if (threadState == null) return;

        _selectedThread = threadState;
        _isPlacementActive = true;

        Color outlineColor = VisualManager.Instance != null ? VisualManager.Instance.ThreadPlacementOutlineColor : Color.cyan;
        SetAllTilesOutline(true, outlineColor);
        CreatePreviewObject();
    }

    /// <summary>
    /// 배치 모드 업데이트. 마우스 위치에 따른 프리뷰 위치와 상태를 업데이트합니다.
    /// </summary>
    /// <param name="mouseWorldPos">마우스의 월드 좌표</param>
    /// <returns>현재 그리드 위치와 배치 가능 여부</returns>
    public (Vector2Int gridPos, bool canPlace) UpdatePlacement(Vector3 mouseWorldPos)
    {
        if (!_isPlacementActive || _previewComponent == null)
            return (Vector2Int.zero, false);

        if (IsPointerOverUI)
        {
            _previewObject.SetActive(false);
            _canPlace = false;
            return (Vector2Int.zero, false);
        }

        _currentGridPos = WorldToGridPosition(mouseWorldPos);
        _canPlace = CanPlaceThread(_currentGridPos);

        _previewObject.SetActive(_canPlace);
        _previewObject.transform.position = GridToWorldPosition(_currentGridPos);
        _previewComponent.SetPreviewColor(_canPlace);

        return (_currentGridPos, _canPlace);
    }

    public void CancelPlacement()
    {
        _isPlacementActive = false;
        _selectedThread = null;
        SetAllTilesOutline(false);

        if (_previewObject != null)
        {
            Object.Destroy(_previewObject);
            _previewObject = null;
            _previewComponent = null;
        }
    }
    public void StartRemoval() => _isRemovalActive = true;

    /// <summary>
    /// 제거 모드 업데이트. 마우스 위치에 따른 하이라이트를 업데이트합니다.
    /// </summary>
    /// <param name="mouseWorldPos">마우스의 월드 좌표</param>
    /// <returns>현재 호버된 스레드 객체 (없으면 null)</returns>
    public ThreadObject UpdateRemoval(Vector3 mouseWorldPos)
    {
        if (!_isRemovalActive) return null;

        if (IsPointerOverUI)
        {
            ResetHighlight();
            return null;
        }

        Vector2Int gridPos = WorldToGridPosition(mouseWorldPos);
        ThreadObject threadObject = GetThreadObjectAt(gridPos);

        if (threadObject != _hoveredThread)
        {
            ResetHighlight();
            if (threadObject != null)
            {
                _hoveredThread = threadObject;
                Color highColor = VisualManager.Instance != null ? VisualManager.Instance.ThreadRemovalHighlightColor : Color.red;
                _hoveredThread.SetHighlight(highColor);
            }
        }

        return _hoveredThread;
    }

    public void CancelRemoval()
    {
        _isRemovalActive = false;
        ResetHighlight();
    }

    /// <summary>
    /// 하이라이트를 초기화합니다. (외부에서 호출 가능)
    /// </summary>
    public void ResetHighlight()
    {
        if (_hoveredThread != null) _hoveredThread.ResetColor();
        _hoveredThread = null;
    }
    public ThreadObject CreateThreadObject(Vector2Int gridPos, ThreadState threadState)
    {
        if (_threadObjectPrefab == null) return null;
        RemoveThreadObject(gridPos);

        GameObject obj = Object.Instantiate(_threadObjectPrefab, _parentTransform);
        obj.transform.position = GridToWorldPosition(gridPos);

        if (!obj.TryGetComponent<ThreadObject>(out ThreadObject threadObj)) threadObj = obj.AddComponent<ThreadObject>();
        threadObj.Init(threadState, GameManager.Instance);
        threadObj.SetGridPosition(gridPos);

        _threadObjects[gridPos] = threadObj;
        SetTileOccupied(gridPos, true);
        return threadObj;
    }

    public void RemoveThreadObject(Vector2Int gridPos)
    {
        if (_threadObjects.TryGetValue(gridPos, out ThreadObject obj))
        {
            if (obj != null) Object.Destroy(obj.gameObject);
            _threadObjects.Remove(gridPos);
        }
        SetTileOccupied(gridPos, false);
    }

    public void ClearAllThreadObjects()
    {
        foreach (ThreadObject obj in _threadObjects.Values) if (obj != null) Object.Destroy(obj.gameObject);
        _threadObjects.Clear();
        foreach (ThreadTile tile in _threadTiles.Values) tile?.SetOccupied(false);
    }

    private void CreatePreviewObject()
    {
        if (_threadObjectPrefab == null) return;
        _previewObject = Object.Instantiate(_threadObjectPrefab, _parentTransform);
        _previewObject.name = "ThreadPreview";
        if (!_previewObject.TryGetComponent<ThreadObject>(out _previewComponent)) _previewComponent = _previewObject.AddComponent<ThreadObject>();
        _previewComponent.InitializePreview(_selectedThread);
        _previewObject.SetActive(false);
    }
}