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
    public Vector2 rawProductionRange = new Vector2(250f, 450f);
    [Tooltip("Base production range for metal resources (min, max)")]
    public Vector2 metalProductionRange = new Vector2(180f, 350f);
    [Tooltip("Base production range for wood resources (min, max)")]
    public Vector2 woodProductionRange = new Vector2(200f, 400f);
    [Tooltip("Base production range for tool resources (min, max)")]
    public Vector2 toolProductionRange = new Vector2(35f, 100f);
    [Tooltip("Base production range for weapon resources (min, max)")]
    public Vector2 weaponProductionRange = new Vector2(25f, 75f);
    [Tooltip("Base production range for other resources (min, max)")]
    public Vector2 otherProductionRange = new Vector2(100f, 220f);

    [Header("Actor Scale Multipliers")]
    [Tooltip("Production/consumption multiplier for Small scale actors")]
    public float smallScaleMultiplier = 0.6f;
    [Tooltip("Production/consumption multiplier for Large scale actors")]
    public float largeScaleMultiplier = 1.6f;
    [Tooltip("Production/consumption multiplier for Medium scale actors (default: 1.0)")]
    public float mediumScaleMultiplier = 1f;

    [Header("Consumer Settings")]
    [Range(0f, 1f)]
    [Tooltip("Consumer consumption multiplier relative to production (0.85 = 85% of production)")]
    public float consumerConsumptionMultiplier = 0.85f;

    [Header("Consumer Budget Ranges")]
    [Tooltip("Budget range for Small scale consumers (min, max)")]
    public Vector2 smallConsumerBudget = new Vector2(600f, 1200f);
    [Tooltip("Budget range for Medium scale consumers (min, max)")]
    public Vector2 mediumConsumerBudget = new Vector2(1200f, 2250f);
    [Tooltip("Budget range for Large scale consumers (min, max)")]
    public Vector2 largeConsumerBudget = new Vector2(2700f, 4800f);

    /// <summary>
    /// Applies initial market data to MarketDataHandler.
    /// </summary>
    /// <param name="marketHandler">MarketDataHandler to apply to</param>
    public void ApplyToMarket(MarketDataHandler marketHandler)
    {
        if (marketHandler == null)
        {
            Debug.LogError("[InitialMarketData] MarketDataHandler is null.");
            return;
        }

        marketHandler.SetMarketSettings(this);
    }

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
        consumerConsumptionMultiplier = Mathf.Clamp01(consumerConsumptionMultiplier);
        
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
    }
}

