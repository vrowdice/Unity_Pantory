using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// 그리드 시스템 내에서 스레드 타일의 생성, 배치, 삭제 및 상호작용을 관리하는 매니저 클래스입니다.
/// </summary>
public class MainRunner : RunnerBase
{
    [Header("UI Manager")]
    [SerializeField] private MainCanvas _mainCanvas;

    [Header("Prefab")]
    [SerializeField] private GameObject _threadTilePrefab;
    [SerializeField] private GameObject _threadObjectPrefab;

    [Header("Grid Settings")]
    [SerializeField] private int _gridWidth = 10;
    [SerializeField] private int _gridHeight = 10;

    private Camera _mainCamera;
    private BoxCollider2D _cameraCollider;
    private MainRunnerThreadGridHandler _gridHandler;
    private GameObject _sharedThreadLabelCanvas;
    private ThreadPlacementDataHandler _threadPlacementHandler;

    internal GameObject ThreadObjectPrefab => _threadObjectPrefab;
    public bool IsPlacementMode => _gridHandler != null && _gridHandler.IsPlacementActive;
    public bool IsRemovalMode => _gridHandler != null && _gridHandler.IsRemovalActive;
    public ThreadState CurrentPlacementThread => _gridHandler?.SelectedThread;

    public Transform SharedThreadLabelCanvas
    {
        get
        {
            if (_sharedThreadLabelCanvas == null && GameManager != null)
            {
                RectTransform canvasRect = GameManager.GetWorldCanvas();
                if (canvasRect != null)
                {
                    _sharedThreadLabelCanvas = canvasRect.gameObject;
                }
            }
            return _sharedThreadLabelCanvas != null ? _sharedThreadLabelCanvas.transform : null;
        }
    }

    private void Update()
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

    /// <summary>
    /// 매니저를 초기화하고 그리드 및 카메라 콜라이더를 설정합니다.
    /// </summary>
    override public void Init()
    {
        base.Init();

        _threadPlacementHandler = DataManager.ThreadPlacement;
        _mainCamera = Camera.main;

        _gridHandler = new MainRunnerThreadGridHandler(transform, _threadTilePrefab, _threadObjectPrefab, _gridWidth, _gridHeight);
        transform.position = new Vector3(-_gridWidth / 2f, _gridHeight / 2f, 11f);

        CreateGrid(_gridWidth, _gridHeight);
        SetCameraCollider();
        CreateSharedThreadLabelCanvas();
        RefreshThreads();

        _mainCanvas.Init(this);
    }

    /// <summary>
    /// 카메라 이동 제한을 위한 그리드 범위 콜라이더를 설정합니다.
    /// </summary>
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

    /// <summary>
    /// 배치 모드 상태에서 마우스 입력 및 미리보기를 처리합니다.
    /// </summary>
    private void UpdatePlacementMode()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        Vector3 mouseWorldPos = GetMouseWorldPosition();
        var (gridPos, canPlace) = _gridHandler.UpdatePlacement(mouseWorldPos);

        if (Input.GetMouseButtonDown(0) && canPlace)
        {
            PlaceThread(gridPos, CurrentPlacementThread);
        }
        else if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
        {
            CancelPlacementMode();
        }
    }

    /// <summary>
    /// 삭제 모드 상태에서 마우스 입력을 처리합니다.
    /// </summary>
    private void UpdateRemovalMode()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        Vector3 mouseWorldPos = GetMouseWorldPosition();
        ThreadObject hoveredThread = _gridHandler.UpdateRemoval(mouseWorldPos);

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
        if (_gridHandler == null) return;

        _gridWidth = width;
        _gridHeight = height;
        _gridHandler.CreateGrid(width, height);
    }

    public bool StartPlacementMode(ThreadState threadState)
    {
        if (threadState == null) return false;

        if (IsRemovalMode) _gridHandler.CancelRemoval();

        _gridHandler?.StartPlacement(threadState);
        return true;
    }

    public void CancelPlacementMode() => _gridHandler?.CancelPlacement();

    public void StartRemovalMode()
    {
        if (IsPlacementMode) _gridHandler.CancelPlacement();
        _gridHandler?.StartRemoval();
    }

    public void CancelRemovalMode() => _gridHandler?.CancelRemoval();

    public void ToggleRemovalMode()
    {
        if (IsRemovalMode) CancelRemovalMode();
        else StartRemovalMode();
    }

    /// <summary>
    /// 그리드의 특정 위치에 스레드를 배치합니다.
    /// </summary>
    public bool PlaceThread(Vector2Int gridPos, ThreadState templateThread)
    {
        if (!_gridHandler.CanPlaceThread(gridPos)) return false;

        ThreadState newThreadInstance = _threadPlacementHandler.PlaceThread(gridPos, templateThread.threadId);
        if (newThreadInstance == null) return false;

        ThreadObject threadObject = _gridHandler.CreateThreadObject(gridPos, newThreadInstance);
        if (threadObject == null) return false;

        threadObject.SetGridPosition(gridPos);
        _gridHandler.SetTileOccupied(gridPos, true);

        return true;
    }

    /// <summary>
    /// 그리드의 특정 위치에 있는 스레드를 제거합니다.
    /// </summary>
    public bool RemoveThread(Vector2Int gridPos)
    {
        ThreadObject threadObject = _gridHandler?.GetThreadObjectAt(gridPos);
        bool placementRemoved = _threadPlacementHandler != null && _threadPlacementHandler.RemovePlacedThread(gridPos);

        if (threadObject == null && !placementRemoved) return false;

        _gridHandler?.RemoveThreadObject(gridPos);
        return true;
    }

    public ThreadObject GetThreadObjectAt(Vector2Int gridPos) => _gridHandler?.GetThreadObjectAt(gridPos);

    /// <summary>
    /// 데이터 핸들러로부터 모든 배치된 스레드 정보를 가져와 그리드 UI를 갱신합니다.
    /// </summary>
    public void RefreshThreads()
    {
        _gridHandler.ClearAllThreadObjects();

        foreach (KeyValuePair<Vector2Int, ThreadPlacementState> kvp in _threadPlacementHandler.GetAllPlacedThreads())
        {
            ThreadState threadState = kvp.Value.RuntimeState;
            if (threadState == null) continue;

            _gridHandler.SetTileOccupied(kvp.Key, true);
            ThreadObject threadObject = _gridHandler.CreateThreadObject(kvp.Key, threadState);
            if (threadObject != null)
            {
                threadObject.SetGridPosition(kvp.Key);
            }
        }
    }

    private void CreateSharedThreadLabelCanvas()
    {
        RectTransform canvasRect = GameManager.GetWorldCanvas();
        if (canvasRect != null)
        {
            _sharedThreadLabelCanvas = canvasRect.gameObject;
        }
    }

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

    private void OnThreadClicked(ThreadObject threadObject)
    {
        if (threadObject == null || threadObject.ThreadState == null) return;

        if (_mainCanvas != null)
        {
            _mainCanvas.ShowThreadInfoPanel(threadObject.ThreadState);
        }
    }
}