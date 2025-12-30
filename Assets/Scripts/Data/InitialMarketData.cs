using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 초기 시장 데이터를 저장하는 ScriptableObject
/// Inspector를 통해 시장 밸런싱을 조정할 수 있습니다.
/// </summary>
[CreateAssetMenu(fileName = "InitialMarketData", menuName = "Game Data/Initial Market Data", order = 2)]
public class InitialMarketData : ScriptableObject
{
    [Header("Transaction Fees")]
    [Range(0f, 0.2f)]
    [Tooltip("시장 거래 수수료율 (0.05 = 5%)")]
    public float marketFeeRate = 0.05f;

    [Header("Market Actor Initial State")]
    [Tooltip("모든 시장 액터의 초기 자산")]
    public float initialActorWealth = 0f;
    [Range(0f, 1f)]
    [Tooltip("모든 시장 액터의 초기 건강도 (0-1)")]
    public float initialActorHealth = 1f;

    [Header("Provider Settings")]
    [Range(0f, 0.2f)]
    [Tooltip("생산 실패 확률 (0.05 = 5%)")]
    public float productionFailureChance = 0.05f;
    [Range(0f, 0.5f)]
    [Tooltip("생산 실패 시 손실률 (0.1 = 수익의 10%)")]
    public float productionFailureLossRate = 0.1f;
    [Range(0f, 1f)]
    [Tooltip("유지비 리소스가 지정되지 않았을 때 기본 유지비율 (0.2 = 생산량의 20%)")]
    public float defaultUpkeepRate = 0.2f;
    [Tooltip("생산 비용 스케일링 계수 (100단위당 비용)")]
    public float productionCostScale = 100f;

    [Header("Consumer Settings")]
    [Range(0f, 1f)]
    [Tooltip("만족도 가치 비율 (0.1 = 구매 비용의 10%)")]
    public float satisfactionValueRate = 0.1f;
    [Range(0f, 1f)]
    [Tooltip("구매 비용 손실률 (0.5 = 구매 비용의 50%)")]
    public float purchaseCostLossRate = 0.5f;
    [Range(0f, 1f)]
    [Tooltip("예산 부족 임계값 (0.3 = 최소 예산의 30%)")]
    public float budgetShortageThreshold = 0.3f;

    [Header("Health System")]
    [Range(0f, 0.1f)]
    [Tooltip("공급자 자산 증가 시 건강도 회복량")]
    public float providerWealthGainHealthRecovery = 0.02f;
    [Range(0f, 0.1f)]
    [Tooltip("공급자 자산 감소 시 건강도 손상량")]
    public float providerWealthLossHealthDamage = 0.05f;
    [Range(0f, 0.01f)]
    [Tooltip("공급자 순위가 1위보다 낮을 때 순위당 건강도 패널티")]
    public float providerRankPenalty = 0.005f;
    [Range(0f, 0.01f)]
    [Tooltip("공급자 일일 자연 건강도 감소량")]
    public float providerNaturalDecay = 0.002f;
    [Range(0f, 0.1f)]
    [Tooltip("소비자 예산 부족 시 건강도 손상량")]
    public float consumerBudgetShortageDamage = 0.03f;
    [Range(0f, 0.1f)]
    [Tooltip("소비자 예산 충분 시 건강도 회복량")]
    public float consumerBudgetSufficientRecovery = 0.01f;
    [Range(0f, 1f)]
    [Tooltip("소비자 건강도가 만족도로 보간되는 비율")]
    public float consumerSatisfactionLerp = 0.1f;

    [Header("Player Trade Impact")]
    [Range(0f, 1f)]
    [Tooltip("플레이어 거래가 시장 수요에 미치는 영향 (0.1 = 10%)")]
    public float playerDemandImpact = 0.1f;
    [Range(0f, 1f)]
    [Tooltip("플레이어 거래가 시장 공급에 미치는 영향 (0.1 = 10%)")]
    public float playerSupplyImpact = 0.1f;

    [Header("Price Signal")]
    [Range(0f, 1f)]
    [Tooltip("최소 가격 신호 값")]
    public float minPriceSignal = 0.1f;

