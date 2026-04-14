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
        if (data != null)
        {
            state.wealth = data.baseWealth;
        }
    }
}

