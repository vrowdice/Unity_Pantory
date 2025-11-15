using System;
using System.Collections.Generic;
using UnityEngine;

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

public enum MarketActorScale
{
    Small,
    Medium,
    Large
}

[Serializable]
public class ResourcePreference
{
    public ResourceData resource;
    public long desiredMin;
    public long desiredMax;
    public float priceSensitivity = 1f;
    public float urgency;

    public ResourcePreference Clone()
    {
        return new ResourcePreference
        {
            resource = resource,
            desiredMin = desiredMin,
            desiredMax = desiredMax,
            priceSensitivity = priceSensitivity,
            urgency = urgency
        };
    }
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

    public ProviderProfile Clone()
    {
        var clone = (ProviderProfile)MemberwiseClone();
        clone.outputs = new List<ResourcePreference>();
        foreach (var output in outputs)
        {
            if (output != null)
            {
                clone.outputs.Add(output.Clone());
            }
        }

        clone.upkeep = new List<ResourceRequirement>();
        foreach (var item in upkeep)
        {
            if (item != null)
            {
                clone.upkeep.Add(item.Clone());
            }
        }

        return clone;
    }
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

    public ConsumerProfile Clone()
    {
        var clone = (ConsumerProfile)MemberwiseClone();
        clone.desiredResources = new List<ResourcePreference>();
        foreach (var resource in desiredResources)
        {
            if (resource != null)
            {
                clone.desiredResources.Add(resource.Clone());
            }
        }

        return clone;
    }
}

public static class MarketActorScaleUtility
{
    public static int GetProviderSlots(MarketActorScale scale, int overrideValue)
    {
        if (overrideValue > 0)
        {
            return overrideValue;
        }

        return scale switch
        {
            MarketActorScale.Small => 1,
            MarketActorScale.Medium => 3,
            MarketActorScale.Large => 5,
            _ => 2
        };
    }

    public static int GetConsumerSlots(MarketActorScale scale, int overrideValue)
    {
        if (overrideValue > 0)
        {
            return overrideValue;
        }

        return scale switch
        {
            MarketActorScale.Small => 1,
            MarketActorScale.Medium => 2,
            MarketActorScale.Large => 4,
            _ => 2
        };
    }
}
