using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 시장 시뮬레이션 로직 (공급/수요 계산, 거래 정산, 가격 조정, 비용 산정)
/// 포함: 액터 성장(Wealth Scaling), 가격 저항선, 공급망 제약, 유틸리티 함수
/// </summary>
public partial class MarketDataHandler
{
    // ==================================================================================
    // 1. 헬퍼 함수: 자산 비례 스케일링 & 가격 시그널
    // ==================================================================================

    /// <summary>
    /// 액터의 현재 자산 수준에 따른 생산/소비 규모 배율을 계산합니다.
    /// </summary>
    private float GetWealthScalingMultiplier(MarketActorEntry entry)
    {
        if (entry.state.wealth <= entry.state.previousWealth) return 1.0f;

        float baseWealth = 50000f; 
        float ratio = entry.state.wealth / baseWealth;
        
        return Mathf.Clamp(ratio, 1.0f, 5.0f);
    }

    /// <summary>
    /// 가격 탄력성에 따른 수량 조절 계수(Multiplier)를 계산합니다.
    /// </summary>
    private float CalculatePriceSignal(ResourceEntry resource, float sensitivity, bool isProducer)
    {
        float baseline = resource.resourceState.GetEffectiveBaseline(resource.resourceData.baseValue);
        float currentPrice = Mathf.Max(0.01f, resource.resourceState.currentValue);
        
        if (baseline <= 0.01f) return 1.0f;
        
        float priceRatio = currentPrice / baseline;
        float delta = isProducer ? (priceRatio - 1f) : (1f - priceRatio);
        
        float signal = 1f + delta * sensitivity;
        float minSignal = _marketSettings != null ? _marketSettings.minPriceSignal : 0.1f;
        
        return Mathf.Max(minSignal, signal);
    }

    // ==================================================================================
    // 2. 공급 시뮬레이션 (Provider Production)
    // ==================================================================================

    private void SimulateProviderProduction(
        MarketActorEntry entry,
        Dictionary<string, ResourceEntry> resources,
        Dictionary<string, float> totalSupply)
    {
        var profile = entry.GetProviderProfile();
        var state = entry.state.provider;
        if (profile == null || state == null) return;

        float wealthMultiplier = GetWealthScalingMultiplier(entry);

        foreach (var preference in profile.outputs)
        {
            if (preference?.resource == null || string.IsNullOrEmpty(preference.resource.id)) continue;
            if (!resources.TryGetValue(preference.resource.id, out var resourceEntry)) continue;

            float priceSignal = CalculatePriceSignal(resourceEntry, preference.priceSensitivity, true);
            float baseQuantity = SampleQuantity(preference.desiredMin, preference.desiredMax);
            float quantity = baseQuantity * wealthMultiplier; 

            // 공급망 제약
            bool isEssentialProducer = (preference.resource.type == ResourceType.raw);
            float resourceAvailability = 1.0f;
            
            if (!isEssentialProducer && profile.upkeep != null)
            {
                foreach (var req in profile.upkeep)
                {
                    if (req?.resource == null || !resources.TryGetValue(req.resource.id, out var reqEntry)) continue;

                    float marketSupply = reqEntry.resourceState.lastSupply;
                    float requiredAmount = req.count * wealthMultiplier; 

                    if (marketSupply < requiredAmount * 1.2f)
                    {
                        float ratio = marketSupply / Mathf.Max(1f, requiredAmount * 1.2f);
                        resourceAvailability = Mathf.Min(resourceAvailability, ratio);
                    }
                    
                    float unitPrice = reqEntry.resourceState.currentValue;
                    float basePrice = reqEntry.resourceData.baseValue;
                    if (basePrice > 0.01f && unitPrice > basePrice * 3.0f)
                    {
                        resourceAvailability *= 0.7f;
                    }
                }
            }

            float batchModifier = profile.allowBatchSelling ? 1f : (_marketSettings != null ? _marketSettings.noBatchSellingModifier : 0.5f);
            float efficiencyVariation = UnityEngine.Random.Range(0.95f, 1.05f);
            
            float output = quantity * priceSignal * profile.basePriceModifier * batchModifier * efficiencyVariation * resourceAvailability;
            float effectiveOutput = output * entry.state.health;

            AddToMap(totalSupply, preference.resource.id, effectiveOutput);

            float productionCost = CalculateProviderCost(profile, resources, effectiveOutput);
            state.dailyProductionCost += productionCost;

            // 랜덤 손실
            if (!isEssentialProducer)
            {
                float failureChance = _marketSettings != null ? _marketSettings.productionFailureChance : 0.05f;
                if (UnityEngine.Random.Range(0f, 1f) < failureChance)
                {
                    float lossRate = _marketSettings != null ? _marketSettings.productionFailureLossRate : 0.1f;
                    float lossAmount = effectiveOutput * lossRate;
                    
                    AddToMap(totalSupply, preference.resource.id, -lossAmount);
                    float currentPrice = Mathf.Max(0.01f, resourceEntry.resourceState.currentValue);
                    state.dailyProductionCost += lossAmount * currentPrice;
                }
            }

            state.priceDelta = priceSignal - 1f;
        }
    }

