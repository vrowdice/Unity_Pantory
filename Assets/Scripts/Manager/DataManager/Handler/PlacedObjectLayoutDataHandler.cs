using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 로드된 세이브 데이터의 배치(건물/도로) 정보를 런너가 소비할 수 있게 보관합니다.
/// DataManager는 DontDestroyOnLoad이므로 씬 전환 후에도 유지됩니다.
/// </summary>
public class PlacedObjectLayoutDataHandler
{
    private readonly DataManager _dataManager;
    private List<PlacedBuildingSaveData> _placedBuildings = new List<PlacedBuildingSaveData>();
    private List<PlacedRoadSaveData> _placedRoads = new List<PlacedRoadSaveData>();

    public PlacedObjectLayoutDataHandler(DataManager dataManager)
    {
        _dataManager = dataManager;
    }

    public void SetFromSave(List<PlacedBuildingSaveData> placedBuildings, List<PlacedRoadSaveData> placedRoads)
    {
        _placedBuildings = placedBuildings ?? new List<PlacedBuildingSaveData>();
        _placedRoads = placedRoads ?? new List<PlacedRoadSaveData>();
    }

    public void Consume(out List<PlacedBuildingSaveData> placedBuildings, out List<PlacedRoadSaveData> placedRoads)
    {
        placedBuildings = _placedBuildings ?? new List<PlacedBuildingSaveData>();
        placedRoads = _placedRoads ?? new List<PlacedRoadSaveData>();

        _placedBuildings = new List<PlacedBuildingSaveData>();
        _placedRoads = new List<PlacedRoadSaveData>();
    }

    public void CopyLayoutSnapshotTo(GameSaveData saveData)
    {
        if (saveData == null)
            return;

        saveData.placedBuildings = CloneBuildingList(_placedBuildings);
        saveData.placedRoads = CloneRoadList(_placedRoads);
    }

    private static List<PlacedBuildingSaveData> CloneBuildingList(List<PlacedBuildingSaveData> source)
    {
        List<PlacedBuildingSaveData> copy = new List<PlacedBuildingSaveData>();
        if (source == null)
            return copy;

        for (int i = 0; i < source.Count; i++)
        {
            PlacedBuildingSaveData item = source[i];
            if (item != null)
                copy.Add(JsonUtility.FromJson<PlacedBuildingSaveData>(JsonUtility.ToJson(item)));
        }

        return copy;
    }

    private static List<PlacedRoadSaveData> CloneRoadList(List<PlacedRoadSaveData> source)
    {
        List<PlacedRoadSaveData> copy = new List<PlacedRoadSaveData>();
        if (source == null)
            return copy;

        for (int i = 0; i < source.Count; i++)
        {
            PlacedRoadSaveData item = source[i];
            if (item != null)
                copy.Add(JsonUtility.FromJson<PlacedRoadSaveData>(JsonUtility.ToJson(item)));
        }

        return copy;
    }

    /// <summary>
    /// 배치 목록의 마지막 건물부터 직원 할당을 해제합니다.
    /// </summary>
    public int UnassignEmployeesFromLastBuildings(EmployeeType type, int count)
    {
        if (count <= 0 || _placedBuildings == null || _placedBuildings.Count == 0)
            return 0;

        int removed = 0;
        for (int i = _placedBuildings.Count - 1; i >= 0 && removed < count; i--)
        {
            PlacedBuildingSaveData saveData = _placedBuildings[i];
            if (saveData == null)
                continue;

            int assigned = type == EmployeeType.Worker
                ? saveData.assignedWorkers
                : saveData.assignedTechnicians;
            if (assigned <= 0)
                continue;

            int toRemove = System.Math.Min(count - removed, assigned);
            if (type == EmployeeType.Worker)
                saveData.assignedWorkers -= toRemove;
            else
                saveData.assignedTechnicians -= toRemove;

            removed += toRemove;
        }

        return removed;
    }

    public int GetTotalAssignedEmployeeCount(EmployeeType type)
    {
        if (_placedBuildings == null || _placedBuildings.Count == 0)
            return 0;

        int total = 0;
        for (int i = 0; i < _placedBuildings.Count; i++)
        {
            PlacedBuildingSaveData saveData = _placedBuildings[i];
            if (saveData == null)
                continue;

            total += type == EmployeeType.Worker
                ? saveData.assignedWorkers
                : saveData.assignedTechnicians;
        }

        return total;
    }

    public int CountPlacedBuildings(string buildingDataId)
    {
        if (string.IsNullOrEmpty(buildingDataId) || _placedBuildings == null)
            return 0;

        BuildingData template = _dataManager?.Building?.GetBuildingData(buildingDataId);
        bool isRawRepresentative = template is RawMaterialFactoryData;

        int count = 0;
        for (int i = 0; i < _placedBuildings.Count; i++)
        {
            PlacedBuildingSaveData saveData = _placedBuildings[i];
            if (saveData == null || saveData.buildingDataId != buildingDataId)
                continue;

            count += isRawRepresentative ? Mathf.Max(0, saveData.rawMaterialScale) : 1;
        }

        return count;
    }

    public bool AnyPlacedBuildingHasConfiguredOutputResource(string buildingDataId)
    {
        if (string.IsNullOrEmpty(buildingDataId) || _placedBuildings == null)
            return false;

        for (int i = 0; i < _placedBuildings.Count; i++)
        {
            PlacedBuildingSaveData saveData = _placedBuildings[i];
            if (saveData != null
                && saveData.buildingDataId == buildingDataId
                && !string.IsNullOrEmpty(saveData.selectedResourceId))
            {
                return true;
            }
        }

        return false;
    }
}
