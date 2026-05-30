using UnityEngine;
using System.Collections.Generic;

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
        Dictionary<string, int> outputs = new Dictionary<string, int>
        {
            { _selectedResource.id, u.pullPerHour }
        };
        if (!CanEmitOutputsToAdjacentRoads(outputs)) return false;

        ResourceEntry entry = dataManager.Resource.GetResourceEntry(_selectedResource.id);
        return entry.state.count >= u.pullPerHour;
    }

    private bool TryCompleteUnloadBatch(DataManager dataManager)
    {
        UnloadStationData u = (UnloadStationData)_buildingData;
        int pull = u.pullPerHour;
        if (!dataManager.Resource.ModifyResourceCount(_selectedResource.id, -pull)) return false;

        Dictionary<string, int> outputs = new Dictionary<string, int>
        {
            { _selectedResource.id, pull }
        };
        if (!TryEmitOutputsToAdjacentRoads(outputs))
        {
            dataManager.Resource.ModifyResourceCount(_selectedResource.id, pull);
            return false;
        }

        ResourceFlowFx.TryPlayFromWarehouse(_selectedResource, transform.position);
        return true;
    }
}
