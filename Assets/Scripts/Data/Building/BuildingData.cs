using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewBuildingData", menuName = "Game Data/Building Data", order = 1)]
public class BuildingData : ScriptableObject
{
    [Header("Basic Info")]
    public string id;
    public string displayName;
    public BuildingType buildingType;
    public Sprite icon;
    [TextArea(3, 10)]
    public string description;
    
    [Header("Cost")]
    public int baseCost;
    public int baseMaintenanceCost;
    
    [Header("Production")]
    public List<ResourceType> allowedResourceTypes;
}
