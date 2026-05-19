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
    [Tooltip("건물 고유 ID. 코드·이펙트 targetId 등에서 참조합니다.")]
    public string id;
    [Tooltip("UI에 표시할 건물 이름")]
    public string displayName;
    [Tooltip("건물 분류(유통·생산·원자재 생산 등)")]
    public BuildingType buildingType;
    [Tooltip("건설 메뉴·정보창에 쓰는 아이콘")]
    public Sprite icon;
    [Tooltip("맵에 배치될 때 보이는 스프라이트")]
    public Sprite buildingSprite;
    [TextArea(3, 10)]
    [Tooltip("건물 설명 문구")]
    public string description;
    [Tooltip("신규 게임에서 별도 연구 없이 건설 가능한지")]
    public bool isUnlockedByDefault = false;
    [Tooltip("true면 requiredEmployees 슬롯은 기술자만 배치. false면 노동자·기술자 혼합.")]
    public bool isProfessional = false;
    [Tooltip("배치 가능한 총 인력 슬롯 수.")]
    public int requiredEmployees = 0;
    [Tooltip("건설 시 소모되는 크레딧")]
    public int buildCost;
    [Tooltip("하루마다 소모되는 유지비(크레딧)")]
    public int maintenanceCost;
    [Tooltip("맵에서 차지하는 칸 크기(가로×세로)")]
    public Vector2Int size = new Vector2Int(1, 1);
    [Tooltip("건설 해금에 필요한 연구. 비우면 연구 조건 없음")]
    public ResearchData requiredResearch;

    [Header("Placed count limit")]
    [Tooltip("true면 baseMaxPlacedCount와 Building_MaxPlacedCount 이펙트(건물 id = EffectData.targetId 또는 Apply instanceId)로 설치 상한")]
    public bool usePlacedCountLimit = false;
    [Tooltip("이펙트 Flat 보너스와 합산되는 기본 최대 설치 수(0이면 이펙트만으로 해금)")]
    public int baseMaxPlacedCount = 0;

    [Tooltip("입력 버퍼(도로·창고 연결) 최대 용량")]
    public int maxInputBufferCapacity = 16;
    [Tooltip("출력 버퍼 최대 용량")]
    public int maxOutputBufferCapacity = 16;

    public virtual bool IsProductionBuilding => false;
    public virtual bool IsLoadStation => false;
    public virtual bool IsUnloadStation => false;
    public virtual bool IsRoad => false;
    public virtual List<ResourceType> AllowedResourceTypes => null;
}
