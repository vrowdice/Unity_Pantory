using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 시장 시뮬레이션 로직 (공급/수요 계산, 거래 정산, 가격 조정)
/// </summary>
public partial class MarketDataHandler
{
    /// <summary>
    /// Provider의 생산량을 계산하고 총 공급량에 집계합니다 (시장 풀 방식).
    /// </summary>
    private void SimulateProviderProduction(
        MarketActorEntry entry,
        Dictionary<string, ResourceEntry> resources,
        Dictionary<string, float> totalSupply)
    {
        if (!entry.data.roles.HasFlag(MarketRoleFlags.Provider))
        {
            return;
        }

        var profile = entry.GetProviderProfile();
        var state = entry.state.provider;
        if (profile == null || state == null)
        {
            return;
        }

        foreach (var preference in profile.outputs)
        {
            if (preference?.resource == null || string.IsNullOrEmpty(preference.resource.id))
            {
                continue;
            }

            if (!resources.TryGetValue(preference.resource.id, out var resourceEntry))
            {
                continue;
            }

            float baseline = resourceEntry.resourceState.GetEffectiveBaseline(resourceEntry.resourceData.baseValue);
            float currentPrice = Mathf.Max(0.01f, resourceEntry.resourceState.currentValue);
            float priceRatio = currentPrice / baseline;
            float priceSignal = 1f + (priceRatio - 1f) * preference.priceSensitivity;
            float minSignal = _marketSettings != null ? _marketSettings.minPriceSignal : 0.1f;
            priceSignal = Mathf.Max(minSignal, priceSignal);

            float quantity = SampleQuantity(preference.desiredMin, preference.desiredMax);
            float batchModifier = profile.allowBatchSelling ? 1f : (_marketSettings != null ? _marketSettings.noBatchSellingModifier : 0.5f);
            
            // 액터별 생산 효율 변동성 추가 (±10%)
            float efficiencyVariation = 1f + UnityEngine.Random.Range(-0.1f, 0.1f);
            
            float output = quantity * priceSignal * profile.basePriceModifier * batchModifier * efficiencyVariation;

            // 건강도 적용
            float effectiveOutput = output * state.health;

            AddToMap(totalSupply, preference.resource.id, effectiveOutput);

            // 생산 비용 계산 (거래 통계에 기록)
            float productionCost = CalculateProviderCost(profile, resources, effectiveOutput);
            state.dailyProductionCost += productionCost;

            // 랜덤 손실 (생산 실패) - 생산량에 직접 반영
            float failureChance = _marketSettings != null ? _marketSettings.productionFailureChance : 0.05f;
            float failureLossRate = _marketSettings != null ? _marketSettings.productionFailureLossRate : 0.1f;
            if (UnityEngine.Random.Range(0f, 1f) < failureChance)
            {
                float lossAmount = effectiveOutput * failureLossRate;
                // 공급량에서 손실 차감
                AddToMap(totalSupply, preference.resource.id, -lossAmount);
                state.dailyProductionCost += lossAmount * currentPrice;
            }

            state.priceDelta = priceSignal - 1f;
        }
    }

    /// <summary>
    /// Consumer의 수요를 계산합니다 (실제 구매는 ExecuteTrades에서 처리).
    /// </summary>
    private void SimulateConsumerDemand(
        MarketActorEntry entry,
        Dictionary<string, ResourceEntry> resources,
        Dictionary<string, float> totalDemand)
    {
        if (!entry.data.roles.HasFlag(MarketRoleFlags.Consumer))
        {
            return;
        }

        var profile = entry.GetConsumerProfile();
        var state = entry.state.consumer;
        if (profile == null || state == null)
        {
            return;
        }

        foreach (var preference in profile.desiredResources)
        {
            if (preference?.resource == null || string.IsNullOrEmpty(preference.resource.id))
            {
                continue;
            }

            if (!resources.TryGetValue(preference.resource.id, out var resourceEntry))
            {
                continue;
            }

            float baseline = resourceEntry.resourceState.GetEffectiveBaseline(resourceEntry.resourceData.baseValue);
            float currentPrice = Mathf.Max(0.01f, resourceEntry.resourceState.currentValue);
            float priceRatio = currentPrice / baseline;
            float priceSignal = 1f + (1f - priceRatio) * preference.priceSensitivity;
            float minSignal = _marketSettings != null ? _marketSettings.minPriceSignal : 0.1f;
            priceSignal = Mathf.Max(minSignal, priceSignal);

            float appetite = SampleQuantity(preference.desiredMin, preference.desiredMax);
            float urgencyBoost = 1f + preference.urgency;
            
            // 소비자 구매 의지 변동성 추가 (±12%)
            float purchaseWillVariation = 1f + UnityEngine.Random.Range(-0.12f, 0.12f);
            
            float desiredAmount = appetite * priceSignal * urgencyBoost * purchaseWillVariation;

            if (desiredAmount <= 0f)
            {
                continue;
            }

            // 건강도 적용
            float effectiveAmount = desiredAmount * state.health;

            // 예산 제한 (실제 구매는 ExecuteTrades에서 처리)
            if (profile.allowBulkBuying && state.currentBudget > 0f)
            {
                float affordable = state.currentBudget / currentPrice;
                effectiveAmount = Mathf.Min(effectiveAmount, affordable);
            }

            AddToMap(totalDemand, preference.resource.id, effectiveAmount);
        }
    }

