using System.Collections.Generic;
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

    private bool _placementMode = false;
    private bool _removalMode = false;
    private bool _blueprintPlacementMode = false;
    private bool _autoEmployeePlacement = false;

    private GameObject _previewObj;
    private PreviewObject _previewObject;
    private bool _isPointerPlacementActive;
    private Vector2Int _lastPlacedOrigin;
    private int _lastPlacedRotation;
    private string _lastPlacedBuildingId;
    private readonly List<PlacedBuildingSaveData> _selectedBlueprintBuildings = new List<PlacedBuildingSaveData>();
    private readonly List<PlacedRoadSaveData> _selectedBlueprintRoads = new List<PlacedRoadSaveData>();
    private readonly List<BlueprintPreviewEntry> _blueprintPreviews = new List<BlueprintPreviewEntry>();
    private GameObject _blueprintPreviewRootObj;
    private BlueprintPreviewObject _blueprintPreviewObject;
    private string _selectedBlueprintName;
    private Vector2Int _blueprintMinOrigin;

    public bool IsPlacementMode => _placementMode;
    public bool IsRemovalMode => _removalMode;
    public bool IsBlueprintPlacementMode => _blueprintPlacementMode;
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
        CancelBlueprintPlacement();
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

    public void StartBlueprintPlacement(string blueprintName, List<PlacedBuildingSaveData> blueprintBuildings, List<PlacedRoadSaveData> blueprintRoads)
    {
        bool hasBuildings = blueprintBuildings != null && blueprintBuildings.Count > 0;
        bool hasRoads = blueprintRoads != null && blueprintRoads.Count > 0;
        if (!hasBuildings && !hasRoads)
            return;

        CancelPlacement();
        CancelRemoval();
        _blueprintPlacementMode = true;
        _selectedBlueprintName = string.IsNullOrWhiteSpace(blueprintName) ? "Blueprint" : blueprintName.Trim();
        _selectedBlueprintBuildings.Clear();
        _selectedBlueprintRoads.Clear();
        _blueprintMinOrigin = new Vector2Int(int.MaxValue, int.MaxValue);

        if (blueprintBuildings != null)
        {
            for (int i = 0; i < blueprintBuildings.Count; i++)
            {
                PlacedBuildingSaveData src = blueprintBuildings[i];
                if (src == null || string.IsNullOrEmpty(src.buildingDataId))
                    continue;

                _blueprintMinOrigin.x = Mathf.Min(_blueprintMinOrigin.x, src.originX);
                _blueprintMinOrigin.y = Mathf.Min(_blueprintMinOrigin.y, src.originY);
                _selectedBlueprintBuildings.Add(src);
            }
        }

        if (blueprintRoads != null)
        {
            for (int i = 0; i < blueprintRoads.Count; i++)
            {
                PlacedRoadSaveData src = blueprintRoads[i];
                if (src == null)
                    continue;

                _blueprintMinOrigin.x = Mathf.Min(_blueprintMinOrigin.x, src.x);
                _blueprintMinOrigin.y = Mathf.Min(_blueprintMinOrigin.y, src.y);
                _selectedBlueprintRoads.Add(src);
            }
        }

        if (_selectedBlueprintBuildings.Count == 0 && _selectedBlueprintRoads.Count == 0)
            CancelBlueprintPlacement();
        else
            CreateBlueprintPreviews();
    }

    public void CancelBlueprintPlacement()
    {
        _blueprintPlacementMode = false;
        _selectedBlueprintName = null;
        _selectedBlueprintBuildings.Clear();
        _selectedBlueprintRoads.Clear();
        _blueprintMinOrigin = new Vector2Int(int.MaxValue, int.MaxValue);
        DestroyBlueprintPreviews();
    }

    public void ToggleAutoEmployeePlacement(bool enabled)
    {
        _autoEmployeePlacement = enabled;
    }

    public void StartRemoval()
    {
        CancelBlueprintPlacement();
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
        if (UIManager.Instance != null && UIManager.Instance.IsTypingInTextInput())
            return;

        if (EventSystem.current.IsPointerOverGameObject()) return;

        if (_placementMode) UpdatePlacement(cam);
        else if (_blueprintPlacementMode) UpdateBlueprintPlacement(cam);
        else if (_removalMode) UpdateRemoval(cam);
    }

    private void UpdateBlueprintPlacement(Camera cam)
    {
        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
        {
            CancelBlueprintPlacement();
            return;
        }

        Vector3 mouseWorld = cam.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0f;
        Vector2Int anchor = _gridHandler.WorldToGridPosition(mouseWorld);

        bool hasAnyInsufficientCredits;
        bool hasAnyCountLimit;
        bool canPlaceBlueprint = CanPlaceBlueprintAt(anchor, out hasAnyInsufficientCredits, out hasAnyCountLimit);
        UpdateBlueprintPreviews(anchor, canPlaceBlueprint);

        if (!Input.GetMouseButtonDown(0))
            return;

        if (!canPlaceBlueprint)
        {
            if (hasAnyInsufficientCredits)
                UIManager.Instance.ShowWarningPopup(WarningMessage.NotEnoughCredits);
            else if (hasAnyCountLimit)
                UIManager.Instance.ShowWarningPopup(WarningMessage.BuildingPlacedCountLimitReached);
            return;
        }

        if (!TryPlaceBlueprintAt(anchor))
            return;
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

    private bool CanPlaceBlueprintAt(Vector2Int anchor, out bool hasAnyInsufficientCredits, out bool hasAnyCountLimit)
    {
        hasAnyInsufficientCredits = false;
        hasAnyCountLimit = false;
        long totalBuildCost = 0;

        for (int i = 0; i < _selectedBlueprintBuildings.Count; i++)
        {
            PlacedBuildingSaveData saveData = _selectedBlueprintBuildings[i];
            BuildingData data = DataManager.Instance.Building.GetBuildingData(saveData.buildingDataId);
            if (data == null)
                return false;

            if (!_gridHandler.CanPlaceMoreInstances(data))
            {
                hasAnyCountLimit = true;
                return false;
            }

            Vector2Int origin = GetBlueprintPlacementOrigin(anchor, saveData);
            Vector2Int rotatedSize = MainBuildingGridHandler.GetRotatedSize(data.size, saveData.rotation);
            if (!_gridHandler.CanPlace(origin, rotatedSize))
                return false;

            totalBuildCost += data.buildCost;
        }

        for (int i = 0; i < _selectedBlueprintRoads.Count; i++)
        {
            PlacedRoadSaveData saveData = _selectedBlueprintRoads[i];
            BuildingData roadData = GetRoadDataForBlueprint(saveData);
            if (roadData == null)
                return false;

            Vector2Int position = GetBlueprintPlacementPosition(anchor, saveData.x, saveData.y);
            if (!_gridHandler.IsWithinBounds(position, Vector2Int.one) || !_gridHandler.CanPlace(position, Vector2Int.one))
                return false;

            totalBuildCost += roadData.buildCost;
        }

        if (DataManager.Instance.Finances.Credit < totalBuildCost)
        {
            hasAnyInsufficientCredits = true;
            return false;
        }

        return true;
    }

    private bool TryPlaceBlueprintAt(Vector2Int anchor)
    {
        for (int i = 0; i < _selectedBlueprintBuildings.Count; i++)
        {
            PlacedBuildingSaveData saveData = _selectedBlueprintBuildings[i];
            BuildingData data = DataManager.Instance.Building.GetBuildingData(saveData.buildingDataId);
            if (data == null)
                return false;

            Vector2Int origin = GetBlueprintPlacementOrigin(anchor, saveData);
            int rotation = saveData.rotation;
            if (!_gridHandler.TryPlaceBuilding(data, origin, rotation, out _, out bool buildingNoMoney))
            {
                if (buildingNoMoney)
                    UIManager.Instance.ShowWarningPopup(WarningMessage.NotEnoughCredits);
                return false;
            }
        }

        for (int i = 0; i < _selectedBlueprintRoads.Count; i++)
        {
            PlacedRoadSaveData saveData = _selectedBlueprintRoads[i];
            BuildingData roadData = GetRoadDataForBlueprint(saveData);
            if (roadData == null)
                return false;

            Vector2Int position = GetBlueprintPlacementPosition(anchor, saveData.x, saveData.y);
            if (!_gridHandler.TryPlaceRoad(roadData, position, saveData.rotation, out _, out bool roadNoMoney))
            {
                if (roadNoMoney)
                    UIManager.Instance.ShowWarningPopup(WarningMessage.NotEnoughCredits);
                return false;
            }
        }

        if (_runner.BuildSound != null)
            _runner.SoundManager?.PlaySFX(_runner.BuildSound);

        return true;
    }

    private Vector2Int GetBlueprintPlacementOrigin(Vector2Int anchor, PlacedBuildingSaveData saveData)
    {
        int offsetX = saveData.originX - _blueprintMinOrigin.x;
        int offsetY = saveData.originY - _blueprintMinOrigin.y;
        return new Vector2Int(anchor.x + offsetX, anchor.y + offsetY);
    }

    private Vector2Int GetBlueprintPlacementPosition(Vector2Int anchor, int originalX, int originalY)
    {
        int offsetX = originalX - _blueprintMinOrigin.x;
        int offsetY = originalY - _blueprintMinOrigin.y;
        return new Vector2Int(anchor.x + offsetX, anchor.y + offsetY);
    }

    private static BuildingData GetRoadDataForBlueprint(PlacedRoadSaveData saveData)
    {
        if (saveData == null)
            return null;

        string id = !string.IsNullOrEmpty(saveData.sourceBuildingDataId) ? saveData.sourceBuildingDataId : saveData.roadDataId;
        if (string.IsNullOrEmpty(id))
            id = "road";
        return DataManager.Instance.Building.GetBuildingData(id);
    }

    private void CreateBlueprintPreviews()
    {
        DestroyBlueprintPreviews();

        if (_runner.BlueprintPreviewPrefab == null)
            return;

        _blueprintPreviewRootObj = MonoBehaviour.Instantiate(_runner.BlueprintPreviewPrefab);
        _blueprintPreviewRootObj.transform.SetParent(_runner.transform, worldPositionStays: true);
        _blueprintPreviewObject = _blueprintPreviewRootObj.GetComponent<BlueprintPreviewObject>();
        if (_blueprintPreviewObject == null)
        {
            Object.Destroy(_blueprintPreviewRootObj);
            _blueprintPreviewRootObj = null;
            return;
        }
        _blueprintPreviewObject.ClearRegisteredSpriteRenderers();

        for (int i = 0; i < _selectedBlueprintBuildings.Count; i++)
        {
            PlacedBuildingSaveData saveData = _selectedBlueprintBuildings[i];
            BuildingData data = DataManager.Instance.Building.GetBuildingData(saveData.buildingDataId);
            if (data == null)
                continue;

            GameObject child = new GameObject($"BlueprintPreview_Building_{i}");
            child.transform.SetParent(_blueprintPreviewRootObj.transform, worldPositionStays: false);
            SpriteRenderer sr = child.AddComponent<SpriteRenderer>();
            sr.sprite = data.buildingSprite;

            BlueprintPreviewEntry entry = new BlueprintPreviewEntry();
            entry.saveData = saveData;
            entry.data = data;
            entry.spriteRenderer = sr;
            _blueprintPreviews.Add(entry);
            _blueprintPreviewObject.RegisterSpriteRenderer(sr);
        }

        for (int i = 0; i < _selectedBlueprintRoads.Count; i++)
        {
            PlacedRoadSaveData saveData = _selectedBlueprintRoads[i];
            BuildingData roadData = GetRoadDataForBlueprint(saveData);
            if (roadData == null)
                continue;

            GameObject child = new GameObject($"BlueprintPreview_Road_{i}");
            child.transform.SetParent(_blueprintPreviewRootObj.transform, worldPositionStays: false);
            SpriteRenderer sr = child.AddComponent<SpriteRenderer>();
            sr.sprite = roadData.buildingSprite;

            BlueprintPreviewEntry entry = new BlueprintPreviewEntry();
            entry.roadSaveData = saveData;
            entry.data = roadData;
            entry.spriteRenderer = sr;
            _blueprintPreviews.Add(entry);
            _blueprintPreviewObject.RegisterSpriteRenderer(sr);
        }

        long totalPrice = CalculateBlueprintTotalPrice();
        _blueprintPreviewObject.SetBlueprintInfo(_selectedBlueprintName, totalPrice);
        _blueprintPreviewObject.SetPlacementState(true);
    }

    private void UpdateBlueprintPreviews(Vector2Int anchor, bool canPlaceAll)
    {
        if (_blueprintPreviewRootObj != null)
            _blueprintPreviewRootObj.transform.position = _gridHandler.GridToWorldPosition(anchor, Vector2Int.one);

        for (int i = 0; i < _blueprintPreviews.Count; i++)
        {
            BlueprintPreviewEntry entry = _blueprintPreviews[i];
            if (entry.spriteRenderer == null || entry.data == null)
                continue;

            Vector2Int origin;
            Vector2Int size;
            if (entry.roadSaveData != null)
            {
                origin = GetBlueprintPlacementPosition(anchor, entry.roadSaveData.x, entry.roadSaveData.y);
                size = Vector2Int.one;
            }
            else if (entry.saveData != null)
            {
                origin = GetBlueprintPlacementOrigin(anchor, entry.saveData);
                size = MainBuildingGridHandler.GetRotatedSize(entry.data.size, entry.saveData.rotation);
            }
            else
            {
                continue;
            }

            int offsetX = origin.x - anchor.x;
            int offsetY = origin.y - anchor.y;
            float localX = offsetX + (size.x - 1) * 0.5f;
            float localY = -offsetY - (size.y - 1) * 0.5f;
            entry.spriteRenderer.transform.localPosition = new Vector3(localX, localY, 0f);
            entry.spriteRenderer.transform.localScale = new Vector3(size.x, size.y, 1f);
            int rotation = entry.roadSaveData != null ? entry.roadSaveData.rotation : entry.saveData.rotation;
            entry.spriteRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, -rotation * 90f);
        }

        if (_blueprintPreviewObject != null)
            _blueprintPreviewObject.SetPlacementState(canPlaceAll);
    }

    private void DestroyBlueprintPreviews()
    {
        for (int i = 0; i < _blueprintPreviews.Count; i++)
        {
            BlueprintPreviewEntry entry = _blueprintPreviews[i];
            if (entry.spriteRenderer != null)
                Object.Destroy(entry.spriteRenderer.gameObject);
        }

        _blueprintPreviews.Clear();
        if (_blueprintPreviewRootObj != null)
            Object.Destroy(_blueprintPreviewRootObj);
        _blueprintPreviewRootObj = null;
        _blueprintPreviewObject = null;
    }

    private long CalculateBlueprintTotalPrice()
    {
        long totalPrice = 0;
        for (int i = 0; i < _selectedBlueprintBuildings.Count; i++)
        {
            BuildingData data = DataManager.Instance.Building.GetBuildingData(_selectedBlueprintBuildings[i].buildingDataId);
            if (data != null)
                totalPrice += data.buildCost;
        }

        for (int i = 0; i < _selectedBlueprintRoads.Count; i++)
        {
            BuildingData data = GetRoadDataForBlueprint(_selectedBlueprintRoads[i]);
            if (data != null)
                totalPrice += data.buildCost;
        }

        return totalPrice;
    }
}

