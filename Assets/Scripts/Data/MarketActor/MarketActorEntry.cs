using System;

[Serializable]
public class MarketActorEntry
{
    public MarketActorData data;
    public MarketActorState state;
    private ProviderProfile _runtimeProviderProfile;
    private ConsumerProfile _runtimeConsumerProfile;

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
            _runtimeProviderProfile = data.providerProfile != null
                ? data.providerProfile.Clone()
                : null;
        }

        if (data.roles.HasFlag(MarketRoleFlags.Consumer))
        {
            state.consumer = new ConsumerState();

            _runtimeConsumerProfile = data.consumerProfile != null
                ? data.consumerProfile.Clone()
                : null;

            var consumerProfile = GetConsumerProfile();
            if (consumerProfile != null)
            {
                state.consumer.currentBudget = consumerProfile.budgetRange.GetRandomBudget();
            }
        }
    }

    public ProviderProfile GetProviderProfile()
    {
        return _runtimeProviderProfile ?? data.providerProfile;
    }

    public ProviderProfile GetOrCreateProviderProfile()
    {
        if (_runtimeProviderProfile == null)
        {
            _runtimeProviderProfile = data.providerProfile != null
                ? data.providerProfile.Clone()
                : new ProviderProfile();
        }

        return _runtimeProviderProfile;
    }

    public ConsumerProfile GetConsumerProfile()
    {
        return _runtimeConsumerProfile ?? data.consumerProfile;
    }

    public ConsumerProfile GetOrCreateConsumerProfile()
    {
        if (_runtimeConsumerProfile == null)
        {
            _runtimeConsumerProfile = data.consumerProfile != null
                ? data.consumerProfile.Clone()
                : new ConsumerProfile();
        }

        return _runtimeConsumerProfile;
    }
}

