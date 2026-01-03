using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewResourceData", menuName = "Game Data/Resource Data", order = 0)]
public class ResourceData : ScriptableObject
{
    public string id;
    public string displayName;

    [Header("Market Settings")]
    public float baseValue = 10f;
    [Range(0.1f, 3f)] public float volatilityMultiplier = 1f;
    [Range(0f, 10f)] public float maxPriceMultiplier = 1.5f;

    [Header("Meta")]
    public ResourceType type;
    public ResourceData nextStage;
    public Sprite icon;
    [TextArea(3, 10)] public string description;

    [Header("Crafting Requirements")]
    public List<ResourceRequirement> requirements = new List<ResourceRequirement>();

    [Header("Initialization")]
    public int initialAmount;
}