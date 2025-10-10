using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewResourceData", menuName = "Game Data/Resource Data", order = 0)]
public class ResourceData : ScriptableObject
{
    public string id;          // "iron_ore"
    public string displayName; // "Iron Ore"
    public float minValue;
    public float maxValue;
    public float baseValue;
    public float rarity;
    public ResourceType type;    // "metal"
    public ResourceData nextStage;   // "iron_ingot"
    public Sprite icon;
    [TextArea(3, 10)]
    public string description;
    
    [Header("Crafting Requirements")]
    public List<ResourceRequirement> requirements = new List<ResourceRequirement>();

    public int initialAmount;
}