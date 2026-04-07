using System.Collections.Generic;
using UnityEngine;

public partial class BuildingObject
{
    public bool TryPush(ResourcePacket packet)
    {
        if (packet == null || _buildingData is RawMaterialFactoryData || _buildingData is UnloadStationData) return false;

        if (_buildingData is LoadStationData || _buildingData is ProductionBuildingData)
        {
            if (_maxInputCapacity <= 0 || _inputBuffer.Count >= _maxInputCapacity) return false;
            _inputBuffer.Enqueue(packet);
            return true;
        }

        return false;
    }

    /// <summary>
    /// 출력(생산·하역) 또는 상역 입력 큐 맨 앞을 이웃에 넣을 수 있을 때만 전달합니다. 실패 시 큐는 그대로입니다.
    /// </summary>
    public bool TryForwardTo(IResourceNode destination)
    {
        if (destination == null) return false;

        if (_buildingData is ProductionBuildingData || _buildingData is UnloadStationData)
        {
            if (_outputBuffer.Count == 0) return false;
            ResourcePacket packet = _outputBuffer.Peek();
            if (!destination.TryPush(packet)) return false;
            _outputBuffer.Dequeue();
            return true;
        }

        if (_buildingData is LoadStationData)
        {
            if (_inputBuffer.Count == 0) return false;
            ResourcePacket packet = _inputBuffer.Peek();
            if (!destination.TryPush(packet)) return false;
            _inputBuffer.Dequeue();
            return true;
        }

        return false;
    }

    public Dictionary<string, int> GetRuntimeInputResourceCounts() => AggregateQueueCounts(_inputBuffer);
    public Dictionary<string, int> GetRuntimeOutputResourceCounts() => AggregateQueueCounts(_outputBuffer);
}

