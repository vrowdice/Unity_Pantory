using UnityEngine;
using System.Collections.Generic;

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

    [Header("Cost")]
    public int baseCost;
    public int baseMaintenanceCost;

    [Header("Size")]
    public Vector2Int size = new Vector2Int(1, 1);

    [Header("Employee Requirements")]
    [Tooltip("건물이 요구하는 최소 기술 수준 티어 (1=Worker, 2=Technician, 3=Researcher)")]
    public int requiredTier = 1;        // 요구되는 최소 기술 수준
    [Tooltip("필요한 Worker 직원 수")]
    public int requiredWorkers = 0;
    [Tooltip("필요한 Technician 직원 수")]
    public int requiredTechnicians = 0;
    [Tooltip("필요한 Researcher 직원 수")]
    public int requiredResearchers = 0;
    [Tooltip("필요한 Manager 직원 수")]
    public int requiredManagers = 0;

    // 가상 속성들 - 파생 클래스에서 구현
    /// <summary>
    /// 생산 건물인지 여부 (ProductionBuildingData 또는 그 파생 클래스)
    /// </summary>
    public virtual bool IsProductionBuilding => false;
    
    /// <summary>
    /// 상역소(Load) 건물인지 여부
    /// </summary>
    public virtual bool IsLoadStation => false;
    
    /// <summary>
    /// 하역소(Unload) 건물인지 여부
    /// </summary>
    public virtual bool IsUnloadStation => false;
    
    /// <summary>
    /// 도로 건물인지 여부
    /// </summary>
    public virtual bool IsRoad => false;
    
    /// <summary>
    /// 입력 위치 (생산 건물만 유효)
    /// </summary>
    public virtual Vector2Int InputPosition => Vector2Int.zero;
    
    /// <summary>
    /// 출력 위치 (생산 건물만 유효)
    /// </summary>
    public virtual Vector2Int OutputPosition => Vector2Int.zero;
    
    /// <summary>
    /// 허용된 자원 타입 리스트 (생산 건물만 유효)
    /// </summary>
    public virtual System.Collections.Generic.List<ResourceType> AllowedResourceTypes => null;
}
