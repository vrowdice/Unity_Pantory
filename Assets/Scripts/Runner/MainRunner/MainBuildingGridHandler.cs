using System;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// 메인에서 사용하는 건물 그리드/점유/오브젝트 생성 핸들러.
/// </summary>
public class MainBuildingGridHandler
{
    private readonly MainRunner _mainRunner;
    private readonly DataManager _dataManager;

    private readonly Transform _tileParent;
    private readonly Transform _buildingParent;
    private readonly Transform _roadParent;
    
    private readonly Dictionary<string, BuildingObject> _rawResourceBuildingObjDict = new();
    private readonly Dictionary<string, BuildingObject> _buildingObjDict = new();
    private readonly Dictionary<string, RoadObject> _roadObjDict = new();
    private readonly Dictionary<Vector2Int, string> _occupiedAsObjectDict = new();
    private readonly Dictionary<Vector2Int, BuildingTile> _tileDict = new();

    private const float TileZ = 10f;
    private const float BuildingZ = 9f;

    public event Action OnBuildingInstanceLayoutChanged;

    public MainBuildingGridHandler(MainRunner runner)
    {
        _mainRunner = runner;
        _dataManager = DataManager.Instance;

        GameObject tileParentObj = new GameObject("Tiles");
        tileParentObj.transform.SetParent(runner.transform, worldPositionStays: false);
        _tileParent = tileParentObj.transform;

        GameObject buildingParentObj = new GameObject("Buildings");
        buildingParentObj.transform.SetParent(runner.transform, worldPositionStays: false);
        _buildingParent = buildingParentObj.transform;

        GameObject roadParentObj = new GameObject("Roads");
        roadParentObj.transform.SetParent(runner.transform, worldPositionStays: false);
        _roadParent = roadParentObj.transform;
    }

