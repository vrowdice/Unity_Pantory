using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 메인에서 건물 프리뷰/회전/설치/제거 모드를 처리합니다.
/// UI는 추후 붙이기 쉽게 public API만 제공합니다.
/// </summary>
public class MainBuildingPlacementHandler
{
    private readonly Transform _parent;
    private readonly MainBuildingGridHandler _gridHandler;

    private BuildingData _selectedBuilding;
    private int _rotation;

    private bool _placementMode;
    private bool _removalMode;

    private GameObject _previewObj;
    private SpriteRenderer _previewRenderer;

    public bool IsPlacementMode => _placementMode;
    public bool IsRemovalMode => _removalMode;
    public BuildingData SelectedBuilding => _selectedBuilding;
    public int Rotation => _rotation;

    public MainBuildingPlacementHandler(Transform parent, MainBuildingGridHandler grid)
    {
        _parent = parent;
        _gridHandler = grid;
    }

    public void StartPlacement(BuildingData data)
    {
        if (data == null) return;
        CancelRemoval();
        _placementMode = true;
        _selectedBuilding = data;
        _rotation = 0;
        CreatePreview();
    }

    public void CancelPlacement()
    {
        _placementMode = false;
        _selectedBuilding = null;
        DestroyPreview();
    }

    public void ToggleRemoval()
    {
        if (_removalMode) CancelRemoval();
        else StartRemoval();
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
        if (_previewObj != null)
        {
            _previewObj.transform.rotation = Quaternion.Euler(0, 0, -_rotation * 90f);
        }
    }

    public void Update(Camera cam)
    {
        if (cam == null) return;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        if (_placementMode) UpdatePlacement(cam);
        else if (_removalMode) UpdateRemoval(cam);
    }

    private void UpdatePlacement(Camera cam)
    {
        if (_selectedBuilding == null) return;

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
        bool canPlace = _gridHandler.CanPlace(origin, size);

        UpdatePreview(origin, size, canPlace);

        if (Input.GetMouseButtonDown(0) && canPlace)
        {
            if (_selectedBuilding.IsRoad)
            {

            }
            else
            {
                _gridHandler.TryPlaceBuilding(_selectedBuilding, origin, _rotation, out _);
            }
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
            _gridHandler.TryRemoveAt(p);
        }
    }

    private void CreatePreview()
    {
        DestroyPreview();
        if (_selectedBuilding == null) return;

        _previewObj = new GameObject("BuildingPreview");
        _previewObj.transform.SetParent(_parent, worldPositionStays: true);

        _previewRenderer = _previewObj.AddComponent<SpriteRenderer>();
        _previewRenderer.transform.localScale = new Vector3(_selectedBuilding.size.x, _selectedBuilding.size.y, 1);
        _previewRenderer.sprite = _selectedBuilding.buildingSprite;
        _previewRenderer.sortingOrder = 1000;
        _previewRenderer.color = new Color(1f, 1f, 1f, 0.65f);

        _previewObj.transform.rotation = Quaternion.Euler(0, 0, -_rotation * 90f);
    }

    private void UpdatePreview(Vector2Int origin, Vector2Int size, bool canPlace)
    {
        if (_previewObj == null) return;

        _previewObj.transform.position = _gridHandler.GridToWorldPosition(origin, size);
        Color okColor = VisualManager.Instance != null ? VisualManager.Instance.ValidColor : Color.green;
        Color noColor = VisualManager.Instance != null ? VisualManager.Instance.InvalidColor : Color.red;
        _previewRenderer.color = canPlace ? new Color(okColor.r, okColor.g, okColor.b, 0.65f) : new Color(noColor.r, noColor.g, noColor.b, 0.65f);
    }

    private void DestroyPreview()
    {
        if (_previewObj != null)
        {
            Object.Destroy(_previewObj);
            _previewObj = null;
            _previewRenderer = null;
        }
    }
}