    /// <summary>
    /// 시장 풀 방식으로 거래를 정산합니다 (O(M+N) 복잡도 최적화).
    /// </summary>
    private void ExecuteTrades(
        Dictionary<string, ResourceEntry> resources,
        Dictionary<string, float> totalSupply,
        Dictionary<string, float> totalDemand)
    {
        if (resources == null || _actors.Count == 0)
        {
            return;
        }

        // 1단계: 자원별 거래 비율 미리 계산 (O(M))
        var sellRatios = new Dictionary<string, float>();
        var buyRatios = new Dictionary<string, float>();

        foreach (var kvp in resources)
        {
            string resourceId = kvp.Key;
            float supply = totalSupply.TryGetValue(resourceId, out var s) ? s : 0f;
            float demand = totalDemand.TryGetValue(resourceId, out var d) ? d : 0f;

            sellRatios[resourceId] = (supply > 0f) ? Mathf.Clamp01(demand / supply) : 0f;
            buyRatios[resourceId] = (demand > 0f) ? Mathf.Clamp01(supply / demand) : 0f;
        }

        // 2단계: 액터는 한 번만 순회하며 자신의 역할 수행 (O(N))
        foreach (var entry in _actors.Values)
        {
            if (entry?.data == null || entry?.state == null)
            {
                continue;
            }

            // Provider 역할 수행
            if (entry.data.roles.HasFlag(MarketRoleFlags.Provider) && entry.state.provider != null)
            {
                ProcessProviderTrade(entry, resources, sellRatios);
            }

            // Consumer 역할 수행
            if (entry.data.roles.HasFlag(MarketRoleFlags.Consumer) && entry.state.consumer != null)
            {
                ProcessConsumerTrade(entry, resources, buyRatios);
            }
        }

        // 3단계: 거래 후 자산 및 순이익 업데이트
        UpdateActorFinances();
    }

    /// <summary>
    /// Provider의 거래를 처리합니다.
    /// </summary>
    private void ProcessProviderTrade(
        MarketActorEntry entry,
        Dictionary<string, ResourceEntry> resources,
        Dictionary<string, float> sellRatios)
    {
        var profile = entry.GetProviderProfile();
        var state = entry.state.provider;
        if (profile == null || state == null)
        {
            return;
        }

        foreach (var preference in profile.outputs)
        {
            if (preference?.resource == null || string.IsNullOrEmpty(preference.resource.id))
            {
                continue;
            }

            string resourceId = preference.resource.id;
            if (!sellRatios.TryGetValue(resourceId, out float sellRatio))
            {
                continue;
            }

            if (!resources.TryGetValue(resourceId, out var resourceEntry))
            {
                continue;
            }

            float currentPrice = Mathf.Max(0.01f, resourceEntry.resourceState.currentValue);

            // 생산량 계산
            float baseline = resourceEntry.resourceState.GetEffectiveBaseline(resourceEntry.resourceData.baseValue);
            float priceRatio = currentPrice / baseline;
            float priceSignal = 1f + (priceRatio - 1f) * preference.priceSensitivity;
            float minSignal = _marketSettings != null ? _marketSettings.minPriceSignal : 0.1f;
            priceSignal = Mathf.Max(minSignal, priceSignal);

            float quantity = SampleQuantity(preference.desiredMin, preference.desiredMax);
            float batchModifier = profile.allowBatchSelling ? 1f : (_marketSettings != null ? _marketSettings.noBatchSellingModifier : 0.5f);
            
            // 액터별 생산 효율 변동성 추가 (±10%)
            float efficiencyVariation = 1f + UnityEngine.Random.Range(-0.1f, 0.1f);
            
            float output = quantity * priceSignal * profile.basePriceModifier * batchModifier * efficiencyVariation;
            float effectiveOutput = output * state.health;

            // 판매량 = 생산량 * 판매 비율
            float soldAmount = effectiveOutput * sellRatio;
            float salesRevenue = soldAmount * currentPrice;

            // 통계 업데이트
            state.dailySalesRevenue += salesRevenue;
            state.dailySalesVolume += soldAmount;
        }
    }

