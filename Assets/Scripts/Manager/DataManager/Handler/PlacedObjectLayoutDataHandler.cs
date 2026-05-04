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
}