    // ==================================================================================
    // 3. 수요 시뮬레이션 (Consumer Demand)
    // ==================================================================================

    private void SimulateConsumerDemand(
        MarketActorEntry entry,
        Dictionary<string, ResourceEntry> resources,
        Dictionary<string, float> totalDemand)
    {
        var profile = entry.GetConsumerProfile();
        var state = entry.state.consumer;
        if (profile == null || state == null) return;

        float wealthMultiplier = GetWealthScalingMultiplier(entry);
        if (entry.data.id == "sys_populace") wealthMultiplier = 1.0f;

        foreach (var preference in profile.desiredResources)
        {
            if (preference?.resource == null || string.IsNullOrEmpty(preference.resource.id)) continue;
            if (!resources.TryGetValue(preference.resource.id, out var resourceEntry)) continue;

            float priceSignal = CalculatePriceSignal(resourceEntry, preference.priceSensitivity, false);
            
            float baseValue = resourceEntry.resourceData.baseValue;
            float currentPrice = resourceEntry.resourceState.currentValue;
            float resistanceThreshold = baseValue * resourceEntry.resourceData.priceResistanceThreshold;

            if (currentPrice > resistanceThreshold && baseValue > 0.01f)
            {
                float overpriceRatio = currentPrice / resistanceThreshold;
                float resistancePenalty = 1.0f / (overpriceRatio * overpriceRatio);
                
                if (preference.urgency > 0.8f) 
                    resistancePenalty = Mathf.Max(resistancePenalty, 0.8f);

                priceSignal *= resistancePenalty;
            }

            float baseAppetite = SampleQuantity(preference.desiredMin, preference.desiredMax);
            float appetite = baseAppetite * wealthMultiplier;
            
            float urgencyBoost = 1f + preference.urgency;
            float purchaseWillVariation = UnityEngine.Random.Range(0.95f, 1.05f);

            float desiredAmount = appetite * priceSignal * urgencyBoost * entry.state.health * purchaseWillVariation;

            if (desiredAmount <= 0f) continue;

            if (state.currentBudget > 0f)
            {
                float affordable = state.currentBudget / currentPrice;
                desiredAmount = Mathf.Min(desiredAmount, affordable);
            }

            AddToMap(totalDemand, preference.resource.id, desiredAmount);
        }
    }

    // ==================================================================================
    // 4. 거래 실행 (Execute Trades) 및 누락되었던 개별 처리 함수들
    // ==================================================================================

    private void ExecuteTrades(
        Dictionary<string, ResourceEntry> resources,
        Dictionary<string, float> totalSupply,
        Dictionary<string, float> totalDemand)
    {
        if (resources == null || _actors.Count == 0) return;

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

        foreach (var entry in _actors.Values)
        {
            if (entry?.state == null) continue;
            if (entry.state.wealth <= 0.1f) continue;

            if (entry.state.provider != null)
                ProcessProviderTrade(entry, resources, sellRatios);

            if (entry.state.consumer != null)
                ProcessConsumerTrade(entry, resources, buyRatios);
        }

        UpdateActorFinances();
    }

