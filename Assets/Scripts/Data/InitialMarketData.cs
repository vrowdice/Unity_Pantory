using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ScriptableObject that stores initial market data.
/// Allows market balancing through the Inspector.
/// </summary>
[CreateAssetMenu(fileName = "InitialMarketData", menuName = "Game Data/Initial Market Data", order = 2)]
public class InitialMarketData : ScriptableObject
{
    [Header("Transaction Fees")]
    [Range(0f, 0.2f)]
    [Tooltip("Market transaction fee rate (0.05 = 5%)")]
    public float marketFeeRate = 0.05f;

    [Header("Market Actor Initial State")]
    [Tooltip("Initial wealth for all market actors")]
    public float initialActorWealth = 0f;
    [Range(0f, 1f)]
    [Tooltip("Initial health for all market actors (0-1)")]
    public float initialActorHealth = 1f;

    [Header("Provider Settings")]
    [Range(0f, 0.2f)]
    [Tooltip("Probability of production failure (0.05 = 5%)")]
    public float productionFailureChance = 0.05f;
    [Range(0f, 0.5f)]
    [Tooltip("Loss rate when production fails (0.1 = 10% of revenue)")]
    public float productionFailureLossRate = 0.1f;
    [Range(0f, 1f)]
    [Tooltip("Default upkeep rate when no upkeep resources specified (0.2 = 20% of output)")]
    public float defaultUpkeepRate = 0.2f;
    [Tooltip("Production cost scaling factor (cost per 100 units)")]
    public float productionCostScale = 100f;

    [Header("Consumer Settings")]
    [Range(0f, 1f)]
    [Tooltip("Satisfaction value rate (0.1 = 10% of purchase cost)")]
    public float satisfactionValueRate = 0.1f;
    [Range(0f, 1f)]
    [Tooltip("Purchase cost loss rate (0.5 = 50% of purchase cost)")]
    public float purchaseCostLossRate = 0.5f;
    [Range(0f, 1f)]
    [Tooltip("Budget shortage threshold (0.3 = 30% of min budget)")]
    public float budgetShortageThreshold = 0.3f;

    [Header("Health System")]
    [Range(0f, 0.1f)]
    [Tooltip("Provider health recovery when wealth increases")]
    public float providerWealthGainHealthRecovery = 0.02f;
    [Range(0f, 0.1f)]
    [Tooltip("Provider health damage when wealth decreases")]
    public float providerWealthLossHealthDamage = 0.05f;
    [Range(0f, 0.01f)]
    [Tooltip("Provider health penalty per rank below #1")]
    public float providerRankPenalty = 0.005f;
    [Range(0f, 0.01f)]
    [Tooltip("Provider natural health decay per day")]
    public float providerNaturalDecay = 0.002f;
    [Range(0f, 0.1f)]
    [Tooltip("Consumer health damage when budget is short")]
    public float consumerBudgetShortageDamage = 0.03f;
    [Range(0f, 0.1f)]
    [Tooltip("Consumer health recovery when budget is sufficient")]
    public float consumerBudgetSufficientRecovery = 0.01f;
    [Range(0f, 1f)]
    [Tooltip("Consumer health lerp towards satisfaction")]
    public float consumerSatisfactionLerp = 0.1f;

    [Header("Player Trade Impact")]
    [Range(0f, 1f)]
    [Tooltip("Player trade impact on market demand (0.1 = 10%)")]
    public float playerDemandImpact = 0.1f;
    [Range(0f, 1f)]
    [Tooltip("Player trade impact on market supply (0.1 = 10%)")]
    public float playerSupplyImpact = 0.1f;

    [Header("Price Signal")]
    [Range(0f, 1f)]
    [Tooltip("Minimum price signal value")]
    public float minPriceSignal = 0.1f;

    [Header("Resource Volatility")]
    [Range(0.001f, 0.1f)]
    [Tooltip("Base maximum daily price swing for all resources (0.01 = 1% per day)")]
    public float baseMaxDailySwing = 0.01f;

    [Header("Batch Selling")]
    [Range(0f, 1f)]
    [Tooltip("Modifier when batch selling is not allowed (0.5 = 50%)")]
    public float noBatchSellingModifier = 0.5f;

