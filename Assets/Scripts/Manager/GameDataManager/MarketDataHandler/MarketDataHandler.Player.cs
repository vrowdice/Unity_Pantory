using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/// <summary>
/// 플레이어 거래 시스템
/// </summary>
public partial class MarketDataHandler
{
    /// <summary>
    /// 플레이어 자동 거래를 실행합니다 (playerTransactionDelta 기반)
    /// 예약 시스템을 통해 처리되므로 직접 금액 변경 없이 자원만 처리합니다.
    /// </summary>
    private void ExecutePlayerAutoTrades(Dictionary<string, ResourceEntry> resources)
    {
        if (resources == null || _dataManager == null) return;

        foreach (KeyValuePair<string, ResourceEntry> kvp in resources)
        {
            ResourceEntry entry = kvp.Value;
            if (entry == null || entry.resourceState == null) continue;

            long delta = entry.resourceState.playerTransactionDelta;

            if (delta == 0) continue;

            if (delta > 0)
            {
                ExecutePlayerBuyResourceWithoutPayment(kvp.Key, delta);
            }
            else
            {
                long requestedAmount = -delta;
                ExecutePlayerSellResourceWithoutPayment(kvp.Key, requestedAmount);
            }
        }
    }

    /// <summary>
    /// 플레이어가 자원을 구매합니다. 구매 시 시장 수요에 즉시 반영됩니다.
    /// </summary>
    /// <param name="resourceId">구매할 자원 ID</param>
    /// <param name="amount">구매할 수량</param>
    /// <returns>성공 시 true, 실패 시 false</returns>
    public bool TryPlayerBuyResource(string resourceId, long amount)
    {
        ResourceEntry resourceEntry = _dataManager.Resource.GetResourceEntry(resourceId);

        float unitPrice = resourceEntry.resourceState.currentValue;
        long baseCost = (long)Mathf.Ceil(unitPrice * amount);
        
        // 시장 수수료 추가
        float feeRate = _marketSettings != null ? _marketSettings.marketFeeRate : 0.05f;
        long marketFee = (long)Mathf.Ceil(baseCost * feeRate);
        long totalCost = baseCost + marketFee;

        if (!_dataManager.Finances.HasEnoughCredit(totalCost))
        {
            Debug.LogWarning($"[MarketDataHandler] Insufficient credit for purchase. Required: {totalCost} (base: {baseCost}, fee: {marketFee}), Available: {_dataManager.Finances.GetCredit()}");
            return false;
        }
        
        // 자원 이동
        _dataManager.Resource.ModifyMarketInventory(resourceId, -amount);
        _dataManager.Resource.ModifyPlayerInventory(resourceId, amount);

        // 시장 수요에 즉시 반영
        ApplyPlayerDemand(resourceEntry, amount);
        Debug.Log($"[MarketDataHandler] Player bought {amount} {resourceEntry.resourceData.displayName} for {totalCost} credits (base: {baseCost}, fee: {marketFee}).");
        OnMarketUpdated?.Invoke();
        return true;
    }

    /// <summary>
    /// 플레이어 자동 거래용 매수
    /// </summary>
    private void ExecutePlayerBuyResourceWithoutPayment(string resourceId, long amount)
    {
        if (string.IsNullOrEmpty(resourceId) || amount <= 0)
        {
            return;
        }

        var resourceEntry = _dataManager.Resource.GetResourceEntry(resourceId);
        if (resourceEntry == null)
        {
            return;
        }

        _dataManager.Resource.ModifyMarketInventory(resourceId, -amount);
        _dataManager.Resource.ModifyPlayerInventory(resourceId, amount);
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
        // 플레이어 재고 확인
        if (!_dataManager.Resource.HasEnoughPlayerResource(resourceId, amount))
        {
            Debug.LogWarning($"[MarketDataHandler] Insufficient resources in player storage. Required: {amount}, Available: {_dataManager.Resource.GetPlayerResourceQuantity(resourceId)}");
            return false;
        }

        ResourceEntry resourceEntry = _dataManager.Resource.GetResourceEntry(resourceId);

        float unitPrice = resourceEntry.resourceState.currentValue;
        long baseRevenue = (long)Mathf.Floor(unitPrice * amount);
        
        // 시장 수수료 차감
        float feeRate = _marketSettings != null ? _marketSettings.marketFeeRate : 0.05f;
        long marketFee = (long)Mathf.Floor(baseRevenue * feeRate);
        long totalRevenue = baseRevenue - marketFee;

        // 거래 실행: 자원 이동: 플레이어(감소) -> 시장(증가)
        _dataManager.Resource.ModifyPlayerInventory(resourceId, -amount);
        _dataManager.Resource.ModifyMarketInventory(resourceId, amount);

        // 시장 공급에 즉시 반영
        ApplyPlayerSupply(resourceEntry, amount);
        Debug.Log($"[MarketDataHandler] Player sold {amount} {resourceEntry.resourceData.displayName} for {totalRevenue} credits (base: {baseRevenue}, fee: {marketFee}).");
        OnMarketUpdated?.Invoke();
        return true;
    }

