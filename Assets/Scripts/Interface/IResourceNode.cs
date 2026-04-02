public interface IResourceNode
{
    bool IsEmpty { get; }
    bool IsFull { get; }

    bool TryPush(ResourcePacket packet);
    bool TryPeek(out ResourcePacket packet);
    bool TryPop(out ResourcePacket packet);
}

