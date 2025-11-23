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
    private void RefreshActor(MarketActorEntry entry)
    {
        if (entry?.data == null)
        {
            return;
        }

        if (entry.data.roles.HasFlag(MarketRoleFlags.Consumer))
        {
            RefreshConsumer(entry);
        }

        if (entry.data.roles.HasFlag(MarketRoleFlags.Provider))
        {
            RefreshProvider(entry);
        }
    }

    /// <summary>
    /// Consumer 상태를 갱신합니다.
    /// </summary>
    private void RefreshConsumer(MarketActorEntry entry)
    {
        var profile = entry.GetConsumerProfile();
        var state = entry.state.consumer;
        if (profile == null || state == null)
        {
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
                float budgetRatio = entry.data.scale switch
                {
                    MarketActorScale.Small => 0.3f,
                    MarketActorScale.Large => 0.4f,
                    _ => 0.35f
                };
                state.currentBudget = Mathf.Max(100f, state.wealth * budgetRatio);
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

            // Provider 건강 효과
            if (entry.state.provider != null)
            {
                var state = entry.state.provider;
                float wealthChange = state.wealth - state.previousWealth;

                // 자산 증가 시 건강도 회복
                if (wealthChange > 0f)
                {
                    float recovery = _marketSettings != null ? _marketSettings.providerWealthGainHealthRecovery : 0.02f;
                    state.health = Mathf.Min(1f, state.health + recovery);
                }
                // 자산 감소 시 건강도 감소
                else if (wealthChange < 0f)
                {
                    float damage = _marketSettings != null ? _marketSettings.providerWealthLossHealthDamage : 0.05f;
                    state.health = Mathf.Max(0.2f, state.health - damage);
                }

                // 경쟁 페널티 (순위가 낮을수록)
                if (state.rank > 1)
                {
                    float penaltyRate = _marketSettings != null ? _marketSettings.providerRankPenalty : 0.005f;
                    float penalty = (state.rank - 1) * penaltyRate;
                    state.health = Mathf.Max(0.3f, state.health - penalty);
                }

                // 자연 감소 (매우 작게)
                float naturalDecay = _marketSettings != null ? _marketSettings.providerNaturalDecay : 0.002f;
                state.health = Mathf.Max(0.2f, state.health - naturalDecay);
            }

            // Consumer 건강 효과
            if (entry.state.consumer != null)
            {
                var state = entry.state.consumer;
                var profile = entry.GetConsumerProfile();

                // 예산 부족 시 건강도 감소
                float shortageThreshold = _marketSettings != null ? _marketSettings.budgetShortageThreshold : 0.3f;
                if (profile != null && state.currentBudget < profile.budgetRange.min * shortageThreshold)
                {
                    float damage = _marketSettings != null ? _marketSettings.consumerBudgetShortageDamage : 0.03f;
                    state.health = Mathf.Max(0.2f, state.health - damage);
                }
                else
                {
                    // 예산 충분 시 건강도 회복
                    float recovery = _marketSettings != null ? _marketSettings.consumerBudgetSufficientRecovery : 0.01f;
                    state.health = Mathf.Min(1f, state.health + recovery);
                }

                // 만족도에 따른 건강도
                float lerpRate = _marketSettings != null ? _marketSettings.consumerSatisfactionLerp : 0.1f;
                state.health = Mathf.Lerp(state.health, state.satisfaction, lerpRate);
                state.health = Mathf.Clamp(state.health, 0.2f, 1f);
            }
        }
    }

    /// <summary>
    /// 모든 액터의 경제력 기반 순위를 계산하고 업데이트합니다.
    /// </summary>
    private void UpdateRevenueRankings()
    {
        // Provider 순위 계산 (경제력 기준)
        var providerEntries = new List<(MarketActorEntry entry, float economicPower)>();
        foreach (var kvp in _actors)
        {
            var entry = kvp.Value;
            if (entry?.state?.provider != null)
            {
                float economicPower = entry.state.CalculateEconomicPower();
                providerEntries.Add((entry, economicPower));
            }
        }

        providerEntries.Sort((a, b) => b.economicPower.CompareTo(a.economicPower)); // 내림차순 정렬
        for (int i = 0; i < providerEntries.Count; i++)
        {
            providerEntries[i].entry.state.provider.rank = i + 1;
        }

        // Consumer 순위 계산 (경제력 기준)
        var consumerEntries = new List<(MarketActorEntry entry, float economicPower)>();
        foreach (var kvp in _actors)
        {
            var entry = kvp.Value;
            if (entry?.state?.consumer != null)
            {
                float economicPower = entry.state.CalculateEconomicPower();
                consumerEntries.Add((entry, economicPower));
            }
        }

        consumerEntries.Sort((a, b) => b.economicPower.CompareTo(a.economicPower)); // 내림차순 정렬
        for (int i = 0; i < consumerEntries.Count; i++)
        {
            consumerEntries[i].entry.state.consumer.rank = i + 1;
        }
    }
}

