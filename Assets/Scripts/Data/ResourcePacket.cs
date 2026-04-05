using UnityEngine;

public class ResourcePacket
{
    public string Id { get; }
    public int Amount { get; }

    /// <summary>
    /// 이번 자원 흐름 틱에 도로에 들어온 직후면 true. 같은 틱에 도로→도로로 한 칸만 이동하도록 막는 데 사용.
    /// </summary>
    public bool BlockRoadForwardThisTick { get; set; }

    public ResourcePacket(string id, int amount = 1)
    {
        Id = id;
        Amount = amount;
    }
}

