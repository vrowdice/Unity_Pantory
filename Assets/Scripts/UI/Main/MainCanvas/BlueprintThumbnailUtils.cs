using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 저장 블루프린트 버튼 썸네일용 대표 스프라이트 선택.
/// </summary>
public static class BlueprintThumbnailUtils
{
    public static Sprite ResolveDominantSprite(List<PlacedBuildingSaveData> buildings, List<PlacedRoadSaveData> roads)
    {
        DataManager dataManager = DataManager.Instance;
        if (dataManager == null)
            return null;

        string dominantBuildingId = GetDominantBuildingId(buildings);
        if (!string.IsNullOrEmpty(dominantBuildingId))
        {
            BuildingData data = dataManager.Building.GetBuildingData(dominantBuildingId);
            Sprite sprite = GetUIButtonSprite(data);
            if (sprite != null)
                return sprite;
        }

        if (roads != null && roads.Count > 0)
        {
            for (int i = 0; i < roads.Count; i++)
            {
                PlacedRoadSaveData road = roads[i];
                if (road == null)
                    continue;

                if (string.IsNullOrEmpty(road.roadDataId))
                    continue;

                BuildingData roadData = dataManager.Building.GetBuildingData(road.roadDataId);
                Sprite sprite = GetUIButtonSprite(roadData);
                if (sprite != null)
                    return sprite;
            }
        }

        return null;
    }

    private static string GetDominantBuildingId(List<PlacedBuildingSaveData> buildings)
    {
        if (buildings == null || buildings.Count == 0)
            return null;

        Dictionary<string, int> counts = new Dictionary<string, int>();
        string dominantId = null;
        int dominantCount = 0;

        for (int i = 0; i < buildings.Count; i++)
        {
            PlacedBuildingSaveData saveData = buildings[i];
            if (saveData == null || string.IsNullOrEmpty(saveData.buildingDataId))
                continue;

            string id = saveData.buildingDataId;
            if (!counts.TryGetValue(id, out int count))
                count = 0;

            count++;
            counts[id] = count;

            if (count > dominantCount)
            {
                dominantCount = count;
                dominantId = id;
            }
        }

        return dominantId;
    }

    private static Sprite GetUIButtonSprite(BuildingData data)
    {
        if (data == null)
            return null;

        if (data.icon != null)
            return data.icon;

        return data.buildingSprite;
    }
}
