using System;
using UnityEngine;

[Serializable]
public class MarketActorEntry
{
    [Tooltip("시장 주체 ScriptableObject")]
    public MarketActorData data;
    [Tooltip("자산·신뢰도 등 런타임 상태")]
    public MarketActorState state;

    public MarketActorEntry(MarketActorData data)
    {
        this.data = data;

        state = new MarketActorState();
        if (data != null)
        {
            state.wealth = data.baseWealth;
        }
    }
}
