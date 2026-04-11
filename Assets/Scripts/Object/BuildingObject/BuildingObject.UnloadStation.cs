using UnityEngine;

public partial class BuildingObject
{
    private void TickSimulationUnloadStation(DataManager dataManager)
    {
        if (_selectedResource == null || string.IsNullOrEmpty(_selectedResource.id)) return;
        TickStaffedBatchWork(
            dataManager,
            () => CanCompleteUnloadBatch(dataManager),
            () => TryCompleteUnloadBatch(dataManager));
    }

    private bool CanCompleteUnloadBatch(DataManager dataManager)
    {
        UnloadStationData u = (UnloadStationData)_buildingData;
        int outCap = u.outputBufferCapacity > 0 ? u.outputBufferCapacity : _maxOutputCapacity;
        if (outCap > 0 && _outputBuffer.Count >= outCap) return false;
        ResourceEntry entry = dataManager.Resource.GetResourceEntry(_selectedResource.id);
        return entry.state.count >= u.pullPerHour;
    }

    private bool TryCompleteUnloadBatch(DataManager dataManager)
    {
        UnloadStationData u = (UnloadStationData)_buildingData;
        int pull = u.pullPerHour;
        if (!dataManager.Resource.ModifyResourceCount(_selectedResource.id, -pull)) return false;
        _outputBuffer.Enqueue(new ResourcePacket(_selectedResource.id, pull));
        return true;
    }
}
