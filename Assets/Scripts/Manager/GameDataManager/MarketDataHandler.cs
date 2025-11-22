using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 시장 행위자 데이터를 관리하고 하루 단위로 공급/수요를 시뮬레이션해 자원 가격에 반영하는 핸들러
/// </summary>
public class MarketDataHandler
{
    private readonly Dictionary<string, MarketActorEntry> _actors = new();
    private readonly GameDataManager _gameDataManager;
    private InitialMarketData _marketSettings;

    public event Action OnMarketUpdated;

    public MarketDataHandler(GameDataManager manager)
    {
        _gameDataManager = manager;
        AutoLoadAllActors();
    }

    /// <summary>
    /// 현재 시장 수수료율을 반환합니다.
    /// </summary>
    public float GetMarketFeeRate()
    {
        return _marketSettings != null ? _marketSettings.marketFeeRate : 0.05f;
    }

    /// <summary>
    /// 마켓 설정을 적용합니다.
    /// </summary>
    public void SetMarketSettings(InitialMarketData settings)
    {
        _marketSettings = settings;
        
        // 기존 액터들의 초기 상태 업데이트
        if (_marketSettings != null)
        {
            foreach (var entry in _actors.Values)
            {
                entry?.ApplyInitialMarketSettings(_marketSettings);
            }
        }
    }

    public void RegisterActor(MarketActorData data)
    {
        if (data == null || string.IsNullOrEmpty(data.id))
        {
            Debug.LogWarning("[MarketDataHandler] Invalid actor data.");
            return;
        }

        if (_actors.ContainsKey(data.id))
        {
            Debug.LogWarning($"[MarketDataHandler] Actor already registered: {data.id}");
            return;
        }

        var entry = new MarketActorEntry(data);
        _actors[data.id] = entry;
        
        // 초기 마켓 설정 적용
        if (_marketSettings != null)
        {
            entry.ApplyInitialMarketSettings(_marketSettings);
        }
    }

    public void RegisterActors(IEnumerable<MarketActorData> actors)
    {
        if (actors == null)
        {
            return;
        }

        foreach (var actor in actors)
        {
            RegisterActor(actor);
        }
    }

    public Dictionary<string, MarketActorEntry> GetAllActors()
    {
        return new Dictionary<string, MarketActorEntry>(_actors);
    }

    public void TickDailyMarket()
    {
        if (_gameDataManager?.Resource == null || _actors.Count == 0)
        {
            return;
        }

        Dictionary<string, ResourceEntry> resourceSnapshot = _gameDataManager.Resource.GetAllResources();
        if (resourceSnapshot == null || resourceSnapshot.Count == 0)
        {
            return;
        }

        var totalSupply = new Dictionary<string, float>();
        var totalDemand = new Dictionary<string, float>();

        // 전일 자산 저장
        foreach (var entry in _actors.Values)
        {
            if (entry?.state == null)
            {
                continue;
            }
            if (entry.state.provider != null)
            {
                entry.state.provider.previousWealth = entry.state.provider.wealth;
            }
            if (entry.state.consumer != null)
            {
                entry.state.consumer.previousWealth = entry.state.consumer.wealth;
            }
        }

        foreach (var entry in _actors.Values)
        {
            MarketActorDynamicAllocator.UpdateAssignments(entry, resourceSnapshot, _marketSettings);
            RefreshActor(entry);
            SimulateProvider(entry, resourceSnapshot, totalSupply);
            SimulateConsumer(entry, resourceSnapshot, totalDemand);
        }

        ApplyPriceAdjustments(resourceSnapshot, totalSupply, totalDemand);
        ApplyBusinessHealthEffects();
        UpdateRevenueRankings();
        
        // 플레이어 자동 거래 실행 (playerTransactionDelta 기반)
        ExecutePlayerAutoTrades(resourceSnapshot);
        OnMarketUpdated?.Invoke();
    }

    /// <summary>
    /// 플레이어 자동 거래를 실행합니다 (playerTransactionDelta 기반)
    /// 예약 시스템을 통해 처리되므로 직접 금액 변경 없이 자원만 처리합니다.
    /// </summary>
    private void ExecutePlayerAutoTrades(Dictionary<string, ResourceEntry> resources)
    {
        if (resources == null || _gameDataManager == null)
        {
            return;
        }

        foreach (var kvp in resources)
        {
            var entry = kvp.Value;
            if (entry?.resourceState == null)
            {
                continue;
            }

            long delta = entry.resourceState.playerTransactionDelta;
            if (delta == 0)
            {
                continue; // 거래 설정이 없으면 스킵
            }

            if (delta > 0)
            {
                // 양수: 매수 (예약 시스템을 통해 처리되므로 자원만 추가)
                ExecutePlayerBuyResourceWithoutPayment(kvp.Key, delta);
            }
            else
            {
                // 음수: 매도 (예약 시스템을 통해 처리되므로 자원만 제거)
                ExecutePlayerSellResourceWithoutPayment(kvp.Key, -delta);
            }
        }
    }

