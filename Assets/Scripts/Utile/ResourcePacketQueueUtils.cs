using System.Collections.Generic;
using UnityEngine;

public static class ResourcePacketQueueUtils
{
    public static Dictionary<string, int> AggregateCounts(Queue<ResourcePacket> queue)
    {
        Dictionary<string, int> counts = new Dictionary<string, int>();
        foreach (ResourcePacket packet in queue)
        {
            if (packet == null || string.IsNullOrEmpty(packet.Id)) continue;
            if (counts.TryGetValue(packet.Id, out int current))
                counts[packet.Id] = current + packet.Amount;
            else
                counts[packet.Id] = packet.Amount;
        }

        return counts;
    }

    public static Dictionary<string, int> AggregateCounts(Queue<ResourcePacket> laneA, Queue<ResourcePacket> laneB)
    {
        Dictionary<string, int> counts = AggregateCounts(laneA);
        foreach (ResourcePacket packet in laneB)
        {
            if (packet == null || string.IsNullOrEmpty(packet.Id)) continue;
            if (counts.TryGetValue(packet.Id, out int current))
                counts[packet.Id] = current + packet.Amount;
            else
                counts[packet.Id] = packet.Amount;
        }

        return counts;
    }

    public static void ResetRoadForwardGates(Queue<ResourcePacket> queue)
    {
        foreach (ResourcePacket packet in queue)
        {
            if (packet != null)
                packet.BlockRoadForwardThisTick = false;
        }
    }

    public static void ResetRoadForwardGates(Queue<ResourcePacket> laneA, Queue<ResourcePacket> laneB)
    {
        ResetRoadForwardGates(laneA);
        ResetRoadForwardGates(laneB);
    }

    public static void ExportToSaveBuffer(Queue<ResourcePacket> queue, List<ResourcePacketSaveData> buffer)
    {
        foreach (ResourcePacket packet in queue)
        {
            if (packet == null || string.IsNullOrEmpty(packet.Id)) continue;
            buffer.Add(new ResourcePacketSaveData(packet.Id, Mathf.Max(1, packet.Amount), packet.TravelDirection));
        }
    }
}
