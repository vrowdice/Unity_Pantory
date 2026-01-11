using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

public class DesignRunnerPlacementHandler
{
    private readonly DesignRunner _manager;
    private readonly DesignRunnerGridHandler _grid;
    private readonly MainCameraController _cameraController;
    private readonly DesignCanvas _ui;

    private bool _isActive;
    private bool _isDragging;
    private bool _canPlace;

    private BuildingData _selectedBuilding;
    private Vector2Int _currentGridPos;
    private Vector2Int _lastPlacedGridPos = new Vector2Int(int.MinValue, int.MinValue);
    private int _rotationIndex = 0; // 0: 0, 1: 90, 2: 180, 3: 270

    private GameObject _previewObj;
    private SpriteRenderer _previewRenderer;
    private BuildingObject _previewComponent;

    private readonly GameObject _buildingPrefab;

    public bool IsActive => _isActive;

    public DesignRunnerPlacementHandler(DesignRunner manager, GameObject prefab)
    {
        _manager = manager;
        _grid = manager.GridGenHandler;
        _cameraController = manager.MainCamera.GetComponent<MainCameraController>();
        _ui = manager.DesignUiManager;
        _buildingPrefab = prefab;
    }

    public void StartPlacement(BuildingData data)
    {
        if (data == null) return;

        ClearPreview();
        _isActive = true;
        _selectedBuilding = data;
        _rotationIndex = 0;

        CreatePreview();
        _ui.UpdateModeBtnImages(true, false);
        _cameraController.SetDragEnabled(false);
    }

    public void CancelPlacement()
    {
        _isActive = false;
        _selectedBuilding = null;
        _isDragging = false;
        _lastPlacedGridPos = new Vector2Int(int.MinValue, int.MinValue);

        ClearPreview();
        _ui.UpdateModeBtnImages(false, false);
        _cameraController.SetDragEnabled(true);
    }

    public void Update()
    {
        if (!_isActive || _selectedBuilding == null) return;

        UpdateMousePosition();
        HandleInput();
    }

    public void Rotate(bool clockwise)
    {
        _rotationIndex = clockwise ? (_rotationIndex + 1) % 4 : (_rotationIndex + 3) % 4;
        ApplyPreviewRotation();
    }

    private void UpdateMousePosition()
    {
        bool isOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        SetPreviewVisible(!isOverUI);

        if (isOverUI)
        {
            _canPlace = false;
            return;
        }

        Vector3 mousePos = _cameraController.Camera.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0;

        _currentGridPos = _grid.WorldToGridPosition(mousePos);
        Vector2Int rotatedSize = GetRotatedSize(_selectedBuilding.size, _rotationIndex);

        _canPlace = _grid.CanPlaceBuilding(_currentGridPos, rotatedSize);

        // 프리뷰 위치 및 색상 업데이트
        if (_previewObj != null)
        {
            _previewObj.transform.position = _grid.GridToWorldPosition(_currentGridPos, rotatedSize);
            _previewRenderer.color = _canPlace ?
                (VisualManager.Instance?.ValidColor ?? Color.green) :
                (VisualManager.Instance?.InvalidColor ?? Color.red);

            _previewComponent?.UpdatePreviewMarkers(_currentGridPos, _grid, _rotationIndex);
        }
    }

    private void HandleInput()
    {
        if (Input.GetMouseButtonDown(0)) _isDragging = true;
        if (Input.GetMouseButtonUp(0)) _isDragging = false;

        if (_isDragging && _canPlace && _currentGridPos != _lastPlacedGridPos)
        {
            PlaceAt(_currentGridPos);
            _lastPlacedGridPos = _currentGridPos;
        }

        // 유틸리티 입력
        if (Input.GetKeyDown(KeyCode.Q)) Rotate(false);
        if (Input.GetKeyDown(KeyCode.E)) Rotate(true);
        if (Input.GetKeyDown(KeyCode.Escape)) CancelPlacement();
    }

    private void ApplyPreviewRotation()
    {
        if (_previewObj == null) return;

        _previewObj.transform.rotation = Quaternion.Euler(0, 0, -_rotationIndex * 90f);

        // 크기 재계산 (스프라이트 스케일 유지)
        _previewObj.transform.localScale = GameObjectUtils.CalculateSpriteScale(
            _selectedBuilding.buildingSprite, _selectedBuilding.size);
    }

    private void SetPreviewVisible(bool visible)
    {
        if (_previewRenderer != null) _previewRenderer.enabled = visible;
        _previewComponent?.SetMarkersActive(visible);
    }

    private void PlaceAt(Vector2Int gridPos)
    {
        string threadId = _manager.CurrentThreadId;
        if (string.IsNullOrEmpty(threadId)) return;

        Vector2Int rotatedSize = GetRotatedSize(_selectedBuilding.size, _rotationIndex);
        if (!_grid.CanPlaceBuilding(gridPos, rotatedSize)) return;

        BuildingState state = new BuildingState(_selectedBuilding.id, gridPos, _selectedBuilding, _rotationIndex);
        _manager.AddBuildingToTemp(state);
        _manager.RefreshBuildings();
    }

    private Vector2Int GetRotatedSize(Vector2Int size, int rotation)
    {
        // 90도(1), 270도(3)일 때 가로세로 반전
        return (rotation % 2 != 0) ? new Vector2Int(size.y, size.x) : size;
    }

    private void CreatePreview()
    {
        if (_buildingPrefab != null)
        {
            _previewObj = Object.Instantiate(_buildingPrefab, _manager.transform);
        }
        else
        {
            _previewObj = new GameObject("PlacementPreview");
            _previewObj.transform.SetParent(_manager.transform);
        }

        _previewRenderer = _previewObj.GetOrAddComponent<SpriteRenderer>();
        _previewRenderer.sprite = _selectedBuilding.buildingSprite;
        _previewRenderer.sortingOrder = 10; // 항상 위에 표시

        _previewComponent = _previewObj.GetOrAddComponent<BuildingObject>();
        _previewComponent.InitializePreview(_selectedBuilding, _manager.InputMarkerPrefab, _manager.OutputMarkerPrefab);

        ApplyPreviewRotation();
    }

    private void ClearPreview()
    {
        if (_previewObj != null)
        {
            Object.Destroy(_previewObj);
            _previewObj = null;
            _previewRenderer = null;
            _previewComponent = null;
        }
    }
}