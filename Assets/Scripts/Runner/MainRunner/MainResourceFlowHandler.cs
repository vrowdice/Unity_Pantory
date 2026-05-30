using System.Collections.Generic;
using UnityEngine;

public class MainResourceFlowHandler
{
    private readonly BuildingSceneRunnerBase _mainRunner;
    private readonly Transform _buildingParent;
    private readonly DataManager _dataManager;

    private readonly Dictionary<string, RoadObject> _roadObjDict;
    private readonly Dictionary<string, DualLaneRoadObject> _dualLaneRoadObjDict;
    private readonly Dictionary<string, int> _buildingOutputRoundRobinIndex;
    private readonly Dictionary<string, BuildingObject> _buildingObjDict;

    private const float BuildingZ = 9f;

    public MainResourceFlowHandler(
        BuildingSceneRunnerBase runner,
        Transform buildingParent,
        Dictionary<string, RoadObject> roadObjDict,
        Dictionary<string, DualLaneRoadObject> dualLaneRoadObjDict,
        Dictionary<string, int> buildingOutputRoundRobinIndex,
        Dictionary<string, BuildingObject> buildingObjDict)
    {
        _mainRunner = runner;
        _buildingParent = buildingParent;
        _roadObjDict = roadObjDict;
        _dualLaneRoadObjDict = dualLaneRoadObjDict;
        _buildingOutputRoundRobinIndex = buildingOutputRoundRobinIndex;
        _buildingObjDict = buildingObjDict;
        _dataManager = DataManager.Instance;
    }



    public bool CanAcceptBuildingOutputs(BuildingObject building, Dictionary<string, int> outputs)
    {
        if (building == null || outputs == null || outputs.Count == 0) return false;

        List<Vector2Int> outputCells = building.OutputGridPositions;
        if (outputCells == null || outputCells.Count == 0) return false;

        string buildingKey = BuildingGridKey(building.Origin);
        _buildingOutputRoundRobinIndex.TryGetValue(buildingKey, out int startIndex);
        startIndex = Mathf.Clamp(startIndex, 0, outputCells.Count - 1);
        bool useRoundRobin = building.BuildingData is ProductionBuildingData && outputCells.Count > 1;

        foreach (KeyValuePair<string, int> kvp in outputs)
        {
            ResourcePacket packet = new ResourcePacket(kvp.Key, Mathf.Max(1, kvp.Value));
            bool placed = false;

            for (int offset = 0; offset < outputCells.Count; offset++)
            {
                int outIndex = useRoundRobin
                    ? (startIndex + offset) % outputCells.Count
                    : offset;
                Vector2Int outCell = outputCells[outIndex];
                if (!TryGetResourceNodeAtCell(outCell, out IResourceNode destNode)) continue;
                if (ReferenceEquals(building, destNode)) continue;
                if (!RoadNodeCanAccept(destNode, packet)) continue;

                placed = true;
                if (useRoundRobin)
                    startIndex = (outIndex + 1) % outputCells.Count;
                break;
            }

            if (!placed) return false;
        }

        return true;
    }

