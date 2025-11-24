using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ProviderState
{
    public List<ResourceStock> stocks = new();
    public float priceDelta;
    public float productionProgress;
    public float cooldownTimer;
    public int activeContracts;
    public int reassignmentCountdown;
    public List<string> activeResourceIds = new();

    [Header("Trading Statistics")]
    public float dailySalesRevenue; // 일일 매출액
    public float dailySalesVolume; // 일일 판매량
    public float dailyProductionCost; // 일일 생산 비용
    public float dailyNetProfit; // 일일 순이익

    public ProviderState()
    {
        priceDelta = 0f;
        productionProgress = 0f;
        cooldownTimer = 0f;
        activeContracts = 0;
        reassignmentCountdown = 0;
        dailySalesRevenue = 0f;
        dailySalesVolume = 0f;
        dailyProductionCost = 0f;
        dailyNetProfit = 0f;
    }
}

[Serializable]
public class ConsumerState
{
    public float currentBudget;
    public List<ResourceStock> holdings = new();
    public float satisfaction = 1f;
    public float desireTimer;
    public int reassignmentCountdown;
    public List<string> activeResourceIds = new();

    [Header("Trading Statistics")]
    public float dailyPurchaseExpense; // 일일 구매 비용
    public float dailyPurchaseVolume; // 일일 구매량
    public float dailyConsumptionValue; // 일일 소비 가치 (만족도 기반)

    public ConsumerState()
    {
        currentBudget = 0f;
        satisfaction = 1f;
        desireTimer = 0f;
        reassignmentCountdown = 0;
        dailyPurchaseExpense = 0f;
        dailyPurchaseVolume = 0f;
        dailyConsumptionValue = 0f;
    }
}

[Serializable]
public class MarketActorState
{
    public ProviderState provider;
    public ConsumerState consumer;

    [Header("Unified Actor State")]
    public float wealth; // 통합 자산 (공급/소비 모두 포함)
    public float previousWealth; // 전일 자산 (비교용)
    public float health = 1f; // 통합 건강도 (0~1)
    public int rank; // 통합 순위

    /// <summary>
    /// 액터의 총 자산을 반환합니다.
    /// </summary>
    public float GetWealth()
    {
        return wealth;
    }

    /// <summary>
    /// 액터의 일일 총 거래액을 반환합니다 (매출 + 구매비용).
    /// </summary>
    public float GetDailyTradeVolume()
    {
        float volume = 0f;
        if (provider != null)
        {
            volume += provider.dailySalesRevenue;
        }
        if (consumer != null)
        {
            volume += consumer.dailyPurchaseExpense;
        }
        return volume;
    }

    /// <summary>
    /// 액터의 일일 순이익을 반환합니다 (매출 - 생산비용 - 구매비용 + 소비가치).
    /// </summary>
    public float GetDailyNetProfit()
    {
        float profit = 0f;
        if (provider != null)
        {
            profit += provider.dailyNetProfit;
        }
        if (consumer != null)
        {
            // Consumer는 구매비용을 투자로 간주하고, 소비가치에서 차감
            profit += consumer.dailyConsumptionValue - consumer.dailyPurchaseExpense * 0.5f;
        }
        return profit;
    }

    /// <summary>
    /// 액터의 경제력을 계산합니다 (자산 + 거래액 + 순이익의 가중 평균).
    /// </summary>
    public float CalculateEconomicPower()
    {
        float wealthWeight = 0.4f;
        float tradeWeight = 0.3f;
        float profitWeight = 0.3f;

        float wealthScore = GetWealth();
        float tradeScore = GetDailyTradeVolume();
        float profitScore = GetDailyNetProfit();

        // 정규화 (임의의 기준값으로 나누어 0~1 범위로)
        float normalizedWealth = Mathf.Clamp01(wealthScore / 100000f);
        float normalizedTrade = Mathf.Clamp01(tradeScore / 50000f);
        float normalizedProfit = Mathf.Clamp01((profitScore + 10000f) / 20000f); // 음수도 고려

        return normalizedWealth * wealthWeight + normalizedTrade * tradeWeight + normalizedProfit * profitWeight;
    }

    /// <summary>
    /// 액터의 순위를 반환합니다.
    /// </summary>
    public int GetRank()
    {
        return rank;
    }

    /// <summary>
    /// 액터의 건강도를 반환합니다.
    /// </summary>
    public float GetHealth()
    {
        return health;
    }

    /// <summary>
    /// 액터의 건강 상태를 문자열로 반환합니다.
    /// </summary>
    public string GetHealthStatus()
    {
        float health = GetHealth();
        if (health >= 0.8f) return "Healthy";
        if (health >= 0.5f) return "Normal";
        if (health >= 0.3f) return "Crisis";
        return "Danger";
    }
}

