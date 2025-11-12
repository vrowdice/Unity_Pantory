using System.Collections.Generic;
using Pantory.Managers;
using UnityEngine;
using UnityEngine.UI;

public class ThreadTileManager : MonoBehaviour
{
    [Header("UI Manager")]
    [SerializeField] private MainUiManager _mainUiManager;

    [Header("Prefab")]
    [SerializeField] private GameObject _threadTilePrefab;
    [SerializeField] private GameObject _threadObjectPrefab;

    [Header("Grid Settings")]
    [SerializeField] private int _gridWidth = 10;
    [SerializeField] private int _gridHeight = 10;

    private GameDataManager _dataManager;
    private MainCameraController _mainCameraController;
    private Camera _mainCamera;
    private BoxCollider2D _cameraCollider;

    private ThreadGridHandler _gridHandler;
    private ThreadPlacementHandler _placementHandler;
    private ThreadRemovalHandler _removalHandler;

    private GameObject _sharedThreadLabelCanvas;

    private readonly Dictionary<Vector2Int, ThreadPlacementInfo> _placedThreads = new Dictionary<Vector2Int, ThreadPlacementInfo>();

    internal GameObject ThreadObjectPrefab => _threadObjectPrefab;
    public bool IsPlacementMode => _placementHandler != null && _placementHandler.IsActive;
    public bool IsRemovalMode => _removalHandler != null && _removalHandler.IsActive;
    public ThreadState CurrentPlacementThread => _placementHandler?.SelectedThread;
    public Transform SharedThreadLabelCanvas => _sharedThreadLabelCanvas?.transform;

    void Start()
    {
        InitializeReferences();
        InitializeHandlers();

        SetPositionCenter();
        CreateGrid(_gridWidth, _gridHeight);
        SetCameraCollider();
        CreateSharedThreadLabelCanvas();

        RefreshThreads();
    }

    public void SetPositionCenter()
    {
        transform.position = new Vector3(-_gridWidth / 2, _gridHeight / 2, 11);
    }

    public void SetCameraCollider()
    {
        _cameraCollider = GetComponent<BoxCollider2D>();
        if (_cameraCollider == null)
        {
            _cameraCollider = gameObject.AddComponent<BoxCollider2D>();
        }

        _cameraCollider.offset = new Vector2(_gridWidth / 2f, -_gridHeight / 2f);
        _cameraCollider.size = new Vector2(_gridWidth, _gridHeight);
    }

    void Update()
    {
        _placementHandler?.Update();
        _removalHandler?.Update();
    }

    #region Public API

    public void CreateGrid(int width, int height)
    {
        if (_gridHandler == null)
            return;

        _gridWidth = width;
        _gridHeight = height;
        _gridHandler.CreateGrid(width, height);
    }

    public bool StartPlacementMode(ThreadState threadState)
    {
        if (threadState == null)
            return false;

        if (IsRemovalMode)
        {
            _removalHandler.CancelRemoval();
        }

        _placementHandler?.StartPlacement(threadState);
        return true;
    }

    public void CancelPlacementMode()
    {
        _placementHandler?.CancelPlacement();
    }

    public void StartRemovalMode()
    {
        if (IsPlacementMode)
        {
            _placementHandler.CancelPlacement();
        }

        _removalHandler?.StartRemoval();
    }

    public void CancelRemovalMode()
    {
        _removalHandler?.CancelRemoval();
    }

    public bool PlaceThread(Vector2Int gridPos, ThreadState threadState)
    {
        if (threadState == null || _gridHandler == null)
            return false;

        if (!_gridHandler.CanPlaceThread(gridPos))
            return false;

        ThreadObject threadObject = _gridHandler.CreateThreadObject(gridPos, threadState);
        if (threadObject == null)
            return false;

        threadObject.SetGridPosition(gridPos);
        _gridHandler.SetTileOccupied(gridPos, true);

        _placedThreads[gridPos] = new ThreadPlacementInfo(threadState.threadId);
        Debug.Log($"[ThreadTileManager] Thread placed: {threadState.threadId} at {gridPos}");
        return true;
    }

    public bool RemoveThread(Vector2Int gridPos)
    {
        if (!_placedThreads.ContainsKey(gridPos))
        {
            Debug.LogWarning($"[ThreadTileManager] No thread at {gridPos} to remove.");
            return false;
        }

        _placedThreads.Remove(gridPos);
        _gridHandler.RemoveThreadObject(gridPos);
        Debug.Log($"[ThreadTileManager] Thread removed at {gridPos}");
        return true;
    }

    public ThreadObject GetThreadObjectAt(Vector2Int gridPos)
    {
        return _gridHandler?.GetThreadObjectAt(gridPos);
    }

    public void RefreshThreads()
    {
        if (_gridHandler == null)
            return;

        _gridHandler.ClearAllThreadObjects();

        foreach (var kvp in _placedThreads)
        {
            ThreadState threadState = _dataManager?.GetThread(kvp.Value.ThreadId);
            if (threadState == null)
                continue;

            _gridHandler.SetTileOccupied(kvp.Key, true);
            ThreadObject threadObject = _gridHandler.CreateThreadObject(kvp.Key, threadState);
            if (threadObject != null)
            {
                threadObject.SetGridPosition(kvp.Key);
            }
        }
    }

    public bool HasThreadTile(Vector2Int gridPos)
    {
        return _gridHandler != null && _gridHandler.HasTile(gridPos);
    }

    public ThreadTile GetThreadTile(Vector2Int gridPos)
    {
        return _gridHandler?.GetTile(gridPos);
    }

    #endregion

    #region Initialization

    private void InitializeReferences()
    {
        _dataManager = GameDataManager.Instance;

        if (_mainUiManager == null && GameManager.Instance?.UiManager is MainUiManager uiManager)
        {
            _mainUiManager = uiManager;
        }

        _mainUiManager?.RegisterThreadTileManager(this);

        _mainCameraController = GameManager.Instance?.MainCameraController;
        if (_mainCameraController == null && Camera.main != null)
        {
            _mainCameraController = Camera.main.GetComponent<MainCameraController>();
        }

        _mainCamera = _mainCameraController != null ? _mainCameraController.Camera : Camera.main;
    }

    private void InitializeHandlers()
    {
        _gridHandler = new ThreadGridHandler(this, _threadTilePrefab, _threadObjectPrefab, _gridWidth, _gridHeight);
        _placementHandler = new ThreadPlacementHandler(this, _gridHandler, _mainCameraController);
        _removalHandler = new ThreadRemovalHandler(this, _gridHandler, _mainCameraController);
    }

    private void CreateSharedThreadLabelCanvas()
    {
        if (_sharedThreadLabelCanvas != null)
            return;

        _sharedThreadLabelCanvas = new GameObject("SharedThreadLabelCanvas");
        _sharedThreadLabelCanvas.transform.SetParent(transform);

        Canvas canvas = _sharedThreadLabelCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 2;
        canvas.worldCamera = _mainCamera != null ? _mainCamera : Camera.main;

        CanvasScaler scaler = _sharedThreadLabelCanvas.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 100;

        CanvasGroup canvasGroup = _sharedThreadLabelCanvas.AddComponent<CanvasGroup>();
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        RectTransform rectTransform = _sharedThreadLabelCanvas.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.sizeDelta = new Vector2(1000f, 1000f);
        }
    }

    #endregion

    #region Internal Types

    private class ThreadPlacementInfo
    {
        public string ThreadId { get; }

        public ThreadPlacementInfo(string threadId)
        {
            ThreadId = threadId;
        }
    }

    #endregion
}