    // [복원 및 수정] Provider 개별 거래 처리 (Simulate와 로직 동기화)
    private void ProcessProviderTrade(
        MarketActorEntry entry,
        Dictionary<string, ResourceEntry> resources,
        Dictionary<string, float> sellRatios)
    {
        var profile = entry.GetProviderProfile();
        var state = entry.state.provider;
        if (profile == null || state == null) return;

        float wealthMultiplier = GetWealthScalingMultiplier(entry);

        foreach (var preference in profile.outputs)
        {
            if (preference?.resource == null) continue;
            if (!sellRatios.TryGetValue(preference.resource.id, out float sellRatio)) continue;
            if (!resources.TryGetValue(preference.resource.id, out var resourceEntry)) continue;

            float currentPrice = Mathf.Max(0.01f, resourceEntry.resourceState.currentValue);
            
            // Simulate와 동일한 생산량 산출 (단, 랜덤값은 다시 계산됨)
            // 정확한 시뮬레이션을 위해서는 Simulate에서 계산된 값을 저장했다가 써야 하지만,
            // 여기서는 근사치로 재계산하여 처리 (성능 및 구조 단순화)
            
            float priceSignal = CalculatePriceSignal(resourceEntry, preference.priceSensitivity, true);
            float baseQuantity = SampleQuantity(preference.desiredMin, preference.desiredMax);
            float quantity = baseQuantity * wealthMultiplier;

            // 공급망 제약 (약식 계산: 여기서는 이미 Simulate에서 걸러졌다고 가정하고 1.0)
            float resourceAvailability = 1.0f; 

            float batchModifier = profile.allowBatchSelling ? 1f : (_marketSettings != null ? _marketSettings.noBatchSellingModifier : 0.5f);
            // Simulate 때와 다른 랜덤값이 나오겠지만, 통계적으론 비슷함
            float efficiencyVariation = UnityEngine.Random.Range(0.95f, 1.05f);

            float output = quantity * priceSignal * profile.basePriceModifier * batchModifier * efficiencyVariation * resourceAvailability;
            float effectiveOutput = output * entry.state.health;

            float soldAmount = effectiveOutput * sellRatio;
            float salesRevenue = soldAmount * currentPrice;

            state.dailySalesRevenue += salesRevenue;
            state.dailySalesVolume += soldAmount;
        }
    }

    // [복원 및 수정] Consumer 개별 거래 처리
    private void ProcessConsumerTrade(
        MarketActorEntry entry,
        Dictionary<string, ResourceEntry> resources,
        Dictionary<string, float> buyRatios)
    {
        var profile = entry.GetConsumerProfile();
        var state = entry.state.consumer;
        if (profile == null || state == null) return;

        float wealthMultiplier = GetWealthScalingMultiplier(entry);
        if (entry.data.id == "sys_populace") wealthMultiplier = 1.0f;

        foreach (var preference in profile.desiredResources)
        {
            if (preference?.resource == null) continue;
            if (!buyRatios.TryGetValue(preference.resource.id, out float buyRatio)) continue;
            if (!resources.TryGetValue(preference.resource.id, out var resourceEntry)) continue;

            float currentPrice = Mathf.Max(0.01f, resourceEntry.resourceState.currentValue);
            float priceSignal = CalculatePriceSignal(resourceEntry, preference.priceSensitivity, false);

            // 저항선 로직 (Simulate와 동일)
            float baseValue = resourceEntry.resourceData.baseValue;
            float resistanceThreshold = baseValue * resourceEntry.resourceData.priceResistanceThreshold;
            if (currentPrice > resistanceThreshold && baseValue > 0.01f)
            {
                float overpriceRatio = currentPrice / resistanceThreshold;
                float resistancePenalty = 1.0f / (overpriceRatio * overpriceRatio);
                if (preference.urgency > 0.8f) resistancePenalty = Mathf.Max(resistancePenalty, 0.8f);
                priceSignal *= resistancePenalty;
            }

            float baseAppetite = SampleQuantity(preference.desiredMin, preference.desiredMax);
            float appetite = baseAppetite * wealthMultiplier;
            float urgencyBoost = 1f + preference.urgency;
            float purchaseWillVariation = UnityEngine.Random.Range(0.95f, 1.05f);

            float desiredAmount = appetite * priceSignal * urgencyBoost * entry.state.health * purchaseWillVariation;

            if (state.currentBudget > 0f)
            {
                float affordable = state.currentBudget / currentPrice;
                desiredAmount = Mathf.Min(desiredAmount, affordable);
            }

            float purchasedAmount = desiredAmount * buyRatio;
            float purchaseCost = purchasedAmount * currentPrice;

            // 예산 차감
            state.currentBudget = Mathf.Max(0f, state.currentBudget - purchaseCost);

            state.dailyPurchaseExpense += purchaseCost;
            state.dailyPurchaseVolume += purchasedAmount;

            float satisfactionRate = _marketSettings != null ? _marketSettings.satisfactionValueRate : 0.1f;
            state.dailyConsumptionValue += state.satisfaction * purchaseCost * satisfactionRate;
        }
    }