    /// <summary>
    /// Consumer의 거래를 처리합니다.
    /// </summary>
    private void ProcessConsumerTrade(
        MarketActorEntry entry,
        Dictionary<string, ResourceEntry> resources,
        Dictionary<string, float> buyRatios)
    {
        var profile = entry.GetConsumerProfile();
        var state = entry.state.consumer;
        if (profile == null || state == null)
        {
            return;
        }

        foreach (var preference in profile.desiredResources)
        {
            if (preference?.resource == null || string.IsNullOrEmpty(preference.resource.id))
            {
                continue;
            }

            string resourceId = preference.resource.id;
            if (!buyRatios.TryGetValue(resourceId, out float buyRatio))
            {
                continue;
            }

            if (!resources.TryGetValue(resourceId, out var resourceEntry))
            {
                continue;
            }

            float currentPrice = Mathf.Max(0.01f, resourceEntry.resourceState.currentValue);

            // 수요량 계산
            float baseline = resourceEntry.resourceState.GetEffectiveBaseline(resourceEntry.resourceData.baseValue);
            float priceRatio = currentPrice / baseline;
            float priceSignal = 1f + (1f - priceRatio) * preference.priceSensitivity;
            float minSignal = _marketSettings != null ? _marketSettings.minPriceSignal : 0.1f;
            priceSignal = Mathf.Max(minSignal, priceSignal);

            float appetite = SampleQuantity(preference.desiredMin, preference.desiredMax);
            float urgencyBoost = 1f + preference.urgency;
            
            // 소비자 구매 의지 변동성 추가 (±12%)
            float purchaseWillVariation = 1f + UnityEngine.Random.Range(-0.12f, 0.12f);
            
            float desiredAmount = appetite * priceSignal * urgencyBoost * state.health * purchaseWillVariation;

            // 예산 제한
            if (profile.allowBulkBuying && state.currentBudget > 0f)
            {
                float affordable = state.currentBudget / currentPrice;
                desiredAmount = Mathf.Min(desiredAmount, affordable);
            }

            // 구매량 = 수요량 * 구매 비율
            float purchasedAmount = desiredAmount * buyRatio;
            float purchaseCost = purchasedAmount * currentPrice;

            // 예산 차감
            state.currentBudget = Mathf.Max(0f, state.currentBudget - purchaseCost);

            // 통계 업데이트
            state.dailyPurchaseExpense += purchaseCost;
            state.dailyPurchaseVolume += purchasedAmount;

            // 소비 가치 계산
            float satisfactionRate = _marketSettings != null ? _marketSettings.satisfactionValueRate : 0.1f;
            float consumptionValue = state.satisfaction * purchaseCost * satisfactionRate;
            state.dailyConsumptionValue += consumptionValue;
        }
    }

    /// <summary>
    /// 액터들의 자산 및 순이익을 업데이트합니다.
    /// </summary>
    private void UpdateActorFinances()
    {
        foreach (var entry in _actors.Values)
        {
            if (entry?.state == null)
            {
                continue;
            }

            // Provider: 매출 - 생산비용 = 순이익
            if (entry.state.provider != null)
            {
                var state = entry.state.provider;
                state.dailyNetProfit = state.dailySalesRevenue - state.dailyProductionCost;
                state.wealth += state.dailyNetProfit;
                state.wealth = Mathf.Max(0f, state.wealth);
            }

            // Consumer: 소비가치 - 구매비용의 일부 = 순이익
            if (entry.state.consumer != null)
            {
                var state = entry.state.consumer;
                float purchaseLossRate = _marketSettings != null ? _marketSettings.purchaseCostLossRate : 0.5f;
                float netValue = state.dailyConsumptionValue - state.dailyPurchaseExpense * purchaseLossRate;
                state.wealth += netValue;
                state.wealth = Mathf.Max(0f, state.wealth);
            }
        }
    }

