using System;
using UnityEngine;

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

        // All actors handle both supply and consumption
        state.provider = new ProviderState();
        _runtimeProviderProfile = data.providerProfile != null
            ? data.providerProfile.Clone()
            : null;

        state.consumer = new ConsumerState();
        _runtimeConsumerProfile = data.consumerProfile != null
            ? data.consumerProfile.Clone()
            : null;

        var consumerProfile = GetConsumerProfile();
        if (consumerProfile != null)
        {
            // budgetRange가 설정되어 있으면 사용, 없으면 0으로 남김 (나중에 RefreshConsumer에서 설정됨)
            if (consumerProfile.budgetRange.max > 0f)
            {
                state.consumer.currentBudget = consumerProfile.budgetRange.GetRandomBudget();
            }
            // budgetRange가 없으면 0으로 남김 (ApplyInitialMarketSettings나 RefreshConsumer에서 설정됨)
        }
    }

    /// <summary>
    /// 초기 마켓 설정을 적용합니다.
    /// settings가 null이어도 기본값으로 초기화합니다.
    /// </summary>
    public void ApplyInitialMarketSettings(InitialMarketData settings)
    {
        if (state == null)
        {
            state = new MarketActorState();
        }

        // 초기 자산이 0이면 기본값 설정 (액터가 시장에 참여할 수 있도록)
        float initialWealth = settings?.initialActorWealth ?? 0f;
        if (initialWealth <= 0f)
        {
            // 스케일에 따라 다른 초기 자산 설정
            // data가 null이거나 scale이 없으면 Medium으로 간주
            MarketActorScale scale = data?.scale ?? MarketActorScale.Medium;
            initialWealth = scale switch
            {
                MarketActorScale.Small => 5000f,
                MarketActorScale.Large => 20000f,
                _ => 10000f // Medium
            };
        }

        // Initialize unified actor state (이미 설정된 값이 있으면 유지, 없으면 초기화)
        if (state.wealth <= 0f)
        {
            state.wealth = initialWealth;
        }
        if (state.previousWealth <= 0f)
        {
            state.previousWealth = state.wealth;
        }
        if (state.health <= 0f)
        {
            state.health = settings?.initialActorHealth ?? 1f;
        }

        if (state.consumer != null)
        {
            var consumerProfile = GetConsumerProfile();
            
            // [개선] 현재 예산이 0일 때만 초기화 (생성자에서 설정한 값 보존)
            // 또한 프로필에 budgetRange가 명시적으로 설정된 경우(max > 0)는 
            // 자산 비율 계산보다 프로필 설정을 우선시함
            bool hasExplicitProfileBudget = consumerProfile != null && consumerProfile.budgetRange.max > 0f;
            
            if (state.consumer.currentBudget <= 0f && !hasExplicitProfileBudget)
            {
                // 예산 범위가 없는 경우에만 자산 기반으로 자동 계산
                float budgetRatio = data?.scale switch
                {
                    MarketActorScale.Small => 0.5f,
                    MarketActorScale.Large => 0.6f,
                    _ => 0.55f
                };
                
                float dailyBudget = state.wealth * budgetRatio;
                
                // 최소 생계비 보장
                float minBudget = data?.scale switch
                {
                    MarketActorScale.Small => 300f,
                    MarketActorScale.Large => 1000f,
                    _ => 500f
                };
                
                state.consumer.currentBudget = Mathf.Max(minBudget, dailyBudget);
            }
        }
    }

    public ProviderProfile GetProviderProfile()
    {
        if (data == null)
        {
            return null;
        }
        return _runtimeProviderProfile ?? data.providerProfile;
    }

    public ProviderProfile GetOrCreateProviderProfile()
    {
        if (data == null)
        {
            return null;
        }

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
        if (data == null)
        {
            return null;
        }
        return _runtimeConsumerProfile ?? data.consumerProfile;
    }

    public ConsumerProfile GetOrCreateConsumerProfile()
    {
        if (data == null)
        {
            return null;
        }

        if (_runtimeConsumerProfile == null)
        {
            _runtimeConsumerProfile = data.consumerProfile != null
                ? data.consumerProfile.Clone()
                : new ConsumerProfile();
        }

        return _runtimeConsumerProfile;
    }
}

