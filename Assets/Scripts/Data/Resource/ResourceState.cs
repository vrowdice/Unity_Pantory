using System;
using UnityEngine;

[Serializable]
public class ResourceState
{
    public long count;                 // 보유 수량
    public float currentValue;         // 현재 가격
    public float priceChangeRate;      // 시장 압력에 따른 변동률
    public bool isEnchanted;           // 마법 주문 적용 여부
    public long deltaCount;            // 최근 변화 수량

    [Header("Market Modifiers")]
    public float permanentModifier;    // 영구적인 가치 조정 (기술, 연구 등)
    public float temporaryModifier;    // 일시적 가치 조정 (이벤트)
    public float temporaryModifierDuration; // 남은 일 수

    [Header("Diagnostics")]
    public float lastSupply;
    public float lastDemand;
    public float lastImbalance;
    public float lastNormalizedImbalance;

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
        currentValue = Mathf.Max(0.01f, data.baseValue);
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
        deltaCount = 0;
        permanentModifier = 1f;
        temporaryModifier = 1f;
        temporaryModifierDuration = 0f;
        lastSupply = 0f;
        lastDemand = 0f;
        lastImbalance = 0f;
        lastNormalizedImbalance = 0f;
    }
}