    [Header("Resource Volatility")]
    [Range(0.001f, 0.1f)]
    [Tooltip("모든 리소스의 기본 최대 일일 가격 변동폭 (0.01 = 일일 1%)")]
    public float baseMaxDailySwing = 0.01f;

    [Header("Batch Selling")]
    [Range(0f, 1f)]
    [Tooltip("일괄 판매가 허용되지 않을 때 수정자 (0.5 = 50%)")]
    public float noBatchSellingModifier = 0.5f;

    [Header("Actor Production/Consumption Ranges")]
    [Tooltip("원자재 리소스의 기본 생산 범위 (최소, 최대)")]
    public Vector2 rawProductionRange = new Vector2(800f, 1500f);
    [Tooltip("금속 리소스의 기본 생산 범위 (최소, 최대)")]
    public Vector2 metalProductionRange = new Vector2(600f, 1200f);
    [Tooltip("목재 리소스의 기본 생산 범위 (최소, 최대)")]
    public Vector2 woodProductionRange = new Vector2(700f, 1300f);
    [Tooltip("도구 리소스의 기본 생산 범위 (최소, 최대)")]
    public Vector2 toolProductionRange = new Vector2(150f, 350f);
    [Tooltip("무기 리소스의 기본 생산 범위 (최소, 최대)")]
    public Vector2 weaponProductionRange = new Vector2(100f, 300f);
    [Tooltip("기타 리소스의 기본 생산 범위 (최소, 최대)")]
    public Vector2 otherProductionRange = new Vector2(400f, 900f);

    [Header("Actor Scale Multipliers")]
    [Tooltip("소규모 액터의 생산/소비 배수")]
    public float smallScaleMultiplier = 1.2f;
    [Tooltip("대규모 액터의 생산/소비 배수")]
    public float largeScaleMultiplier = 3.0f;
    [Tooltip("중규모 액터의 생산/소비 배수 (기본값: 2.0)")]
    public float mediumScaleMultiplier = 2.0f;

    [Header("Consumer Settings")]
    [Range(0f, 2f)]
    [Tooltip("소비자 소비 배수 (생산량 대비, 1.2 = 생산량의 120%, 수요 창출)")]
    public float consumerConsumptionMultiplier = 1.2f;

    [Header("Consumer Budget Ranges")]
    [Tooltip("소규모 소비자의 예산 범위 (최소, 최대) - 구매력 향상을 위해 증가")]
    public Vector2 smallConsumerBudget = new Vector2(2000f, 4000f);  // 1200-2400 -> 2000-4000
    [Tooltip("중규모 소비자의 예산 범위 (최소, 최대) - 구매력 향상을 위해 증가")]
    public Vector2 mediumConsumerBudget = new Vector2(4000f, 7500f);  // 2400-4500 -> 4000-7500
    [Tooltip("대규모 소비자의 예산 범위 (최소, 최대) - 구매력 향상을 위해 증가")]
    public Vector2 largeConsumerBudget = new Vector2(9000f, 16000f);  // 5400-9600 -> 9000-16000

    [Header("System Actor (General Population)")]
    [Tooltip("시스템 인구 액터의 예산 범위 (최소, 최대)")]
    public Vector2 systemPopulaceBudget = new Vector2(5000f, 10000f);
    [Tooltip("시스템 인구 액터의 자산 (무한 예산)")]
    public float systemPopulaceWealth = 1000000f;
    [Tooltip("시스템 인구의 원하는 리소스 수량 범위 (최소, 최대)")]
    public Vector2 systemPopulaceQuantityRange = new Vector2(50f, 150f);
    [Tooltip("시스템 인구의 가격 민감도 (높을수록 가격에 더 민감)")]
    [Range(0f, 5f)]
    public float systemPopulacePriceSensitivity = 2.0f;
    [Tooltip("시스템 인구의 긴급도 (0 = 긴급하지 않음, 1 = 매우 긴급)")]
    [Range(0f, 1f)]
    public float systemPopulaceUrgency = 0.0f;
    [Tooltip("시스템 인구 액터의 기본 자산 (설정 파일 누락 시 사용)")]
    public float defaultPopulaceWealth = 1000000f;
    [Tooltip("대규모 액터의 최소 생존 자산 임계값")]
    public float minSurvivalWealthLarge = 100000f;
    [Tooltip("소규모 액터의 최소 생존 자산 임계값")]
    public float minSurvivalWealthSmall = 10000f;

