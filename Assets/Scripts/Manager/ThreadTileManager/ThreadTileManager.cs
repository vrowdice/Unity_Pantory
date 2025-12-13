using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ThreadTileManager : MonoBehaviour, IGameSceneManager
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

    private GameObject _sharedThreadLabelCanvas;

    private ThreadPlacementDataHandler _threadPlacementHandler;
    private GameManager _gameManager;
    private bool _isInitialized;

    internal GameObject ThreadObjectPrefab => _threadObjectPrefab;
    public bool IsPlacementMode => _gridHandler != null && _gridHandler.IsPlacementActive;
    public bool IsRemovalMode => _gridHandler != null && _gridHandler.IsRemovalActive;
    public ThreadState CurrentPlacementThread => _gridHandler?.SelectedThread;
    public Transform SharedThreadLabelCanvas
    {
        get
        {
            if (_sharedThreadLabelCanvas == null && _gameManager != null)
            {
                Camera targetCamera = _mainCamera ?? Camera.main;
                RectTransform canvasRect = _gameManager.GetWorldCanvas();
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
            OnInitialize(GameManager.Instance, GameDataManager.Instance);
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
        _gridHandler?.UpdatePlacement();
        _gridHandler?.UpdateRemoval();
        
        // 배치/제거 모드가 아닐 때만 클릭 처리
        if (!IsPlacementMode && !IsRemovalMode)
        {
            HandleThreadClick();
        }
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
            _gridHandler?.CancelRemoval();
        }

        _gridHandler?.StartPlacement(threadState);
        return true;
    }

    public void CancelPlacementMode()
    {
        _gridHandler?.CancelPlacement();
    }

    public void StartRemovalMode()
    {
        if (IsPlacementMode)
        {
            _gridHandler?.CancelPlacement();
        }

        _gridHandler?.StartRemoval();
    }

    public void CancelRemovalMode()
    {
        _gridHandler?.CancelRemoval();
    }

    public void ToggleRemovalMode()
    {
        if (IsRemovalMode)
        {
            _gridHandler?.CancelRemoval();
        }
        else
        {
            StartRemovalMode();
        }
    }

    public bool PlaceThread(Vector2Int gridPos, ThreadState templateThread)
    {
        if (templateThread == null || _gridHandler == null || _dataManager == null || _threadPlacementHandler == null)
            return false;

        if (!_gridHandler.CanPlaceThread(gridPos))
            return false;

        // ThreadPlacementDataHandler에서 템플릿을 복사하여 새로운 인스턴스 생성 및 배치
        ThreadState newThreadInstance = _threadPlacementHandler.PlaceThread(gridPos, templateThread.threadId);
        if (newThreadInstance == null)
        {
            Debug.LogError($"[ThreadTileManager] Failed to place thread instance from template: {templateThread.threadId}");
            return false;
        }

        // 새로운 인스턴스로 ThreadObject 생성
        ThreadObject threadObject = _gridHandler.CreateThreadObject(gridPos, newThreadInstance);
        if (threadObject == null)
            return false;

        threadObject.SetGridPosition(gridPos);
        _gridHandler.SetTileOccupied(gridPos, true);

        return true;
    }

    public bool RemoveThread(Vector2Int gridPos)
    {
        ThreadObject threadObject = _gridHandler?.GetThreadObjectAt(gridPos);

        bool placementRemoved = false;
        if (_threadPlacementHandler != null)
        {
            placementRemoved = _threadPlacementHandler.RemovePlacedThread(gridPos);

            // 레거시 호환: placement 데이터가 없더라도 ThreadState 정보를 기반으로 이벤트를 트리거
            // 하지만 새로운 구조에서는 ThreadPlacementDataHandler가 직접 관리하므로 이 로직은 불필요
            // 주석 처리: if (!placementRemoved && threadObject?.ThreadState != null) { ... }
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
            // 각 배치된 인스턴스의 독립적인 상태를 가져옴
            ThreadState threadState = kvp.Value.RuntimeState;
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
    #endregion

    #region Initialization

    public void OnInitialize(GameManager gameManager, GameDataManager dataManager)
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
        _gridHandler = new ThreadGridHandler(this, _threadTilePrefab, _threadObjectPrefab, _gridWidth, _gridHeight, _mainCameraController);
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
        RectTransform canvasRect = _gameManager.GetWorldCanvas();

        if (canvasRect == null)
        {
            Debug.LogWarning("[ThreadTileManager] Failed to acquire shared thread label canvas from GameManager.");
            return;
        }

        _sharedThreadLabelCanvas = canvasRect.gameObject;
    }

    #endregion

    #region Thread Click Handling

    /// <summary>
    /// 스레드 클릭을 처리합니다.
    /// </summary>
    private void HandleThreadClick()
    {
        if (UnityEngine.EventSystems.EventSystem.current != null && 
            UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            return;

        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mouseWorldPos = _mainCamera.ScreenToWorldPoint(Input.mousePosition);
            mouseWorldPos.z = 0f;
            
            // 그리드 위치로 변환
            if (_gridHandler != null)
            {
                Vector2Int gridPos = _gridHandler.WorldToGridPosition(mouseWorldPos);
                ThreadObject clickedThread = GetThreadObjectAt(gridPos);

                if (clickedThread != null && !clickedThread.IsPreview)
                {
                    OnThreadClicked(clickedThread);
                }
            }
        }
    }

    /// <summary>
    /// 스레드가 클릭되었을 때 정보 패널을 표시합니다.
    /// </summary>
    private void OnThreadClicked(ThreadObject threadObject)
    {
        if (threadObject == null || threadObject.ThreadState == null)
            return;

        if (_mainUiManager != null)
        {
            _mainUiManager.ShowThreadInfo(threadObject.ThreadState);
        }
        else
        {
            Debug.LogWarning("[ThreadTileManager] MainUiManager is not assigned. Cannot show thread info.");
        }
    }

    #endregion

}







