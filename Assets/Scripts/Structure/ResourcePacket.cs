using UnityEngine;

public class ResourcePacket
{
    public string Id { get; }
    public int Amount { get; }
    public bool BlockRoadForwardThisTick { get; set; }
    public FlowDirection TravelDirection { get; set; }

    public ResourcePacket(string id, int amount = 1, FlowDirection travelDirection = FlowDirection.None)
    {
        Id = id;
        Amount = amount;
        TravelDirection = travelDirection;
    }
}

