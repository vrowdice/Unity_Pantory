using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 시장 시뮬레이션 로직 (공급/수요 계산, 거래 정산, 가격 조정)
/// </summary>
public partial class MarketDataHandler
{
    /// <summary>
    /// 가격 탄력성에 따른 수량 조절 계수(Multiplier)를 계산합니다.
    /// </summary>
    private float CalculatePriceSignal(ResourceEntry resource, float sensitivity, bool isProducer)
    {
        float baseline = resource.resourceState.GetEffectiveBaseline(resource.resourceData.baseValue);
        float currentPrice = Mathf.Max(0.01f, resource.resourceState.currentValue);
        
        // baseline이 0이거나 너무 작으면 기본값 반환 (division by zero 방지)
        if (baseline <= 0.01f)
        {
            return 1.0f;
        }
        
        float priceRatio = currentPrice / baseline;
        
        // 생산자: 비쌀수록 많이(ratio > 1), 소비자: 쌀수록 많이(ratio < 1)
        float delta = isProducer ? (priceRatio - 1f) : (1f - priceRatio);
        
        float signal = 1f + delta * sensitivity;
        float minSignal = _marketSettings != null ? _marketSettings.minPriceSignal : 0.1f;
        
        return Mathf.Max(minSignal, signal);
    }

    /// <summary>
    /// Provider의 생산량을 계산하고 총 공급량에 집계합니다 (시장 풀 방식).
    /// </summary>
    private void SimulateProviderProduction(
        MarketActorEntry entry,
        Dictionary<string, ResourceEntry> resources,
        Dictionary<string, float> totalSupply)
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

            if (!resources.TryGetValue(preference.resource.id, out var resourceEntry))
            {
                continue;
            }

            // 1. 가격 시그널 계산 (헬퍼 함수 사용)
            float priceSignal = CalculatePriceSignal(resourceEntry, preference.priceSensitivity, true);

            // 2. 기본 생산량 샘플링
            float quantity = SampleQuantity(preference.desiredMin, preference.desiredMax);
            
            // 3. [공급망 제약] 원자재 가용성 체크 (공급량 + 가격 기준)
            // [솔루션 3] 원자재 공급자 고정: 기초 자원(raw, metal, wood) 생산자는 특례 적용
            bool isEssentialProducer = CheckIfEssentialProducer(preference.resource);
            float resourceAvailability = 1.0f;
            
            if (!isEssentialProducer && profile.upkeep != null && profile.upkeep.Count > 0)
            {
                foreach (var req in profile.upkeep)
                {
                    if (req?.resource == null || string.IsNullOrEmpty(req.resource.id))
                    {
                        continue;
                    }

                    if (resources.TryGetValue(req.resource.id, out var reqEntry))
                    {
                        // 원자재의 시장 공급량 확인 (재고가 아닌 공급량 기준)
                        float marketSupply = reqEntry.resourceState.lastSupply;
                        float requiredAmount = req.count;
                        
                        // 원자재가 요구량보다 적으면 생산 효율 감소
                        if (marketSupply < requiredAmount * 10f) // 임계값: 요구량의 10배 미만이면 부족
                        {
                            float shortageRatio = marketSupply / (requiredAmount * 10f);
                            resourceAvailability *= Mathf.Clamp01(shortageRatio); // 0~1 사이로 제한
                        }
                        
                        // [가격 기반 제약] 원자재 가격이 폭등하면 구하기 힘든 것으로 간주
                        float unitPrice = reqEntry.resourceState.currentValue;
                        float basePrice = reqEntry.resourceData.baseValue;
                        
                        // 가격이 기준가의 3배가 넘으면(너무 비싸면) 구하기 힘든 것으로 간주
                        if (basePrice > 0.01f && unitPrice > basePrice * 3.0f)
                        {
                            // 가격이 비쌀수록 생산 효율 감소 (최대 50% 감소)
                            float pricePenalty = Mathf.Clamp01((unitPrice / basePrice - 3.0f) / 2.0f); // 3배~5배 사이에서 0~1 페널티
                            resourceAvailability *= (1.0f - pricePenalty * 0.5f); // 최대 50% 감소
                        }
                    }
                    else
                    {
                        // 원자재가 시장에 없으면 생산 불가
                        resourceAvailability = 0f;
                        break;
                    }
                }
            }
            // [솔루션 3] 기초 자원 생산자는 원자재 부족 무시, 생산 실패 없음
            else if (isEssentialProducer)
            {
                resourceAvailability = 1.0f; // 원자재 부족 무시
            }

            // 4. 배치 판매, 효율 변동성 등 적용
            float batchModifier = profile.allowBatchSelling ? 1f : (_marketSettings != null ? _marketSettings.noBatchSellingModifier : 0.5f);
            float efficiencyVariation = 1f + UnityEngine.Random.Range(-0.1f, 0.1f);
            
