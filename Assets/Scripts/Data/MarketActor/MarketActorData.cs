using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewMarketActorData", menuName = "Game Data/Market Actor Data")]
public class MarketActorData : ScriptableObject
{
    public string id;
    public string displayName;
    public Sprite icon;
    [TextArea(3, 10)] public string description;
    public MarketActorType marketActorType;

    public long baseWealth;
    public long baseProductionCount;
    public List<ResourceData> comsumeResourceList = new List<ResourceData>();
    public List<ResourceData> productionResourceList = new List<ResourceData>();
}

