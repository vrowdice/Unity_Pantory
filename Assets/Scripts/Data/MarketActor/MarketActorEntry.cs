using System;

[Serializable]
public class MarketActorEntry
{
    public MarketActorData data;
    public MarketActorState state;

    public MarketActorEntry(MarketActorData data)
    {
        this.data = data;
        state = new MarketActorState();

        if (data == null)
        {
            return;
        }

        if (data.roles.HasFlag(MarketRoleFlags.Provider))
        {
            state.provider = new ProviderState();
        }

        if (data.roles.HasFlag(MarketRoleFlags.Consumer))
        {
            state.consumer = new ConsumerState();

            if (data.consumerProfile != null)
            {
                state.consumer.currentBudget = data.consumerProfile.budgetRange.GetRandomBudget();
            }
        }
    }
}