    [Header("Actor Production/Consumption Ranges")]
    [Tooltip("Base production range for raw resources (min, max)")]
    public Vector2 rawProductionRange = new Vector2(800f, 1500f);
    [Tooltip("Base production range for metal resources (min, max)")]
    public Vector2 metalProductionRange = new Vector2(600f, 1200f);
    [Tooltip("Base production range for wood resources (min, max)")]
    public Vector2 woodProductionRange = new Vector2(700f, 1300f);
    [Tooltip("Base production range for tool resources (min, max)")]
    public Vector2 toolProductionRange = new Vector2(150f, 350f);
    [Tooltip("Base production range for weapon resources (min, max)")]
    public Vector2 weaponProductionRange = new Vector2(100f, 300f);
    [Tooltip("Base production range for other resources (min, max)")]
    public Vector2 otherProductionRange = new Vector2(400f, 900f);

    [Header("Actor Scale Multipliers")]
    [Tooltip("Production/consumption multiplier for Small scale actors")]
    public float smallScaleMultiplier = 1.2f;
    [Tooltip("Production/consumption multiplier for Large scale actors")]
    public float largeScaleMultiplier = 3.0f;
    [Tooltip("Production/consumption multiplier for Medium scale actors (default: 2.0)")]
    public float mediumScaleMultiplier = 2.0f;

    [Header("Consumer Settings")]
    [Range(0f, 2f)]
    [Tooltip("Consumer consumption multiplier relative to production (1.2 = 120% of production, creates demand)")]
    public float consumerConsumptionMultiplier = 1.2f;

    [Header("Consumer Budget Ranges")]
    [Tooltip("Budget range for Small scale consumers (min, max) - 구매력 향상을 위해 증가")]
    public Vector2 smallConsumerBudget = new Vector2(2000f, 4000f);  // 1200-2400 -> 2000-4000
    [Tooltip("Budget range for Medium scale consumers (min, max) - 구매력 향상을 위해 증가")]
    public Vector2 mediumConsumerBudget = new Vector2(4000f, 7500f);  // 2400-4500 -> 4000-7500
    [Tooltip("Budget range for Large scale consumers (min, max) - 구매력 향상을 위해 증가")]
    public Vector2 largeConsumerBudget = new Vector2(9000f, 16000f);  // 5400-9600 -> 9000-16000

    [Header("System Actor (General Population)")]
    [Tooltip("Budget range for system populace actor (min, max)")]
    public Vector2 systemPopulaceBudget = new Vector2(5000f, 10000f);
    [Tooltip("Wealth for system populace actor (infinite budget)")]
    public float systemPopulaceWealth = 1000000f;
    [Tooltip("Desired resource quantity range for system populace (min, max)")]
    public Vector2 systemPopulaceQuantityRange = new Vector2(50f, 150f);
    [Tooltip("Price sensitivity for system populace (higher = more price sensitive)")]
    [Range(0f, 5f)]
    public float systemPopulacePriceSensitivity = 2.0f;
    [Tooltip("Urgency for system populace (0 = not urgent, 1 = very urgent)")]
    [Range(0f, 1f)]
    public float systemPopulaceUrgency = 0.0f;

    [Header("Trade Port (Price Stabilization)")]
    [Tooltip("Price multiplier threshold for trade port intervention (1.3 = 130% of base price)")]
    [Range(1.1f, 2f)]
    public float tradePortPriceThreshold = 1.3f;
    [Tooltip("Supply boost ratio relative to demand (0.5 = 50% of demand)")]
    [Range(0.1f, 1f)]
    public float tradePortSupplyBoostRatio = 0.5f;
    [Tooltip("Minimum supply boost amount")]
    public float tradePortMinSupply = 100f;
    [Tooltip("Price drop rate when trade port intervenes (0.9 = 10% drop)")]
    [Range(0.5f, 1f)]
    public float tradePortPriceDropRate = 0.9f;
    [Tooltip("Emergency supply amount when inventory is low")]
    public float tradePortEmergencySupply = 100f;
    [Tooltip("Export threshold: supply must be this many times demand to export (2.0 = 2x)")]
    [Range(1.5f, 5f)]
    public float tradePortExportSurplusRatio = 2.0f;
    [Tooltip("Export price threshold: only export when price is below this ratio of base price (0.8 = 80%)")]
    [Range(0.5f, 1f)]
    public float tradePortExportPriceThreshold = 0.8f;
    [Tooltip("Export dump ratio: percentage of surplus to export (0.5 = 50%)")]
    [Range(0.1f, 1f)]
    public float tradePortExportDumpRatio = 0.5f;