            float output = quantity * priceSignal * profile.basePriceModifier * batchModifier * efficiencyVariation * resourceAvailability;

            // 5. 건강도 적용
            float effectiveOutput = output * entry.state.health;

            AddToMap(totalSupply, preference.resource.id, effectiveOutput);

            // 6. 생산 비용 계산 (거래 통계에 기록)
            float productionCost = CalculateProviderCost(profile, resources, effectiveOutput);
            state.dailyProductionCost += productionCost;

            // 7. 랜덤 손실 (생산 실패) - 생산량에 직접 반영
            // [솔루션 3] 기초 자원 생산자는 생산 실패 없음
            if (!isEssentialProducer)
            {
                float failureChance = _marketSettings != null ? _marketSettings.productionFailureChance : 0.05f;
                float failureLossRate = _marketSettings != null ? _marketSettings.productionFailureLossRate : 0.1f;
                if (UnityEngine.Random.Range(0f, 1f) < failureChance)
                {
                    float lossAmount = effectiveOutput * failureLossRate;
                    // 공급량에서 손실 차감
                    AddToMap(totalSupply, preference.resource.id, -lossAmount);
                    float currentPrice = Mathf.Max(0.01f, resourceEntry.resourceState.currentValue);
                    state.dailyProductionCost += lossAmount * currentPrice;
                }
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

            // 가격 시그널 계산 (헬퍼 함수 사용)
            float priceSignal = CalculatePriceSignal(resourceEntry, preference.priceSensitivity, false);

            // [추가] 과열 방지: 가격 저항선 체크 (Price Resistance)
            float baseValue = resourceEntry.resourceData.baseValue;
            float resourceCurrentPrice = resourceEntry.resourceState.currentValue;
            float resistanceThreshold = baseValue * resourceEntry.resourceData.priceResistanceThreshold;

            // 가격이 저항선을 넘으면 구매 의욕 급감
            if (resourceCurrentPrice > resistanceThreshold && baseValue > 0.01f)
            {
                // 초과된 비율만큼 구매량 삭감 (지수적으로 감소)
                float overpriceRatio = resourceCurrentPrice / resistanceThreshold;
                // 예: 저항선의 2배 가격이면 -> 1 / (2^2) = 1/4로 구매량 감소
                float resistancePenalty = 1.0f / (overpriceRatio * overpriceRatio);
                
                // (선택) 전쟁 중인 군사 액터 등은 이 저항을 무시하게 할 수도 있음 (urgency 체크)
                float urgencyThreshold = _marketSettings != null ? _marketSettings.priceResistanceUrgencyThreshold : 0.8f;
                float minPenalty = _marketSettings != null ? _marketSettings.minResistancePenalty : 0.5f;
                if (preference.urgency > urgencyThreshold) 
                {
                    // 급한 애들은 비싸도 좀 더 삼 (패널티 완화)
                    resistancePenalty = Mathf.Max(resistancePenalty, minPenalty); // 최소 비율은 유지
                }
                
                priceSignal *= resistancePenalty;
            }

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
            float effectiveAmount = desiredAmount * entry.state.health;

            // 예산 제한 (실제 구매는 ExecuteTrades에서 처리)
            if (profile.allowBulkBuying && state.currentBudget > 0f)
            {
                float currentPrice = Mathf.Max(0.01f, resourceEntry.resourceState.currentValue);
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

            // 파산한 액터는 거래 불가
            if (entry.state.wealth <= 0.1f)
            {
                continue;
            }

            // All actors handle both supply and consumption
            if (entry.state.provider != null)
            {
                ProcessProviderTrade(entry, resources, sellRatios);
            }

            if (entry.state.consumer != null)
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

            // 생산량 계산 (가격 시그널은 헬퍼 함수 사용)
            float priceSignal = CalculatePriceSignal(resourceEntry, preference.priceSensitivity, true);

            float quantity = SampleQuantity(preference.desiredMin, preference.desiredMax);
            float batchModifier = profile.allowBatchSelling ? 1f : (_marketSettings != null ? _marketSettings.noBatchSellingModifier : 0.5f);
            
            // 액터별 생산 효율 변동성 추가 (±10%)
            float efficiencyVariation = 1f + UnityEngine.Random.Range(-0.1f, 0.1f);
            
            float output = quantity * priceSignal * profile.basePriceModifier * batchModifier * efficiencyVariation;
            float effectiveOutput = output * entry.state.health;

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

            // 수요량 계산 (가격 시그널은 헬퍼 함수 사용)
            float priceSignal = CalculatePriceSignal(resourceEntry, preference.priceSensitivity, false);

            // [추가] 과열 방지: 가격 저항선 체크 (Price Resistance) - 실제 거래 시점에서도 체크
            float baseValue = resourceEntry.resourceData.baseValue;
            float resourceCurrentPrice = resourceEntry.resourceState.currentValue;
            float resistanceThreshold = baseValue * resourceEntry.resourceData.priceResistanceThreshold;

            // 가격이 저항선을 넘으면 구매 의욕 급감
            if (resourceCurrentPrice > resistanceThreshold && baseValue > 0.01f)
            {
                // 초과된 비율만큼 구매량 삭감 (지수적으로 감소)
                float overpriceRatio = resourceCurrentPrice / resistanceThreshold;
                // 예: 저항선의 2배 가격이면 -> 1 / (2^2) = 1/4로 구매량 감소
                float resistancePenalty = 1.0f / (overpriceRatio * overpriceRatio);
                
                // (선택) 전쟁 중인 군사 액터 등은 이 저항을 무시하게 할 수도 있음 (urgency 체크)
                float urgencyThreshold = _marketSettings != null ? _marketSettings.priceResistanceUrgencyThreshold : 0.8f;
                float minPenalty = _marketSettings != null ? _marketSettings.minResistancePenalty : 0.5f;
                if (preference.urgency > urgencyThreshold) 
                {
                    // 급한 애들은 비싸도 좀 더 삼 (패널티 완화)
                    resistancePenalty = Mathf.Max(resistancePenalty, minPenalty); // 최소 비율은 유지
                }
                
                priceSignal *= resistancePenalty;
            }

            float appetite = SampleQuantity(preference.desiredMin, preference.desiredMax);
            float urgencyBoost = 1f + preference.urgency;
            
            // 소비자 구매 의지 변동성 추가 (±12%)
            float purchaseWillVariation = 1f + UnityEngine.Random.Range(-0.12f, 0.12f);
            
            float desiredAmount = appetite * priceSignal * urgencyBoost * entry.state.health * purchaseWillVariation;

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

            // Calculate net profit from both provider and consumer activities
            float netProfit = 0f;
            
            if (entry.state.provider != null)
            {
                var providerState = entry.state.provider;
                providerState.dailyNetProfit = providerState.dailySalesRevenue - providerState.dailyProductionCost;
                netProfit += providerState.dailyNetProfit;
            }

            if (entry.state.consumer != null)
            {
                var consumerState = entry.state.consumer;
                float purchaseLossRate = _marketSettings != null ? _marketSettings.purchaseCostLossRate : 0.5f;
                float consumerNetValue = consumerState.dailyConsumptionValue - consumerState.dailyPurchaseExpense * purchaseLossRate;
                netProfit += consumerNetValue;
            }

            // Update unified wealth (무한 부활 방지 - 초기화 로직 제거)
            entry.state.wealth += netProfit;
            entry.state.wealth = Mathf.Max(0f, entry.state.wealth);
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

            // Calculate baseline first (needed for normalization)
            float baseline = entry.resourceState.GetEffectiveBaseline(entry.resourceData.baseValue);
            entry.resourceState.AdvanceOneDay();

            float imbalance = totalDemand - totalSupply;
            float turnover = Mathf.Max(1f, totalSupply + totalDemand);
            
            // Normalize imbalance, but use a minimum turnover threshold to avoid extreme swings when market is inactive
            float minTurnoverForNormalization = baseline * 10f; // Use baseline as reference for minimum activity
            float effectiveTurnover = Mathf.Max(turnover, minTurnoverForNormalization);
            float normalizedImbalance = Mathf.Clamp(imbalance / effectiveTurnover, -1f, 1f);

            entry.resourceState.lastImbalance = imbalance;
            entry.resourceState.lastNormalizedImbalance = normalizedImbalance;

            float currentPrice = Mathf.Max(0.01f, entry.resourceState.currentValue);
            float baseValue = entry.resourceData.baseValue; // Baseline 대신 고정 BaseValue 사용
            float deviation = baseValue > 0.01f ? (currentPrice - baseValue) / baseValue : 0f;

            float marketPressure = normalizedImbalance * entry.resourceData.marketSensitivity;
            
            // [수정] 가격 편차(Deviation)가 클수록 회귀력(Mean Reversion)을 기하급수적으로 강화
            float meanReversionStrength = entry.resourceData.meanReversionStrength;
            
            if (deviation < 0f)
            {
                // Price below baseline - reduce mean reversion to allow natural recovery
                meanReversionStrength *= 0.3f;
            }
            else if (deviation > 1.0f)
            {
                // 가격이 2배(100% 상승) 넘어가면 회귀력 강화
                // 벗어난 만큼 더 강하게 당김 (Squaring penalty)
                float reversionMult = _marketSettings != null ? _marketSettings.meanReversionMultiplier : 0.5f;
                meanReversionStrength *= (deviation * reversionMult + 1f);
            }
            
            float meanReversion = -deviation * meanReversionStrength;
            
            float scarcity = Mathf.Max(0f, normalizedImbalance) * entry.resourceData.scarcityWeight;

            float rate = marketPressure + meanReversion + scarcity;
            
            // 변동성 계산: 기본값 × 자원별 배율 + 추가 랜덤 변동성
            float baseVolatility = _marketSettings != null ? _marketSettings.baseMaxDailySwing : 0.01f;
            float maxSwing = baseVolatility * entry.resourceData.volatilityMultiplier;
            
            // 추가 랜덤 변동성 (±30% 추가)
            float randomVolatility = maxSwing * UnityEngine.Random.Range(-0.3f, 0.3f);
            maxSwing = Mathf.Max(0.005f, maxSwing + randomVolatility);
            
            rate = Mathf.Clamp(rate, -maxSwing, maxSwing);

            // [추가] 절대 상한선(Hard Cap) 적용 - clamp 이후에도 체크
            float maxAllowedPrice = baseValue * entry.resourceData.maxPriceMultiplier;
            
            // 계산된 가격이 상한선을 넘으려 하면 강제로 억제
            float projectedPrice = currentPrice * (1f + rate);
            if (projectedPrice > maxAllowedPrice && baseValue > 0.01f)
            {
                // 상한선으로 강제 복귀시키는 음수 변동률 적용
                // 상한선까지의 거리를 계산하여 더 강력하게 하락
                float overRatio = projectedPrice / maxAllowedPrice;
                float overThreshold = _marketSettings != null ? _marketSettings.overRatioThreshold : 1.5f;
                float maxDropRate = _marketSettings != null ? -_marketSettings.maxPriceDropRate : -0.2f;
                float normalDropRate = _marketSettings != null ? -_marketSettings.normalPriceDropRate : -0.1f;
                
                if (overRatio > overThreshold)
                {
                    // 상한선을 임계값 이상 넘으면 더 강하게 하락
                    rate = maxDropRate; // 하루에 최대 하락률씩 강제 하락
                }
                else
                {
                    // 상한선 근처면 일반 하락률로 하락
                    rate = normalDropRate; // 하루에 일반 하락률씩 강제 하락
                }
            }

            entry.resourceState.priceChangeRate = rate;
            float newPrice = currentPrice * (1f + rate);
            
            // 최종 가격도 상한선 체크 (이중 안전장치)
            if (newPrice > maxAllowedPrice && baseValue > 0.01f)
            {
                newPrice = maxAllowedPrice;
                entry.resourceState.priceChangeRate = (maxAllowedPrice - currentPrice) / currentPrice;
            }
            
            entry.resourceState.currentValue = Mathf.Max(0.01f, newPrice);
            
            entry.resourceState.RecordPrice(entry.resourceState.currentValue);
        }
    }