    [Header("Trade Port (Price Stabilization)")]
    [Tooltip("무역항 개입 가격 배수 임계값 (1.3 = 기본 가격의 130%)")]
    [Range(1.1f, 2f)]
    public float tradePortPriceThreshold = 1.3f;
    [Tooltip("수요 대비 공급 증가 비율 (0.5 = 수요의 50%)")]
    [Range(0.1f, 1f)]
    public float tradePortSupplyBoostRatio = 0.5f;
    [Tooltip("최소 공급 증가량")]
    public float tradePortMinSupply = 100f;
    [Tooltip("무역항 개입 시 가격 하락률 (0.9 = 10% 하락)")]
    [Range(0.5f, 1f)]
    public float tradePortPriceDropRate = 0.9f;
    [Tooltip("재고가 부족할 때 비상 공급량")]
    public float tradePortEmergencySupply = 100f;
    [Tooltip("수출 임계값: 수출하려면 공급이 수요의 이 배수여야 함 (2.0 = 2배)")]
    [Range(1.5f, 5f)]
    public float tradePortExportSurplusRatio = 2.0f;
    [Tooltip("수출 가격 임계값: 기본 가격의 이 비율 이하일 때만 수출 (0.8 = 80%)")]
    [Range(0.5f, 1f)]
    public float tradePortExportPriceThreshold = 0.8f;
    [Tooltip("수출 덤핑 비율: 잉여분 중 수출할 비율 (0.5 = 50%)")]
    [Range(0.1f, 1f)]
    public float tradePortExportDumpRatio = 0.5f;

    [Header("Stimulus Packages (Government Subsidies)")]
    [Tooltip("보조금 자격을 위한 자산 임계값")]
    public float stimulusWealthThreshold = 1000f;
    [Tooltip("어려움을 겪는 액터에게 지급하는 보조금 금액")]
    public float stimulusSubsidyAmount = 500f;
    [Tooltip("보조금 수령 시 건강도 패널티 (좀비 기업 지표)")]
    [Range(0f, 0.2f)]
    public float stimulusHealthPenalty = 0.05f;
    [Tooltip("재난 구호를 위한 예산 임계값")]
    public float stimulusBudgetThreshold = 100f;
    [Tooltip("예산이 낮은 액터에게 지급하는 재난 구호 금액")]
    public float stimulusDisasterRelief = 500f;

    [Header("Initial Market Seeding")]
    [Tooltip("모든 리소스의 초기 리소스 수량")]
    public long initialResourceCount = 1000L;
    [Tooltip("이전 날 공급을 시뮬레이션하기 위한 초기 lastSupply 값")]
    public float initialLastSupply = 500f;
    [Tooltip("모든 액터의 초기 자산 보너스")]
    public float initialWealthBonus = 10000f;

    [Header("Budget Ratios (Auto-calculated budgets)")]
    [Tooltip("소규모 액터의 예산 비율 (자산의 백분율)")]
    [Range(0f, 1f)]
    public float smallBudgetRatio = 0.5f;
    [Tooltip("중규모 액터의 예산 비율 (자산의 백분율)")]
    [Range(0f, 1f)]
    public float mediumBudgetRatio = 0.55f;
    [Tooltip("대규모 액터의 예산 비율 (자산의 백분율)")]
    [Range(0f, 1f)]
    public float largeBudgetRatio = 0.6f;
    [Tooltip("소규모 액터의 최소 예산")]
    public float smallMinBudget = 500f;
    [Tooltip("중규모 액터의 최소 예산")]
    public float mediumMinBudget = 1000f;
    [Tooltip("대규모 액터의 최소 예산")]
    public float largeMinBudget = 2000f;