    /// <summary>
    /// 가격 조정을 적용합니다.
    /// </summary>
    private void ApplyPriceAdjustments(
        Dictionary<string, ResourceEntry> resources,
        Dictionary<string, float> supply,
        Dictionary<string, float> demand)
    {
        foreach (var entry in resources.Values)
        {
            if (entry?.resourceData == null || entry.resourceState == null)
            {
                continue;
            }

            string resourceId = entry.resourceData.id;
            float totalSupply = supply.TryGetValue(resourceId, out var s) ? s : 0f;
            float totalDemand = demand.TryGetValue(resourceId, out var d) ? d : 0f;

            entry.resourceState.lastSupply = totalSupply;
            entry.resourceState.lastDemand = totalDemand;

            float imbalance = totalDemand - totalSupply;
            float turnover = Mathf.Max(1f, totalSupply + totalDemand);
            float normalizedImbalance = Mathf.Clamp(imbalance / turnover, -1f, 1f);

            entry.resourceState.lastImbalance = imbalance;
            entry.resourceState.lastNormalizedImbalance = normalizedImbalance;

            float baseline = entry.resourceState.GetEffectiveBaseline(entry.resourceData.baseValue);
            entry.resourceState.AdvanceOneDay();

            float currentPrice = Mathf.Max(0.01f, entry.resourceState.currentValue);
            float deviation = baseline > 0.01f ? (currentPrice - baseline) / baseline : 0f;

            float marketPressure = normalizedImbalance * entry.resourceData.marketSensitivity;
            float meanReversion = -deviation * entry.resourceData.meanReversionStrength;
            float scarcity = Mathf.Max(0f, normalizedImbalance) * entry.resourceData.scarcityWeight;

            float rate = marketPressure + meanReversion + scarcity;
            
            // 변동성 계산: 기본값 × 자원별 배율 + 추가 랜덤 변동성
            float baseVolatility = _marketSettings != null ? _marketSettings.baseMaxDailySwing : 0.01f;
            float maxSwing = baseVolatility * entry.resourceData.volatilityMultiplier;
            
            // 추가 랜덤 변동성 (±30% 추가)
            float randomVolatility = maxSwing * UnityEngine.Random.Range(-0.3f, 0.3f);
            maxSwing = Mathf.Max(0.005f, maxSwing + randomVolatility);
            
            rate = Mathf.Clamp(rate, -maxSwing, maxSwing);

            entry.resourceState.priceChangeRate = rate;
            entry.resourceState.currentValue = Mathf.Max(0.01f, currentPrice * (1f + rate));
            
            entry.resourceState.RecordPrice(entry.resourceState.currentValue);
        }
    }

    /// <summary>
    /// Provider의 생산 비용을 계산합니다.
    /// </summary>
    private float CalculateProviderCost(
        ProviderProfile profile,
        Dictionary<string, ResourceEntry> resources,
        float outputAmount)
    {
        if (profile == null || profile.upkeep == null || profile.upkeep.Count == 0)
        {
            // 기본 운영비
            float upkeepRate = _marketSettings != null ? _marketSettings.defaultUpkeepRate : 0.2f;
            return outputAmount * upkeepRate;
        }

        float totalCost = 0f;
        foreach (var requirement in profile.upkeep)
        {
            if (requirement?.resource == null || string.IsNullOrEmpty(requirement.resource.id))
            {
                continue;
            }

            if (!resources.TryGetValue(requirement.resource.id, out var resourceEntry))
            {
                continue;
            }

            float unitPrice = resourceEntry.resourceState.currentValue;
            float cost = requirement.count * unitPrice;
            totalCost += cost;
        }

        // 생산량에 비례한 비용 (스케일링)
        float costScale = _marketSettings != null ? _marketSettings.productionCostScale : 100f;
        return totalCost * (outputAmount / costScale);
    }

    // ==================== 유틸리티 ====================

    private static void AddToMap(Dictionary<string, float> map, string resourceId, float value)
    {
        if (map == null || string.IsNullOrEmpty(resourceId) || value == 0f)
        {
            return;
        }

        if (map.TryGetValue(resourceId, out var current))
        {
            map[resourceId] = current + value;
        }
        else
        {
            map[resourceId] = value;
        }
    }

    /// <summary>
    /// 생산량/수요량을 샘플링합니다. 변동성을 추가하여 예측 불가능성을 높입니다.
    /// </summary>
    private static float SampleQuantity(long min, long max)
    {
        if (max < min)
        {
            (min, max) = (max, min);
        }

        float sampleMin = Mathf.Max(0f, min);
        float sampleMax = Mathf.Max(sampleMin, max);

        if (Mathf.Approximately(sampleMin, sampleMax))
        {
            return sampleMin;
        }

        // 기본 랜덤 샘플
        float baseValue = UnityEngine.Random.Range(sampleMin, sampleMax + 1f);
        
        // 추가 변동성: ±15% 랜덤 변동
        float variationRange = (sampleMax - sampleMin) * 0.15f;
        float variation = UnityEngine.Random.Range(-variationRange, variationRange);
        
        // 최종값 계산 (범위 내로 클램프)
        float finalValue = baseValue + variation;
        return Mathf.Clamp(finalValue, sampleMin, sampleMax);
    }
}

