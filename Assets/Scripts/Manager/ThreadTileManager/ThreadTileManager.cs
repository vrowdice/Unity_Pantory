using System.Collections.Generic;
using Pantory.Managers;
using UnityEngine;
using UnityEngine.UI;

public class ThreadTileManager : MonoBehaviour, ISceneManagerComponent
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

    private ThreadPlacementDataHandler _threadPlacementHandler;
    private GameManager _gameManager;
    private bool _isInitialized;

    internal GameObject ThreadObjectPrefab => _threadObjectPrefab;
    public bool IsPlacementMode => _placementHandler != null && _placementHandler.IsActive;
    public bool IsRemovalMode => _removalHandler != null && _removalHandler.IsActive;
    public ThreadState CurrentPlacementThread => _placementHandler?.SelectedThread;
    public Transform SharedThreadLabelCanvas
    {
        get
        {
            if (_sharedThreadLabelCanvas == null && _gameManager != null)
            {
                Camera targetCamera = _mainCamera ?? Camera.main;
                RectTransform canvasRect = _gameManager.GetWorldCanvas(transform, targetCamera);
                if (canvasRect != null)
                {
                    _sharedThreadLabelCanvas = canvasRect.gameObject;
                }
            }

            return _sharedThreadLabelCanvas != null ? _sharedThreadLabelCanvas.transform : null;
        }
    }

    void Start()
    {
        if (!_isInitialized)
        {
            Initialize(GameManager.Instance, GameDataManager.Instance);
        }
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

    public void ToggleRemovalMode()
    {
        if (IsRemovalMode)
        {
            _removalHandler?.CancelRemoval();
        }
        else
        {
            StartRemovalMode();
        }
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

        _threadPlacementHandler?.SetPlacedThread(gridPos, threadState.threadId);
        return true;
    }

    public bool RemoveThread(Vector2Int gridPos)
    {
        ThreadObject threadObject = _gridHandler?.GetThreadObjectAt(gridPos);

        bool placementRemoved = false;
        if (_threadPlacementHandler != null)
        {
            placementRemoved = _threadPlacementHandler.RemovePlacedThread(gridPos);

            if (!placementRemoved && threadObject?.ThreadState != null)
            {
                // placement 데이터가 없더라도 ThreadState 정보를 기반으로 이벤트를 트리거하기 위해 임시 등록 후 제거
                _threadPlacementHandler.SetPlacedThread(gridPos, threadObject.ThreadState.threadId);
                placementRemoved = _threadPlacementHandler.RemovePlacedThread(gridPos);
            }
        }

        if (threadObject == null && !placementRemoved)
        {
            Debug.LogWarning($"[ThreadTileManager] No thread object at {gridPos} to remove.");
            return false;
        }

        _gridHandler?.RemoveThreadObject(gridPos);
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

        if (_threadPlacementHandler == null)
            return;

        foreach (var kvp in _threadPlacementHandler.GetAllPlacedThreads())
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

    public void Initialize(GameManager gameManager, GameDataManager dataManager)
    {
        _gameManager = gameManager ?? GameManager.Instance;
        InitializeReferences(_gameManager, dataManager ?? GameDataManager.Instance);

        if (_gridHandler == null)
        {
            InitializeHandlers();
            SetPositionCenter();
            CreateGrid(_gridWidth, _gridHeight);
            SetCameraCollider();
            CreateSharedThreadLabelCanvas();
        }
        else
        {
            CreateSharedThreadLabelCanvas();
        }

        RefreshThreads();
        _isInitialized = true;
    }

    private void InitializeReferences(GameManager gameManager, GameDataManager dataManager)
    {
        _dataManager = dataManager ?? GameDataManager.Instance;

        if (_mainUiManager == null && gameManager?.UiManager is MainUiManager uiManager)
        {
            _mainUiManager = uiManager;
        }

        _threadPlacementHandler = _dataManager?.ThreadPlacement;

        _mainUiManager?.RegisterThreadTileManager(this);

        _mainCameraController = gameManager?.MainCameraController;
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

        if (_gameManager == null)
        {
            Debug.LogWarning("[ThreadTileManager] GameManager is null. Cannot create shared thread label canvas.");
            return;
        }

        Camera targetCamera = _mainCamera ?? Camera.main;
        RectTransform canvasRect = _gameManager.GetWorldCanvas(transform, targetCamera);

        if (canvasRect == null)
        {
            Debug.LogWarning("[ThreadTileManager] Failed to acquire shared thread label canvas from GameManager.");
            return;
        }

        _sharedThreadLabelCanvas = canvasRect.gameObject;
    }

    #endregion

}