    // [복원] 자산 업데이트 함수
    private void UpdateActorFinances()
    {
        foreach (var entry in _actors.Values)
        {
            if (entry?.state == null) continue;

            float netProfit = 0f;

            if (entry.state.provider != null)
            {
                var pState = entry.state.provider;
                pState.dailyNetProfit = pState.dailySalesRevenue - pState.dailyProductionCost;
                netProfit += pState.dailyNetProfit;
            }

            if (entry.state.consumer != null)
            {
                var cState = entry.state.consumer;
                float purchaseLossRate = _marketSettings != null ? _marketSettings.purchaseCostLossRate : 0.5f;
                // 소비 만족도 가치에서 지출 비용 일부를 뺀 것을 순이익으로 간주 (게임적 허용)
                float consumerNetValue = cState.dailyConsumptionValue - (cState.dailyPurchaseExpense * purchaseLossRate);
                netProfit += consumerNetValue;
            }

            entry.state.wealth += netProfit;
            entry.state.wealth = Mathf.Max(0f, entry.state.wealth);
        }
    }

    // ==================================================================================
    // 5. 가격 조정 (Price Adjustment)
    // ==================================================================================
    
    private void ApplyPriceAdjustments(
       Dictionary<string, ResourceEntry> resources,
       Dictionary<string, float> supply,
       Dictionary<string, float> demand)
    {
        foreach (var entry in resources.Values)
        {
            if (entry?.resourceData == null || entry.resourceState == null) continue;

            string resourceId = entry.resourceData.id;
            float baseSupply = supply.TryGetValue(resourceId, out var s) ? s : 0f;
            float baseDemand = demand.TryGetValue(resourceId, out var d) ? d : 0f;
            
            // 플레이어 거래량 합산 (지속적 영향력)
            float playerDemand = entry.resourceState.accumulatedPlayerDemand;
            float playerSupply = entry.resourceState.accumulatedPlayerSupply;
            
            float totalSupply = baseSupply + playerSupply;
            float totalDemand = baseDemand + playerDemand;

            entry.resourceState.lastSupply = totalSupply;
            entry.resourceState.lastDemand = totalDemand;
            
            // 사용 후 초기화 (다음 날 거래량 수집을 위해)
            entry.resourceState.accumulatedPlayerDemand = 0f;
            entry.resourceState.accumulatedPlayerSupply = 0f;

            float baseline = entry.resourceState.GetEffectiveBaseline(entry.resourceData.baseValue);
            entry.resourceState.AdvanceOneDay();

            float turnover = Mathf.Max(1f, totalSupply + totalDemand);
            float minTurnover = Mathf.Max(turnover, 1000f); 
            float imbalance = totalDemand - totalSupply;
            float normalizedImbalance = Mathf.Clamp(imbalance / minTurnover, -1f, 1f);

            entry.resourceState.lastImbalance = imbalance;
            entry.resourceState.lastNormalizedImbalance = normalizedImbalance;

            float currentPrice = Mathf.Max(0.01f, entry.resourceState.currentValue);
            float baseValue = entry.resourceData.baseValue;
            
            float deviation = (baseValue > 0.01f) ? (currentPrice - baseValue) / baseValue : 0f;
            float meanReversionStrength = entry.resourceData.meanReversionStrength;
            
            if (Mathf.Abs(deviation) > 1.0f) meanReversionStrength *= 1.5f; 
            float meanReversion = -deviation * meanReversionStrength;

            float marketPressure = normalizedImbalance * entry.resourceData.marketSensitivity;
            float scarcity = Mathf.Max(0f, normalizedImbalance) * entry.resourceData.scarcityWeight;

            float rate = marketPressure + meanReversion + scarcity;
            rate = Mathf.Clamp(rate, -0.15f, 0.15f);

            // Hard Cap 체크
            float maxAllowedPrice = baseValue * entry.resourceData.maxPriceMultiplier;
            float projectedPrice = currentPrice * (1f + rate);

            if (projectedPrice > maxAllowedPrice && baseValue > 0.01f)
            {
                float overRatio = projectedPrice / maxAllowedPrice;
                if (overRatio > 1.5f) rate = -0.2f;
                else rate = -0.1f;
            }

            entry.resourceState.priceChangeRate = rate;
            float newPrice = currentPrice * (1f + rate);
            
            if (newPrice > maxAllowedPrice && baseValue > 0.01f) newPrice = maxAllowedPrice;
            
            entry.resourceState.currentValue = Mathf.Max(0.01f, newPrice);
            entry.resourceState.RecordPrice(entry.resourceState.currentValue);
        }
    }

