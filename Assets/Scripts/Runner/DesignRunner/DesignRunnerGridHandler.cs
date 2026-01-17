using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using System.Linq;
using Unity.VisualScripting;

/// <summary>
/// 그리드 타일의 생성, 건물 오브젝트의 배치 및 좌표 변환을 관리하는 핸들러입니다.
/// 배치, 제거, 계산 기능을 모두 포함합니다.
/// </summary>
public class DesignRunnerGridHandler
{
    private readonly DesignRunner _manager;
    private readonly Transform _parentTransform;

    private readonly Dictionary<Vector2Int, GameObject> _tileObjectMap = new Dictionary<Vector2Int, GameObject>();
    private readonly Dictionary<Vector2Int, GameObject> _buildingOriginMap = new Dictionary<Vector2Int, GameObject>();
    private readonly Dictionary<Vector2Int, GameObject> _occupancyMap = new Dictionary<Vector2Int, GameObject>();

    private List<BuildingState> _currentStates = new List<BuildingState>();
    private Dictionary<Vector2Int, BuildingState> _stateGridMap = new Dictionary<Vector2Int, BuildingState>();

    private bool _isPlacementActive;
    private bool _canPlace;
    private BuildingData _selectedBuilding;
    private Vector2Int _currentGridPos;
    private int _rotationIndex = 0;

    private GameObject _previewObj;
    private SpriteRenderer _previewRenderer;
    private BuildingObject _previewComponent;

    private bool _isRemovalActive;
    private GameObject _hoveredBuilding;

    private const float TileZDepth = 10f;
    private const float BuildingZDepth = 9f;

    public int Width => _manager.GridWidth;
    public int Height => _manager.GridHeight;
    public bool IsPlacementActive => _isPlacementActive;
    public bool IsRemovalActive => _isRemovalActive;
    public BuildingData SelectedBuilding => _selectedBuilding;
    public int RotationIndex => _rotationIndex;
    private DataManager DataManager => _manager.DataManager;

    public DesignRunnerGridHandler(DesignRunner manager)
    {
        _manager = manager;
        _parentTransform = manager.transform;
    }

    #region Grid Management

