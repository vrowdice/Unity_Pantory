using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 블루프린트(건물 배치 프리셋) 목록을 런타임에 보관하고 세이브 데이터와 변환합니다.
/// </summary>
public class BlueprintLayoutDataHandler
{
    private readonly List<BlueprintLayoutSaveData> _blueprintLayouts = new List<BlueprintLayoutSaveData>();

    public IReadOnlyList<BlueprintLayoutSaveData> GetAll()
    {
        return _blueprintLayouts;
    }

    public void Clear()
    {
        _blueprintLayouts.Clear();
    }

    public void Add(string blueprintName, List<PlacedBuildingSaveData> buildings, List<PlacedRoadSaveData> roads)
    {
        bool hasBuildings = buildings != null && buildings.Count > 0;
        bool hasRoads = roads != null && roads.Count > 0;
        if (!hasBuildings && !hasRoads)
            return;

        BlueprintLayoutSaveData layout = new BlueprintLayoutSaveData();
        layout.blueprintName = blueprintName;
        layout.buildings = ClonePlacedBuildingList(buildings);
        layout.roads = ClonePlacedRoadList(roads);
        _blueprintLayouts.Add(layout);
    }

    public void SetFromSave(List<BlueprintLayoutSaveData> layouts)
    {
        _blueprintLayouts.Clear();
        if (layouts == null) return;

        for (int i = 0; i < layouts.Count; i++)
        {
            BlueprintLayoutSaveData src = layouts[i];
            BlueprintLayoutSaveData dst = new BlueprintLayoutSaveData();
            dst.blueprintName = src != null ? src.blueprintName : null;
            dst.buildings = ClonePlacedBuildingList(src != null ? src.buildings : null);
            dst.roads = ClonePlacedRoadList(src != null ? src.roads : null);
            _blueprintLayouts.Add(dst);
        }
    }

    public List<BlueprintLayoutSaveData> ExportSaveData()
    {
        List<BlueprintLayoutSaveData> result = new List<BlueprintLayoutSaveData>(_blueprintLayouts.Count);
        for (int i = 0; i < _blueprintLayouts.Count; i++)
        {
            BlueprintLayoutSaveData src = _blueprintLayouts[i];
            BlueprintLayoutSaveData dst = new BlueprintLayoutSaveData();
            dst.blueprintName = src != null ? src.blueprintName : null;
            dst.buildings = ClonePlacedBuildingList(src != null ? src.buildings : null);
            dst.roads = ClonePlacedRoadList(src != null ? src.roads : null);
            result.Add(dst);
        }

        return result;
    }

    public bool RemoveAt(int index)
    {
        if (index < 0 || index >= _blueprintLayouts.Count)
            return false;

        _blueprintLayouts.RemoveAt(index);
        return true;
    }

    private static List<PlacedBuildingSaveData> ClonePlacedBuildingList(List<PlacedBuildingSaveData> list)
    {
        List<PlacedBuildingSaveData> clone = new List<PlacedBuildingSaveData>();
        if (list == null) return clone;

        for (int i = 0; i < list.Count; i++)
        {
            PlacedBuildingSaveData src = list[i];
            if (src == null)
                continue;

            PlacedBuildingSaveData dst = new PlacedBuildingSaveData();
            dst.buildingDataId = src.buildingDataId;
            dst.originX = src.originX;
            dst.originY = src.originY;
            dst.rotation = src.rotation;
            dst.selectedResourceId = src.selectedResourceId;
            dst.workProgress = src.workProgress;
            dst.assignedWorkers = src.assignedWorkers;
            dst.assignedTechnicians = src.assignedTechnicians;
            dst.inputBuffer = ClonePacketList(src.inputBuffer);
            dst.outputBuffer = ClonePacketList(src.outputBuffer);
            clone.Add(dst);
        }

        return clone;
    }

    private static List<ResourcePacketSaveData> ClonePacketList(List<ResourcePacketSaveData> list)
    {
        List<ResourcePacketSaveData> clone = new List<ResourcePacketSaveData>();
        if (list == null) return clone;

        for (int i = 0; i < list.Count; i++)
        {
            ResourcePacketSaveData src = list[i];
            if (src == null)
                continue;

            clone.Add(new ResourcePacketSaveData(src.id, src.amount, src.direction));
        }

        return clone;
    }

    private static List<PlacedRoadSaveData> ClonePlacedRoadList(List<PlacedRoadSaveData> list)
    {
        List<PlacedRoadSaveData> clone = new List<PlacedRoadSaveData>();
        if (list == null) return clone;

        for (int i = 0; i < list.Count; i++)
        {
            PlacedRoadSaveData src = list[i];
            if (src == null)
                continue;

            PlacedRoadSaveData dst = new PlacedRoadSaveData();
            dst.x = src.x;
            dst.y = src.y;
            dst.rotation = src.rotation;
            dst.roadDataId = src.roadDataId;
            dst.sourceBuildingDataId = src.sourceBuildingDataId;
            dst.buffer = ClonePacketList(src.buffer);
            clone.Add(dst);
        }

        return clone;
    }
}