    private void RefreshActor(MarketActorEntry entry)
    {
        if (entry?.data == null)
        {
            return;
        }

        if (entry.data.roles.HasFlag(MarketRoleFlags.Consumer))
        {
            RefreshConsumer(entry);
        }

        if (entry.data.roles.HasFlag(MarketRoleFlags.Provider))
        {
            RefreshProvider(entry);
        }
    }

    private void RefreshConsumer(MarketActorEntry entry)
    {
        var profile = entry.GetConsumerProfile();
        var state = entry.state.consumer;
        if (profile == null || state == null)
        {
            return;
        }

        if (!profile.persistentOrders || state.currentBudget <= 0f)
        {
            state.currentBudget = profile.budgetRange.GetRandomBudget();
        }

        state.desireTimer = Mathf.Max(0f, state.desireTimer - 1f);
        if (state.desireTimer <= 0f)
        {
            state.desireTimer = Mathf.Max(1f, profile.patienceSeconds / 86400f); // 하루 단위로 환산
        }
    }

    private void RefreshProvider(MarketActorEntry entry)
    {
        var state = entry.state.provider;
        if (state == null)
        {
            return;
        }

        state.cooldownTimer = Mathf.Max(0f, state.cooldownTimer - 1f);
        state.productionProgress = 0f;
        state.priceDelta = 0f;
    }