    [Header("Price Adjustment")]
    [Tooltip("가격이 크게 벗어날 때 평균 회귀 배수 (0.5 = 중간)")]
    [Range(0.1f, 2f)]
    public float meanReversionMultiplier = 0.5f;
    [Tooltip("가격 상한을 초과할 때 최대 가격 하락률 (0.2 = 일일 20% 하락)")]
    [Range(0.05f, 0.5f)]
    public float maxPriceDropRate = 0.2f;
    [Tooltip("가격 상한 근처일 때 일반 가격 하락률 (0.1 = 일일 10% 하락)")]
    [Range(0.05f, 0.3f)]
    public float normalPriceDropRate = 0.1f;
    [Tooltip("공격적 가격 하락을 위한 초과 비율 임계값 (1.5 = 최대 가격의 150%)")]
    [Range(1.2f, 2f)]
    public float overRatioThreshold = 1.5f;

    [Header("Price Resistance")]
    [Tooltip("가격 저항 패널티를 줄이기 위한 긴급도 임계값 (0.8 = 80% 긴급도)")]
    [Range(0.5f, 1f)]
    public float priceResistanceUrgencyThreshold = 0.8f;
    [Tooltip("높은 긴급도 액터의 최소 저항 패널티 (0.5 = 50% 유지)")]
    [Range(0.1f, 1f)]
    public float minResistancePenalty = 0.5f;

    [Header("War State")]
    [Tooltip("전쟁 중 군사 액터의 예산 배수 (10 = 예산의 10배)")]
    [Range(2f, 20f)]
    public float warBudgetMultiplier = 10f;
    [Tooltip("전쟁 중 군사 액터의 긴급도 (1.0 = 최대)")]
    [Range(0.5f, 1f)]
    public float warUrgency = 1.0f;
    [Tooltip("전쟁 중 군사 액터의 가격 민감도 (0.1 = 가격에 둔감)")]
    [Range(0f, 0.5f)]
    public float warPriceSensitivity = 0.1f;
    [Tooltip("전쟁 중 민간인 예산 감소 (0.5 = 정상의 50%)")]
    [Range(0.1f, 1f)]
    public float civilianBudgetReduction = 0.5f;
    [Tooltip("평화 시 액터의 기본 긴급도 (0.25 = 낮은 긴급도)")]
    [Range(0f, 0.5f)]
    public float peaceUrgency = 0.25f;
    [Tooltip("평화 시 액터의 기본 가격 민감도 (0.55 = 중간)")]
    [Range(0.3f, 1f)]
    public float peacePriceSensitivity = 0.55f;

