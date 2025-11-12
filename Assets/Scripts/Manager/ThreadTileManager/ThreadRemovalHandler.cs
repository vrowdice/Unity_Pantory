using UnityEngine;
using UnityEngine.EventSystems;

namespace Pantory.Managers
{
    public class ThreadRemovalHandler
    {
        private readonly global::ThreadTileManager _manager;
    private readonly ThreadGridHandler _gridHandler;
    private readonly MainCameraController _cameraController;
    private readonly Camera _camera;

    private bool _isActive;
    private ThreadObject _hoveredThread;

    private static readonly Color HIGHLIGHT_COLOR = new Color(1f, 0.4f, 0.4f, 0.9f);

    public bool IsActive => _isActive;

        public ThreadRemovalHandler(global::ThreadTileManager manager, ThreadGridHandler gridHandler, MainCameraController cameraController)
        {
            _manager = manager;
            _gridHandler = gridHandler;
            _cameraController = cameraController;
            _camera = cameraController != null ? cameraController.Camera : Camera.main;
        }

    public void StartRemoval()
    {
        if (_isActive)
            return;

        _isActive = true;
        _cameraController?.SetDragEnabled(false);
    }

    public void CancelRemoval()
    {
        if (!_isActive)
            return;

        _isActive = false;
        _cameraController?.SetDragEnabled(true);
        ResetHighlight();
    }

    public void Update()
    {
        if (!_isActive || _camera == null)
            return;

        UpdateHover();
        HandleInput();
    }

    private void UpdateHover()
    {
        if (IsPointerOverUI())
        {
            ResetHighlight();
            return;
        }

        Vector3 mouseWorldPos = _camera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0f;

        Vector2Int gridPos = _gridHandler.WorldToGridPosition(mouseWorldPos);
        ThreadObject threadObject = _manager.GetThreadObjectAt(gridPos);

        if (threadObject == _hoveredThread)
            return;

        ResetHighlight();

        if (threadObject != null)
        {
            _hoveredThread = threadObject;
            _hoveredThread.SetHighlight(HIGHLIGHT_COLOR);
        }
    }

    private void HandleInput()
    {
        if (Input.GetMouseButtonDown(0) && _hoveredThread != null && !IsPointerOverUI())
        {
            if (_manager.RemoveThread(_hoveredThread.GridPosition))
            {
                ResetHighlight();
            }
        }

        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
        {
            CancelRemoval();
        }
    }

    private bool IsPointerOverUI()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }

    private void ResetHighlight()
    {
        if (_hoveredThread != null)
        {
            _hoveredThread.ResetColor();
            _hoveredThread = null;
        }
    }
    }
}

