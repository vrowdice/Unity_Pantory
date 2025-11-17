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

    [Header("Wealth & Ranking")]
    public float wealth; // 자산 (현재 보유 자산, 손익에 따라 증감)
    public float previousWealth; // 전일 자산 (비교용)
    public int rank; // 순위
    [Header("Business Health")]
    public float health = 1f; // 건강도 (0~1, 단순화)

    public ProviderState()
    {
        priceDelta = 0f;
        productionProgress = 0f;
        cooldownTimer = 0f;
        activeContracts = 0;
        reassignmentCountdown = 0;
        wealth = 0f;
        previousWealth = 0f;
        rank = 0;
        health = 1f;
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

    [Header("Wealth & Ranking")]
    public float wealth; // 자산 (현재 보유 자산, 손익에 따라 증감)
    public float previousWealth; // 전일 자산 (비교용)
    public int rank; // 순위
    [Header("Business Health")]
    public float health = 1f; // 건강도 (0~1, 단순화)

    public ConsumerState()
    {
        currentBudget = 0f;
        satisfaction = 1f;
        desireTimer = 0f;
        reassignmentCountdown = 0;
        wealth = 0f;
        previousWealth = 0f;
        rank = 0;
        health = 1f;
    }
}

[Serializable]
public class MarketActorState
{
    public ProviderState provider;
    public ConsumerState consumer;

    /// <summary>
    /// 액터의 총 자산을 반환합니다 (Provider + Consumer 합계).
    /// </summary>
    public float GetWealth()
    {
        float totalWealth = 0f;
        if (provider != null)
        {
            totalWealth += provider.wealth;
        }
        if (consumer != null)
        {
            totalWealth += consumer.wealth;
        }
        return totalWealth;
    }

    /// <summary>
    /// 액터의 순위를 반환합니다 (더 높은 자산 기준).
    /// </summary>
    public int GetRank()
    {
        int providerRank = provider?.rank ?? int.MaxValue;
        int consumerRank = consumer?.rank ?? int.MaxValue;
        return Math.Min(providerRank, consumerRank);
    }

    /// <summary>
    /// 액터의 평균 건강도를 반환합니다.
    /// </summary>
    public float GetHealth()
    {
        float totalHealth = 0f;
        int count = 0;
        if (provider != null)
        {
            totalHealth += provider.health;
            count++;
        }
        if (consumer != null)
        {
            totalHealth += consumer.health;
            count++;
        }
        return count > 0 ? totalHealth / count : 1f;
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

