using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewBuildingData", menuName = "Game Data/Building Data", order = 1)]
public class BuildingData : ScriptableObject
{
    public string id;
    public string displayName;
    public Sprite icon;
    [TextArea(3, 10)]
    public string description;
    public int baseCost;
    public int baseMaintenanceCost;
    public List<ResourceType> allowedResourceTypes;
}
