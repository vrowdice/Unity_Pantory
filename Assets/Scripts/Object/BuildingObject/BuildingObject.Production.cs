using System.Collections.Generic;
using UnityEngine;

public partial class BuildingObject
{
    private void TickSimulationProduction(DataManager dataManager)
    {
        if (_selectedResource == null) return;
        TickStaffedBatchWork(dataManager, CanCompleteProductionBatch, TryCompleteProductionBatch);
    }

    private bool CanCompleteProductionBatch()
    {
        Dictionary<string, int> need = GetInputNeedForBatch(_selectedResource);
        if (need.Count > 0 && !InputBufferSatisfies(need)) return false;

        Dictionary<string, int> outputs = _selectedResource.GetBatchOutputCounts();
        int newPackets = outputs.Count;
        if (_maxOutputCapacity > 0 && _outputBuffer.Count + newPackets > _maxOutputCapacity) return false;

        return true;
    }

    private bool TryCompleteProductionBatch()
    {
        Dictionary<string, int> need = GetInputNeedForBatch(_selectedResource);
        if (need.Count > 0)
        {
            if (!InputBufferSatisfies(need)) return false;
            if (!TryConsumeFromInputBuffer(need)) return false;
        }

        Dictionary<string, int> outputs = _selectedResource.GetBatchOutputCounts();
        foreach (KeyValuePair<string, int> kvp in outputs)
            _outputBuffer.Enqueue(new ResourcePacket(kvp.Key, Mathf.Max(1, kvp.Value)));

        return true;
    }

    private static Dictionary<string, int> GetInputNeedForBatch(ResourceData recipe)
    {
        Dictionary<string, int> need = new Dictionary<string, int>();
        if (recipe.requirements != null)
        {
            foreach (ResourceRequirement req in recipe.requirements)
            {
                int c = Mathf.Max(1, req.count);
                if (need.TryGetValue(req.resource.id, out int existing)) need[req.resource.id] = existing + c;
                else need[req.resource.id] = c;
            }
        }

        return need;
    }

    private bool InputBufferSatisfies(Dictionary<string, int> need)
    {
        Dictionary<string, int> have = AggregateQueueCounts(_inputBuffer);
        foreach (KeyValuePair<string, int> kvp in need)
        {
            if (!have.TryGetValue(kvp.Key, out int h) || h < kvp.Value)
                return false;
        }
        return true;
    }

    private bool TryConsumeFromInputBuffer(Dictionary<string, int> need)
    {
        if (!InputBufferSatisfies(need)) return false;

        List<ResourcePacket> snapshot = new List<ResourcePacket>();
        while (_inputBuffer.Count > 0)
            snapshot.Add(_inputBuffer.Dequeue());

        List<ResourcePacket> working = new List<ResourcePacket>(snapshot);
        Dictionary<string, int> remaining = new Dictionary<string, int>(need);

        for (int i = 0; i < working.Count; i++)
        {
            ResourcePacket p = working[i];
            if (string.IsNullOrEmpty(p.Id)) continue;

            if (!remaining.TryGetValue(p.Id, out int rem) || rem <= 0)
                continue;

            if (p.Amount <= rem)
            {
                rem -= p.Amount;
                remaining[p.Id] = rem;
                working.RemoveAt(i);
                i--;
            }
            else
            {
                working[i] = new ResourcePacket(p.Id, p.Amount - rem);
                remaining[p.Id] = 0;
            }
        }

        foreach (KeyValuePair<string, int> kvp in remaining)
        {
            if (kvp.Value > 0)
            {
                foreach (ResourcePacket p in snapshot)
                    _inputBuffer.Enqueue(p);
                return false;
            }
        }

        foreach (ResourcePacket p in working)
            _inputBuffer.Enqueue(p);

        return true;
    }

    private static bool IsResourceAllowedForProduction(ProductionBuildingData prod, ResourceData resource)
    {
        List<ResourceData> list = prod.ProducibleResources;
        if (list != null && list.Count > 0)
        {
            foreach (ResourceData item in list)
            {
                if (item.id == resource.id) return true;
            }
            return false;
        }

        List<ResourceType> types = prod.AllowedResourceTypes;
        return types == null || types.Count == 0 || types.Contains(resource.type);
    }
}
