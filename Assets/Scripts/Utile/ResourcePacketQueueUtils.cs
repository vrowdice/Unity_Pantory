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

    public static bool CanAcceptResourceAmount(
        Dictionary<string, int> currentCounts,
        string resourceId,
        int addAmount,
        int maxPerResource,
        int maxKinds)
    {
        if (string.IsNullOrEmpty(resourceId) || addAmount <= 0) return false;
        if (maxPerResource <= 0) return false;

        int current = currentCounts.TryGetValue(resourceId, out int existing) ? existing : 0;
        if (current + addAmount > maxPerResource) return false;

        if (!currentCounts.ContainsKey(resourceId) && maxKinds > 0 && currentCounts.Count >= maxKinds)
            return false;

        return true;
    }

    public static bool CanAcceptResourceAmounts(
        Dictionary<string, int> currentCounts,
        Dictionary<string, int> toAdd,
        int maxPerResource,
        int maxKinds)
    {
        if (toAdd == null || toAdd.Count == 0) return true;

        Dictionary<string, int> simulated = new Dictionary<string, int>(currentCounts);
        foreach (KeyValuePair<string, int> kvp in toAdd)
        {
            if (!CanAcceptResourceAmount(simulated, kvp.Key, kvp.Value, maxPerResource, maxKinds))
                return false;

            if (simulated.TryGetValue(kvp.Key, out int existing))
                simulated[kvp.Key] = existing + kvp.Value;
            else
                simulated[kvp.Key] = kvp.Value;
        }

        return true;
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
