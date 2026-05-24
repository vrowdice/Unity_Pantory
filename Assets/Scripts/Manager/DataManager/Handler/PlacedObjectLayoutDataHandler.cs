using System.Collections.Generic;

/// <summary>
/// 로드된 세이브 데이터의 배치(건물/도로) 정보를 런너가 소비할 수 있게 보관합니다.
/// DataManager는 DontDestroyOnLoad이므로 씬 전환 후에도 유지됩니다.
/// </summary>
public class PlacedObjectLayoutDataHandler
{
    private List<PlacedBuildingSaveData> _placedBuildings = new List<PlacedBuildingSaveData>();
    private List<PlacedRoadSaveData> _placedRoads = new List<PlacedRoadSaveData>();

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
}

