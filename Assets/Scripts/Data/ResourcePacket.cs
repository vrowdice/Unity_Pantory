using UnityEngine;

public class ResourcePacket
{
    public string Id { get; }
    public int Amount { get; }

    public ResourcePacket(string id, int amount = 1)
    {
        Id = id;
        Amount = amount;
    }
}

