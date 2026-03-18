using System;
using UnityEngine;

[Serializable]
public class MarketActorEntry
{
    public MarketActorData data;
    public MarketActorState state;

    public MarketActorEntry(MarketActorData data)
    {
        this.data = data;

        state = new MarketActorState();
        state.wealth = data.baseWealth;
    }
}

