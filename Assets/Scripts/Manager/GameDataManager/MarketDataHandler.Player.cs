using System.Collections.Generic;
using UnityEngine;

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
}