    public void CreateGrid(int width, int height)
    {
        ClearGrid();

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector2Int gridPosition = new Vector2Int(x, y);
                GameObject tileObject = Object.Instantiate(_manager.BuildingTilePrefab, new Vector3(x, -y, TileZDepth), Quaternion.identity, _parentTransform);
                tileObject.name = $"Tile_{x}_{y}";

                if (tileObject.TryGetComponent(out BuildingTile tileComponent))
                {
                    tileComponent.Initialize(gridPosition, null);
                }

                _tileObjectMap[gridPosition] = tileObject;
            }
        }
    }

    public void ExpandGrid(int newWidth, int newHeight) => CreateGrid(newWidth, newHeight);

    public void ClearGrid()
    {
        ClearAllPlacedBuildings();
        foreach (GameObject tile in _tileObjectMap.Values)
        {
            if (tile != null) Object.Destroy(tile);
        }
        _tileObjectMap.Clear();
    }

    public void ClearAllPlacedBuildings()
    {
        foreach (GameObject building in _buildingOriginMap.Values)
        {
            if (building != null) Object.Destroy(building);
        }
        _buildingOriginMap.Clear();
        _occupancyMap.Clear();
        ClearAllTileOccupancyStatus();
    }

    private void ClearAllTileOccupancyStatus()
    {
        foreach (GameObject tileObject in _tileObjectMap.Values)
        {
            if (tileObject.TryGetComponent(out BuildingTile tile))
            {
                tile.SetOccupied(false);
            }
        }
    }

    #endregion

    #region Building Object Management

    public GameObject CreateBuildingObject(Vector2Int gridPosition, BuildingData buildingData, BuildingState buildingState = null)
    {
        if (buildingData?.buildingSprite == null) return null;

        GameObject buildingObject;
        if (_manager.BuildingObjectPrefab != null)
            buildingObject = Object.Instantiate(_manager.BuildingObjectPrefab, _parentTransform);
        else
            buildingObject = new GameObject($"Building_{buildingData.id}");

        buildingObject.name = $"Building_{buildingData.id}_{gridPosition}";
        
        int rotation = buildingState?.rotation ?? 0;
        Vector2Int rotatedSize = GridMathUtility.GetRotatedSize(buildingData.size, rotation);
        
        buildingObject.transform.position = GridMathUtility.GetGridToWorldPos(_parentTransform, gridPosition, rotatedSize, BuildingZDepth);
        buildingObject.transform.rotation = Quaternion.Euler(0, 0, -rotation * 90f);

        SpriteRenderer renderer = buildingObject.GetOrAddComponent<SpriteRenderer>();
        renderer.sprite = buildingData.buildingSprite;
        buildingObject.transform.localScale = GameObjectUtils.CalculateSpriteScale(buildingData.buildingSprite, buildingData.size);

        BuildingObject buildingComponent = buildingObject.GetOrAddComponent<BuildingObject>();
        BoxCollider2D boxCollider = buildingObject.GetOrAddComponent<BoxCollider2D>();
        boxCollider.size = new Vector2(buildingData.size.x, buildingData.size.y);

        if (buildingState != null)
        {
            buildingComponent.Initialize(buildingData, buildingState, _manager.InputMarkerPrefab, _manager.OutputMarkerPrefab, this);
            buildingComponent.SetupProductionIcons();
        }

        RegisterBuildingToMaps(gridPosition, rotatedSize, buildingObject);

        return buildingObject;
    }

    private void RegisterBuildingToMaps(Vector2Int origin, Vector2Int size, GameObject buildingObj)
    {
        _buildingOriginMap[origin] = buildingObj;
        
        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                Vector2Int pos = new Vector2Int(origin.x + x, origin.y + y);
                _occupancyMap[pos] = buildingObj;
                SetTileOccupied(pos, true);
            }
        }
    }

    public void RemoveBuildingObject(Vector2Int origin)
    {
        if (_buildingOriginMap.TryGetValue(origin, out GameObject buildingObj))
        {
            if (buildingObj.TryGetComponent(out BuildingObject comp) && comp.BuildingData != null)
            {
                Vector2Int rotatedSize = GridMathUtility.GetRotatedSize(comp.BuildingData.size, comp.BuildingState.rotation);
                
                for (int x = 0; x < rotatedSize.x; x++)
                {
                    for (int y = 0; y < rotatedSize.y; y++)
                    {
                        Vector2Int pos = new Vector2Int(origin.x + x, origin.y + y);
                        _occupancyMap.Remove(pos);
                        SetTileOccupied(pos, false);
                    }
                }
            }

            Object.Destroy(buildingObj);
            _buildingOriginMap.Remove(origin);
        }
    }

    public GameObject GetBuildingAtPosition(Vector2Int gridPosition)
    {
        return _occupancyMap.TryGetValue(gridPosition, out GameObject obj) ? obj : null;
    }

    private void SetTileOccupied(Vector2Int pos, bool occupied)
    {
        if (_tileObjectMap.TryGetValue(pos, out GameObject tileObj) && 
            tileObj.TryGetComponent(out BuildingTile tile))
        {
            tile.SetOccupied(occupied);
        }
    }

    public void MarkTilesAsOccupied(Vector2Int startPosition, Vector2Int size, bool occupied = true)
    {
        for (int y = 0; y < size.y; y++)
        {
            for (int x = 0; x < size.x; x++)
            {
                Vector2Int currentPosition = new Vector2Int(startPosition.x + x, startPosition.y + y);
                SetTileOccupied(currentPosition, occupied);
            }
        }
    }

    #endregion

    #region Input & Logic (Placement/Removal)

    public (Vector2Int gridPos, bool canPlace) UpdatePlacement(Vector3 mouseWorldPos)
    {
        if (!_isPlacementActive || _selectedBuilding == null)
            return (Vector2Int.zero, false);

        bool isOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        SetPreviewVisible(!isOverUI);

        if (isOverUI)
        {
            _canPlace = false;
            return (Vector2Int.zero, false);
        }

        _currentGridPos = GridMathUtility.GetWorldToGridPos(_parentTransform, mouseWorldPos);
        Vector2Int rotatedSize = GridMathUtility.GetRotatedSize(_selectedBuilding.size, _rotationIndex);
        _canPlace = CanPlaceBuilding(_currentGridPos, rotatedSize);

        UpdatePreviewVisuals(rotatedSize);

        return (_currentGridPos, _canPlace);
    }

    private void UpdatePreviewVisuals(Vector2Int rotatedSize)
    {
        if (_previewObj == null) return;

        _previewObj.transform.position = GridMathUtility.GetGridToWorldPos(_parentTransform, _currentGridPos, rotatedSize, BuildingZDepth);
        
        Color stateColor = _canPlace 
            ? (VisualManager.Instance?.ValidColor ?? Color.green) 
            : (VisualManager.Instance?.InvalidColor ?? Color.red);
        
        if (_previewRenderer != null) _previewRenderer.color = stateColor;
        _previewComponent?.UpdatePreviewMarkers(_currentGridPos, this, _rotationIndex);
    }

    public void StartPlacement(BuildingData data)
    {
        if (data == null) return;
        ClearPreview();
        _isPlacementActive = true;
        _selectedBuilding = data;
        _rotationIndex = 0;

        CreatePreview();
        _manager.DesignUiManager.UpdateModeBtnImages(true, false);
        _manager.MainCameraController.SetDragEnabled(false);
    }

    public void CancelPlacement()
    {
        _isPlacementActive = false;
        _selectedBuilding = null;

        ClearPreview();
        _manager.DesignUiManager.UpdateModeBtnImages(false, false);
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

        bool isOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        if (isOverUI)
        {
            ResetBuildingHighlight();
            return null;
        }

        Vector2Int gridPos = GridMathUtility.GetWorldToGridPos(_parentTransform, mouseWorldPos);
        GameObject buildingAtPos = GetBuildingAtPosition(gridPos);

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

    #endregion

    #region Helpers (Visual & Check)

    private bool CanPlaceBuilding(Vector2Int startGridPos, Vector2Int rotatedSize)
    {
        if (startGridPos.x < 0 || startGridPos.y < 0 ||
            startGridPos.x + rotatedSize.x > _manager.GridWidth || startGridPos.y + rotatedSize.y > _manager.GridHeight)
            return false;

        for (int y = 0; y < rotatedSize.y; y++)
        {
            for (int x = 0; x < rotatedSize.x; x++)
            {
                Vector2Int pos = new Vector2Int(startGridPos.x + x, startGridPos.y + y);
                if (_occupancyMap.ContainsKey(pos)) return false;
            }
        }
        return true;
    }

    private void CreatePreview()
    {
        _previewObj = _manager.BuildingObjectPrefab != null 
            ? Object.Instantiate(_manager.BuildingObjectPrefab, _manager.transform) 
            : new GameObject("PlacementPreview");

        _previewRenderer = _previewObj.GetOrAddComponent<SpriteRenderer>();
        _previewRenderer.sprite = _selectedBuilding.buildingSprite;
        _previewRenderer.sortingOrder = 10;
        
        Color c = _previewRenderer.color;
        _previewRenderer.color = new Color(c.r, c.g, c.b, 0.6f);

        _previewComponent = _previewObj.GetOrAddComponent<BuildingObject>();
        _previewComponent.InitializePreview(_selectedBuilding, _manager.InputMarkerPrefab, _manager.OutputMarkerPrefab);
        
        Rotate(false);
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

    private void HighlightBuilding(GameObject building)
    {
        if (building != null && building.TryGetComponent(out SpriteRenderer r))
            r.color = VisualManager.Instance?.InvalidColor ?? new Color(1, 0, 0, 0.5f);
    }

    #endregion

    #region Calculation Wrappers (Delegates)

    public void RefreshCalculationData(List<BuildingState> states)
    {
        _currentStates = states ?? new List<BuildingState>();
        _stateGridMap.Clear();
        
        foreach (BuildingState state in _currentStates)
        {
            BuildingData data = DataManager.Building.GetBuildingData(state.buildingId);
            if (data == null) continue;

            Vector2Int size = GridMathUtility.GetRotatedSize(data.size, state.rotation);
            for (int x = 0; x < size.x; x++)
            {
                for (int y = 0; y < size.y; y++)
                {
                    Vector2Int pos = new Vector2Int(state.positionX + x, state.positionY + y);
                    _stateGridMap[pos] = state;
                }
            }
        }
    }

    public int CalculateThreadOutputs(string threadId, List<BuildingState> customStates = null)
    {
        if (DataManager == null) return 0;
        List<BuildingState> states = customStates ?? _currentStates;
        Dictionary<Vector2Int, BuildingState> map = customStates != null ? BuildTempMap(customStates) : _stateGridMap;
        int count = 0;

        foreach (BuildingState state in states)
        {
            BuildingData data = DataManager.Building.GetBuildingData(state.buildingId);
            if (data == null || !data.IsProductionBuilding || !state.IsUnlocked(DataManager)) continue;

            Vector2Int basePos = new Vector2Int(state.positionX, state.positionY);
            Vector2Int inPos = basePos + GridMathUtility.GetRotatedOffset(data.InputPosition, state.rotation);
            Vector2Int outPos = basePos + GridMathUtility.GetRotatedOffset(data.OutputPosition, state.rotation);

            if (RoadNetworkAnalyzer.IsConnected(inPos, true, false, map, DataManager) &&
                RoadNetworkAnalyzer.IsConnected(outPos, false, true, map, DataManager))
            {
                count++;
            }
        }
        return count;
    }

    public int CalculateTotalMaintenanceCost(string threadId, List<BuildingState> customStates = null)
    {
        List<BuildingState> statesToUse = customStates ?? _currentStates;
        ThreadCalculationResult stats = BuildingCalculationUtility.CalculateProductionStats(DataManager, statesToUse);
        return stats.TotalMaintenanceCost;
    }

    public void CalculateProductionChain(string threadId, List<BuildingState> states,
        out List<string> inputIds, out Dictionary<string, int> inputCounts,
        out List<string> outputIds, out Dictionary<string, int> outputCounts)
    {
        inputCounts = new Dictionary<string, int>();
        outputCounts = new Dictionary<string, int>();
        HashSet<string> reachableOutputs = new HashSet<string>();
        
        List<BuildingState> targetStates = states ?? _currentStates;
        Dictionary<Vector2Int, BuildingState> map = states != null ? BuildTempMap(states) : _stateGridMap;

        foreach (BuildingState state in targetStates)
        {
            BuildingData data = DataManager.Building.GetBuildingData(state.buildingId);
            if (data == null || !data.IsProductionBuilding || !state.IsUnlocked(DataManager)) continue;

            Vector2Int basePos = new Vector2Int(state.positionX, state.positionY);
            Vector2Int outPos = basePos + GridMathUtility.GetRotatedOffset(data.OutputPosition, state.rotation);
            Vector2Int inPos = basePos + GridMathUtility.GetRotatedOffset(data.InputPosition, state.rotation);

            if (RoadNetworkAnalyzer.IsConnected(outPos, false, true, map, DataManager))
            {
                if (state.outputProductionIds != null)
                {
                    foreach (string id in state.outputProductionIds)
                    {
                        if (string.IsNullOrEmpty(id)) continue;
                        reachableOutputs.Add(id);
                        outputCounts[id] = outputCounts.GetValueOrDefault(id, 0) + 1;
                    }
                }
            }

            if (RoadNetworkAnalyzer.IsConnected(inPos, true, false, map, DataManager))
            {
                ProcessInputs(state, inputCounts);
            }
        }

        inputIds = inputCounts.Keys.ToList();
        outputIds = reachableOutputs.ToList();
    }

    private Dictionary<Vector2Int, BuildingState> BuildTempMap(List<BuildingState> states)
    {
        Dictionary<Vector2Int, BuildingState> map = new Dictionary<Vector2Int, BuildingState>();
        foreach (BuildingState s in states)
        {
            BuildingData d = DataManager.Building.GetBuildingData(s.buildingId);
            if (d == null) continue;
            Vector2Int size = GridMathUtility.GetRotatedSize(d.size, s.rotation);
            for (int x = 0; x < size.x; x++)
            {
                for (int y = 0; y < size.y; y++)
                {
                    map[new Vector2Int(s.positionX + x, s.positionY + y)] = s;
                }
            }
        }
        return map;
    }

    private void ProcessInputs(BuildingState state, Dictionary<string, int> counts)
    {
        if (state.inputProductionIds != null && state.inputProductionIds.Count > 0)
        {
            foreach (string id in state.inputProductionIds)
            {
                if (string.IsNullOrEmpty(id)) continue;
                counts[id] = counts.GetValueOrDefault(id, 0) + 1;
            }
        }
        else if (state.outputProductionIds != null && state.outputProductionIds.Count > 0)
        {
            foreach (string outId in state.outputProductionIds)
            {
                if (string.IsNullOrEmpty(outId)) continue;

                ResourceEntry entry = DataManager.Resource.GetResourceEntry(outId);
                if (entry?.data?.requirements == null) continue;

                foreach (ResourceRequirement req in entry.data.requirements)
                {
                    if (req.resource != null && !string.IsNullOrEmpty(req.resource.id))
                    {
                        int amount = Mathf.Max(1, req.count);
                        counts[req.resource.id] = counts.GetValueOrDefault(req.resource.id, 0) + amount;
                    }
                }
            }
        }
    }

    #endregion

    #region Coordinate Conversion (Legacy Support)

    public Vector2Int WorldToGridPosition(Vector3 worldPosition)
    {
        return GridMathUtility.GetWorldToGridPos(_parentTransform, worldPosition);
    }

    public Vector3 GridToWorldPosition(Vector2Int gridPosition, Vector2Int size)
    {
        return GridMathUtility.GetGridToWorldPos(_parentTransform, gridPosition, size, BuildingZDepth);
    }

    #endregion
}
