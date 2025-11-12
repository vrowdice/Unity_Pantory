using UnityEngine;
using UnityEngine.EventSystems;

namespace Pantory.Managers
{
    public class ThreadPlacementHandler
    {
        private readonly global::ThreadTileManager _manager;
    private readonly ThreadGridHandler _gridHandler;
    private readonly MainCameraController _cameraController;
    private readonly Camera _camera;

    private ThreadState _selectedThread;
    private GameObject _previewObject;
    private ThreadObject _previewComponent;
    private Vector2Int _currentGridPos;
    private bool _canPlace;
    private bool _isActive;

    private static readonly Color OUTLINE_COLOR = new Color(0.2f, 0.8f, 1f, 0.8f);

    public bool IsActive => _isActive;
    public ThreadState SelectedThread => _selectedThread;

        public ThreadPlacementHandler(global::ThreadTileManager manager, ThreadGridHandler gridHandler, MainCameraController cameraController)
        {
            _manager = manager;
            _gridHandler = gridHandler;
            _cameraController = cameraController;
            _camera = cameraController != null ? cameraController.Camera : Camera.main;
        }

    public void StartPlacement(ThreadState threadState)
    {
        CancelPlacementInternal();

        if (threadState == null)
            return;

        _selectedThread = threadState;
        _isActive = true;
        _gridHandler.SetAllTilesOutline(true, OUTLINE_COLOR);
        _cameraController?.SetDragEnabled(false);

        CreatePreviewObject();
    }

    public void CancelPlacement()
    {
        CancelPlacementInternal();
    }

    public void Update()
    {
        if (!_isActive || _selectedThread == null)
            return;

        if (_camera == null)
            return;

        UpdatePreview();
        HandleInput();
    }

    private void UpdatePreview()
    {
        if (_previewComponent == null)
            return;

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            _previewObject.SetActive(false);
            _canPlace = false;
            return;
        }

        _previewObject.SetActive(true);

        Vector3 mouseWorldPos = _camera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0f;

        _currentGridPos = _gridHandler.WorldToGridPosition(mouseWorldPos);
        _canPlace = _gridHandler.CanPlaceThread(_currentGridPos);

        Vector3 worldPos = _gridHandler.GridToWorldPosition(_currentGridPos);
        _previewObject.transform.position = worldPos;

        _previewComponent.SetPreviewColor(_canPlace);
    }

    private void HandleInput()
    {
        if (Input.GetMouseButtonDown(0) && _canPlace && !IsPointerOverUI())
        {
            _manager.PlaceThread(_currentGridPos, _selectedThread);
        }

        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
        {
            CancelPlacement();
        }
    }

    private bool IsPointerOverUI()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }

    private void CreatePreviewObject()
    {
        if (_previewObject != null)
        {
            Object.Destroy(_previewObject);
        }

        if (_manager.ThreadObjectPrefab == null)
            return;

        _previewObject = Object.Instantiate(_manager.ThreadObjectPrefab, _manager.transform);
        _previewObject.name = "ThreadPreview";

        _previewComponent = _previewObject.GetComponent<ThreadObject>();
        if (_previewComponent == null)
        {
            _previewComponent = _previewObject.AddComponent<ThreadObject>();
        }

        _previewComponent.InitializePreview(_selectedThread, _manager.SharedThreadLabelCanvas);
    }

    private void CancelPlacementInternal()
    {
        _isActive = false;
        _selectedThread = null;
        _gridHandler.SetAllTilesOutline(false);
        _cameraController?.SetDragEnabled(true);

        if (_previewObject != null)
        {
            Object.Destroy(_previewObject);
            _previewObject = null;
            _previewComponent = null;
        }
    }
    }
}