    public void CreateGrid(int width, int height)
    {
        ClearGrid();

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector2Int p = new Vector2Int(x, y);
                GameObject tileObj = MonoBehaviour.Instantiate(_mainRunner.TilePrefab, _tileParent);
                tileObj.transform.localPosition = new Vector3(x, -y, TileZ);

                if (!tileObj.TryGetComponent(out BuildingTile tile))
                {
                    tile = tileObj.AddComponent<BuildingTile>();
                }

                tile.Initialize(p);
                _tileDict[p] = tile;
            }
        }
    }

    public void ClearGrid()
    {
        ClearAllBuildings();

        foreach (BuildingTile tile in _tileDict.Values)
            MonoBehaviour.Destroy(tile.gameObject);
        _tileDict.Clear();
    }

    public void ClearAllBuildings()
    {
        _dataManager.Finances.ClearPlacedBuildingMaintenanceTotal();

        foreach (BuildingObject building in _buildingObjDict.Values)
            MonoBehaviour.Destroy(building.gameObject);
        _buildingObjDict.Clear();
        _rawResourceBuildingObjDict.Clear();

        foreach (RoadObject road in _roadObjDict.Values)
            MonoBehaviour.Destroy(road.gameObject);
        _roadObjDict.Clear();

        _occupiedAsObjectDict.Clear();

        foreach (BuildingTile tile in _tileDict.Values)
            tile.SetOccupied(false);
    }

    public List<PlacedBuildingSaveData> ExportPlacedBuildings()
    {
        List<PlacedBuildingSaveData> list = new List<PlacedBuildingSaveData>();

        foreach (BuildingObject building in _buildingObjDict.Values)
        {
            if (building.IsRemovalAnimating) continue;
            list.Add(building.ExportSaveData());
        }

        return list;
    }

    public List<PlacedRoadSaveData> ExportPlacedRoads()
    {
        List<PlacedRoadSaveData> list = new List<PlacedRoadSaveData>();

        foreach (RoadObject road in _roadObjDict.Values)
            list.Add(road.ExportSaveData());

        return list;
    }

    public void RestoreFromSave(List<PlacedBuildingSaveData> buildings, List<PlacedRoadSaveData> roads)
    {
        ClearAllBuildings();

        if (roads != null)
        {
            for (int i = 0; i < roads.Count; i++)
            {
                PlacedRoadSaveData s = roads[i];
                TryPlaceRoadFromSave(s);
            }
        }

        if (buildings != null)
        {
            for (int i = 0; i < buildings.Count; i++)
            {
                PlacedBuildingSaveData s = buildings[i];
                TryPlaceBuildingFromSave(s);
            }
        }

        OnBuildingInstanceLayoutChanged?.Invoke();
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

    public int CountPlacedBuildingsWithId(string buildingDataId)
    {
        if (string.IsNullOrEmpty(buildingDataId))
            return 0;

        BuildingData template = _dataManager.Building.GetBuildingData(buildingDataId);
        if (template is RawMaterialFactoryData)
        {
            int n = 0;
            foreach (BuildingObject building in _rawResourceBuildingObjDict.Values)
            {
                if (building.BuildingData.id == buildingDataId)
                    n++;
            }
            return n;
        }

        int m = 0;
        foreach (BuildingObject building in _buildingObjDict.Values)
        {
            if (building.BuildingData.id == buildingDataId)
                m++;
        }
        return m;
    }

    public bool CanPlaceMoreInstances(BuildingData data)
    {
        if (data == null)
            return false;
        if (!data.usePlacedCountLimit)
            return true;

        int max = _dataManager.Building.GetMaxPlacedCount(data);
        if (max <= 0)
            return false;

        return CountPlacedBuildingsWithId(data.id) < max;
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

        if (!IsWithinBounds(position, Vector2Int.one) || _occupiedAsObjectDict.ContainsKey(position)) return false;

        GameObject obj = MonoBehaviour.Instantiate(_mainRunner.RoadObjectPrefab, _roadParent);
        obj.name = $"Road_{position.x}_{position.y}";

        obj.transform.position = GridToWorldPosition(position, Vector2Int.one);
        Vector3 s = obj.transform.localScale;

        obj.transform.localScale = Vector3.zero;
        obj.transform.DOScale(s, 0.18f).SetEase(Ease.OutBack).SetUpdate(true).SetLink(obj);

        RoadObject roadObject = obj.GetComponent<RoadObject>();
        roadObject.Init(position, rotation, roadData);
        string key = RoadGridKey(position);
        _roadObjDict[key] = roadObject;
        _occupiedAsObjectDict[position] = key;
        if (_tileDict.TryGetValue(position, out BuildingTile t)) t.SetOccupied(true);

        ApplyPlacementFinances(roadData);

        placed = obj;
        return true;
    }

    private bool TryPlaceRoadFromSave(PlacedRoadSaveData saveData)
    {
        if (saveData == null) return false;

        Vector2Int position = new Vector2Int(saveData.x, saveData.y);
        int rotation = saveData.rotation;

        if (!IsWithinBounds(position, Vector2Int.one) || _occupiedAsObjectDict.ContainsKey(position)) return false;

        string roadDataId = !string.IsNullOrEmpty(saveData.roadDataId) ? saveData.roadDataId : "road";
        BuildingData roadData = _dataManager.Building.GetBuildingData(roadDataId);

        GameObject obj = MonoBehaviour.Instantiate(_mainRunner.RoadObjectPrefab, _roadParent);
        obj.name = $"Road_{position.x}_{position.y}";

        obj.transform.position = GridToWorldPosition(position, Vector2Int.one);

        RoadObject roadObject = obj.GetComponent<RoadObject>();

        BuildingData sourceBuildingData = roadData;
        if (!string.IsNullOrEmpty(saveData.sourceBuildingDataId))
            sourceBuildingData = _dataManager.Building.GetBuildingData(saveData.sourceBuildingDataId);

        roadObject.Init(position, rotation, sourceBuildingData);
        roadObject.ImportSaveData(saveData);

        string key = RoadGridKey(position);
        _roadObjDict[key] = roadObject;
        _occupiedAsObjectDict[position] = key;
        if (_tileDict.TryGetValue(position, out BuildingTile t)) t.SetOccupied(true);

        if (roadData != null)
            _dataManager.Finances.RegisterPlacedBuildingMaintenance(roadData);

        return true;
    }

    public bool TryPlaceBuilding(BuildingData data, Vector2Int position, int rotation, out GameObject placed, out bool insufficientCredits)
    {
        placed = null;
        insufficientCredits = false;
        if (data == null || data.buildingSprite == null) return false;
        if (!CanPlaceMoreInstances(data)) return false;

        if (data.buildCost > 0 && _dataManager.Finances.Credit < data.buildCost)
        {
            insufficientCredits = true;
            return false;
        }

        Vector2Int rotatedSize = GetRotatedSize(data.size, rotation);
        if (!CanPlace(position, rotatedSize)) return false;

        GameObject obj = MonoBehaviour.Instantiate(_mainRunner.BuildingObjectPrefab, _buildingParent);
        obj.name = $"Building_{data.id}_{position.x}_{position.y}";
        obj.transform.position = GridToWorldPosition(position, rotatedSize);

        BuildingObject building = obj.GetComponent<BuildingObject>();
        Vector3 targetLocalScale = obj.transform.localScale;
        obj.transform.localScale = Vector3.zero;
        building.Init(_mainRunner, data, position, rotatedSize, rotation, _mainRunner.PlacementHandler.IsAutoEmployeePlacement);

        string key = BuildingGridKey(position);
        RegisterBuildingOccupancy(position, rotatedSize, key);
        _buildingObjDict[key] = building;
        if (data is RawMaterialFactoryData)
            _rawResourceBuildingObjDict[key] = building;

        building.PlayPlaceEntranceAnimation(targetLocalScale);
        GameManager.Instance.MainCameraController.ShakeForConstruction();

        ApplyPlacementFinances(data);

        placed = obj;
        OnBuildingInstanceLayoutChanged?.Invoke();
        return true;
    }

    private bool TryPlaceBuildingFromSave(PlacedBuildingSaveData saveData)
    {
        if (saveData == null) return false;
        if (string.IsNullOrEmpty(saveData.buildingDataId)) return false;

        BuildingData data = _dataManager.Building.GetBuildingData(saveData.buildingDataId);
        if (data == null) return false;

        Vector2Int origin = new Vector2Int(saveData.originX, saveData.originY);
        int rotation = saveData.rotation;

        Vector2Int rotatedSize = GetRotatedSize(data.size, rotation);
        if (!CanPlace(origin, rotatedSize)) return false;

        GameObject obj = MonoBehaviour.Instantiate(_mainRunner.BuildingObjectPrefab, _buildingParent);
        obj.name = $"Building_{data.id}_{origin.x}_{origin.y}";
        obj.transform.position = GridToWorldPosition(origin, rotatedSize);

        BuildingObject building = obj.GetComponent<BuildingObject>();
        building.Init(_mainRunner, data, origin, rotatedSize, rotation);
        building.ImportSaveData(saveData, _dataManager);

        string key = BuildingGridKey(origin);
        RegisterBuildingOccupancy(origin, rotatedSize, key);
        _buildingObjDict[key] = building;
        if (data is RawMaterialFactoryData)
            _rawResourceBuildingObjDict[key] = building;

        ApplyPlacementFinances(data);

        return true;
    }

    private void ApplyPlacementFinances(BuildingData data)
    {
        if (data == null) return;

        if (data.buildCost != 0)
            _dataManager.Finances.ModifyCredit(-data.buildCost);
        _dataManager.Finances.RegisterPlacedBuildingMaintenance(data);
    }

    public bool TryRemoveAt(Vector2Int anyOccupiedCell)
    {
        if (!_occupiedAsObjectDict.TryGetValue(anyOccupiedCell, out string key)) return false;

        if (_roadObjDict.TryGetValue(key, out RoadObject road))
        {
            Vector2Int pos = road.GridPosition;
            _occupiedAsObjectDict.Remove(pos);
            _roadObjDict.Remove(key);
            if (_tileDict.TryGetValue(pos, out BuildingTile t)) t.SetOccupied(false);
            if (road.SourceBuildingData != null)
                _dataManager.Finances.UnregisterPlacedBuildingMaintenance(road.SourceBuildingData);
            MonoBehaviour.Destroy(road.gameObject);
            return true;
        }

        if (_buildingObjDict.TryGetValue(key, out BuildingObject building))
        {
            if (building.IsRemovalAnimating) return false;

            Vector2Int origin = building.Origin;
            Vector2Int size = building.Size;
            building.PlayRemovalAnimation(() =>
            {
                if (building.BuildingData != null)
                    _dataManager.Finances.UnregisterPlacedBuildingMaintenance(building.BuildingData);
                UnregisterOccupancy(origin, size);
                _buildingObjDict.Remove(key);
                _rawResourceBuildingObjDict.Remove(key);
                MonoBehaviour.Destroy(building.gameObject);
                OnBuildingInstanceLayoutChanged?.Invoke();
            });
            return true;
        }

        return false;
    }

    /// <summary>
    /// 시간 틱마다 건물 시뮬레이션(생산/하역 진행·적재) 후 도로/건물 간 패킷 이동.
    /// </summary>
    public void OnMainHourChanged()
    {
        foreach (BuildingObject building in _buildingObjDict.Values)
        {
            if (building.IsRemovalAnimating) continue;
            building.TickSimulation(_dataManager);
        }

        TickResourceFlow();
    }

    public void TickResourceFlow()
    {
        foreach (RoadObject road in _roadObjDict.Values)
        {
            road.ResetRoadForwardGatesForQueuedPackets();
        }

        foreach (RoadObject road in _roadObjDict.Values)
        {
            if (road.IsEmpty) continue;

            foreach (Vector2Int outCell in road.OutputGridPositions)
            {
                if (!TryGetResourceNodeAtCell(outCell, out IResourceNode destNode)) continue;
                if (ReferenceEquals(road, destNode)) continue;
                road.TryForwardTo(destNode);
                break;
            }
        }

        foreach (BuildingObject building in _buildingObjDict.Values)
        {
            if (building.IsRemovalAnimating) continue;
            foreach (Vector2Int outCell in building.OutputGridPositions)
            {
                if (!TryGetResourceNodeAtCell(outCell, out IResourceNode destNode)) continue;
                if (ReferenceEquals(building, destNode)) continue;
                building.TryForwardTo(destNode);
                break;
            }
        }
    }

    private static string BuildingGridKey(Vector2Int origin)
    {
        return $"b:{origin.x}_{origin.y}";
    }

    private static string RoadGridKey(Vector2Int pos)
    {
        return $"r:{pos.x}_{pos.y}";
    }

    private bool TryGetResourceNodeAtCell(Vector2Int pos, out IResourceNode node)
    {
        node = null;
        if (!_occupiedAsObjectDict.TryGetValue(pos, out string key)) return false;
        if (_roadObjDict.TryGetValue(key, out RoadObject road))
        {
            node = road;
            return true;
        }
        if (_buildingObjDict.TryGetValue(key, out BuildingObject building))
        {
            node = building;
            return true;
        }
        return false;
    }

    private void RegisterBuildingOccupancy(Vector2Int origin, Vector2Int size, string instanceKey)
    {
        for (int sizeX = 0; sizeX < size.x; sizeX++)
        {
            for (int sizeY = 0; sizeY < size.y; sizeY++)
            {
                Vector2Int pos = new Vector2Int(origin.x + sizeX, origin.y + sizeY);
                _occupiedAsObjectDict[pos] = instanceKey;
                if (_tileDict.TryGetValue(pos, out BuildingTile t)) t.SetOccupied(true);
            }
        }
    }

    private void UnregisterOccupancy(Vector2Int position, Vector2Int size)
    {
        for (int sizeX = 0; sizeX < size.x; sizeX++)
        {
            for (int sizey = 0; sizey < size.y; sizey++)
            {
                Vector2Int pos = new Vector2Int(position.x + sizeX, position.y + sizey);
                _occupiedAsObjectDict.Remove(pos);
                if (_tileDict.TryGetValue(pos, out BuildingTile t)) t.SetOccupied(false);
            }
        }
    }
}
