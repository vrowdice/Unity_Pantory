using System.Collections.Generic;
using UnityEngine;

public partial class BuildingObject
{
    private void TickSimulationLoadStation(DataManager dataManager)
    {
        TickStaffedBatchWork(
            dataManager,
            () => CanCompleteLoadBatch(dataManager),
            () => TryCompleteLoadBatch(dataManager));
    }

    private bool CanCompleteLoadBatch(DataManager dataManager)
    {
        if (_inputBuffer.Count == 0) return false;
        LoadStationData l = (LoadStationData)_buildingData;
        ResourcePacket p = _inputBuffer.Peek();
        int move = Mathf.Min(Mathf.Max(1, l.pushPerHour), p.Amount);
        return dataManager.Resource.GetResourceEntry(p.Id) != null && move > 0;
    }

    private bool TryCompleteLoadBatch(DataManager dataManager)
    {
        if (_inputBuffer.Count == 0) return false;
        LoadStationData l = (LoadStationData)_buildingData;
        ResourcePacket p = _inputBuffer.Peek();
        int move = Mathf.Min(Mathf.Max(1, l.pushPerHour), p.Amount);
        if (!dataManager.Resource.ModifyResourceCount(p.Id, move)) return false;
        ConsumeFromInputFront(move);
        return true;
    }

    private void ConsumeFromInputFront(int amountFromFrontPacket)
    {
        ResourcePacket p = _inputBuffer.Dequeue();
        int remainder = p.Amount - amountFromFrontPacket;
        if (remainder <= 0) return;

        List<ResourcePacket> tail = new List<ResourcePacket>();
        while (_inputBuffer.Count > 0)
            tail.Add(_inputBuffer.Dequeue());

        _inputBuffer.Enqueue(new ResourcePacket(p.Id, remainder));
        foreach (ResourcePacket t in tail)
            _inputBuffer.Enqueue(t);
    }
}
