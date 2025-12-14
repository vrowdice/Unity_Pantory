using UnityEngine;
using System.Collections.Generic;
using Unity.VisualScripting;

/// <summary>
/// 건물 데이터의 기반 클래스 (abstract)
/// 모든 건물에 공통적으로 필요한 필드만 포함합니다.
/// </summary>
public abstract class BuildingData : ScriptableObject
{
    [Header("Basic Info")]
    public string id;
    public string displayName;
    public BuildingType buildingType;
    public Sprite icon;
    public Sprite buildingSprite;
    [TextArea(3, 10)]
    public string description;
    public bool isUnlockedByDefault = false;
    public int requiredEmployees = 0;
    public int baseCost;
    public int baseMaintenanceCost;
    public Vector2Int size = new Vector2Int(1, 1);
    public string requiredResearchId;
    public ResourceData handlingResource;

    public virtual ResourceData HandlingResource => handlingResource;
    public virtual bool IsProductionBuilding => false;
    public virtual bool IsLoadStation => false;
    public virtual bool IsUnloadStation => false;
    public virtual bool IsRoad => false;
    public virtual Vector2Int InputPosition => Vector2Int.zero;
    public virtual Vector2Int OutputPosition => Vector2Int.zero;
    public virtual List<ResourceType> AllowedResourceTypes => null;
}
