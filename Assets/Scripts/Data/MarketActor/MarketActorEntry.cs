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

        // [추가] 생성 즉시 최소 자산 보장 (안전장치 1)
        float minWealth = data.scale switch
        {
            MarketActorScale.Large => 1000000f,  // 대기업: 100만
            MarketActorScale.Medium => 200000f,  // 중기업: 20만
            _ => 50000f                          // 소기업: 5만
        };
        state.wealth = minWealth;
        state.previousWealth = minWealth;

        // [추가] 예산도 즉시 할당
        if (state.consumer != null)
        {
            if (consumerProfile != null && consumerProfile.budgetRange.max > 0f)
            {
                state.consumer.currentBudget = consumerProfile.budgetRange.GetRandomBudget();
            }
            else
            {
                state.consumer.currentBudget = state.wealth * 0.2f;
            }
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

        // 1. [핵심 수정] 스케일별 '최소 보장 자산' 설정 (밸런싱 패치 반영)
        // 기존: 5천 / 1만 / 2만 (너무 적음)
        // 수정: 5만 / 20만 / 100만 (현실적)
        float minStartingWealth = data.scale switch
        {
            MarketActorScale.Small => 50000f,    // 소기업: 최소 5만
            MarketActorScale.Medium => 200000f,  // 중기업: 최소 20만
            MarketActorScale.Large => 1000000f,  // 대기업: 최소 100만
            _ => 50000f
        };

        // 현재 자산이 최소치보다 적으면 강제로 채워넣음 (기존 세이브 파일 보정용)
        if (state.wealth < minStartingWealth)
        {
            state.wealth = minStartingWealth;
            // 디버깅용 (필요시 주석 해제)
            // Debug.Log($"[MarketActor] Wealth Boost for {data.displayName}: {minStartingWealth}");
        }

        // 전일 자산 동기화
        if (state.previousWealth <= 0f)
        {
            state.previousWealth = state.wealth;
        }
        
        // 건강도 초기화
        if (state.health <= 0.2f)
        {
            state.health = settings?.initialActorHealth ?? 1f;
        }

        // 2. Consumer 예산(Budget) 재설정
        if (state.consumer != null)
        {
            var consumerProfile = GetConsumerProfile();
            
            // 예산이 너무 적으면(1000 이하) 자산의 10%~20%를 예산으로 강제 할당
            if (state.consumer.currentBudget < 1000f)
            {
                float budgetRatio = data.scale == MarketActorScale.Large ? 0.3f : 0.1f;
                state.consumer.currentBudget = state.wealth * budgetRatio;
            }
            
            // 프로필에 설정된 예산 범위가 있다면 그것을 우선시
            bool hasExplicitProfileBudget = consumerProfile != null && consumerProfile.budgetRange.max > 0f;
            
            if (hasExplicitProfileBudget)
            {
                // 현재 예산이 설정된 최소치보다 낮으면 범위 내 랜덤값으로 재설정
                if (state.consumer.currentBudget < consumerProfile.budgetRange.min)
                {
                    state.consumer.currentBudget = consumerProfile.budgetRange.GetRandomBudget();
                }
            }
            // 프로필 예산이 없으면 자산 비율로 계산 (기존 로직 유지하되 수치 상향)
            else if (state.consumer.currentBudget <= 0f)
            {
                float budgetRatio = data.scale switch
                {
                    MarketActorScale.Small => 0.5f,
                    MarketActorScale.Large => 0.6f,
                    _ => 0.55f
                };
                
                float dailyBudget = state.wealth * budgetRatio;
                float minBudget = data.scale switch
                {
                    MarketActorScale.Small => 5000f,
                    MarketActorScale.Large => 50000f,
                    _ => 10000f
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

