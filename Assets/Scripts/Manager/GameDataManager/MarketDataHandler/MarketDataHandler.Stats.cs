using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 액터 통계 및 후처리 (건강도, 랭킹, 상태 갱신)
/// </summary>
public partial class MarketDataHandler
{
    /// <summary>
    /// 액터 상태를 갱신합니다.
    /// </summary>
    /// <summary>
    /// MarketDataHandler 통계 파트
    /// </summary>
    private void RefreshActor(MarketActorEntry entry)
    {
        if (entry?.data == null)
        {
            return;
        }

        // All actors handle both supply and consumption
        RefreshConsumer(entry);
        RefreshProvider(entry);
    }

    /// <summary>
    /// Consumer 상태를 갱신합니다.
    /// </summary>
    private void RefreshConsumer(MarketActorEntry entry)
    {
        var profile = entry.GetConsumerProfile();
        var state = entry.state.consumer;
        if (profile == null || state == null || entry.data == null)
        {
            return;
        }

        // 시스템 액터(일반 시민): 무한 예산 유지
        if (entry.data.id == "sys_populace")
        {
            state.currentBudget = profile.budgetRange.max; // 항상 최대 예산
            return;
        }

        // 예산 갱신: persistentOrders가 아니거나 예산이 부족한 경우
        if (!profile.persistentOrders || state.currentBudget <= 0f)
        {
            // 예산 범위가 설정된 경우
            if (profile.budgetRange.max > 0f)
            {
                state.currentBudget = profile.budgetRange.GetRandomBudget();
            }
            else
            {
                // 예산 범위가 없는 경우 자산의 일부를 예산으로 사용
                // 구매력 향상을 위해 예산 비율 증가
                float budgetRatio = entry.data.scale switch
                {
                    MarketActorScale.Small => _marketSettings != null ? _marketSettings.smallBudgetRatio : 0.5f,
                    MarketActorScale.Large => _marketSettings != null ? _marketSettings.largeBudgetRatio : 0.6f,
                    _ => _marketSettings != null ? _marketSettings.mediumBudgetRatio : 0.55f
                };
                // Ensure minimum budget to allow consumption even with low initial wealth
                float calculatedBudget = entry.state.wealth * budgetRatio;
                float minBudget = entry.data.scale switch
                {
                    MarketActorScale.Small => _marketSettings != null ? _marketSettings.smallMinBudget : 500f,
                    MarketActorScale.Large => _marketSettings != null ? _marketSettings.largeMinBudget : 2000f,
                    _ => _marketSettings != null ? _marketSettings.mediumMinBudget : 1000f
                };
                state.currentBudget = Mathf.Max(minBudget, calculatedBudget);
            }
        }

        state.desireTimer = Mathf.Max(0f, state.desireTimer - 1f);
        if (state.desireTimer <= 0f)
        {
            state.desireTimer = Mathf.Max(1f, profile.patienceSeconds / 86400f); // 하루 단위로 환산
        }
    }

    /// <summary>
    /// Provider 상태를 갱신합니다.
    /// </summary>
    private void RefreshProvider(MarketActorEntry entry)
    {
        var state = entry.state.provider;
        if (state == null)
        {
            return;
        }

        state.cooldownTimer = Mathf.Max(0f, state.cooldownTimer - 1f);
        state.productionProgress = 0f;
        state.priceDelta = 0f;
    }

    /// <summary>
    /// 일일 거래 통계를 초기화합니다.
    /// </summary>
    private void ResetDailyTradingStats(MarketActorEntry entry)
    {
        if (entry?.state == null)
        {
            return;
        }

        if (entry.state.provider != null)
        {
            entry.state.provider.dailySalesRevenue = 0f;
            entry.state.provider.dailySalesVolume = 0f;
            entry.state.provider.dailyProductionCost = 0f;
            entry.state.provider.dailyNetProfit = 0f;
        }

        if (entry.state.consumer != null)
        {
            entry.state.consumer.dailyPurchaseExpense = 0f;
            entry.state.consumer.dailyPurchaseVolume = 0f;
            entry.state.consumer.dailyConsumptionValue = 0f;
        }
    }