    /// <summary>
    /// Editor에서 값 검증 (유효하지 않은 값 방지)
    /// </summary>
    private void OnValidate()
    {
        marketFeeRate = Mathf.Clamp(marketFeeRate, 0f, 0.2f);
        initialActorHealth = Mathf.Clamp01(initialActorHealth);
        productionFailureChance = Mathf.Clamp(productionFailureChance, 0f, 0.2f);
        productionFailureLossRate = Mathf.Clamp(productionFailureLossRate, 0f, 0.5f);
        defaultUpkeepRate = Mathf.Clamp01(defaultUpkeepRate);
        satisfactionValueRate = Mathf.Clamp01(satisfactionValueRate);
        purchaseCostLossRate = Mathf.Clamp01(purchaseCostLossRate);
        budgetShortageThreshold = Mathf.Clamp01(budgetShortageThreshold);
        minPriceSignal = Mathf.Clamp01(minPriceSignal);
        noBatchSellingModifier = Mathf.Clamp01(noBatchSellingModifier);
        consumerConsumptionMultiplier = Mathf.Clamp(consumerConsumptionMultiplier, 0f, 2f);
        
        // Ensure production ranges are valid (min <= max)
        if (rawProductionRange.x > rawProductionRange.y) rawProductionRange = new Vector2(rawProductionRange.y, rawProductionRange.x);
        if (metalProductionRange.x > metalProductionRange.y) metalProductionRange = new Vector2(metalProductionRange.y, metalProductionRange.x);
        if (woodProductionRange.x > woodProductionRange.y) woodProductionRange = new Vector2(woodProductionRange.y, woodProductionRange.x);
        if (toolProductionRange.x > toolProductionRange.y) toolProductionRange = new Vector2(toolProductionRange.y, toolProductionRange.x);
        if (weaponProductionRange.x > weaponProductionRange.y) weaponProductionRange = new Vector2(weaponProductionRange.y, weaponProductionRange.x);
        if (otherProductionRange.x > otherProductionRange.y) otherProductionRange = new Vector2(otherProductionRange.y, otherProductionRange.x);
        
        // Ensure budget ranges are valid
        if (smallConsumerBudget.x > smallConsumerBudget.y) smallConsumerBudget = new Vector2(smallConsumerBudget.y, smallConsumerBudget.x);
        if (mediumConsumerBudget.x > mediumConsumerBudget.y) mediumConsumerBudget = new Vector2(mediumConsumerBudget.y, mediumConsumerBudget.x);
        if (largeConsumerBudget.x > largeConsumerBudget.y) largeConsumerBudget = new Vector2(largeConsumerBudget.y, largeConsumerBudget.x);
        if (systemPopulaceBudget.x > systemPopulaceBudget.y) systemPopulaceBudget = new Vector2(systemPopulaceBudget.y, systemPopulaceBudget.x);
        if (systemPopulaceQuantityRange.x > systemPopulaceQuantityRange.y) systemPopulaceQuantityRange = new Vector2(systemPopulaceQuantityRange.y, systemPopulaceQuantityRange.x);
        
        // Clamp ranges
        systemPopulacePriceSensitivity = Mathf.Clamp(systemPopulacePriceSensitivity, 0f, 5f);
        systemPopulaceUrgency = Mathf.Clamp01(systemPopulaceUrgency);
        tradePortPriceThreshold = Mathf.Clamp(tradePortPriceThreshold, 1.1f, 2f);
        tradePortSupplyBoostRatio = Mathf.Clamp01(tradePortSupplyBoostRatio);
        tradePortPriceDropRate = Mathf.Clamp(tradePortPriceDropRate, 0.5f, 1f);
        tradePortExportSurplusRatio = Mathf.Clamp(tradePortExportSurplusRatio, 1.5f, 5f);
        tradePortExportPriceThreshold = Mathf.Clamp(tradePortExportPriceThreshold, 0.5f, 1f);
        tradePortExportDumpRatio = Mathf.Clamp01(tradePortExportDumpRatio);
        stimulusHealthPenalty = Mathf.Clamp(stimulusHealthPenalty, 0f, 0.2f);
        smallBudgetRatio = Mathf.Clamp01(smallBudgetRatio);
        mediumBudgetRatio = Mathf.Clamp01(mediumBudgetRatio);
        largeBudgetRatio = Mathf.Clamp01(largeBudgetRatio);
        meanReversionMultiplier = Mathf.Clamp(meanReversionMultiplier, 0.1f, 2f);
        maxPriceDropRate = Mathf.Clamp(maxPriceDropRate, 0.05f, 0.5f);
        normalPriceDropRate = Mathf.Clamp(normalPriceDropRate, 0.05f, 0.3f);
        overRatioThreshold = Mathf.Clamp(overRatioThreshold, 1.2f, 2f);
        priceResistanceUrgencyThreshold = Mathf.Clamp(priceResistanceUrgencyThreshold, 0.5f, 1f);
        minResistancePenalty = Mathf.Clamp01(minResistancePenalty);
        warBudgetMultiplier = Mathf.Clamp(warBudgetMultiplier, 2f, 20f);
        warUrgency = Mathf.Clamp(warUrgency, 0.5f, 1f);
        warPriceSensitivity = Mathf.Clamp(warPriceSensitivity, 0f, 0.5f);
        civilianBudgetReduction = Mathf.Clamp01(civilianBudgetReduction);
        peaceUrgency = Mathf.Clamp(peaceUrgency, 0f, 0.5f);
        peacePriceSensitivity = Mathf.Clamp(peacePriceSensitivity, 0.3f, 1f);
    }
}

