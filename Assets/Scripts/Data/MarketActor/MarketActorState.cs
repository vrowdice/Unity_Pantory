using System;
using System.Collections.Generic;

[Serializable]
public class ProviderState
{
    public List<ResourceStock> stocks = new();
    public float priceDelta;
    public float productionProgress;
    public float cooldownTimer;
    public int activeContracts;

    public ProviderState()
    {
        priceDelta = 0f;
        productionProgress = 0f;
        cooldownTimer = 0f;
        activeContracts = 0;
    }
}

[Serializable]
public class ConsumerState
{
    public float currentBudget;
    public List<ResourceStock> holdings = new();
    public float satisfaction = 1f;
    public float desireTimer;

    public ConsumerState()
    {
        currentBudget = 0f;
        satisfaction = 1f;
        desireTimer = 0f;
    }
}

[Serializable]
public class MarketActorState
{
    public ProviderState provider;
    public ConsumerState consumer;
}

