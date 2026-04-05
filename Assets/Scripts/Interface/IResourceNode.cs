using UnityEngine;

public interface IResourceNode
{
    public bool TryPush(ResourcePacket resourcePacket);
}
