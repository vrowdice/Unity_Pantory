using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewResourceData", menuName = "Game Data/Resource Data", order = 0)]
public class ResourceData : ScriptableObject
{
    public string id;          // "iron_ore"
    public string displayName; // "Iron Ore"
    [Header("Market Settings")]
    public float baseValue = 10f;
    [Range(0f, 1f)] public float marketSensitivity = 0.4f;
    [Range(0f, 1f)] public float meanReversionStrength = 0.25f;
    [Range(0.1f, 3f)] 
    [Tooltip("Volatility multiplier relative to base market volatility (1.0 = normal, 0.5 = half, 2.0 = double)")]
    public float volatilityMultiplier = 1f;
    [Range(0f, 5f)] public float scarcityWeight = 1f;

    [Header("Market Stability")]
    [Tooltip("가격이 BaseValue의 몇 배가 되면 강력한 저항(수요 급감)이 발생하는지 설정")]
    [Range(1.5f, 10f)] 
    public float priceResistanceThreshold = 3.0f; // 기본값 3배 (300% 가격)

    [Tooltip("최대 허용 가격 배율 (이 이상으로는 절대 안 오름, 안전장치)")]
    [Range(2f, 20f)]
    public float maxPriceMultiplier = 10.0f;

    [Header("Meta")]
    public float rarity;
    public ResourceType type;        // "metal"
    public ResourceData nextStage;   // "iron_ingot"
    public Sprite icon;
    [TextArea(3, 10)]
    public string description;
    
    [Header("Crafting Requirements")]
    public List<ResourceRequirement> requirements = new List<ResourceRequirement>();

    public int initialAmount;
}