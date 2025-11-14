using UnityEngine;

[CreateAssetMenu(fileName = "NewMarketActorData", menuName = "Game Data/Market Actor", order = 0)]
public class MarketActorData : ScriptableObject
{
    [Header("Identity")]
    public string id;
    public string displayName;
    public Sprite portrait;
    public MarketActorArchetype archetype = MarketActorArchetype.Generalist;

    [Header("Roles")]
    public MarketRoleFlags roles = MarketRoleFlags.Provider;

    [Header("Profiles")]
    public ProviderProfile providerProfile;
    public ConsumerProfile consumerProfile;
}