    [Header("Stimulus Packages (Government Subsidies)")]
    [Tooltip("Wealth threshold for subsidy eligibility")]
    public float stimulusWealthThreshold = 1000f;
    [Tooltip("Subsidy amount for struggling actors")]
    public float stimulusSubsidyAmount = 500f;
    [Tooltip("Health penalty when receiving subsidy (zombie company indicator)")]
    [Range(0f, 0.2f)]
    public float stimulusHealthPenalty = 0.05f;
    [Tooltip("Budget threshold for disaster relief")]
    public float stimulusBudgetThreshold = 100f;
    [Tooltip("Disaster relief amount for actors with low budget")]
    public float stimulusDisasterRelief = 500f;

    [Header("Initial Market Seeding")]
    [Tooltip("Initial resource count for all resources")]
    public long initialResourceCount = 1000L;
    [Tooltip("Initial lastSupply value to simulate previous day supply")]
    public float initialLastSupply = 500f;
    [Tooltip("Initial wealth bonus for all actors")]
    public float initialWealthBonus = 10000f;

    [Header("Budget Ratios (Auto-calculated budgets)")]
    [Tooltip("Budget ratio for Small scale actors (percentage of wealth)")]
    [Range(0f, 1f)]
    public float smallBudgetRatio = 0.5f;
    [Tooltip("Budget ratio for Medium scale actors (percentage of wealth)")]
    [Range(0f, 1f)]
    public float mediumBudgetRatio = 0.55f;
    [Tooltip("Budget ratio for Large scale actors (percentage of wealth)")]
    [Range(0f, 1f)]
    public float largeBudgetRatio = 0.6f;
    [Tooltip("Minimum budget for Small scale actors")]
    public float smallMinBudget = 500f;
    [Tooltip("Minimum budget for Medium scale actors")]
    public float mediumMinBudget = 1000f;
    [Tooltip("Minimum budget for Large scale actors")]
    public float largeMinBudget = 2000f;

    [Header("Price Adjustment")]
    [Tooltip("Mean reversion multiplier when price deviates significantly (0.5 = moderate)")]
    [Range(0.1f, 2f)]
    public float meanReversionMultiplier = 0.5f;
    [Tooltip("Maximum price drop rate when exceeding price cap (0.2 = 20% drop per day)")]
    [Range(0.05f, 0.5f)]
    public float maxPriceDropRate = 0.2f;
    [Tooltip("Normal price drop rate when near price cap (0.1 = 10% drop per day)")]
    [Range(0.05f, 0.3f)]
    public float normalPriceDropRate = 0.1f;
    [Tooltip("Over ratio threshold for aggressive price drop (1.5 = 150% of max price)")]
    [Range(1.2f, 2f)]
    public float overRatioThreshold = 1.5f;

    [Header("Price Resistance")]
    [Tooltip("Urgency threshold to reduce price resistance penalty (0.8 = 80% urgency)")]
    [Range(0.5f, 1f)]
    public float priceResistanceUrgencyThreshold = 0.8f;
    [Tooltip("Minimum resistance penalty for high urgency actors (0.5 = 50% maintained)")]
    [Range(0.1f, 1f)]
    public float minResistancePenalty = 0.5f;

    [Header("War State")]
    [Tooltip("Budget multiplier for military actors during war (10 = 10x budget)")]
    [Range(2f, 20f)]
    public float warBudgetMultiplier = 10f;
    [Tooltip("Urgency for military actors during war (1.0 = maximum)")]
    [Range(0.5f, 1f)]
    public float warUrgency = 1.0f;
    [Tooltip("Price sensitivity for military actors during war (0.1 = price insensitive)")]
    [Range(0f, 0.5f)]
    public float warPriceSensitivity = 0.1f;
    [Tooltip("Civilian budget reduction during war (0.5 = 50% of normal)")]
    [Range(0.1f, 1f)]
    public float civilianBudgetReduction = 0.5f;
    [Tooltip("Default urgency for actors during peace (0.25 = low urgency)")]
    [Range(0f, 0.5f)]
    public float peaceUrgency = 0.25f;
    [Tooltip("Default price sensitivity for actors during peace (0.55 = moderate)")]
    [Range(0.3f, 1f)]
    public float peacePriceSensitivity = 0.55f;

    /// <summary>
    /// Validates values in the Editor (prevents invalid values).
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

