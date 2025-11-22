using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class MarketActorDynamicAllocator
{
    private static readonly ResourceType[] AllTypes =
        (ResourceType[])System.Enum.GetValues(typeof(ResourceType));

    public static void UpdateAssignments(
        MarketActorEntry entry,
        Dictionary<string, ResourceEntry> resources,
        InitialMarketData marketSettings = null)
    {
        if (entry?.data == null || resources == null || resources.Count == 0)
        {
            return;
        }

        if (!entry.data.useDynamicResourceAllocation)
        {
            return;
        }

        if (entry.data.roles.HasFlag(MarketRoleFlags.Provider))
        {
            UpdateProviderAssignment(entry, resources, marketSettings);
        }

        if (entry.data.roles.HasFlag(MarketRoleFlags.Consumer))
        {
            UpdateConsumerAssignment(entry, resources, marketSettings);
        }
    }

    private static void UpdateProviderAssignment(
        MarketActorEntry entry,
        Dictionary<string, ResourceEntry> resources,
        InitialMarketData marketSettings)
    {
        if (entry.state.provider == null)
        {
            return;
        }

        entry.state.provider.reassignmentCountdown =
            Mathf.Max(0, entry.state.provider.reassignmentCountdown - 1);

        if (entry.state.provider.activeResourceIds.Count == 0 ||
            entry.state.provider.reassignmentCountdown <= 0)
        {
            var profile = entry.GetOrCreateProviderProfile();
            if (profile == null)
            {
                return;
            }

            var selectedResources = SelectResources(entry, resources, true);
            profile.outputs = BuildPreferences(
                selectedResources,
                entry.data.scale,
                true,
                marketSettings);

            entry.state.provider.activeResourceIds =
                selectedResources.Select(r => r.resourceData.id).ToList();
            entry.state.provider.reassignmentCountdown =
                Mathf.Max(1, entry.data.providerReassignmentDays);
        }
    }

    private static void UpdateConsumerAssignment(
        MarketActorEntry entry,
        Dictionary<string, ResourceEntry> resources,
        InitialMarketData marketSettings)
    {
        if (entry.state.consumer == null)
        {
            return;
        }

        entry.state.consumer.reassignmentCountdown =
            Mathf.Max(0, entry.state.consumer.reassignmentCountdown - 1);

        if (entry.state.consumer.activeResourceIds.Count == 0 ||
            entry.state.consumer.reassignmentCountdown <= 0)
        {
            var profile = entry.GetOrCreateConsumerProfile();
            if (profile == null)
            {
                return;
            }

            EnsureBudgetRange(entry, profile, marketSettings);

            var selectedResources = SelectResources(entry, resources, false);
            profile.desiredResources = BuildPreferences(
                selectedResources,
                entry.data.scale,
                false,
                marketSettings);

            entry.state.consumer.activeResourceIds =
                selectedResources.Select(r => r.resourceData.id).ToList();
            entry.state.consumer.reassignmentCountdown =
                Mathf.Max(1, entry.data.consumerReassignmentDays);
        }
    }

    private static void EnsureBudgetRange(MarketActorEntry entry, ConsumerProfile profile, InitialMarketData marketSettings)
    {
        if (profile == null)
        {
            return;
        }

        if (profile.budgetRange.max <= 0f ||
            profile.budgetRange.max <= profile.budgetRange.min)
        {
            Vector2 budgetRange = entry.data.scale switch
            {
                MarketActorScale.Small => marketSettings != null ? marketSettings.smallConsumerBudget : new Vector2(600f, 1200f),
                MarketActorScale.Large => marketSettings != null ? marketSettings.largeConsumerBudget : new Vector2(2700f, 4800f),
                _ => marketSettings != null ? marketSettings.mediumConsumerBudget : new Vector2(1200f, 2250f)
            };
            
            profile.budgetRange = new BudgetRange { min = budgetRange.x, max = budgetRange.y };
        }
    }

    private static List<ResourceEntry> SelectResources(
        MarketActorEntry entry,
        Dictionary<string, ResourceEntry> resources,
        bool forProvider)
    {
        var preferredTypes = entry.data.preferredResourceTypes != null &&
                             entry.data.preferredResourceTypes.Length > 0
            ? entry.data.preferredResourceTypes
            : AllTypes;

        var candidates = new List<ResourceEntry>();
        foreach (var resource in resources.Values)
        {
            if (resource?.resourceData == null || resource.resourceState == null)
            {
                continue;
            }

            if (!preferredTypes.Contains(resource.resourceData.type))
            {
                continue;
            }

            candidates.Add(resource);
        }

        if (candidates.Count == 0)
        {
            return new List<ResourceEntry>();
        }

        int slots = forProvider
            ? MarketActorScaleUtility.GetProviderSlots(
                entry.data.scale,
                entry.data.providerSlotOverride)
            : MarketActorScaleUtility.GetConsumerSlots(
                entry.data.scale,
                entry.data.consumerSlotOverride);

        slots = Mathf.Max(1, slots);

        var selected = new List<ResourceEntry>();
        for (int i = 0; i < slots; i++)
        {
            var pick = PickResource(candidates, selected, forProvider);
            if (pick == null)
            {
                break;
            }

            selected.Add(pick);
        }

        if (selected.Count == 0)
        {
            selected.Add(candidates[Random.Range(0, candidates.Count)]);
        }

        return selected;
    }

    private static ResourceEntry PickResource(
        List<ResourceEntry> candidates,
        List<ResourceEntry> alreadySelected,
        bool forProvider)
    {
        ResourceEntry best = null;
        float bestScore = float.MinValue;

        foreach (var candidate in candidates)
        {
            if (alreadySelected.Any(x => x.resourceData == candidate.resourceData))
            {
                continue;
            }

            float supply = candidate.resourceState.lastSupply;
            float demand = candidate.resourceState.lastDemand;
            float imbalance = demand - supply;

            float score = forProvider ? imbalance : -imbalance;
            score += Random.Range(-0.25f, 0.25f);

            if (score > bestScore)
            {
                bestScore = score;
                best = candidate;
            }
        }

        return best ?? (candidates.Count > 0
            ? candidates[Random.Range(0, candidates.Count)]
            : null);
    }

    private static List<ResourcePreference> BuildPreferences(
        List<ResourceEntry> resources,
        MarketActorScale scale,
        bool forProvider,
        InitialMarketData marketSettings)
    {
        var result = new List<ResourcePreference>();
        foreach (var resource in resources)
        {
            if (resource?.resourceData == null)
            {
                continue;
            }

            var preference = new ResourcePreference
            {
                resource = resource.resourceData,
                priceSensitivity = forProvider ? 0.35f : 0.55f,
                urgency = forProvider ? 0.1f : 0.25f
            };

            (long min, long max) = GetQuantityRange(resource.resourceData.type, scale, forProvider, marketSettings);
            preference.desiredMin = min;
            preference.desiredMax = max;

            result.Add(preference);
        }

        return result;
    }

    private static (long min, long max) GetQuantityRange(
        ResourceType type,
        MarketActorScale scale,
        bool forProvider,
        InitialMarketData marketSettings)
    {
        float scaleMultiplier = scale switch
        {
            MarketActorScale.Small => marketSettings != null ? marketSettings.smallScaleMultiplier : 0.6f,
            MarketActorScale.Large => marketSettings != null ? marketSettings.largeScaleMultiplier : 1.6f,
            _ => marketSettings != null ? marketSettings.mediumScaleMultiplier : 1f
        };

        Vector2 baseRangeVector = type switch
        {
            ResourceType.raw => marketSettings != null ? marketSettings.rawProductionRange : new Vector2(250f, 450f),
            ResourceType.metal => marketSettings != null ? marketSettings.metalProductionRange : new Vector2(180f, 350f),
            ResourceType.wood => marketSettings != null ? marketSettings.woodProductionRange : new Vector2(200f, 400f),
            ResourceType.tool => marketSettings != null ? marketSettings.toolProductionRange : new Vector2(35f, 100f),
            ResourceType.weapon => marketSettings != null ? marketSettings.weaponProductionRange : new Vector2(25f, 75f),
            ResourceType.furniture => marketSettings != null ? marketSettings.otherProductionRange : new Vector2(15f, 40f),
            ResourceType.clothing => marketSettings != null ? marketSettings.otherProductionRange : new Vector2(20f, 50f),
            _ => marketSettings != null ? marketSettings.otherProductionRange : new Vector2(100f, 220f)
        };

        (float min, float max) baseRange = (baseRangeVector.x, baseRangeVector.y);

        if (!forProvider)
        {
            // Consumer 소비량 배율 적용
            float consumerMultiplier = marketSettings != null ? marketSettings.consumerConsumptionMultiplier : 0.85f;
            baseRange.min *= consumerMultiplier;
            baseRange.max *= consumerMultiplier;
        }

        long minValue = Mathf.RoundToInt(baseRange.min * scaleMultiplier);
        long maxValue = Mathf.RoundToInt(baseRange.max * scaleMultiplier);

        if (maxValue < minValue)
        {
            (minValue, maxValue) = (maxValue, minValue);
        }

        long finalMin = Mathf.RoundToInt(minValue);
        finalMin = System.Math.Max(1L, finalMin);

        long finalMax = Mathf.RoundToInt(maxValue);
        finalMax = System.Math.Max(finalMin, finalMax);

        return (finalMin, finalMax);
    }
}