    // ==================================================================================
    // 6. 비용 계산 (Cost Calculation)
    // ==================================================================================

    private float CalculateProviderCost(
        ProviderProfile profile,
        Dictionary<string, ResourceEntry> resources,
        float outputAmount)
    {
        if (profile == null || profile.upkeep == null || profile.upkeep.Count == 0)
        {
            return outputAmount * 5.0f; 
        }

        float totalMaterialCost = 0f;

        foreach (var requirement in profile.upkeep)
        {
            if (requirement?.resource == null) continue;

            float unitPrice = 100f; 
            if (resources.TryGetValue(requirement.resource.id, out var resourceEntry))
            {
                unitPrice = resourceEntry.resourceState.currentValue;
            }
            
            totalMaterialCost += requirement.count * unitPrice;
        }

        float operationCost = outputAmount * 2.0f; 
        return (totalMaterialCost * outputAmount) + operationCost;
    }

    // ==================================================================================
    // 7. 유틸리티 함수들 (복원됨)
    // ==================================================================================

    private static void AddToMap(Dictionary<string, float> map, string resourceId, float value)
    {
        if (map == null || string.IsNullOrEmpty(resourceId) || value == 0f) return;

        if (map.TryGetValue(resourceId, out var current))
        {
            map[resourceId] = current + value;
        }
        else
        {
            map[resourceId] = value;
        }
    }

    private static float SampleQuantity(long min, long max)
    {
        if (max < min) (min, max) = (max, min);

        float sampleMin = Mathf.Max(0f, min);
        float sampleMax = Mathf.Max(sampleMin, max);

        if (Mathf.Approximately(sampleMin, sampleMax)) return sampleMin;

        float baseValue = UnityEngine.Random.Range(sampleMin, sampleMax + 1f);
        
        // 변동성 축소 (예측 가능성 향상)
        float variationRange = (sampleMax - sampleMin) * 0.1f; 
        float variation = UnityEngine.Random.Range(-variationRange, variationRange);
        
        float finalValue = baseValue + variation;
        return Mathf.Clamp(finalValue, sampleMin, sampleMax);
    }
}