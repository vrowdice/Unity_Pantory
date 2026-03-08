using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 건물 배치 및 제거 모드를 관리하는 핸들러입니다.
/// </summary>
public class DesignRunnerPlacementHandler
{
    private readonly DesignRunner _manager;
    private readonly DesignRunnerGridHandler _gridHandler;
    
    private bool _isPlacementActive;
    private BuildingData _selectedBuilding;
    private Vector2Int _currentGridPos;
    private int _rotationIndex = 0;
    
    private GameObject _previewObj;
    private SpriteRenderer _previewRenderer;
    private BuildingObject _previewComponent;
    
    private bool _isRemovalActive;
    private GameObject _hoveredBuilding;
    
    private const float BuildingZDepth = 9f;
    
    public bool IsPlacementActive => _isPlacementActive;
    public bool IsRemovalActive => _isRemovalActive;
    public BuildingData SelectedBuilding => _selectedBuilding;
    public int RotationIndex => _rotationIndex;
    
    public DesignRunnerPlacementHandler(DesignRunner manager, DesignRunnerGridHandler gridHandler)
    {
        _manager = manager;
        _gridHandler = gridHandler;
    }
    
    public (Vector2Int gridPos, bool canPlace) UpdatePlacement(Vector3 mouseWorldPos)
    {
        if (!_isPlacementActive || _selectedBuilding == null)
            return (Vector2Int.zero, false);

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            SetPreviewVisible(false);
            return (Vector2Int.zero, false);
        }

        _currentGridPos = GridMathUtils.GetWorldToGridPos(_manager.transform, mouseWorldPos);
        Vector2Int rotatedSize = GridMathUtils.GetRotatedSize(_selectedBuilding.size, _rotationIndex);
        
        bool isWithinBounds = IsWithinBounds(_currentGridPos, rotatedSize);
        if (!isWithinBounds)
        {
            SetPreviewVisible(false);
            return (_currentGridPos, false);
        }

        SetPreviewVisible(true);
        bool canPlace = _gridHandler.CanPlaceBuilding(_currentGridPos, rotatedSize);
        UpdatePreviewVisuals(rotatedSize);

        return (_currentGridPos, canPlace);
    }
    
    public void StartPlacement(BuildingData data)
    {
        if (data == null) return;
        ClearPreview();
        _isPlacementActive = true;
        _selectedBuilding = data;
        _rotationIndex = 0;

        CreatePreview();
        _manager.DesignUiManager?.UpdateModeBtnImages(true, false);
        _manager.MainCameraController.SetDragEnabled(false);
    }
    
    public void CancelPlacement()
    {
        _isPlacementActive = false;
        _selectedBuilding = null;
        ClearPreview();
        _manager.DesignUiManager?.UpdateModeBtnImages(false, false);
        _manager.MainCameraController.SetDragEnabled(true);
    }
    
    public void Rotate(bool clockwise)
    {
        _rotationIndex = clockwise ? (_rotationIndex + 1) % 4 : (_rotationIndex + 3) % 4;
        if (_previewObj != null)
        {
            _previewObj.transform.rotation = Quaternion.Euler(0, 0, -_rotationIndex * 90f);
            _previewObj.transform.localScale = GameObjectUtils.CalculateSpriteScale(
                _selectedBuilding.buildingSprite, _selectedBuilding.size);
        }
    }
    
    public void StartRemoval()
    {
        _isRemovalActive = true;
        _manager.DesignUiManager?.UpdateModeBtnImages(false, true);
    }
    
    public void CancelRemoval()
    {
        _isRemovalActive = false;
        ResetBuildingHighlight();
        _manager.DesignUiManager?.UpdateModeBtnImages(false, false);
    }
    
    public GameObject UpdateRemoval(Vector3 mouseWorldPos)
    {
        if (!_isRemovalActive) return null;

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            ResetBuildingHighlight();
            return null;
        }

        Vector2Int gridPos = GridMathUtils.GetWorldToGridPos(_manager.transform, mouseWorldPos);
        GameObject buildingAtPos = _gridHandler.GetBuildingAtPosition(gridPos);

        if (buildingAtPos != _hoveredBuilding)
        {
            ResetBuildingHighlight();
            _hoveredBuilding = buildingAtPos;
            HighlightBuilding(_hoveredBuilding);
        }

        return _hoveredBuilding;
    }
    
    public void ResetBuildingHighlight()
    {
        if (_hoveredBuilding != null && _hoveredBuilding.TryGetComponent(out SpriteRenderer r))
            r.color = Color.white;
        _hoveredBuilding = null;
    }
    
    private void CreatePreview()
    {
        _previewObj = _manager.BuildingObjectPrefab != null 
            ? Object.Instantiate(_manager.BuildingObjectPrefab, _manager.transform) 
            : new GameObject("PlacementPreview");

        _previewRenderer = _previewObj.GetComponent<SpriteRenderer>();
        _previewRenderer.sprite = _selectedBuilding.buildingSprite;
        _previewRenderer.sortingOrder = 10;
        
        Color c = _previewRenderer.color;
        _previewRenderer.color = new Color(c.r, c.g, c.b, 0.6f);

        _previewComponent = _previewObj.GetComponent<BuildingObject>();
        _previewComponent.InitializePreview(_selectedBuilding, _manager.InputMarkerPrefab, _manager.OutputMarkerPrefab);
        
        Rotate(false);
        SetPreviewVisible(false);
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
    
    private void SetPreviewVisible(bool visible)
    {
        if (_previewRenderer != null) _previewRenderer.enabled = visible;
        _previewComponent?.SetMarkersActive(visible);
    }
    
    private void UpdatePreviewVisuals(Vector2Int rotatedSize)
    {
        if (_previewObj == null) return;

        _previewObj.transform.position = GridMathUtils.GetGridToWorldPos(_manager.transform, _currentGridPos, rotatedSize, BuildingZDepth);
        
        bool canPlace = _gridHandler.CanPlaceBuilding(_currentGridPos, rotatedSize);
        Color stateColor = canPlace 
            ? (VisualManager.Instance?.ValidColor ?? Color.green) 
            : (VisualManager.Instance?.InvalidColor ?? Color.red);
        
        if (_previewRenderer != null) _previewRenderer.color = stateColor;
        _previewComponent?.UpdatePreviewMarkers(_currentGridPos, _gridHandler, _rotationIndex);
    }
    
    private bool IsWithinBounds(Vector2Int gridPos, Vector2Int size)
    {
        return gridPos.x >= 0 && gridPos.y >= 0 &&
               gridPos.x + size.x <= _manager.GridWidth && 
               gridPos.y + size.y <= _manager.GridHeight;
    }
    
    private void HighlightBuilding(GameObject building)
    {
        if (building == null)
        {
            return;
        }

        if (building.TryGetComponent(out SpriteRenderer r))
        {
            r.color = VisualManager.Instance?.InvalidColor ?? new Color(1, 0, 0, 0.5f);
        }
    }
}
