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
    public Sprite buildingSprite;
    [TextArea(3, 10)]
    public string description;

    [Header("Cost")]
    public int baseCost;
    public int baseMaintenanceCost;

    [Header("Production")]
    public List<ResourceType> allowedResourceTypes;
    public Vector2Int inputPosition = new Vector2Int(-1, 1);
    public Vector2Int outputPosition = new Vector2Int(2, 1);

    [Header("Size")]
    public Vector2Int size = new Vector2Int(1, 1);
}