    /// <summary>
    /// 비즈니스 건강 효과를 적용합니다 (단순화된 건강도 시스템).
    /// </summary>
    private void ApplyBusinessHealthEffects()
    {
        foreach (var entry in _actors.Values)
        {
            if (entry?.state == null)
            {
                continue;
            }

            // 통합 건강 효과 계산
            float wealthChange = entry.state.wealth - entry.state.previousWealth;
            float currentWealth = Mathf.Max(1f, entry.state.previousWealth); // 0 나누기 방지
            var consumerProfile = entry.GetConsumerProfile();
            var consumerState = entry.state.consumer;

            // [수정] 변동률(Rate) 기반 건강도 변화 (노이즈 필터링)
            // 자산의 0.1% 이상 변동이 있을 때만 건강도에 영향을 줌
            float changeRate = wealthChange / currentWealth;
            float threshold = 0.001f; // 0.1%

            if (changeRate > threshold)
            {
                // 이익: 회복 (변동폭이 클수록 더 많이 회복, 최대 2배)
                float recovery = _marketSettings != null ? _marketSettings.providerWealthGainHealthRecovery : 0.02f;
                float boost = Mathf.Clamp(changeRate * 10f, 1f, 2f);
                entry.state.health = Mathf.Min(1f, entry.state.health + (recovery * boost));
            }
            else if (changeRate < -threshold)
            {
                // 손실: 데미지 (변동폭이 클수록 더 많이 감소, 최대 2배)
                float damage = _marketSettings != null ? _marketSettings.providerWealthLossHealthDamage : 0.05f;
                float boost = Mathf.Clamp(Mathf.Abs(changeRate) * 10f, 1f, 2f);
                entry.state.health = Mathf.Max(0.2f, entry.state.health - (damage * boost));
            }

            // 경쟁 페널티 (순위가 낮을수록)
            if (entry.state.rank > 1)
            {
                float penaltyRate = _marketSettings != null ? _marketSettings.providerRankPenalty : 0.005f;
                float penalty = (entry.state.rank - 1) * penaltyRate;
                entry.state.health = Mathf.Max(0.3f, entry.state.health - penalty);
            }

            // 자연 감소 (매우 작게)
            float naturalDecay = _marketSettings != null ? _marketSettings.providerNaturalDecay : 0.002f;
            entry.state.health = Mathf.Max(0.2f, entry.state.health - naturalDecay);

            // Consumer 예산 및 만족도 효과
            if (consumerState != null && consumerProfile != null)
            {
                // 예산 부족 시 건강도 감소
                float shortageThreshold = _marketSettings != null ? _marketSettings.budgetShortageThreshold : 0.3f;
                if (consumerState.currentBudget < consumerProfile.budgetRange.min * shortageThreshold)
                {
                    float damage = _marketSettings != null ? _marketSettings.consumerBudgetShortageDamage : 0.03f;
                    entry.state.health = Mathf.Max(0.2f, entry.state.health - damage);
                }
                else
                {
                    // 예산 충분 시 건강도 회복
                    float recovery = _marketSettings != null ? _marketSettings.consumerBudgetSufficientRecovery : 0.01f;
                    entry.state.health = Mathf.Min(1f, entry.state.health + recovery);
                }

                // 만족도에 따른 건강도
                float lerpRate = _marketSettings != null ? _marketSettings.consumerSatisfactionLerp : 0.1f;
                entry.state.health = Mathf.Lerp(entry.state.health, consumerState.satisfaction, lerpRate);
            }

            entry.state.health = Mathf.Clamp(entry.state.health, 0.2f, 1f);
        }
    }

    // 순위 계산 최적화를 위한 캐시 리스트
    private List<MarketActorEntry> _cachedActorList = new List<MarketActorEntry>();

    /// <summary>
    /// 모든 액터의 자산 기준 순위를 계산하고 업데이트합니다.
    /// </summary>
    private void UpdateRevenueRankings()
    {
        // 캐시 리스트 재사용 (GC 부하 감소)
        _cachedActorList.Clear();
        foreach (var kvp in _actors)
        {
            var entry = kvp.Value;
            if (entry?.state != null)
            {
                _cachedActorList.Add(entry);
            }
        }

        // 자산 기준 내림차순 정렬
        _cachedActorList.Sort((a, b) => b.state.GetWealth().CompareTo(a.state.GetWealth()));

        // 순위 할당
        for (int i = 0; i < _cachedActorList.Count; i++)
        {
            _cachedActorList[i].state.rank = i + 1;
        }
    }
}

