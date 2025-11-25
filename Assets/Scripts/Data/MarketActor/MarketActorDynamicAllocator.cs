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
        InitialMarketData marketSettings = null,
        Dictionary<string, int> globalProviderCounts = null)
    {
        if (entry?.data == null || resources == null || resources.Count == 0)
        {
            return;
        }

        if (!entry.data.useDynamicResourceAllocation)
        {
            return;
        }

        // All actors handle both supply and consumption
        UpdateProviderAssignment(entry, resources, marketSettings, globalProviderCounts);
        UpdateConsumerAssignment(entry, resources, marketSettings);
    }

    private static void UpdateProviderAssignment(
        MarketActorEntry entry,
        Dictionary<string, ResourceEntry> resources,
        InitialMarketData marketSettings,
        Dictionary<string, int> globalProviderCounts = null)
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
            // [안정화 전략 1] 관성(Inertia): 현재 수익이 좋으면 직업 유지
            if (entry.state.provider.activeResourceIds.Count > 0)
            {
                bool isStillProfitable = CheckCurrentProfitability(entry, resources);
                // 수익성이 좋고, 10% 확률로만 강제 변경 (90% 확률로 유지)
                if (isStillProfitable && Random.value > 0.1f)
                {
                    // 카운트다운만 리셋하고 직업 유지
                    int inertiaBaseDays = entry.data.providerReassignmentDays;
                    int inertiaVariation = Mathf.RoundToInt(inertiaBaseDays * Random.Range(-0.3f, 0.3f));
                    entry.state.provider.reassignmentCountdown =
                        Mathf.Max(1, inertiaBaseDays + inertiaVariation);
                    return;
                }
            }

            var profile = entry.GetOrCreateProviderProfile();
            if (profile == null)
            {
                return;
            }

            var selectedResources = SelectResources(entry, resources, true, globalProviderCounts);
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

    /// <summary>
    /// 현재 생산 중인 자원의 수익성을 체크합니다 (관성 로직).
    /// 정규화된 상대적 가치 평가를 사용하여 자원 스케일에 무관하게 판단합니다.
    /// </summary>
    private static bool CheckCurrentProfitability(
        MarketActorEntry entry,
        Dictionary<string, ResourceEntry> resources)
    {
        if (entry.state?.provider == null || entry.state.provider.activeResourceIds.Count == 0)
        {
            return false;
        }

        var profile = entry.GetProviderProfile();
        if (profile?.outputs == null)
        {
            return false;
        }

        // 현재 생산 중인 자원들의 수익성 평가 (정규화된 점수 사용)
        float totalScore = 0f;
        int validOutputs = 0;

        foreach (var output in profile.outputs)
        {
            if (output?.resource == null || string.IsNullOrEmpty(output.resource.id))
            {
                continue;
            }

            // 현재 생산 중인 자원인지 확인
            if (!entry.state.provider.activeResourceIds.Contains(output.resource.id))
            {
                continue;
            }

            if (!resources.TryGetValue(output.resource.id, out var resourceEntry))
            {
                continue;
            }

            // [개선] 절대적인 수량(Imbalance) 대신 '매력도 점수'를 계산
            float supply = resourceEntry.resourceState.lastSupply;
            float demand = resourceEntry.resourceState.lastDemand;
            float currentPrice = resourceEntry.resourceState.currentValue;
            float basePrice = resourceEntry.resourceData.baseValue;

            // 1. 가격 매력도: 기준가보다 비싸게 팔리고 있는가? (가장 중요)
            float priceRatio = currentPrice / Mathf.Max(0.01f, basePrice);
            
            // 2. 수요 매력도: 공급 대비 수요가 많은가?
            float demandRatio = (supply > 0.01f) ? demand / supply : 2.0f; // 공급 0이면 매력도 최상
            demandRatio = Mathf.Clamp(demandRatio, 0f, 2.0f); // 0~2 범위로 제한

            // 종합 점수 (가격이 좋거나, 수요가 많으면 유지)
            // 1.0 이상이면 평균 이상
            float score = priceRatio * 0.6f + demandRatio * 0.4f;
            
            // 가격 변동성이 너무 크면 감점 (안정성 추구)
            float volatility = Mathf.Abs(resourceEntry.resourceState.priceChangeRate);
            if (volatility > 0.3f)
            {
                score *= 0.8f; // 변동성이 크면 20% 감점
            }

            totalScore += score;
            validOutputs++;
        }

        if (validOutputs > 0)
        {
            float avgScore = totalScore / validOutputs;
            // 기준점 1.1: 시장 평균보다 10% 이상 좋을 때만 유지
            // (너무 쉽게 직업을 바꾸지 않도록 관성을 부여)
            return avgScore > 1.1f;
        }

        return false;
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

            // Consumer는 자신이 생산하는 자원을 제외하고, 생산에 필요한 자원(upkeep)만 소비
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
        bool forProvider,
        Dictionary<string, int> globalProviderCounts = null)
    {
        var preferredTypes = entry.data.preferredResourceTypes != null &&
                             entry.data.preferredResourceTypes.Length > 0
            ? entry.data.preferredResourceTypes
            : AllTypes;

        // Provider가 생산하는 자원 목록 가져오기 (Consumer 선택 시 제외용)
        var providerOutputIds = new HashSet<string>();
        // Provider의 생산에 필요한 자원 목록 (upkeep + 생산품의 requirements)
        var relatedResourceIds = new HashSet<string>();
        if (!forProvider)
        {
            var providerProfile = entry.GetProviderProfile();
            if (providerProfile != null)
            {
                // 생산하는 자원 제외
                if (providerProfile.outputs != null)
                {
                    foreach (var output in providerProfile.outputs)
                    {
                        if (output?.resource != null && !string.IsNullOrEmpty(output.resource.id))
                        {
                            providerOutputIds.Add(output.resource.id);
                            
                            // 생산품의 requirements(필요 자원)도 관련 자원으로 추가
                            if (output.resource.requirements != null)
                            {
                                foreach (var req in output.resource.requirements)
                                {
                                    if (req?.resource != null && !string.IsNullOrEmpty(req.resource.id))
                                    {
                                        relatedResourceIds.Add(req.resource.id);
                                    }
                                }
                            }
                            
                            // nextStage 역추적: 이 자원을 만드는 데 필요한 자원들도 찾기
                            // (예: weapon을 만들면 iron_ingot이 필요하고, iron_ingot을 만들면 iron_ore가 필요)
                            var current = output.resource;
                            int depth = 0;
                            while (current != null && depth < 5) // 최대 5단계까지 추적
                            {
                                if (current.requirements != null)
                                {
                                    foreach (var req in current.requirements)
                                    {
                                        if (req?.resource != null && !string.IsNullOrEmpty(req.resource.id))
                                        {
                                            relatedResourceIds.Add(req.resource.id);
                                        }
                                    }
                                }
                                // nextStage를 역추적하려면 모든 자원을 검색해야 하므로 여기서는 requirements만 사용
                                depth++;
                                break; // 일단 1단계만
                            }
                        }
                    }
                }
                
                // 생산에 필요한 자원(upkeep) 우선 포함
                if (providerProfile.upkeep != null)
                {
                    foreach (var upkeep in providerProfile.upkeep)
                    {
                        if (upkeep?.resource != null && !string.IsNullOrEmpty(upkeep.resource.id))
                        {
                            relatedResourceIds.Add(upkeep.resource.id);
                        }
                    }
                }
            }
        }

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

            // Consumer인 경우
            if (!forProvider)
            {
                // 자신이 생산하는 자원은 제외
                if (providerOutputIds.Contains(resource.resourceData.id))
                {
                    continue;
                }
                
                // 생산품과 관련된 자원(upkeep, requirements) 우선 선택
                // 관련 자원이 있으면 그것만, 없으면 일반 자원도 선택 가능
                if (relatedResourceIds.Count > 0 && !relatedResourceIds.Contains(resource.resourceData.id))
                {
                    // 관련 자원이 정의되어 있지만 이 자원이 관련 자원이 아닌 경우
                    // 완전히 제외하지 않고 후순위로 두기 위해 점수 조정은 PickResource에서 처리
                    // 여기서는 후보에 포함시키되, 나중에 우선순위로 정렬
                }
            }

            // [안정화 전략 3] 공급망 강제성: Provider가 원자재가 없는 품목은 제외
            if (forProvider && resource.resourceData.requirements != null && resource.resourceData.requirements.Count > 0)
            {
                bool hasAllRequirements = true;
                foreach (var req in resource.resourceData.requirements)
                {
                    if (req?.resource == null || string.IsNullOrEmpty(req.resource.id))
                    {
                        continue;
                    }

                    if (!resources.TryGetValue(req.resource.id, out var reqEntry))
                    {
                        hasAllRequirements = false;
                        break;
                    }

                    // 원자재의 공급이 수요보다 너무 적거나, 재고가 바닥이면 제외
                    float reqImbalance = reqEntry.resourceState.lastDemand - reqEntry.resourceState.lastSupply;
                    // 원자재가 심각하게 부족하면 (불균형이 -100 이상) 생산 불가
                    if (reqImbalance < -100f)
                    {
                        hasAllRequirements = false;
                        break;
                    }
                }

                if (!hasAllRequirements)
                {
                    continue; // 원자재가 없으면 후보에서 제외
                }
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
        
        // Consumer인 경우 관련 자원 목록 전달
        HashSet<string> relatedIds = null;
        if (!forProvider)
        {
            var providerProfile = entry.GetProviderProfile();
            relatedIds = new HashSet<string>();
            if (providerProfile?.upkeep != null)
            {
                foreach (var upkeep in providerProfile.upkeep)
                {
                    if (upkeep?.resource != null && !string.IsNullOrEmpty(upkeep.resource.id))
                    {
                        relatedIds.Add(upkeep.resource.id);
                    }
                }
            }
            if (providerProfile?.outputs != null)
            {
                foreach (var output in providerProfile.outputs)
                {
                    if (output?.resource?.requirements != null)
                    {
                        foreach (var req in output.resource.requirements)
                        {
                            if (req?.resource != null && !string.IsNullOrEmpty(req.resource.id))
                            {
                                relatedIds.Add(req.resource.id);
                            }
                        }
                    }
                }
            }
        }
        
        for (int i = 0; i < slots; i++)
        {
            var pick = PickResource(candidates, selected, forProvider, relatedIds, resources, globalProviderCounts);
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
        bool forProvider,
        HashSet<string> relatedResourceIds = null,
        Dictionary<string, ResourceEntry> resources = null,
        Dictionary<string, int> globalProviderCounts = null)
    {
        ResourceEntry best = null;
        float bestScore = float.MinValue;

        foreach (var candidate in candidates)
        {
            if (alreadySelected.Any(x => x.resourceData == candidate.resourceData))
            {
                continue;
            }

            // 1. 기본 점수: 수급 불균형 (정규화하여 극단적 값 방지)
            float supply = candidate.resourceState.lastSupply;
            float demand = candidate.resourceState.lastDemand;
            float imbalance = demand - supply;
            
            // 불균형을 정규화 (로그 스케일 또는 제한)하여 극단적 값 방지
            float normalizedImbalance = Mathf.Sign(imbalance) * Mathf.Log10(Mathf.Abs(imbalance) + 1f);
            float baseScore = forProvider ? normalizedImbalance : -normalizedImbalance;

            // 2. [안정화 전략 3] 공급망 체크: 원자재 가용성 확인
            if (forProvider && candidate.resourceData.requirements != null && resources != null)
            {
                foreach (var req in candidate.resourceData.requirements)
                {
                    if (req?.resource == null || string.IsNullOrEmpty(req.resource.id))
                    {
                        continue;
                    }

                    if (!resources.TryGetValue(req.resource.id, out var reqEntry))
                    {
                        baseScore -= 1000f; // 원자재가 시장에 없으면 대폭 감점
                        continue;
                    }

                    // 원자재의 가격이 너무 비싸면 (기준가의 200% 이상) 페널티
                    float reqBaseline = reqEntry.resourceState.GetEffectiveBaseline(reqEntry.resourceData.baseValue);
                    float reqPriceRatio = reqEntry.resourceState.currentValue / reqBaseline;
                    if (reqPriceRatio > 2.0f)
                    {
                        baseScore -= 500f; // 원자재가 너무 비싸면 감점
                    }
                }
            }

            // 3. [안정화 전략 2] 경쟁 과열 방지 (Crowding Penalty)
            if (forProvider && globalProviderCounts != null)
            {
                if (globalProviderCounts.TryGetValue(candidate.resourceData.id, out int competitorCount))
                {
                    // 경쟁자가 많을수록 점수 차감 (경쟁자 1명당 5점 감점)
                    baseScore -= competitorCount * 5.0f;
                }
            }

            // 4. [안정화 전략 2] 변동성 완화 (Damping)
            float priceVolatility = Mathf.Abs(candidate.resourceState.priceChangeRate);
            if (forProvider)
            {
                // 가격 변동성이 크면 위험 회피 (진입 꺼림)
                baseScore -= priceVolatility * 2.0f;
            }

            // 5. [안정화 전략 2] 무작위성 축소 (±40% -> ±10%)
            float randomFactor = Random.Range(0.9f, 1.1f);
            float finalScore = baseScore * randomFactor;

            // 6. Consumer의 연관 자원 보너스 (기존 로직 유지, 보너스 증가)
            if (!forProvider && relatedResourceIds != null && relatedResourceIds.Count > 0)
            {
                if (relatedResourceIds.Contains(candidate.resourceData.id))
                {
                    // 관련 자원에 확실한 우선순위 부여 (보너스 증가)
                    finalScore += 500f;
                }
            }

            if (finalScore > bestScore)
            {
                bestScore = finalScore;
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
            // [수정] 원자재 생산량을 대폭 늘림 (800~1500 -> 2000~4000)
            // 원자재는 가격이 싸기 때문에 물량이 많아야 밸런스가 맞음
            ResourceType.raw => marketSettings != null ? marketSettings.rawProductionRange : new Vector2(2000f, 4000f),
            
            // 금속/나무 등 1차 가공품도 소폭 상향
            ResourceType.metal => marketSettings != null ? marketSettings.metalProductionRange : new Vector2(1000f, 2000f),
            ResourceType.weapon => marketSettings != null ? marketSettings.weaponProductionRange : new Vector2(100f, 300f),
            ResourceType.Essentials => marketSettings != null ? marketSettings.otherProductionRange : new Vector2(80f, 200f),
            ResourceType.Luxuries => marketSettings != null ? marketSettings.otherProductionRange : new Vector2(100f, 250f),
            ResourceType.component => marketSettings != null ? marketSettings.otherProductionRange : new Vector2(150f, 350f),
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

