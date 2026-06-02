using System.Collections.Generic;
using UnityEngine;

public class MainResourceFlowHandler
{
    private readonly MainRunner _mainRunner;
    private readonly DataManager _dataManager;

    private readonly Dictionary<string, RoadObject> _roadObjDict;
    private readonly Dictionary<string, DualLaneRoadObject> _dualLaneRoadObjDict;
    private readonly Dictionary<string, BuildingObject> _buildingObjDict;

    public MainResourceFlowHandler(
        MainRunner runner,
        Dictionary<string, RoadObject> roadObjDict,
        Dictionary<string, DualLaneRoadObject> dualLaneRoadObjDict,
        Dictionary<string, BuildingObject> buildingObjDict)
    {
        _mainRunner = runner;
        _roadObjDict = roadObjDict;
        _dualLaneRoadObjDict = dualLaneRoadObjDict;
        _buildingObjDict = buildingObjDict;
        _dataManager = DataManager.Instance;
    }

    public bool CanAcceptBuildingOutputs(BuildingObject building, Dictionary<string, int> outputs)
    {
        if (building == null || outputs == null || outputs.Count == 0)
            return false;

        Vector2Int outCell = building.OutputGridPosition;
        if (!TryGetResourceNodeAtCell(outCell, out IResourceNode destNode))
            return false;
        if (ReferenceEquals(building, destNode))
            return false;

        foreach (KeyValuePair<string, int> kvp in outputs)
        {
            ResourcePacket packet = new ResourcePacket(kvp.Key, Mathf.Max(1, kvp.Value));
            if (!RoadNodeCanAccept(destNode, packet))
                return false;
        }

        return true;
    }

    public bool TryEmitBuildingOutputs(BuildingObject building, Dictionary<string, int> outputs)
    {
        if (building == null || outputs == null || outputs.Count == 0)
            return false;

        Vector2Int outCell = building.OutputGridPosition;
        if (!TryGetResourceNodeAtCell(outCell, out IResourceNode destNode))
            return false;
        if (ReferenceEquals(building, destNode))
            return false;
        if (destNode is not RoadObject && destNode is not DualLaneRoadObject)
            return false;

        foreach (KeyValuePair<string, int> kvp in outputs)
        {
            ResourcePacket packet = new ResourcePacket(kvp.Key, Mathf.Max(1, kvp.Value));
            packet.TravelDirection = DirectionFromBuildingOutput(building, outCell);
            if (!destNode.TryPush(packet))
                return false;
        }

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
        if (_mainRunner.IsRestoringPlacedLayout)
            return;

        if (_mainRunner.RawBuildingHandler != null)
            _mainRunner.RawBuildingHandler.TickRawBuildingsSimulation();

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

            Vector2Int outCell = road.OutputGridPosition;
            if (!TryGetResourceNodeAtCell(outCell, out IResourceNode destNode)) continue;
            if (ReferenceEquals(road, destNode)) continue;
            road.TryForwardTo(destNode, GridFlowUtils.DirectionFromDelta(outCell - road.GridPosition));
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
        if (GameManager.Instance?.CurrentRunner is not MainRunner runner
            || runner.GridHandler == null)
        {
            return false;
        }

        return runner.GridHandler.TryGetResourceNodeAtCell(pos, out node);
    }

    private static FlowDirection DirectionFromBuildingOutput(BuildingObject building, Vector2Int outputCell)
    {
        Vector2Int delta = outputCell - building.Origin;
        if (delta.x >= building.Size.x) return FlowDirection.Right;
        if (delta.x < 0) return FlowDirection.Left;
        if (delta.y >= building.Size.y) return FlowDirection.Down;
        return FlowDirection.Up;
    }
}
