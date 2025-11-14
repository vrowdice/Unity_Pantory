using System;
using System.Collections.Generic;
using UnityEngine;

public enum MarketActorArchetype
{
    Generalist,
    Specialist,
    Trader,
    Guild
}

[Flags]
public enum MarketRoleFlags
{
    None = 0,
    Provider = 1 << 0,
    Consumer = 1 << 1
}

[Serializable]
public class ResourcePreference
{
    public ResourceData resource;
    public long desiredMin;
    public long desiredMax;
    public float priceSensitivity = 1f;
    public float urgency;
}

[Serializable]
public class ResourceStock
{
    public ResourceData resource;
    public long amount;
}

[Serializable]
public struct BudgetRange
{
    public float min;
    public float max;

    public float GetRandomBudget()
    {
        return UnityEngine.Random.Range(min, max);
    }
}

[Serializable]
public class ProviderProfile
{
    [Header("Production Output")]
    public List<ResourcePreference> outputs = new();

    [Header("Upkeep")]
    public List<ResourceRequirement> upkeep = new();

    [Header("Pricing & Cadence")]
    public float basePriceModifier = 1f;
    public Vector2 productionCooldownRange = new(15f, 30f);

    [Header("Behavior Flags")]
    public bool allowBatchSelling = true;
    public int maxConcurrentContracts = 1;
}

[Serializable]
public class ConsumerProfile
{
    [Header("Budget & Demand")]
    public BudgetRange budgetRange;
    public List<ResourcePreference> desiredResources = new();

    [Header("Behavior")]
    public float patienceSeconds = 30f;
    public float satisfactionDecay = 0.1f;
    public bool allowBulkBuying = true;
    public bool persistentOrders;
}