    private void SimulateProvider(
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
            float output = quantity * priceSignal * profile.basePriceModifier * batchModifier;

            // 건강도 적용
            float effectiveOutput = output * state.health;

            AddToMap(totalSupply, preference.resource.id, effectiveOutput);
            UpdateStock(state.stocks, preference.resource, Mathf.RoundToInt(effectiveOutput));

            // 매출 계산: 판매액 = 생산량 * 가격
            float salesRevenue = effectiveOutput * currentPrice;

            // 비용 계산: 생산 비용 (upkeep)
            float productionCost = CalculateProviderCost(profile, resources, effectiveOutput);

            // 순이익 계산 (매출 - 비용)
            float netProfit = salesRevenue - productionCost;

            // 랜덤 손실 (생산 실패)
            float failureChance = _marketSettings != null ? _marketSettings.productionFailureChance : 0.05f;
            float failureLossRate = _marketSettings != null ? _marketSettings.productionFailureLossRate : 0.1f;
            if (UnityEngine.Random.Range(0f, 1f) < failureChance)
            {
                netProfit -= salesRevenue * failureLossRate;
            }

            // 자산 업데이트 (순이익이 양수면 증가, 음수면 감소)
            state.wealth += netProfit;
            state.wealth = Mathf.Max(0f, state.wealth); // 자산은 0 이하로 내려가지 않음

            state.priceDelta = priceSignal - 1f;
        }
    }

    private void SimulateConsumer(
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
            float desiredAmount = appetite * priceSignal * urgencyBoost;

            if (desiredAmount <= 0f)
            {
                continue;
            }

            float finalAmount = desiredAmount;
            if (profile.allowBulkBuying && state.currentBudget > 0f)
            {
                float affordable = state.currentBudget / currentPrice;
                finalAmount = Mathf.Min(desiredAmount, affordable);
                state.currentBudget = Mathf.Max(0f, state.currentBudget - finalAmount * currentPrice);
                UpdateStock(state.holdings, preference.resource, Mathf.RoundToInt(finalAmount));
            }

            // 건강도 적용
            float effectiveAmount = finalAmount * state.health;

            // 구매 비용
            float purchaseCost = effectiveAmount * currentPrice;

            // Consumer는 구매를 "투자"로 간주
            // 만족도가 높으면 가치가 있지만, 구매 비용은 자산을 감소시킴
            float satisfactionRate = _marketSettings != null ? _marketSettings.satisfactionValueRate : 0.1f;
            float purchaseLossRate = _marketSettings != null ? _marketSettings.purchaseCostLossRate : 0.5f;
            float satisfactionValue = state.satisfaction * purchaseCost * satisfactionRate;
            float netValue = satisfactionValue - purchaseCost * purchaseLossRate;

            // 자산 업데이트 (netValue가 양수면 증가, 음수면 감소)
            state.wealth += netValue;
            state.wealth = Mathf.Max(0f, state.wealth);

            AddToMap(totalDemand, preference.resource.id, effectiveAmount);
        }
    }

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
            
            // 변동성 계산: 기본값 × 자원별 배율
            float baseVolatility = _marketSettings != null ? _marketSettings.baseMaxDailySwing : 0.01f;
            float maxSwing = baseVolatility * entry.resourceData.volatilityMultiplier;
            rate = Mathf.Clamp(rate, -maxSwing, maxSwing);

            entry.resourceState.priceChangeRate = rate;
            entry.resourceState.currentValue = Mathf.Max(0.01f, currentPrice * (1f + rate));
            
            entry.resourceState.RecordPrice(entry.resourceState.currentValue);
        }
    }

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

    private static void UpdateStock(List<ResourceStock> stocks, ResourceData resource, long delta)
    {
        if (stocks == null || resource == null || delta == 0)
        {
            return;
        }

        for (int i = 0; i < stocks.Count; i++)
        {
            if (stocks[i]?.resource == resource)
            {
                stocks[i].amount = Math.Max(0, stocks[i].amount + delta);
                return;
            }
        }

        stocks.Add(new ResourceStock
        {
            resource = resource,
            amount = Math.Max(0, delta)
        });
    }

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

        return UnityEngine.Random.Range(sampleMin, sampleMax + 1f);
    }

    // ==================== 플레이어 거래 시스템 ====================

    /// <summary>
    /// 플레이어가 자원을 구매합니다. 구매 시 시장 수요에 즉시 반영됩니다.
    /// </summary>
    /// <param name="resourceId">구매할 자원 ID</param>
    /// <param name="amount">구매할 수량</param>
    /// <returns>성공 시 true, 실패 시 false</returns>
    public bool TryPlayerBuyResource(string resourceId, long amount)
    {
        if (_gameDataManager?.Resource == null || _gameDataManager.Finances == null)
        {
            Debug.LogWarning("[MarketDataHandler] GameDataManager or required handlers are not available.");
            return false;
        }

        if (string.IsNullOrEmpty(resourceId) || amount <= 0)
        {
            Debug.LogWarning($"[MarketDataHandler] Invalid buy request: resourceId={resourceId}, amount={amount}");
            return false;
        }

        var resourceEntry = _gameDataManager.Resource.GetResourceEntry(resourceId);
        if (resourceEntry == null)
        {
            Debug.LogWarning($"[MarketDataHandler] Resource not found: {resourceId}");
            return false;
        }

        float unitPrice = resourceEntry.resourceState.currentValue;
        long baseCost = (long)Mathf.Ceil(unitPrice * amount);
        
        // 시장 수수료 추가
        float feeRate = _marketSettings != null ? _marketSettings.marketFeeRate : 0.05f;
        long marketFee = (long)Mathf.Ceil(baseCost * feeRate);
        long totalCost = baseCost + marketFee;

        // 돈 확인
        if (!_gameDataManager.Finances.HasEnoughCredit(totalCost))
        {
            Debug.LogWarning($"[MarketDataHandler] Insufficient credit for purchase. Required: {totalCost} (base: {baseCost}, fee: {marketFee}), Available: {_gameDataManager.Finances.GetCredit()}");
            return false;
        }

        // 거래 실행
        // 예약된 비용 처리 중이면 비용 차감을 하지 않음 (이미 예약된 비용에서 처리됨)
        bool shouldDeduct = _gameDataManager == null || !_gameDataManager.IsProcessingReservedExpenses;
        
        if (shouldDeduct)
        {
            // 돈 확인
            if (!_gameDataManager.Finances.HasEnoughCredit(totalCost))
            {
                Debug.LogWarning($"[MarketDataHandler] Insufficient credit for purchase. Required: {totalCost} (base: {baseCost}, fee: {marketFee}), Available: {_gameDataManager.Finances.GetCredit()}");
                return false;
            }
            
            _gameDataManager.Finances.TryRemoveCredit(totalCost);
        }
        
        _gameDataManager.Resource.AddResource(resourceId, amount);

        // 시장 수요에 즉시 반영
        ApplyPlayerDemand(resourceEntry, amount);

        Debug.Log($"[MarketDataHandler] Player bought {amount} {resourceEntry.resourceData.displayName} for {totalCost} credits (base: {baseCost}, fee: {marketFee}).");
        OnMarketUpdated?.Invoke();
        return true;
    }

    /// <summary>
    /// 플레이어 자동 거래용 매수 (예약 시스템을 통해 처리되므로 금액 변경 없이 자원만 처리)
    /// </summary>
    private void ExecutePlayerBuyResourceWithoutPayment(string resourceId, long amount)
    {
        if (_gameDataManager?.Resource == null)
        {
            return;
        }

        if (string.IsNullOrEmpty(resourceId) || amount <= 0)
        {
            return;
        }

        var resourceEntry = _gameDataManager.Resource.GetResourceEntry(resourceId);
        if (resourceEntry == null)
        {
            return;
        }

        // 자원 추가
        _gameDataManager.Resource.AddResource(resourceId, amount);

        // 시장 수요에 즉시 반영
        ApplyPlayerDemand(resourceEntry, amount);
    }

    /// <summary>
    /// 플레이어가 자원을 판매합니다. 판매 시 시장 공급에 즉시 반영됩니다.
    /// </summary>
    /// <param name="resourceId">판매할 자원 ID</param>
    /// <param name="amount">판매할 수량</param>
    /// <returns>성공 시 true, 실패 시 false</returns>
    public bool TryPlayerSellResource(string resourceId, long amount)
    {
        if (_gameDataManager?.Resource == null || _gameDataManager.Finances == null)
        {
            Debug.LogWarning("[MarketDataHandler] GameDataManager or required handlers are not available.");
            return false;
        }

        if (string.IsNullOrEmpty(resourceId) || amount <= 0)
        {
            Debug.LogWarning($"[MarketDataHandler] Invalid sell request: resourceId={resourceId}, amount={amount}");
            return false;
        }

        var resourceEntry = _gameDataManager.Resource.GetResourceEntry(resourceId);
        if (resourceEntry == null)
        {
            Debug.LogWarning($"[MarketDataHandler] Resource not found: {resourceId}");
            return false;
        }

        // 자원 확인
        if (!_gameDataManager.Resource.HasEnoughResource(resourceId, amount))
        {
            Debug.LogWarning($"[MarketDataHandler] Insufficient resources for sale. Required: {amount}, Available: {_gameDataManager.Resource.GetResourceQuantity(resourceId)}");
            return false;
        }

        float unitPrice = resourceEntry.resourceState.currentValue;
        long baseRevenue = (long)Mathf.Floor(unitPrice * amount);
        
        // 시장 수수료 차감
        float feeRate = _marketSettings != null ? _marketSettings.marketFeeRate : 0.05f;
        long marketFee = (long)Mathf.Floor(baseRevenue * feeRate);
        long totalRevenue = baseRevenue - marketFee;

        // 거래 실행
        _gameDataManager.Resource.TryRemoveResource(resourceId, amount);
        
        // 예약된 비용 처리 중이면 수익 추가를 하지 않음 (이미 예약된 비용에서 처리됨)
        bool shouldAdd = _gameDataManager == null || !_gameDataManager.IsProcessingReservedExpenses;
        
        if (shouldAdd)
        {
            _gameDataManager.Finances.AddCredit(totalRevenue);
        }

        // 시장 공급에 즉시 반영
        ApplyPlayerSupply(resourceEntry, amount);

        Debug.Log($"[MarketDataHandler] Player sold {amount} {resourceEntry.resourceData.displayName} for {totalRevenue} credits (base: {baseRevenue}, fee: {marketFee}).");
        OnMarketUpdated?.Invoke();
        return true;
    }

    /// <summary>
    /// 플레이어 자동 거래용 매도 (예약 시스템을 통해 처리되므로 금액 변경 없이 자원만 처리)
    /// </summary>
    private void ExecutePlayerSellResourceWithoutPayment(string resourceId, long amount)
    {
        if (_gameDataManager?.Resource == null)
        {
            return;
        }

        if (string.IsNullOrEmpty(resourceId) || amount <= 0)
        {
            return;
        }

        var resourceEntry = _gameDataManager.Resource.GetResourceEntry(resourceId);
        if (resourceEntry == null)
        {
            return;
        }

        // 자원 확인 및 제거
        if (!_gameDataManager.Resource.HasEnoughResource(resourceId, amount))
        {
            return;
        }

        _gameDataManager.Resource.TryRemoveResource(resourceId, amount);

        // 시장 공급에 즉시 반영
        ApplyPlayerSupply(resourceEntry, amount);
    }

    /// <summary>
    /// 플레이어의 구매를 시장 수요에 반영합니다.
    /// </summary>
    private void ApplyPlayerDemand(ResourceEntry resourceEntry, long amount)
    {
        if (resourceEntry?.resourceState == null)
        {
            return;
        }

        // 수요 증가
        resourceEntry.resourceState.lastDemand += amount;

        // 즉시 가격 조정 (시장 전체 공급/수요에 비해 상대적으로 작은 영향)
        float impactRate = _marketSettings != null ? _marketSettings.playerDemandImpact : 0.02f;
        float totalMarketSupply = resourceEntry.resourceState.lastSupply;
        float totalMarketDemand = resourceEntry.resourceState.lastDemand;
        float marketVolume = Mathf.Max(1f, totalMarketSupply + totalMarketDemand);
        
        // 플레이어의 영향력을 시장 규모에 비례하여 감소
        float normalizedImpact = amount / marketVolume;
        float demandImpact = normalizedImpact * impactRate * 100f; // 시장 규모에 비례하여 영향력 감소
        float currentPrice = resourceEntry.resourceState.currentValue;
        float priceAdjustment = demandImpact * resourceEntry.resourceData.marketSensitivity * 0.01f;
        resourceEntry.resourceState.currentValue = Mathf.Max(0.01f, currentPrice * (1f + priceAdjustment));
        resourceEntry.resourceState.RecordPrice(resourceEntry.resourceState.currentValue);
    }

    /// <summary>
    /// 플레이어의 판매를 시장 공급에 반영합니다.
    /// </summary>
    private void ApplyPlayerSupply(ResourceEntry resourceEntry, long amount)
    {
        if (resourceEntry?.resourceState == null)
        {
            return;
        }

        // 공급 증가
        resourceEntry.resourceState.lastSupply += amount;

        // 즉시 가격 조정 (시장 전체 공급/수요에 비해 상대적으로 작은 영향)
        float impactRate = _marketSettings != null ? _marketSettings.playerSupplyImpact : 0.02f;
        float totalMarketSupply = resourceEntry.resourceState.lastSupply;
        float totalMarketDemand = resourceEntry.resourceState.lastDemand;
        float marketVolume = Mathf.Max(1f, totalMarketSupply + totalMarketDemand);
        
        // 플레이어의 영향력을 시장 규모에 비례하여 감소
        float normalizedImpact = amount / marketVolume;
        float supplyImpact = normalizedImpact * impactRate * 100f; // 시장 규모에 비례하여 영향력 감소
        float currentPrice = resourceEntry.resourceState.currentValue;
        float priceAdjustment = -supplyImpact * resourceEntry.resourceData.marketSensitivity * 0.01f;
        resourceEntry.resourceState.currentValue = Mathf.Max(0.01f, currentPrice * (1f + priceAdjustment));
        resourceEntry.resourceState.RecordPrice(resourceEntry.resourceState.currentValue);
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

    /// <summary>
    /// 비즈니스 건강 효과를 적용합니다 (단순화된 건강도 시스템).
    /// </summary>
    private void ApplyBusinessHealthEffects()
    {
        foreach (var entry in _actors.Values)
        {
            if (entry?.state == null)
            {
                continue;
            }

            // Provider 건강 효과
            if (entry.state.provider != null)
            {
                var state = entry.state.provider;
                float wealthChange = state.wealth - state.previousWealth;

                // 자산 증가 시 건강도 회복
                if (wealthChange > 0f)
                {
                    float recovery = _marketSettings != null ? _marketSettings.providerWealthGainHealthRecovery : 0.02f;
                    state.health = Mathf.Min(1f, state.health + recovery);
                }
                // 자산 감소 시 건강도 감소
                else if (wealthChange < 0f)
                {
                    float damage = _marketSettings != null ? _marketSettings.providerWealthLossHealthDamage : 0.05f;
                    state.health = Mathf.Max(0.2f, state.health - damage);
                }

                // 경쟁 페널티 (순위가 낮을수록)
                if (state.rank > 1)
                {
                    float penaltyRate = _marketSettings != null ? _marketSettings.providerRankPenalty : 0.005f;
                    float penalty = (state.rank - 1) * penaltyRate;
                    state.health = Mathf.Max(0.3f, state.health - penalty);
                }

                // 자연 감소 (매우 작게)
                float naturalDecay = _marketSettings != null ? _marketSettings.providerNaturalDecay : 0.002f;
                state.health = Mathf.Max(0.2f, state.health - naturalDecay);
            }

            // Consumer 건강 효과
            if (entry.state.consumer != null)
            {
                var state = entry.state.consumer;
                var profile = entry.GetConsumerProfile();

                // 예산 부족 시 건강도 감소
                float shortageThreshold = _marketSettings != null ? _marketSettings.budgetShortageThreshold : 0.3f;
                if (profile != null && state.currentBudget < profile.budgetRange.min * shortageThreshold)
                {
                    float damage = _marketSettings != null ? _marketSettings.consumerBudgetShortageDamage : 0.03f;
                    state.health = Mathf.Max(0.2f, state.health - damage);
                }
                else
                {
                    // 예산 충분 시 건강도 회복
                    float recovery = _marketSettings != null ? _marketSettings.consumerBudgetSufficientRecovery : 0.01f;
                    state.health = Mathf.Min(1f, state.health + recovery);
                }

                // 만족도에 따른 건강도
                float lerpRate = _marketSettings != null ? _marketSettings.consumerSatisfactionLerp : 0.1f;
                state.health = Mathf.Lerp(state.health, state.satisfaction, lerpRate);
                state.health = Mathf.Clamp(state.health, 0.2f, 1f);
            }
        }
    }

    /// <summary>
    /// 모든 액터의 자산 순위를 계산하고 업데이트합니다.
    /// </summary>
    private void UpdateRevenueRankings()
    {
        // Provider 순위 계산 (자산 기준)
        var providerEntries = new List<(MarketActorEntry entry, float wealth)>();
        foreach (var kvp in _actors)
        {
            var entry = kvp.Value;
            if (entry?.state?.provider != null)
            {
                providerEntries.Add((entry, entry.state.provider.wealth));
            }
        }

        providerEntries.Sort((a, b) => b.wealth.CompareTo(a.wealth)); // 내림차순 정렬
        for (int i = 0; i < providerEntries.Count; i++)
        {
            providerEntries[i].entry.state.provider.rank = i + 1;
        }

        // Consumer 순위 계산 (자산 기준)
        var consumerEntries = new List<(MarketActorEntry entry, float wealth)>();
        foreach (var kvp in _actors)
        {
            var entry = kvp.Value;
            if (entry?.state?.consumer != null)
            {
                consumerEntries.Add((entry, entry.state.consumer.wealth));
            }
        }

        consumerEntries.Sort((a, b) => b.wealth.CompareTo(a.wealth)); // 내림차순 정렬
        for (int i = 0; i < consumerEntries.Count; i++)
        {
            consumerEntries[i].entry.state.consumer.rank = i + 1;
        }
    }

    /// <summary>
    /// 모든 액터를 자산 기준으로 정렬하여 반환합니다.
    /// </summary>
    public List<MarketActorEntry> GetActorsSortedByWealth(bool ascending = false)
    {
        var sortedList = new List<MarketActorEntry>(_actors.Values);
        if (ascending)
        {
            sortedList.Sort((a, b) => a.state.GetWealth().CompareTo(b.state.GetWealth()));
        }
        else
        {
            sortedList.Sort((a, b) => b.state.GetWealth().CompareTo(a.state.GetWealth()));
        }
        return sortedList;
    }

    /// <summary>
    /// 특정 액터의 자산 순위를 반환합니다.
    /// </summary>
    public int GetActorWealthRank(string actorId)
    {
        if (!_actors.TryGetValue(actorId, out var entry))
        {
            return -1;
        }

        var sorted = GetActorsSortedByWealth(false);
        for (int i = 0; i < sorted.Count; i++)
        {
            if (sorted[i].data.id == actorId)
            {
                return i + 1;
            }
        }

        return -1;
    }

    private void AutoLoadAllActors()
    {
#if UNITY_EDITOR
        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:MarketActorData");
        int count = 0;

        foreach (var guid in guids)
        {
            string assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            var data = UnityEditor.AssetDatabase.LoadAssetAtPath<MarketActorData>(assetPath);
            if (data == null)
            {
                continue;
            }

            RegisterActor(data);
            count++;
        }

        Debug.Log($"[MarketDataHandler] Auto load completed: {count} actors registered.");
#else
        Debug.LogWarning("[MarketDataHandler] Auto load is only available in the editor. Register actors manually at runtime.");
#endif
    }
}