    /// <summary>
    /// 플레이어 자동 거래용 매도 (예약 시스템을 통해 처리되므로 금액 변경 없이 자원만 처리)
    /// </summary>
    /// <returns>실제 판매된 수량 (재고 부족 시 요청량보다 적을 수 있음)</returns>
    private long ExecutePlayerSellResourceWithoutPayment(string resourceId, long amount)
    {
        if (string.IsNullOrEmpty(resourceId) || amount <= 0)
        {
            return 0;
        }

        ResourceEntry resourceEntry = _dataManager.Resource.GetResourceEntry(resourceId);
        long availableInventory = _dataManager.Resource.GetPlayerResourceQuantity(resourceId);
        if (availableInventory <= 0)
        {
            Debug.LogWarning($"[MarketDataHandler] Cannot sell {resourceId}: player has no inventory (requested: {amount}, available: {availableInventory})");
            return 0;
        }

        // 실제 판매 가능 수량 = 요청량과 보유량 중 작은 값
        long actualSellAmount = System.Math.Min(amount, availableInventory);
        
        if (actualSellAmount < amount)
        {
            Debug.LogWarning($"[MarketDataHandler] Insufficient player inventory for {resourceId}. Requested: {amount}, Available: {availableInventory}, Selling: {actualSellAmount}");
        }

        _dataManager.Resource.ModifyPlayerInventory(resourceId, -actualSellAmount);
        _dataManager.Resource.ModifyMarketInventory(resourceId, actualSellAmount);
        ApplyPlayerSupply(resourceEntry, actualSellAmount);
        
        return actualSellAmount;
    }

    /// <summary>
    /// 플레이어의 구매를 시장 수요에 반영합니다.
    /// </summary>
    private void ApplyPlayerDemand(ResourceEntry resourceEntry, long amount)
    {
        // 1. 누적 수요 기록 (다음 날 시뮬레이션에 반영)
        resourceEntry.resourceState.accumulatedPlayerDemand += amount;

        // 2. 즉시 가격 조정
        float impactRate = _marketSettings.playerDemandImpact;
        float totalMarketSupply = resourceEntry.resourceState.lastSupply;
        float totalMarketDemand = resourceEntry.resourceState.lastDemand;
        float marketVolume = Mathf.Max(1f, totalMarketSupply + totalMarketDemand);
        
        // 플레이어의 영향력을 시장 규모에 비례하여 감소
        float normalizedImpact = amount / marketVolume;
        float baseDemandImpact = normalizedImpact * impactRate * 100f;
        
        // 3. [개선] 시장 장악력에 따른 지수적 가격 상승
        float currentMarketInventory = resourceEntry.resourceState.count;
        float dominationRatio = (float)amount / Mathf.Max(1f, currentMarketInventory);

        // 시장 재고의 10% 이상을 사면 가격 충격 발생
        float monopolyImpact = 0f;
        if (dominationRatio > 0.1f)
        {
            // 제곱 비례로 충격 (많이 살수록 기하급수적으로 비싸짐)
            monopolyImpact = dominationRatio * dominationRatio * 5.0f;
        }
        
        // 기본 영향력 + 독점 충격
        float finalImpact = baseDemandImpact + monopolyImpact;
        float currentPrice = resourceEntry.resourceState.currentValue;
        float priceAdjustment = finalImpact * resourceEntry.resourceData.marketSensitivity * 0.01f;
        resourceEntry.resourceState.currentValue = Mathf.Max(0.01f, currentPrice * (1f + priceAdjustment));
        resourceEntry.resourceState.RecordPrice(resourceEntry.resourceState.currentValue);
        
        // 즉시 반영용 (레거시 호환)
        resourceEntry.resourceState.lastDemand += amount;
    }

    /// <summary>
    /// 플레이어의 판매를 시장 공급에 반영합니다.
    /// </summary>
    private void ApplyPlayerSupply(ResourceEntry resourceEntry, long amount)
    {
        // 1. 누적 공급 기록 (다음 날 시뮬레이션에 반영)
        resourceEntry.resourceState.accumulatedPlayerSupply += amount;

        // 2. 즉시 가격 조정
        float impactRate = _marketSettings != null ? _marketSettings.playerSupplyImpact : 0.02f;
        float totalMarketSupply = resourceEntry.resourceState.lastSupply;
        float totalMarketDemand = resourceEntry.resourceState.lastDemand;
        float marketVolume = Mathf.Max(1f, totalMarketSupply + totalMarketDemand);
        
        // 플레이어의 영향력을 시장 규모에 비례하여 감소
        float normalizedImpact = amount / marketVolume;
        float supplyImpact = normalizedImpact * impactRate * 100f;
        float currentPrice = resourceEntry.resourceState.currentValue;
        float priceAdjustment = -supplyImpact * resourceEntry.resourceData.marketSensitivity * 0.01f;
        resourceEntry.resourceState.currentValue = Mathf.Max(0.01f, currentPrice * (1f + priceAdjustment));
        resourceEntry.resourceState.RecordPrice(resourceEntry.resourceState.currentValue);
        
        // 즉시 반영용 (레거시 호환)
        resourceEntry.resourceState.lastSupply += amount;
    }
}

