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
            
            // 재할당 주기에 ±30% 변동성 추가 (더 불규칙한 타이밍)
            int baseDays = entry.data.providerReassignmentDays;
            int variation = Mathf.RoundToInt(baseDays * Random.Range(-0.3f, 0.3f));
            entry.state.provider.reassignmentCountdown =
                Mathf.Max(1, baseDays + variation);
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
            
            // 재할당 주기에 ±30% 변동성 추가 (더 불규칙한 타이밍)
            int baseDays = entry.data.consumerReassignmentDays;
            int variation = Mathf.RoundToInt(baseDays * Random.Range(-0.3f, 0.3f));
            entry.state.consumer.reassignmentCountdown =
                Mathf.Max(1, baseDays + variation);
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
                MarketActorScale.Small => marketSettings != null ? marketSettings.smallConsumerBudget : new Vector2(1200f, 2400f),
                MarketActorScale.Large => marketSettings != null ? marketSettings.largeConsumerBudget : new Vector2(5400f, 9600f),
                _ => marketSettings != null ? marketSettings.mediumConsumerBudget : new Vector2(2400f, 4500f)
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
            
            // 더 큰 랜덤 변동성 추가 (±40%) - 액터들이 더 다양한 선택을 하도록
            float randomVariation = Random.Range(-0.4f, 0.4f);
            score += randomVariation;
            
            // 가격 변동성도 고려 (가격이 변동이 클수록 더 매력적)
            float priceVolatility = Mathf.Abs(candidate.resourceState.priceChangeRate);
            score += priceVolatility * Random.Range(-0.2f, 0.2f);

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

            // 액터별로 다른 가격 민감도와 긴급도 적용 (변동성 증가)
            float basePriceSensitivity = forProvider ? 0.35f : 0.55f;
            float baseUrgency = forProvider ? 0.1f : 0.25f;
            
            // ±20% 변동성 추가
            float priceSensitivityVariation = Random.Range(-0.2f, 0.2f);
            float urgencyVariation = Random.Range(-0.2f, 0.2f);
            
            var preference = new ResourcePreference
            {
                resource = resource.resourceData,
                priceSensitivity = Mathf.Clamp01(basePriceSensitivity * (1f + priceSensitivityVariation)),
                urgency = Mathf.Max(0f, baseUrgency * (1f + urgencyVariation))
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
            MarketActorScale.Small => marketSettings != null ? marketSettings.smallScaleMultiplier : 1.2f,
            MarketActorScale.Large => marketSettings != null ? marketSettings.largeScaleMultiplier : 3.0f,
            _ => marketSettings != null ? marketSettings.mediumScaleMultiplier : 2.0f
        };

        Vector2 baseRangeVector = type switch
        {
            ResourceType.raw => marketSettings != null ? marketSettings.rawProductionRange : new Vector2(800f, 1500f),
            ResourceType.metal => marketSettings != null ? marketSettings.metalProductionRange : new Vector2(600f, 1200f),
            ResourceType.wood => marketSettings != null ? marketSettings.woodProductionRange : new Vector2(700f, 1300f),
            ResourceType.tool => marketSettings != null ? marketSettings.toolProductionRange : new Vector2(150f, 350f),
            ResourceType.weapon => marketSettings != null ? marketSettings.weaponProductionRange : new Vector2(100f, 300f),
            ResourceType.furniture => marketSettings != null ? marketSettings.otherProductionRange : new Vector2(80f, 200f),
            ResourceType.clothing => marketSettings != null ? marketSettings.otherProductionRange : new Vector2(100f, 250f),
            ResourceType.component => marketSettings != null ? marketSettings.otherProductionRange : new Vector2(150f, 350f),
            ResourceType.electronics => marketSettings != null ? marketSettings.otherProductionRange : new Vector2(100f, 280f),
            ResourceType.vehicle => marketSettings != null ? marketSettings.otherProductionRange : new Vector2(30f, 80f),
            _ => marketSettings != null ? marketSettings.otherProductionRange : new Vector2(400f, 900f)
        };

        (float min, float max) baseRange = (baseRangeVector.x, baseRangeVector.y);

        if (!forProvider)
        {
            // Consumer 소비량 배율 적용 (소비량을 생산량보다 크게 설정하여 수요를 증가)
            float consumerMultiplier = marketSettings != null ? marketSettings.consumerConsumptionMultiplier : 1.2f;
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

