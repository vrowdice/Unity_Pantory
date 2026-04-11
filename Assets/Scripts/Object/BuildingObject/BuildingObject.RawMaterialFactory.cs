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
        return dataManager.Resource.GetResourceEntry(_selectedResource.id) != null;
    }

    private bool TryCompleteRawMaterialFactoryBatch(DataManager dataManager)
    {
        return dataManager.Resource.ModifyResourceCount(_selectedResource.id, 1);
    }

    private static bool IsResourceAllowedForRawFactory(RawMaterialFactoryData raw, ResourceData resource)
    {
        List<ResourceData> list = raw.ProducibleResources;
        if (list != null && list.Count > 0)
        {
            foreach (ResourceData item in list)
            {
                if (item.id == resource.id) return true;
            }
            return false;
        }

        return resource.type == ResourceType.raw;
    }
}