    public bool TryEmitBuildingOutputs(BuildingObject building, Dictionary<string, int> outputs)
    {
        if (building == null || outputs == null || outputs.Count == 0) return false;

        List<Vector2Int> outputCells = building.OutputGridPositions;
        if (outputCells == null || outputCells.Count == 0) return false;

        string buildingKey = BuildingGridKey(building.Origin);
        _buildingOutputRoundRobinIndex.TryGetValue(buildingKey, out int startIndex);
        startIndex = Mathf.Clamp(startIndex, 0, outputCells.Count - 1);
        bool useRoundRobin = building.BuildingData is ProductionBuildingData && outputCells.Count > 1;

        foreach (KeyValuePair<string, int> kvp in outputs)
        {
            ResourcePacket packet = new ResourcePacket(kvp.Key, Mathf.Max(1, kvp.Value));
            bool placed = false;

            for (int offset = 0; offset < outputCells.Count; offset++)
            {
                int outIndex = useRoundRobin
                    ? (startIndex + offset) % outputCells.Count
                    : offset;
                Vector2Int outCell = outputCells[outIndex];
                if (!TryGetResourceNodeAtCell(outCell, out IResourceNode destNode)) continue;
                if (ReferenceEquals(building, destNode)) continue;
                if (destNode is not RoadObject && destNode is not DualLaneRoadObject) continue;

                packet.TravelDirection = DirectionFromBuildingOutput(building, outCell);
                if (!destNode.TryPush(packet)) continue;

                placed = true;
                if (useRoundRobin)
                    startIndex = (outIndex + 1) % outputCells.Count;
                break;
            }

            if (!placed) return false;
        }

        if (useRoundRobin)
            _buildingOutputRoundRobinIndex[buildingKey] = startIndex;

        return true;
    }

    private static bool RoadNodeCanAccept(IResourceNode node, ResourcePacket packet)
    {
        if (node is RoadObject road)
            return !road.IsFull;
        if (node is DualLaneRoadObject dual)
            return dual.CanAcceptIncoming(packet);
        return false;
    }

    public void OnMainHourChanged()
    {
        if (_mainRunner.RawBuildingHandler != null)
        {
            _mainRunner.RawBuildingHandler.TickRawBuildingsSimulation();
        }

        TickResourceFlowFull();

        foreach (BuildingObject building in _buildingObjDict.Values)
        {
            if (building.IsRemovalAnimating) continue;
            building.TickSimulation(_dataManager);
        }
    }

    public void TickResourceFlowFull()
    {
        foreach (RoadObject road in _roadObjDict.Values)
            road.ResetRoadForwardGatesForQueuedPackets();
        foreach (DualLaneRoadObject dualRoad in _dualLaneRoadObjDict.Values)
            dualRoad.ResetRoadForwardGatesForQueuedPackets();

        foreach (RoadObject road in _roadObjDict.Values)
        {
            if (road.IsEmpty) continue;

            foreach (Vector2Int outCell in road.OutputGridPositions)
            {
                if (!TryGetResourceNodeAtCell(outCell, out IResourceNode destNode)) continue;
                if (ReferenceEquals(road, destNode)) continue;
                road.TryForwardTo(destNode, GridFlowUtils.DirectionFromDelta(outCell - road.GridPosition));
                break;
            }
        }

        foreach (DualLaneRoadObject dualRoad in _dualLaneRoadObjDict.Values)
        {
            if (dualRoad.IsEmpty) continue;
            for (int i = 0; i < dualRoad.OutputGridPositions.Count; i++)
            {
                Vector2Int outCell = dualRoad.OutputGridPositions[i];
                if (!TryGetResourceNodeAtCell(outCell, out IResourceNode destNode)) continue;
                if (ReferenceEquals(dualRoad, destNode)) continue;
                dualRoad.TryForwardToCell(outCell, destNode);
            }
        }
    }

    private bool TryGetResourceNodeAtCell(Vector2Int pos, out IResourceNode node)
    {
        node = null;
        BuildingSceneRunnerBase runner = GameManager.Instance?.CurrentRunner as BuildingSceneRunnerBase;
        return runner != null && runner.GridHandler != null && runner.GridHandler.TryGetResourceNodeAtCell(pos, out node);
    }

    private static FlowDirection DirectionFromBuildingOutput(BuildingObject building, Vector2Int outputCell)
    {
        Vector2Int delta = outputCell - building.Origin;
        if (delta.x >= building.Size.x) return FlowDirection.Right;
        if (delta.x < 0) return FlowDirection.Left;
        if (delta.y >= building.Size.y) return FlowDirection.Down;
        return FlowDirection.Up;
    }

    private static string BuildingGridKey(Vector2Int origin) => $"{origin.x}_{origin.y}";
}
