using UnityEngine;

[CreateAssetMenu(fileName = "NewMarketActorData", menuName = "Game Data/Market Actor", order = 0)]
public class MarketActorData : ScriptableObject
{
    [Header("Identity")]
    public string id;
    public string displayName;
    public Sprite icon;
    [TextArea(3, 10)]
    public string description;
    public MarketActorArchetype archetype = MarketActorArchetype.Generalist;

    [Header("Roles")]
    public MarketRoleFlags roles = MarketRoleFlags.Provider;

    [Header("Profiles")]
    public ProviderProfile providerProfile;
    public ConsumerProfile consumerProfile;

    [Header("Dynamic Allocation")]
    public bool useDynamicResourceAllocation = true;
    public MarketActorScale scale = MarketActorScale.Medium;
    [Tooltip("Preferred resource types for dynamic allocation. Leave empty for all types.")]
    public ResourceType[] preferredResourceTypes;
    [Range(1, 30)] public int providerReassignmentDays = 7;
    [Range(1, 30)] public int consumerReassignmentDays = 7;
    [Tooltip("Override provider slot count. <= 0 uses scale defaults.")]
    public int providerSlotOverride = -1;
    [Tooltip("Override consumer slot count. <= 0 uses scale defaults.")]
    public int consumerSlotOverride = -1;
}

