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

    /// <summary>
    /// 초기 마켓 설정을 적용합니다.
    /// </summary>
    public void ApplyInitialMarketSettings(InitialMarketData settings)
    {
        if (settings == null)
        {
            return;
        }

        if (state.provider != null)
        {
            state.provider.wealth = settings.initialActorWealth;
            state.provider.previousWealth = settings.initialActorWealth;
            state.provider.health = settings.initialActorHealth;
        }

        if (state.consumer != null)
        {
            state.consumer.wealth = settings.initialActorWealth;
            state.consumer.previousWealth = settings.initialActorWealth;
            state.consumer.health = settings.initialActorHealth;
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

