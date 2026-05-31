using System;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class MainBuildingGridHandler
{
    private readonly MainRunner _mainRunner;
    private readonly DataManager _dataManager;

    private readonly Transform _tileParent;
    private readonly Transform _buildingParent;
    private readonly Transform _roadParent;

    private readonly Dictionary<string, BuildingObject> _buildingObjDict = new();
    private readonly Dictionary<string, RoadObject> _roadObjDict = new();
    private readonly Dictionary<string, DualLaneRoadObject> _dualLaneRoadObjDict = new();

    private readonly Dictionary<string, int> _buildingOutputRoundRobinIndex = new();
    private readonly Dictionary<Vector2Int, string> _occupiedAsObjectDict = new();

    private const float TileZ = 10f;
    private const float BuildingZ = 9f;

    public event Action OnBuildingInstanceLayoutChanged;

    public Dictionary<string, BuildingObject> BuildingObjDict => _buildingObjDict;
    public Dictionary<string, RoadObject> RoadObjDict => _roadObjDict;
    public Dictionary<string, DualLaneRoadObject> DualLaneRoadObjDict => _dualLaneRoadObjDict;
    public Dictionary<string, int> BuildingOutputRoundRobinIndex => _buildingOutputRoundRobinIndex;
    public Dictionary<Vector2Int, string> OccupiedAsObjectDict => _occupiedAsObjectDict;
    public Transform TileParent => _tileParent;
    public Transform BuildingParent => _buildingParent;
    public Transform RoadParent => _roadParent;
    private bool AllowRawBuildingPlacement => _mainRunner.AllowRawBuildingPlacement;

    public IEnumerable<IBuilding> GetAllBuildings()
    {
        foreach (var building in _buildingObjDict.Values)
            yield return building;
    }

    public MainBuildingGridHandler(MainRunner runner)
    {
        _mainRunner = runner;
        _dataManager = DataManager.Instance;

        _tileParent = CreateChildTransform(runner.transform, "Tiles");
        _buildingParent = CreateChildTransform(runner.transform, "Buildings");
        _roadParent = CreateChildTransform(runner.transform, "Roads");
    }

    public void ClearAllBuildings()
    {
        _dataManager.Finances.ClearPlacedBuildingMaintenanceTotal();

        foreach (IBuilding building in GetAllBuildings())
        {
            building.ReleaseAssignedEmployees();
            MonoBehaviour.Destroy(building.gameObject);
        }
        _buildingObjDict.Clear();
        _buildingOutputRoundRobinIndex.Clear();

        foreach (RoadObject road in _roadObjDict.Values)
            MonoBehaviour.Destroy(road.gameObject);
        _roadObjDict.Clear();

        foreach (DualLaneRoadObject dualRoad in _dualLaneRoadObjDict.Values)
            MonoBehaviour.Destroy(dualRoad.gameObject);
        _dualLaneRoadObjDict.Clear();

        _occupiedAsObjectDict.Clear();

        if (_mainRunner.TileGridHandler != null)
        {
            foreach (BuildingTile tile in _mainRunner.TileGridHandler.TileDict.Values)
                tile.SetOccupied(false);
        }

        if (_mainRunner.RawBuildingHandler != null)
            _mainRunner.RawBuildingHandler.ClearAllBuildings();
    }

    public List<PlacedBuildingSaveData> ExportPlacedBuildings()
    {
        List<PlacedBuildingSaveData> list = new List<PlacedBuildingSaveData>();

        foreach (IBuilding building in GetAllBuildings())
        {
            if (building.IsRemovalAnimating) continue;
            list.Add(building.ExportSaveData());
        }

        return list;
    }

    public List<PlacedRoadSaveData> ExportPlacedRoads()
    {
        List<PlacedRoadSaveData> list = new List<PlacedRoadSaveData>(_roadObjDict.Count + _dualLaneRoadObjDict.Count);

        foreach (RoadObject road in _roadObjDict.Values)
            list.Add(road.ExportSaveData());
        foreach (DualLaneRoadObject dualRoad in _dualLaneRoadObjDict.Values)
            list.Add(dualRoad.ExportSaveData());

        return list;
    }

    public List<PlacedBuildingSaveData> ExportBuildingsIntersectingGridRect(Vector2Int cellMin, Vector2Int cellMax)
    {
        ClampRectToGrid(ref cellMin, ref cellMax);

        List<PlacedBuildingSaveData> list = new List<PlacedBuildingSaveData>();

        foreach (BuildingObject building in _buildingObjDict.Values)
        {
            if (building.IsRemovalAnimating) continue;

            Vector2Int o = building.Origin;
            Vector2Int s = building.Size;
            if (o.x <= cellMax.x && o.x + s.x - 1 >= cellMin.x &&
                o.y <= cellMax.y && o.y + s.y - 1 >= cellMin.y)
                list.Add(building.ExportSaveData());
        }

        return list;
    }

    public List<PlacedRoadSaveData> ExportRoadsIntersectingGridRect(Vector2Int cellMin, Vector2Int cellMax)
    {
        ClampRectToGrid(ref cellMin, ref cellMax);

        List<PlacedRoadSaveData> list = new List<PlacedRoadSaveData>();

        foreach (RoadObject road in _roadObjDict.Values)
        {
            Vector2Int p = road.GridPosition;
            if (p.x >= cellMin.x && p.x <= cellMax.x && p.y >= cellMin.y && p.y <= cellMax.y)
                list.Add(road.ExportSaveData());
        }

        foreach (DualLaneRoadObject dualRoad in _dualLaneRoadObjDict.Values)
        {
            Vector2Int p = dualRoad.GridPosition;
            if (p.x >= cellMin.x && p.x <= cellMax.x && p.y >= cellMin.y && p.y <= cellMax.y)
                list.Add(dualRoad.ExportSaveData());
        }

        return list;
    }

    public int UnassignEmployeesFromLastBuildings(EmployeeType type, int count)
    {
        if (count <= 0)
            return 0;

        int removed = 0;
        for (int i = _buildingParent.childCount - 1; i >= 0 && removed < count; i--)
        {
            if (!_buildingParent.GetChild(i).TryGetComponent(out IBuilding building) ||
                building.IsRemovalAnimating)
                continue;

            int assigned = type == EmployeeType.Worker
                ? building.AssignedWorkers
                : building.AssignedTechnicians;
            if (assigned <= 0)
                continue;

            int toRemove = Mathf.Min(count - removed, assigned);
            if (building.TryApplyEmployeeDelta(type, -toRemove))
            {
                removed += toRemove;
                continue;
            }

            int forcedRemoved = building.ForceRemoveAssignedEmployees(type, toRemove);
            if (forcedRemoved > 0)
            {
                _dataManager.Employee.UnassignUpTo(type, forcedRemoved);
                removed += forcedRemoved;
            }
        }

        if (removed > 0)
        {
            _mainRunner.FlushPlacedLayoutToDataManager();
            OnBuildingInstanceLayoutChanged?.Invoke();
        }

        return removed;
    }

    public int GetTotalAssignedEmployeeCount(EmployeeType type)
    {
        int total = 0;
        foreach (IBuilding building in GetAllBuildings())
        {
            if (building.IsRemovalAnimating) continue;
            total += type == EmployeeType.Worker
                ? building.AssignedWorkers
                : building.AssignedTechnicians;
        }

        if (_mainRunner.RawBuildingHandler != null)
        {
            total += _mainRunner.RawBuildingHandler.GetTotalAssignedEmployeeCount(type);
        }

        return total;
    }

    public void RestoreFromSave(List<PlacedBuildingSaveData> buildings, List<PlacedRoadSaveData> roads)
    {
        ClearAllBuildings();

        if (roads != null)
        {
            for (int i = 0; i < roads.Count; i++)
                TryPlaceRoadFromSave(roads[i]);
        }

        if (buildings != null)
        {
            for (int i = 0; i < buildings.Count; i++)
                TryPlaceBuildingFromSave(buildings[i]);
        }

        RefreshAllBuildingOutgoingResourceIcons();
        OnBuildingInstanceLayoutChanged?.Invoke();
    }

    public void TryAutoStaffAllBuildings()
    {
        _dataManager.Employee.TryEnsureRequiredManagers();

        foreach (BuildingObject building in _buildingObjDict.Values)
        {
            if (building.IsRemovalAnimating) continue;
            building.TryAutoAssignEmployeesToFill();
        }

        _dataManager.Employee.TryEnsureRequiredManagers();
    }

    public void RefreshAllBuildingOutgoingResourceIcons()
    {
        foreach (BuildingObject building in _buildingObjDict.Values)
            building.RefreshOutgoingResourceIcons();
    }

    public Vector2Int WorldToGridPosition(Vector3 worldPos)
    {
        Vector3 lp = worldPos - _tileParent.position;
        return new Vector2Int(Mathf.RoundToInt(lp.x), Mathf.RoundToInt(-lp.y));
    }

    public Vector3 GridToWorldPosition(Vector2Int gridPos, Vector2Int size)
    {
        float cx = gridPos.x + (size.x - 1) * 0.5f;
        float cy = -gridPos.y - (size.y - 1) * 0.5f;
        return _tileParent.position + new Vector3(cx, cy, BuildingZ);
    }

    public static Vector2Int GetRotatedSize(Vector2Int size, int rotation)
    {
        rotation %= 4;
        return (rotation == 1 || rotation == 3) ? new Vector2Int(size.y, size.x) : size;
    }

    public bool IsWithinBounds(Vector2Int position, Vector2Int size)
    {
        return position.x >= 0 && position.y >= 0 &&
               position.x + size.x <= _mainRunner.GridWidth &&
               position.y + size.y <= _mainRunner.GridHeight;
    }

    public bool CanPlace(Vector2Int position, Vector2Int size)
    {
        if (!IsWithinBounds(position, size)) return false;

        for (int dx = 0; dx < size.x; dx++)
        {
            for (int dy = 0; dy < size.y; dy++)
            {
                if (_occupiedAsObjectDict.ContainsKey(new Vector2Int(position.x + dx, position.y + dy)))
                    return false;
            }
        }
        return true;
    }

    public virtual int CountPlacedBuildingsWithId(string buildingDataId)
    {
        if (string.IsNullOrEmpty(buildingDataId))
            return 0;

        BuildingData data = _dataManager.Building.GetBuildingData(buildingDataId);
        if (data is RawMaterialFactoryData && !AllowRawBuildingPlacement)
        {
            if (_mainRunner.RawBuildingHandler != null)
                return _mainRunner.RawBuildingHandler.CountPlacedBuildingsWithId(buildingDataId);
            return 0;
        }

        int count = 0;
        foreach (BuildingObject building in _buildingObjDict.Values)
        {
            if (building.BuildingData.id == buildingDataId)
                count++;
        }
        return count;
    }

    public int CountPlacedLayoutEntries(string buildingDataId)
    {
        if (string.IsNullOrEmpty(buildingDataId))
            return 0;

        BuildingData template = _dataManager.Building.GetBuildingData(buildingDataId);
        if (template != null && template.IsRoad)
            return _roadObjDict.Count + _dualLaneRoadObjDict.Count;

        return CountPlacedBuildingsWithId(buildingDataId);
    }

    public bool AnyPlacedBuildingHasConfiguredOutputResource(string buildingDataId)
    {
        if (string.IsNullOrEmpty(buildingDataId))
            return false;

        foreach (BuildingObject building in _buildingObjDict.Values)
        {
            if (building.BuildingData.id == buildingDataId && building.HasConfiguredOutputResource)
                return true;
        }

        if (_mainRunner.RawBuildingHandler != null && _mainRunner.RawBuildingHandler.AnyPlacedBuildingHasConfiguredOutputResource(buildingDataId))
            return true;

        return false;
    }

    public bool IsCellOccupied(Vector2Int position) => _occupiedAsObjectDict.ContainsKey(position);

    public bool TryGetPlacedGameObjectAt(Vector2Int gridPosition, out GameObject placedObject)
    {
        placedObject = null;
        if (!_occupiedAsObjectDict.TryGetValue(gridPosition, out string key))
            return false;

        if (_roadObjDict.TryGetValue(key, out RoadObject road))
        {
            placedObject = road.gameObject;
            return true;
        }

        if (_dualLaneRoadObjDict.TryGetValue(key, out DualLaneRoadObject dualLaneRoad))
        {
            placedObject = dualLaneRoad.gameObject;
            return true;
        }

        if (_buildingObjDict.TryGetValue(key, out BuildingObject building))
        {
            placedObject = building.gameObject;
            return true;
        }

        return false;
    }

    public bool TryGetRoadAt(Vector2Int gridPosition, out RoadObject road)
    {
        road = null;
        if (!_occupiedAsObjectDict.TryGetValue(gridPosition, out string key))
            return false;

        if (_roadObjDict.TryGetValue(key, out RoadObject foundRoad))
        {
            road = foundRoad;
            return true;
        }

        return false;
    }

    public virtual bool CanPlaceMoreInstances(BuildingData data)
    {
        if (data == null || !data.usePlacedCountLimit)
            return data != null;

        int max = _dataManager.Building.GetMaxPlacedCount(data);
        return max > 0 && CountPlacedBuildingsWithId(data.id) < max;
    }

    public bool TryPlaceRoad(BuildingData roadData, Vector2Int position, int rotation, out GameObject placed, out bool insufficientCredits)
    {
        placed = null;
        insufficientCredits = false;

        if (roadData != null && roadData.buildCost > 0 &&
            _dataManager.Finances.Credit < roadData.buildCost)
        {
            insufficientCredits = true;
            return false;
        }

        if (!TryInstantiateRoad(position, rotation, roadData, saveData: null, animatePlacement: true, out placed))
            return false;

        ApplyPlacementFinances(roadData);
        OnBuildingInstanceLayoutChanged?.Invoke();
        return true;
    }

    public bool TryPlaceBuildingFromBlueprintSave(PlacedBuildingSaveData saveData, bool autoEmployeeAssignment, out bool insufficientCredits)
    {
        insufficientCredits = false;
        if (saveData == null || string.IsNullOrEmpty(saveData.buildingDataId))
            return false;

        BuildingData data = _dataManager.Building.GetBuildingData(saveData.buildingDataId);
        if (data == null || data.buildingSprite == null || !CanPlaceMoreInstances(data))
            return false;

        if (data.buildCost > 0 && _dataManager.Finances.Credit < data.buildCost)
        {
            insufficientCredits = true;
            return false;
        }

        Vector2Int origin = new Vector2Int(saveData.originX, saveData.originY);
        int rotation = saveData.rotation;
        Vector2Int rotatedSize = GetRotatedSize(data.size, rotation);
        if (!CanPlace(origin, rotatedSize))
            return false;

        BuildingObject building = CreateBuildingObject(data, origin, rotatedSize, rotation, out Vector3 targetLocalScale);
        saveData.assignedWorkers = 0;
        saveData.assignedTechnicians = 0;
        building.ImportSaveData(saveData, _dataManager);

        if (autoEmployeeAssignment)
            _dataManager.TryAutoStaffBuilding(building);

        RegisterPlacedBuilding(building, data, origin, rotatedSize);
        building.PlayPlaceEntranceAnimation(targetLocalScale);
        GameManager.Instance.MainCameraController.ShakeForConstruction();
        ApplyPlacementFinances(data);
        OnBuildingInstanceLayoutChanged?.Invoke();
        return true;
    }

    public virtual bool TryPlaceBuilding(BuildingData data, Vector2Int position, int rotation, out GameObject placed, out bool insufficientCredits)
    {
        placed = null;
        insufficientCredits = false;
        if (data == null || (data is RawMaterialFactoryData && !AllowRawBuildingPlacement) || data.buildingSprite == null || !CanPlaceMoreInstances(data))
            return false;

        if (data.buildCost > 0 && _dataManager.Finances.Credit < data.buildCost)
        {
            insufficientCredits = true;
            return false;
        }

        Vector2Int rotatedSize = GetRotatedSize(data.size, rotation);
        if (!CanPlace(position, rotatedSize))
            return false;

        BuildingObject building = CreateBuildingObject(
            data, position, rotatedSize, rotation, out Vector3 targetLocalScale,
            _mainRunner.PlacementHandler.IsAutoEmployeePlacement);

        RegisterPlacedBuilding(building, data, position, rotatedSize);
        building.PlayPlaceEntranceAnimation(targetLocalScale);
        GameManager.Instance.MainCameraController.ShakeForConstruction();
        ApplyPlacementFinances(data);

        placed = building.gameObject;
        OnBuildingInstanceLayoutChanged?.Invoke();
        return true;
    }

    public bool TryRemoveAt(Vector2Int anyOccupiedCell)
    {
        if (!_occupiedAsObjectDict.TryGetValue(anyOccupiedCell, out string key))
            return false;

        if (_roadObjDict.TryGetValue(key, out RoadObject road))
            return RemoveRoadInstance(road.GridPosition, key, road.SourceBuildingData, road.gameObject, _roadObjDict);

        if (_dualLaneRoadObjDict.TryGetValue(key, out DualLaneRoadObject dualRoad))
            return RemoveRoadInstance(dualRoad.GridPosition, key, dualRoad.SourceBuildingData, dualRoad.gameObject, _dualLaneRoadObjDict);

        if (!_buildingObjDict.TryGetValue(key, out BuildingObject building) || building.IsRemovalAnimating)
            return false;

        Vector2Int origin = building.Origin;
        Vector2Int size = building.Size;
        _mainRunner.PlayBuildingRemovalFeedback();
        building.PlayRemovalAnimation(() =>
        {
            building.ReleaseAssignedEmployees();
            _dataManager.Finances.UnregisterPlacedBuildingMaintenance(building.BuildingData);
            UnregisterOccupancy(origin, size);
            _buildingObjDict.Remove(key);
            _buildingOutputRoundRobinIndex.Remove(key);
            MonoBehaviour.Destroy(building.gameObject);
            OnBuildingInstanceLayoutChanged?.Invoke();
        });
        return true;
    }

    public bool TryGetResourceNodeAtCell(Vector2Int pos, out IResourceNode node)
    {
        node = null;
        if (!_occupiedAsObjectDict.TryGetValue(pos, out string key))
            return false;

        if (_roadObjDict.TryGetValue(key, out RoadObject road))
        {
            node = road;
            return true;
        }
        if (_dualLaneRoadObjDict.TryGetValue(key, out DualLaneRoadObject dualRoad))
        {
            node = dualRoad;
            return true;
        }
        if (_buildingObjDict.TryGetValue(key, out BuildingObject building))
        {
            node = building;
            return true;
        }
        return false;
    }

    private BuildingObject CreateBuildingObject(
        BuildingData data,
        Vector2Int origin,
        Vector2Int rotatedSize,
        int rotation,
        out Vector3 targetLocalScale,
        bool autoEmployeeAssignment = false,
        bool zeroScaleForEntranceAnimation = true)
    {
        GameObject obj = MonoBehaviour.Instantiate(_mainRunner.BuildingObjectPrefab, _buildingParent);
        obj.name = $"Building_{data.id}_{origin.x}_{origin.y}";
        obj.transform.position = GridToWorldPosition(origin, rotatedSize);

        BuildingObject building = obj.GetComponent<BuildingObject>();
        targetLocalScale = obj.transform.localScale;
        if (zeroScaleForEntranceAnimation)
            obj.transform.localScale = Vector3.zero;

        building.Init(_mainRunner, data, origin, rotatedSize, rotation, autoEmployeeAssignment);
        return building;
    }

    private void RegisterPlacedBuilding(BuildingObject building, BuildingData data, Vector2Int origin, Vector2Int rotatedSize)
    {
        string key = BuildingGridKey(origin);
        RegisterBuildingOccupancy(origin, rotatedSize, key);
        _buildingObjDict[key] = building;
        _buildingOutputRoundRobinIndex[key] = 0;
    }

    private bool TryInstantiateRoad(
        Vector2Int position,
        int rotation,
        BuildingData sourceBuildingData,
        PlacedRoadSaveData saveData,
        bool animatePlacement,
        out GameObject placed)
    {
        placed = null;

        if (!IsWithinBounds(position, Vector2Int.one) || _occupiedAsObjectDict.ContainsKey(position))
            return false;

        bool isDualLaneRoad = IsDualLaneRoadData(sourceBuildingData);
        GameObject roadPrefab = isDualLaneRoad ? _mainRunner.DualLaneRoadObjectPrefab : _mainRunner.RoadObjectPrefab;
        if (roadPrefab == null)
            return false;

        GameObject obj = MonoBehaviour.Instantiate(roadPrefab, _roadParent);
        obj.name = $"Road_{position.x}_{position.y}";
        obj.transform.position = GridToWorldPosition(position, Vector2Int.one);

        if (animatePlacement)
        {
            Vector3 targetScale = obj.transform.localScale;
            obj.transform.localScale = Vector3.zero;
            obj.transform.DOScale(targetScale, 0.18f).SetEase(Ease.OutBack).SetUpdate(true).SetLink(obj);
        }

        string key = RoadGridKey(position);
        if (isDualLaneRoad)
        {
            DualLaneRoadObject dualRoadObject = obj.GetComponent<DualLaneRoadObject>();
            if (dualRoadObject == null)
            {
                MonoBehaviour.Destroy(obj);
                return false;
            }

            dualRoadObject.Init(position, rotation, sourceBuildingData);
            if (saveData != null)
                dualRoadObject.ImportSaveData(saveData);
            _dualLaneRoadObjDict[key] = dualRoadObject;
        }
        else
        {
            RoadObject roadObject = obj.GetComponent<RoadObject>();
            if (roadObject == null)
            {
                MonoBehaviour.Destroy(obj);
                return false;
            }

            roadObject.Init(position, rotation, sourceBuildingData);
            if (saveData != null)
                roadObject.ImportSaveData(saveData);
            _roadObjDict[key] = roadObject;
        }

        _occupiedAsObjectDict[position] = key;

        if (_mainRunner.TileGridHandler != null)
        {
            if (_mainRunner.TileGridHandler.TileDict.TryGetValue(position, out BuildingTile tile))
                tile.SetOccupied(true);
        }

        placed = obj;
        return true;
    }

    private bool RemoveRoadInstance<T>(Vector2Int pos, string key, BuildingData sourceData, GameObject instance, Dictionary<string, T> registry)
    {
        _occupiedAsObjectDict.Remove(pos);
        registry.Remove(key);

        if (_mainRunner.TileGridHandler != null)
        {
            if (_mainRunner.TileGridHandler.TileDict.TryGetValue(pos, out BuildingTile tile))
                tile.SetOccupied(false);
        }

        if (sourceData != null)
            _dataManager.Finances.UnregisterPlacedBuildingMaintenance(sourceData);

        MonoBehaviour.Destroy(instance);
        OnBuildingInstanceLayoutChanged?.Invoke();
        return true;
    }

    private void ApplyPlacementFinances(BuildingData data)
    {
        if (data.buildCost != 0)
            _dataManager.Finances.ModifyCredit(-data.buildCost);
        _dataManager.Finances.RegisterPlacedBuildingMaintenance(data);
    }

    private static string BuildingGridKey(Vector2Int origin) => $"b:{origin.x}_{origin.y}";
    private static string RoadGridKey(Vector2Int pos) => $"r:{pos.x}_{pos.y}";

    private static bool IsDualLaneRoadData(BuildingData roadData) =>
        roadData != null && (roadData.id == "splitter" || roadData.id == "tunnel");

    private void RegisterBuildingOccupancy(Vector2Int origin, Vector2Int size, string instanceKey)
    {
        for (int sizeX = 0; sizeX < size.x; sizeX++)
        {
            for (int sizeY = 0; sizeY < size.y; sizeY++)
            {
                Vector2Int pos = new Vector2Int(origin.x + sizeX, origin.y + sizeY);
                _occupiedAsObjectDict[pos] = instanceKey;

                if (_mainRunner.TileGridHandler != null)
                {
                    if (_mainRunner.TileGridHandler.TileDict.TryGetValue(pos, out BuildingTile tile))
                        tile.SetOccupied(true);
                }
            }
        }
    }

    private void UnregisterOccupancy(Vector2Int position, Vector2Int size)
    {
        for (int sizeX = 0; sizeX < size.x; sizeX++)
        {
            for (int sizeY = 0; sizeY < size.y; sizeY++)
            {
                Vector2Int pos = new Vector2Int(position.x + sizeX, position.y + sizeY);
                _occupiedAsObjectDict.Remove(pos);

                if (_mainRunner.TileGridHandler != null)
                {
                    if (_mainRunner.TileGridHandler.TileDict.TryGetValue(pos, out BuildingTile tile))
                        tile.SetOccupied(false);
                }
            }
        }
    }

    private void ClampRectToGrid(ref Vector2Int cellMin, ref Vector2Int cellMax)
    {
        int gx0 = Mathf.Clamp(Mathf.Min(cellMin.x, cellMax.x), 0, _mainRunner.GridWidth - 1);
        int gy0 = Mathf.Clamp(Mathf.Min(cellMin.y, cellMax.y), 0, _mainRunner.GridHeight - 1);
        int gx1 = Mathf.Clamp(Mathf.Max(cellMin.x, cellMax.x), 0, _mainRunner.GridWidth - 1);
        int gy1 = Mathf.Clamp(Mathf.Max(cellMin.y, cellMax.y), 0, _mainRunner.GridHeight - 1);
        cellMin = new Vector2Int(gx0, gy0);
        cellMax = new Vector2Int(gx1, gy1);
    }

    private bool TryPlaceBuildingFromSave(PlacedBuildingSaveData saveData)
    {
        if (saveData == null || string.IsNullOrEmpty(saveData.buildingDataId))
            return false;

        BuildingData data = _dataManager.Building.GetBuildingData(saveData.buildingDataId);
        if (data == null)
            return false;

        Vector2Int origin = new Vector2Int(saveData.originX, saveData.originY);

        if (data is RawMaterialFactoryData && !AllowRawBuildingPlacement)
        {
            return true;
        }

        Vector2Int rotatedSize = GetRotatedSize(data.size, saveData.rotation);
        if (!CanPlace(origin, rotatedSize))
            return false;

        BuildingObject building = CreateBuildingObject(
            data,
            origin,
            rotatedSize,
            saveData.rotation,
            out _,
            zeroScaleForEntranceAnimation: false);
        building.ImportSaveData(saveData, _dataManager);
        RegisterPlacedBuilding(building, data, origin, rotatedSize);
        _dataManager.Finances.RegisterPlacedBuildingMaintenance(data);
        return true;
    }

    private bool TryPlaceRoadFromSave(PlacedRoadSaveData saveData)
    {
        if (saveData == null) return false;

        Vector2Int position = new Vector2Int(saveData.x, saveData.y);
        BuildingData roadData = _dataManager.Building.GetBuildingData(
            string.IsNullOrEmpty(saveData.roadDataId) ? "road" : saveData.roadDataId);

        BuildingData sourceBuildingData = roadData;
        if (!string.IsNullOrEmpty(saveData.sourceBuildingDataId))
            sourceBuildingData = _dataManager.Building.GetBuildingData(saveData.sourceBuildingDataId);

        if (!TryInstantiateRoad(position, saveData.rotation, sourceBuildingData, saveData, animatePlacement: false, out _))
            return false;

        if (roadData != null)
            _dataManager.Finances.RegisterPlacedBuildingMaintenance(roadData);

        return true;
    }

    private Transform CreateChildTransform(Transform parent, string name)
    {
        Transform t = parent.Find(name);
        if (t == null)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            t = go.transform;
        }
        return t;
    }
}
