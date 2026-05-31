using System;
using System.Collections.Generic;
using UnityEngine;

public class MainRawBuildingHandler
{
    private readonly MainRunner _mainRunner;
    private readonly Transform _buildingParent;
    private readonly DataManager _dataManager;

    private readonly Dictionary<string, RawBuildingObject> _rawResourceBuildingObjDict = new();
    private const float BuildingZ = 9f;

    public Dictionary<string, RawBuildingObject> RawResourceBuildingObjDict => _rawResourceBuildingObjDict;

    public MainRawBuildingHandler(MainRunner runner, Transform buildingParent)
    {
        _mainRunner = runner;
        _buildingParent = buildingParent;
        _dataManager = DataManager.Instance;
    }

    public void InitializeRepresentativeRawBuildings()
    {
        SyncRepresentativeRawBuildings();
    }

    public void OnResearchCompleted(string researchId)
    {
        SyncRepresentativeRawBuildings();
    }

    public void SyncRepresentativeRawBuildings()
    {
        List<(RawMaterialFactoryData rawData, ResourceData resData)> spawnList = BuildUnlockedRawBuildingSpawnList();

        List<string> staleIds = new List<string>();
        foreach (KeyValuePair<string, RawBuildingObject> pair in _rawResourceBuildingObjDict)
        {
            RawMaterialFactoryData buildingData = pair.Value != null ? pair.Value.BuildingData : null;
            if (buildingData == null || !_dataManager.Building.IsBuildingUnlocked(buildingData))
                staleIds.Add(pair.Key);
        }

        for (int i = 0; i < staleIds.Count; i++)
            DestroyRawBuildingInstance(staleIds[i]);

        for (int i = 0; i < spawnList.Count; i++)
        {
            RawMaterialFactoryData rawData = spawnList[i].rawData;
            ResourceData resData = spawnList[i].resData;

            if (_rawResourceBuildingObjDict.TryGetValue(rawData.id, out RawBuildingObject existing))
            {
                RepositionRawBuilding(existing, i);
                continue;
            }

            SpawnRawBuildingInstance(rawData, resData, i);
        }
    }

    private List<(RawMaterialFactoryData rawData, ResourceData resData)> BuildUnlockedRawBuildingSpawnList()
    {
        List<(RawMaterialFactoryData rawData, ResourceData resData)> spawnList = new();

        foreach (BuildingData data in _dataManager.Building.GetBuildingDataList(BuildingType.RawProduction))
        {
            if (data is not RawMaterialFactoryData rawData || rawData.ProducibleResources == null)
                continue;

            if (!_dataManager.Building.IsBuildingUnlocked(rawData))
                continue;

            foreach (ResourceData res in rawData.ProducibleResources)
                spawnList.Add((rawData, res));
        }

        return spawnList;
    }

    private void SpawnRawBuildingInstance(RawMaterialFactoryData rawData, ResourceData resData, int layoutIndex)
    {
        Vector3 worldPos = GetRawBuildingWorldPosition(layoutIndex);

        GameObject prefabToSpawn = _mainRunner.RawBuildingObjectPrefab != null
            ? _mainRunner.RawBuildingObjectPrefab
            : _mainRunner.BuildingObjectPrefab;

        GameObject obj = MonoBehaviour.Instantiate(prefabToSpawn, _buildingParent);
        obj.name = $"RawBuilding_{rawData.id}_{resData.id}";
        obj.transform.position = worldPos;
        obj.transform.localScale = Vector3.one;

        RawBuildingObject rawObj = obj.GetComponent<RawBuildingObject>();
        if (rawObj == null)
        {
            BuildingObject oldComp = obj.GetComponent<BuildingObject>();
            if (oldComp != null)
                MonoBehaviour.DestroyImmediate(oldComp);

            rawObj = obj.AddComponent<RawBuildingObject>();
        }

        Vector2Int dummyOrigin = new Vector2Int(Mathf.RoundToInt(worldPos.x), 0);
        rawObj.Init(rawData, dummyOrigin, _mainRunner);
        rawObj.TrySetSelectedResource(resData);

        UIManager.Instance?.ShowRawBuildingInfoPanel(rawObj);
        _rawResourceBuildingObjDict[rawData.id] = rawObj;
    }

