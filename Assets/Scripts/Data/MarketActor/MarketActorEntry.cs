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

        // 초기 자산이 0이면 기본값 설정 (액터가 시장에 참여할 수 있도록)
        float initialWealth = settings.initialActorWealth;
        if (initialWealth <= 0f)
        {
            // 스케일에 따라 다른 초기 자산 설정
            initialWealth = data.scale switch
            {
                MarketActorScale.Small => 5000f,
                MarketActorScale.Large => 20000f,
                _ => 10000f // Medium
            };
        }

        if (state.provider != null)
        {
            state.provider.wealth = initialWealth;
            state.provider.previousWealth = initialWealth;
            state.provider.health = settings.initialActorHealth;
        }

        if (state.consumer != null)
        {
            state.consumer.wealth = initialWealth;
            state.consumer.previousWealth = initialWealth;
            state.consumer.health = settings.initialActorHealth;
            
            // Consumer의 경우 예산도 초기 자산 기반으로 설정
            var consumerProfile = GetConsumerProfile();
            if (consumerProfile != null && consumerProfile.budgetRange.max <= 0f)
            {
                // 예산 범위가 설정되지 않은 경우 초기 자산의 일부를 예산으로 사용
                float budgetRatio = data.scale switch
                {
                    MarketActorScale.Small => 0.3f,
                    MarketActorScale.Large => 0.4f,
                    _ => 0.35f
                };
                float dailyBudget = initialWealth * budgetRatio;
                state.consumer.currentBudget = dailyBudget;
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

