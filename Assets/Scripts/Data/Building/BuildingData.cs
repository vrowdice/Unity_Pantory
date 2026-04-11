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
    [Tooltip("true면 requiredEmployees 슬롯은 기술자만 배치. false면 노동자·기술자 혼합.")]
    public bool isProfessional = false;
    [Tooltip("배치 가능한 총 인력 슬롯 수.")]
    public int requiredEmployees = 0;
    public int buildCost;
    public int maintenanceCost;
    public Vector2Int size = new Vector2Int(1, 1);
    public ResearchData requiredResearch;

    [Header("Placed count limit")]
    [Tooltip("true면 baseMaxPlacedCount와 Building_MaxPlacedCount 이펙트(건물 id = EffectData.targetId 또는 Apply instanceId)로 설치 상한")]
    public bool usePlacedCountLimit = false;
    [Tooltip("이펙트 Flat 보너스와 합산되는 기본 최대 설치 수(0이면 이펙트만으로 해금)")]
    public int baseMaxPlacedCount = 0;

    public int maxInputBufferCapacity = 16;
    public int maxOutputBufferCapacity = 16;

    public virtual bool IsProductionBuilding => false;
    public virtual bool IsLoadStation => false;
    public virtual bool IsUnloadStation => false;
    public virtual bool IsRoad => false;
    public virtual List<ResourceType> AllowedResourceTypes => null;
}
