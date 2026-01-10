using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewMarketActorData", menuName = "Game Data/Market Actor", order = 0)]
public class MarketActorData : ScriptableObject
{
    public string id;
    public string displayName;
    public Sprite icon;
    [TextArea(3, 10)] public string description;

    public long wealth;
    public long baseProductionCount;
    public List<ResourceData> productionResources = new List<ResourceData>();
}

