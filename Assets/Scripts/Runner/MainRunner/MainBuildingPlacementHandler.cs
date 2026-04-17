using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 메인에서 건물 프리뷰/회전/설치/제거 모드를 처리합니다.
/// UI는 추후 붙이기 쉽게 public API만 제공합니다.
/// </summary>
public class MainBuildingPlacementHandler
{
    private readonly MainRunner _runner;
    private readonly MainBuildingGridHandler _gridHandler;

    private BuildingData _selectedBuilding;
    private int _rotation;

    private bool _placementMode;
    private bool _removalMode;
    private bool _autoEmployeePlacement;

    private GameObject _previewObj;
    private PreviewObject _previewObject;
    private bool _isPointerPlacementActive;
    private Vector2Int _lastPlacedOrigin;
    private int _lastPlacedRotation;
    private string _lastPlacedBuildingId;

    public bool IsPlacementMode => _placementMode;
    public bool IsRemovalMode => _removalMode;
    public bool IsAutoEmployeePlacement => _autoEmployeePlacement;
    public bool IsPointerPlacementActive => _isPointerPlacementActive;
    public BuildingData SelectedBuilding => _selectedBuilding;
    public int Rotation => _rotation;

    public MainBuildingPlacementHandler(MainRunner runner)
    {
        _runner = runner;

        _gridHandler = _runner.GridHandler;
    }

    public void StartPlacement(BuildingData data)
    {
        CancelRemoval();
        _placementMode = true;
        _selectedBuilding = data;
        _rotation = 0;
        _isPointerPlacementActive = false;
        _lastPlacedOrigin = new Vector2Int(int.MinValue, int.MinValue);
        _lastPlacedRotation = -1;
        _lastPlacedBuildingId = null;
        CreatePreview();
    }

    public void CancelPlacement()
    {
        _placementMode = false;
        _selectedBuilding = null;
        _isPointerPlacementActive = false;
        DestroyPreview();
    }

    public void ToggleAutoEmployeePlacement(bool enabled)
    {
        _autoEmployeePlacement = enabled;
    }

    public void StartRemoval()
    {
        CancelPlacement();
        _removalMode = true;
    }

    public void CancelRemoval()
    {
        _removalMode = false;
    }

    public void Rotate(bool clockwise)
    {
        if (!_placementMode) return;
        _rotation = clockwise ? (_rotation + 1) % 4 : (_rotation + 3) % 4;
        if (_previewObject != null)
        {
            _previewObject.SetPreviewRotation(_rotation);
        }
    }

    public void Update(Camera cam)
    {
        if (EventSystem.current.IsPointerOverGameObject()) return;

        if (_placementMode) UpdatePlacement(cam);
        else if (_removalMode) UpdateRemoval(cam);
    }

    private void UpdatePlacement(Camera cam)
    {
        if (Input.GetKeyDown(KeyCode.Q)) Rotate(false);
        if (Input.GetKeyDown(KeyCode.E)) Rotate(true);

        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
        {
            CancelPlacement();
            return;
        }

        Vector3 mouseWorld = cam.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0f;

        Vector2Int origin = _gridHandler.WorldToGridPosition(mouseWorld);
        Vector2Int size = MainBuildingGridHandler.GetRotatedSize(_selectedBuilding.size, _rotation);
        bool canPlaceByGrid = _gridHandler.CanPlace(origin, size);
        bool canPlaceByCount = _gridHandler.CanPlaceMoreInstances(_selectedBuilding);
        bool canPlace = canPlaceByGrid && canPlaceByCount;

        UpdatePreview(origin, size, canPlace);

        if (Input.GetMouseButtonDown(0))
        {
            if (!canPlace)
            {
                if (!canPlaceByCount)
                {
                    UIManager.Instance.ShowWarningPopup(WarningMessage.BuildingPlacedCountLimitReached);
                }
                return;
            }
            _isPointerPlacementActive = true;
            TryPlaceAtPreview(origin);
        }

        if (Input.GetMouseButton(0) && canPlace)
        {
            _isPointerPlacementActive = true;
            TryPlaceAtPreview(origin);
        }

        if (Input.GetMouseButtonUp(0))
        {
            _isPointerPlacementActive = false;
            _lastPlacedOrigin = new Vector2Int(int.MinValue, int.MinValue);
            _lastPlacedRotation = -1;
            _lastPlacedBuildingId = null;
        }
    }

    private void UpdateRemoval(Camera cam)
    {
        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
        {
            CancelRemoval();
            return;
        }

        Vector3 mouseWorld = cam.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0f;
        Vector2Int p = _gridHandler.WorldToGridPosition(mouseWorld);

        if (Input.GetMouseButtonDown(0))
        {
            if (_gridHandler.TryRemoveAt(p) && _runner.RemovalSound != null)
            {
                _runner.SoundManager?.PlaySFX(_runner.RemovalSound);
            }
        }
    }

    private void CreatePreview()
    {
        DestroyPreview();

        _previewObj = MonoBehaviour.Instantiate(_runner.PreviewPrefab);
        _previewObj.transform.SetParent(_runner.transform, worldPositionStays: true);

        _previewObject = _previewObj.GetComponent<PreviewObject>();
        if (_previewObject == null)
        {
            Debug.LogError("[MainBuildingPlacementHandler] Preview prefab is missing PreviewObject component.");
            return;
        }

        _previewObject.SetBuildingData(_selectedBuilding);
        _previewObject.SetPreviewScale(_selectedBuilding.size);
        _previewObject.SetPlacementState(true);
        _previewObject.SetPreviewRotation(_rotation);
    }

    private void UpdatePreview(Vector2Int origin, Vector2Int size, bool canPlace)
    {
        if (_previewObj == null || _previewObject == null) return;

        _previewObj.transform.position = _gridHandler.GridToWorldPosition(origin, size);
        _previewObject.SetPlacementState(canPlace);
    }

    private void DestroyPreview()
    {
        if (_previewObj != null)
        {
            Object.Destroy(_previewObj);
            _previewObj = null;
            _previewObject = null;
        }
    }

    private void TryPlaceAtPreview(Vector2Int origin)
    {
        if (!_isPointerPlacementActive || _selectedBuilding == null)
            return;

        string buildingId = _selectedBuilding.id;
        if (_lastPlacedOrigin == origin && _lastPlacedRotation == _rotation && _lastPlacedBuildingId == buildingId)
            return;

        _lastPlacedOrigin = origin;
        _lastPlacedRotation = _rotation;
        _lastPlacedBuildingId = buildingId;

        if (_selectedBuilding.IsRoad)
        {
            if (_gridHandler.TryPlaceRoad(_selectedBuilding, origin, _rotation, out _, out bool roadNoMoney))
            {
                if (_runner.BuildSound != null)
                {
                    _runner.SoundManager?.PlaySFX(_runner.BuildSound);
                }
            }
            else if (roadNoMoney)
            {
                UIManager.Instance.ShowWarningPopup(WarningMessage.NotEnoughCredits);
            }
            return;
        }

        if (_gridHandler.TryPlaceBuilding(_selectedBuilding, origin, _rotation, out _, out bool buildingNoMoney))
        {
            if (_runner.BuildSound != null)
            {
                _runner.SoundManager?.PlaySFX(_runner.BuildSound);
            }
        }
        else if (buildingNoMoney)
        {
            UIManager.Instance.ShowWarningPopup(WarningMessage.NotEnoughCredits);
        }
    }
}

