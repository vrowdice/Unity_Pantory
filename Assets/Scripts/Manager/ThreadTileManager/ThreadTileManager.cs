using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ThreadTileManager : MonoBehaviour, ISceneGameManager
{
    [Header("UI Manager")]
    [SerializeField] private MainUiManager _mainUiManager;

    [Header("Prefab")]
    [SerializeField] private GameObject _threadTilePrefab;
    [SerializeField] private GameObject _threadObjectPrefab;

    [Header("Grid Settings")]
    [SerializeField] private int _gridWidth = 10;
    [SerializeField] private int _gridHeight = 10;

    private DataManager _dataManager;
    private MainCameraController _mainCameraController;
    private Camera _mainCamera;
    private BoxCollider2D _cameraCollider;

    private ThreadGridHandler _gridHandler;

    private GameObject _sharedThreadLabelCanvas;

    private ThreadPlacementDataHandler _threadPlacementHandler;
    private GameManager _gameManager;

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
        if (IsPlacementMode)
        {
            UpdatePlacementMode();
        }
        else if (IsRemovalMode)
        {
            UpdateRemovalMode();
        }
        else
        {
            HandleThreadClick();
        }
    }

    private void UpdatePlacementMode()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        Vector3 mouseWorldPos = GetMouseWorldPosition();
        var (gridPos, canPlace) = _gridHandler.UpdatePlacement(mouseWorldPos);

        // 입력 처리
        if (Input.GetMouseButtonDown(0) && canPlace)
        {
            PlaceThread(gridPos, CurrentPlacementThread);
        }
        else if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
        {
            CancelPlacementMode();
        }
    }

    private void UpdateRemovalMode()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        Vector3 mouseWorldPos = GetMouseWorldPosition();
        ThreadObject hoveredThread = _gridHandler.UpdateRemoval(mouseWorldPos);

        // 입력 처리
        if (Input.GetMouseButtonDown(0) && hoveredThread != null)
        {
            if (RemoveThread(hoveredThread.GridPosition))
            {
                _gridHandler.ResetHighlight();
            }
        }
        else if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
        {
            CancelRemovalMode();
        }
    }

    private Vector3 GetMouseWorldPosition()
    {
        Vector3 mouseWorldPos = _mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0f;
        return mouseWorldPos;
    }

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
            _gridHandler.CancelRemoval();
        }

        _gridHandler?.StartPlacement(threadState);
        return true;
    }

    public void CancelPlacementMode()
    {
        _gridHandler.CancelPlacement();
    }

    public void StartRemovalMode()
    {
        if (IsPlacementMode)
        {
            _gridHandler.CancelPlacement();
        }

        _gridHandler.StartRemoval();
    }

    public void CancelRemovalMode()
    {
        _gridHandler.CancelRemoval();
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
        return _gridHandler.GetThreadObjectAt(gridPos);
    }

    public void RefreshThreads()
    {
        _gridHandler.ClearAllThreadObjects();

        foreach (var kvp in _threadPlacementHandler.GetAllPlacedThreads())
        {
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

    public void OnInitialize(GameManager gameManager, DataManager dataManager)
    {
        _gameManager = gameManager;
        _dataManager = dataManager;
        _threadPlacementHandler = dataManager.ThreadPlacement;
        _mainCamera = Camera.main;
        _mainCameraController = Camera.main.GetComponent<MainCameraController>();

        _gridHandler = new ThreadGridHandler(transform, _threadTilePrefab, _threadObjectPrefab, _gridWidth, _gridHeight);
        transform.position = new Vector3(-_gridWidth / 2, _gridHeight / 2, 11);

        CreateGrid(_gridWidth, _gridHeight);
        SetCameraCollider();
        CreateSharedThreadLabelCanvas();

        RefreshThreads();
    }

    private void CreateSharedThreadLabelCanvas()
    {
        Camera targetCamera = _mainCamera ?? Camera.main;
        RectTransform canvasRect = _gameManager.GetWorldCanvas();
        _sharedThreadLabelCanvas = canvasRect.gameObject;
    }

    /// <summary>
    /// 스레드 클릭을 처리합니다.
    /// </summary>
    private void HandleThreadClick()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mouseWorldPos = GetMouseWorldPosition();
            Vector2Int gridPos = _gridHandler.WorldToGridPosition(mouseWorldPos);
            ThreadObject clickedThread = GetThreadObjectAt(gridPos);

            if (clickedThread != null && !clickedThread.IsPreview)
            {
                OnThreadClicked(clickedThread);
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
            _mainUiManager.ShowThreadInfoPanel(threadObject.ThreadState);
        }
        else
        {
            Debug.LogWarning("[ThreadTileManager] MainUiManager is not assigned. Cannot show thread info.");
        }
    }
}