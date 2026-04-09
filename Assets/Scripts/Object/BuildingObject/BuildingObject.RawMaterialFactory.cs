using System.Collections.Generic;
using UnityEngine;

public partial class BuildingObject
{
    private void TickSimulationRawMaterialFactory(DataManager dataManager, RawMaterialFactoryData rawFactory)
    {
        if (_selectedResource == null || string.IsNullOrEmpty(_selectedResource.id)) return;
        if (!IsResourceAllowedForRawFactory(rawFactory, _selectedResource)) return;
        TickStaffedBatchWork(
            dataManager,
            () => CanCompleteRawMaterialFactoryBatch(dataManager),
            () => TryCompleteRawMaterialFactoryBatch(dataManager));
    }

    private bool CanCompleteRawMaterialFactoryBatch(DataManager dataManager)
    {
        if (dataManager == null || _selectedResource == null) return false;
        return dataManager.Resource.GetResourceEntry(_selectedResource.id) != null;
    }

    private bool TryCompleteRawMaterialFactoryBatch(DataManager dataManager)
    {
        if (!CanCompleteRawMaterialFactoryBatch(dataManager)) return false;
        return dataManager.Resource.ModifyResourceCount(_selectedResource.id, 1);
    }

    private static bool IsResourceAllowedForRawFactory(RawMaterialFactoryData raw, ResourceData resource)
    {
        if (resource == null || string.IsNullOrEmpty(resource.id)) return false;

        List<ResourceData> list = raw.ProducibleRawResources;
        if (list != null && list.Count > 0)
        {
            foreach (ResourceData item in list)
            {
                if (item != null && item.id == resource.id) return true;
            }
            return false;
        }

        return resource.type == ResourceType.raw;
    }
}