    /// <summary>
    /// Provider의 생산 비용을 계산합니다.
    /// 원자재 가격이 비싸면 생산 비용도 증가합니다.
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
                // 원자재가 시장에 없으면 기본 가격으로 추정 (또는 페널티)
                float estimatedPrice = requirement.resource.baseValue;
                if (estimatedPrice <= 0f)
                {
                    estimatedPrice = 100f; // 기본값
                }
                totalCost += requirement.count * estimatedPrice * 2f; // 부재 시 2배 페널티
                continue;
            }

            float unitPrice = resourceEntry.resourceState.currentValue;
            float basePrice = resourceEntry.resourceData.baseValue;
            
            // 원자재 가격이 기준가보다 비싸면 비용 증가 (현실성 향상)
            float priceMultiplier = 1.0f;
            if (basePrice > 0.01f && unitPrice > basePrice)
            {
                // 가격이 기준가의 2배 이상이면 추가 비용 발생
                priceMultiplier = 1.0f + Mathf.Clamp01((unitPrice / basePrice - 1.0f) * 0.5f);
            }
            
            float cost = requirement.count * unitPrice * priceMultiplier;
            totalCost += cost;
        }

        // 생산량에 비례한 비용 (스케일링)
        float costScale = _marketSettings != null ? _marketSettings.productionCostScale : 100f;
        return totalCost * (outputAmount / costScale);
    }

    /// <summary>
    /// [솔루션 3] 기초 자원(raw, metal, wood) 생산자인지 확인합니다.
    /// </summary>
    private static bool CheckIfEssentialProducer(ResourceData resource)
    {
        if (resource == null)
        {
            return false;
        }

        // 기초 자원 타입: raw(0), metal(1), wood(2)
        return resource.type == ResourceType.raw || 
               resource.type == ResourceType.metal;
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

