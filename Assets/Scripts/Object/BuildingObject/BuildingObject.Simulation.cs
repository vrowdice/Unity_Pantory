using System;
using System.Collections.Generic;
using UnityEngine;

public partial class BuildingObject
{
    public void TickSimulation(DataManager dataManager)
    {
        if (_buildingData is RawMaterialFactoryData rawFactory)
        {
            if (_selectedResource == null || string.IsNullOrEmpty(_selectedResource.id)) return;
            if (!IsResourceAllowedForRawFactory(rawFactory, _selectedResource)) return;
            TickStaffedBatchWork(dataManager, () => CanCompleteRawMaterialFactoryBatch(dataManager), () => TryCompleteRawMaterialFactoryBatch(dataManager));
            return;
        }

        if (_buildingData is LoadStationData)
        {
            TickStaffedBatchWork(dataManager, () => CanCompleteLoadBatch(dataManager), () => TryCompleteLoadBatch(dataManager));
            return;
        }

        if (_buildingData is UnloadStationData)
        {
            if (_selectedResource == null || string.IsNullOrEmpty(_selectedResource.id)) return;
            TickStaffedBatchWork(dataManager, () => CanCompleteUnloadBatch(dataManager), () => TryCompleteUnloadBatch(dataManager));
            return;
        }

        if (_buildingData is ProductionBuildingData)
        {
            if (_selectedResource == null) return;
            TickStaffedBatchWork(dataManager, CanCompleteProductionBatch, TryCompleteProductionBatch);
        }
    }

    private void TickStaffedBatchWork(DataManager dataManager, Func<bool> canCompleteBatch, Func<bool> tryCompleteBatch)
    {
        float delta = GetWorkProgressDeltaPerTick(dataManager);
        if (delta <= 0f) return;

        _workProgress += delta;
        if (_workProgress > 1f && !canCompleteBatch()) _workProgress = 1f;

        while (_workProgress >= 1f)
        {
            if (!tryCompleteBatch()) break;
            _workProgress -= 1f;
        }

        // 진행도가 1.0(100%)에 고정되면 "멈춘 것처럼" 보여 UI가 어색해질 수 있어,
        // 실제로 완료 불가능한(입력 부족/출력 버퍼 가득참 등) 상태에서는 1.0 미만으로 유지합니다.
        if (_workProgress >= 1f && !canCompleteBatch())
            _workProgress = 0.999f;
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

    private bool CanCompleteLoadBatch(DataManager dataManager)
    {
        if (!(_buildingData is LoadStationData l) || dataManager == null || _inputBuffer.Count == 0) return false;
        ResourcePacket p = _inputBuffer.Peek();
        int move = Mathf.Min(Mathf.Max(1, l.pushPerHour), p.Amount);
        return dataManager.Resource.GetResourceEntry(p.Id) != null && move > 0;
    }

    private bool TryCompleteLoadBatch(DataManager dataManager)
    {
        if (!(_buildingData is LoadStationData l) || dataManager == null || _inputBuffer.Count == 0) return false;
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

    private bool CanCompleteUnloadBatch(DataManager dataManager)
    {
        if (!(_buildingData is UnloadStationData u) || _selectedResource == null) return false;
        int outCap = u.outputBufferCapacity > 0 ? u.outputBufferCapacity : _maxOutputCapacity;
        if (outCap > 0 && _outputBuffer.Count >= outCap) return false;
        ResourceEntry entry = dataManager.Resource.GetResourceEntry(_selectedResource.id);
        if (entry == null) return false;
        return entry.state.count >= u.pullPerHour;
    }

    private bool TryCompleteUnloadBatch(DataManager dataManager)
    {
        if (!(_buildingData is UnloadStationData u) || _selectedResource == null) return false;
        int pull = u.pullPerHour;
        if (!dataManager.Resource.ModifyResourceCount(_selectedResource.id, -pull)) return false;
        _outputBuffer.Enqueue(new ResourcePacket(_selectedResource.id, pull));
        return true;
    }

    private bool CanCompleteProductionBatch()
    {
        if (_selectedResource == null) return false;

        Dictionary<string, int> need = GetInputNeedForBatch(_selectedResource);
        if (need.Count > 0 && !InputBufferSatisfies(need)) return false;

        Dictionary<string, int> outputs = _selectedResource.GetBatchOutputCounts();
        int newPackets = outputs.Count;
        if (_maxOutputCapacity > 0 && _outputBuffer.Count + newPackets > _maxOutputCapacity) return false;

        return true;
    }

    private bool TryCompleteProductionBatch()
    {
        if (_selectedResource == null) return false;

        Dictionary<string, int> need = GetInputNeedForBatch(_selectedResource);
        if (need.Count > 0)
        {
            if (!InputBufferSatisfies(need)) return false;
            if (!TryConsumeFromInputBuffer(need)) return false;
        }

        Dictionary<string, int> outputs = _selectedResource.GetBatchOutputCounts();
        foreach (KeyValuePair<string, int> kvp in outputs)
        {
            if (string.IsNullOrEmpty(kvp.Key)) continue;
            _outputBuffer.Enqueue(new ResourcePacket(kvp.Key, Mathf.Max(1, kvp.Value)));
        }

        return true;
    }

    private static Dictionary<string, int> GetInputNeedForBatch(ResourceData recipe)
    {
        Dictionary<string, int> need = new Dictionary<string, int>();
        if (recipe == null) return need;
        if (recipe.requirements != null)
        {
            foreach (ResourceRequirement req in recipe.requirements)
            {
                if (req.resource == null) continue;
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
            if (p == null || string.IsNullOrEmpty(p.Id)) continue;

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
        if (resource == null || string.IsNullOrEmpty(resource.id)) return false;

        List<ResourceData> list = prod.ProducibleResources;
        if (list != null && list.Count > 0)
        {
            foreach (ResourceData item in list)
            {
                if (item != null && item.id == resource.id) return true;
            }
            return false;
        }

        List<ResourceType> types = prod.AllowedResourceTypes;
        return types == null || types.Count == 0 || types.Contains(resource.type);
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

