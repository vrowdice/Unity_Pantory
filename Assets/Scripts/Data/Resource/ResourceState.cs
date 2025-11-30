using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ResourceState
{
    [Header("Market Inventory")]
    public long count;                 // 시장 전체 유통 재고 (Market Inventory)
    
    [Header("Player Inventory")]
    public long playerInventory;       // 플레이어 개인 창고 (Player Storage)
    public long playerInventoryDelta;  // 플레이어 재고 변화량 (생산/소비/거래)
    
    public float currentValue;         // 현재 가격
    public float priceChangeRate;      // 시장 압력에 따른 변동률
    public bool isEnchanted;           // 마법 주문 적용 여부
    public long deltaCount;            // 최근 변화 수량 (시장 재고용)

    [Header("Player Transactions")]
    public long playerTransactionDelta; // 플레이어 거래 델타 (매수 +, 매도 -)
    public float accumulatedPlayerDemand; // 하루 동안 누적된 플레이어 구매량 (시장 영향력 계산용)
    public float accumulatedPlayerSupply; // 하루 동안 누적된 플레이어 판매량 (시장 영향력 계산용)

    [Header("Market Modifiers")]
    public float permanentModifier;    // 영구적인 가치 조정 (기술, 연구 등)
    public float temporaryModifier;    // 일시적 가치 조정 (이벤트)
    public float temporaryModifierDuration; // 남은 일 수

    [Header("Diagnostics")]
    public float lastSupply;
    public float lastDemand;
    public float lastImbalance;
    public float lastNormalizedImbalance;

    public const int PriceHistoryCapacity = 60;
    [SerializeField] private List<float> _priceHistory = new List<float>(PriceHistoryCapacity);
    public IReadOnlyList<float> PriceHistory => _priceHistory;

    public ResourceState()
    {
        InitializeDefaults();
    }

    public void InitializeFromData(ResourceData data)
    {
        InitializeDefaults();

        if (data == null)
        {
            return;
        }

        count = data.initialAmount;
        // Initialize price to match effective baseline to avoid immediate mean reversion pressure
        float effectiveBaseline = GetEffectiveBaseline(data.baseValue);
        currentValue = Mathf.Max(0.01f, effectiveBaseline);
        RecordPrice(currentValue);
    }

    public float GetEffectiveBaseline(float baseValue)
    {
        float modifier = permanentModifier;
        if (temporaryModifierDuration > 0f)
        {
            modifier *= temporaryModifier;
        }

        return Mathf.Max(0.01f, baseValue * modifier);
    }

    public void AdvanceOneDay()
    {
        if (temporaryModifierDuration <= 0f)
        {
            return;
        }

        temporaryModifierDuration = Mathf.Max(0f, temporaryModifierDuration - 1f);
        if (temporaryModifierDuration <= 0f)
        {
            temporaryModifier = 1f;
        }
    }

    public void ApplyTemporaryModifier(float multiplier, float durationDays)
    {
        temporaryModifier = Mathf.Max(0.01f, multiplier);
        temporaryModifierDuration = Mathf.Max(0f, durationDays);
    }

    public void ApplyPermanentModifier(float multiplier)
    {
        permanentModifier = Mathf.Max(0.01f, multiplier);
    }

    private void InitializeDefaults()
    {
        currentValue = 0f;
        priceChangeRate = 0f;
        count = 0;
        playerInventory = 0;
        playerInventoryDelta = 0;
        deltaCount = 0;
        playerTransactionDelta = 0;
        accumulatedPlayerDemand = 0f;
        accumulatedPlayerSupply = 0f;
        permanentModifier = 1f;
        temporaryModifier = 1f;
        temporaryModifierDuration = 0f;
        lastSupply = 0f;
        lastDemand = 0f;
        lastImbalance = 0f;
        lastNormalizedImbalance = 0f;
        if (_priceHistory == null)
        {
            _priceHistory = new List<float>(PriceHistoryCapacity);
        }
        else
        {
            _priceHistory.Clear();
        }
    }

    /// <summary>
    /// 플레이어 매수량을 추가합니다 (양수로 추가)
    /// </summary>
    public void AddPlayerBuyAmount(long amount)
    {
        if (amount > 0)
        {
            playerTransactionDelta += amount;
        }
    }

    /// <summary>
    /// 플레이어 매도량을 추가합니다 (음수로 추가)
    /// </summary>
    public void AddPlayerSellAmount(long amount)
    {
        if (amount > 0)
        {
            playerTransactionDelta -= amount;
        }
    }

    /// <summary>
    /// 플레이어 거래 정보를 초기화합니다 (하루가 지날 때 호출)
    /// </summary>
    public void ResetPlayerTransactions()
    {
        playerTransactionDelta = 0;
        // accumulatedPlayerDemand와 accumulatedPlayerSupply는 ApplyPriceAdjustments에서 사용 후 초기화
    }

    public void RecordPrice(float price)
    {
        if (_priceHistory == null)
        {
            _priceHistory = new List<float>(PriceHistoryCapacity);
        }

        float clampedPrice = Mathf.Max(0.01f, price);
        _priceHistory.Add(clampedPrice);
        if (_priceHistory.Count > PriceHistoryCapacity)
        {
            _priceHistory.RemoveAt(0);
        }
    }
}