    private void RepositionRawBuilding(RawBuildingObject rawBuilding, int layoutIndex)
    {
        if (rawBuilding == null)
            return;

        rawBuilding.transform.position = GetRawBuildingWorldPosition(layoutIndex);
    }

    private Vector3 GetRawBuildingWorldPosition(int layoutIndex)
    {
        float x = _mainRunner.RawBuildingsStartX + (layoutIndex * _mainRunner.RawBuildingsSpacingX);
        return new Vector3(x, _mainRunner.RawBuildingsSpawnY, BuildingZ);
    }

    private void DestroyRawBuildingInstance(string buildingId)
    {
        if (!_rawResourceBuildingObjDict.TryGetValue(buildingId, out RawBuildingObject rawBuilding))
            return;

        rawBuilding.ReleaseAssignedEmployees();
        _rawResourceBuildingObjDict.Remove(buildingId);
        MonoBehaviour.Destroy(rawBuilding.gameObject);
    }

    public List<PlacedBuildingSaveData> ExportPlacedBuildings()
    {
        List<PlacedBuildingSaveData> list = new List<PlacedBuildingSaveData>();

        foreach (RawBuildingObject rawBuilding in _rawResourceBuildingObjDict.Values)
        {
            if (rawBuilding.IsRemovalAnimating) continue;
            list.Add(rawBuilding.ExportSaveData());
        }

        return list;
    }

    public void RestoreFromSave(List<PlacedBuildingSaveData> buildings)
    {
        if (buildings == null) return;

        foreach (PlacedBuildingSaveData saveData in buildings)
        {
            if (string.IsNullOrEmpty(saveData.buildingDataId)) continue;
            BuildingData data = _dataManager.Building.GetBuildingData(saveData.buildingDataId);
            if (data is not RawMaterialFactoryData) continue;

            if (_rawResourceBuildingObjDict.TryGetValue(saveData.buildingDataId, out RawBuildingObject rawBuilding))
            {
                rawBuilding.ImportSaveData(saveData, _dataManager);

                long maintenance = data.maintenanceCost * rawBuilding.RawMaterialCount;
                long assetValue = data.buildCost * rawBuilding.RawMaterialCount;
                _dataManager.Finances.ModifyPlacedBuildingMaintenance(maintenance, assetValue);
            }
        }
    }

    public void ClearAllBuildings()
    {
        foreach (RawBuildingObject rawBuilding in _rawResourceBuildingObjDict.Values)
        {
            if (rawBuilding != null)
            {
                rawBuilding.ReleaseAssignedEmployees();
                MonoBehaviour.Destroy(rawBuilding.gameObject);
            }
        }
        _rawResourceBuildingObjDict.Clear();
    }

    public int GetTotalAssignedEmployeeCount(EmployeeType type)
    {
        int total = 0;
        foreach (RawBuildingObject rawBuilding in _rawResourceBuildingObjDict.Values)
        {
            if (rawBuilding.IsRemovalAnimating) continue;
            total += type == EmployeeType.Worker
                ? rawBuilding.AssignedWorkers
                : rawBuilding.AssignedTechnicians;
        }
        return total;
    }

    public int CountPlacedBuildingsWithId(string buildingDataId)
    {
        foreach (RawBuildingObject rawBuilding in _rawResourceBuildingObjDict.Values)
        {
            if (rawBuilding.BuildingData.id == buildingDataId)
                return rawBuilding.RawMaterialCount;
        }
        return 0;
    }

    public bool AnyPlacedBuildingHasConfiguredOutputResource(string buildingDataId)
    {
        foreach (RawBuildingObject rawBuilding in _rawResourceBuildingObjDict.Values)
        {
            if (rawBuilding.BuildingData.id == buildingDataId && rawBuilding.HasConfiguredOutputResource)
                return true;
        }
        return false;
    }

    public void TickRawBuildingsSimulation()
    {
        foreach (RawBuildingObject rawBuilding in _rawResourceBuildingObjDict.Values)
        {
            if (rawBuilding != null)
                rawBuilding.TickSimulation(_dataManager);
        }
    }
}
