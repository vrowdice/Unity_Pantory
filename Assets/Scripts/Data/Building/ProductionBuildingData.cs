using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 생산 건물 데이터
/// 자원을 생산하거나 가공하는 건물에 사용됩니다.
/// </summary>
[CreateAssetMenu(fileName = "NewProductionBuilding", menuName = "Game Data/Building Data/Production", order = 2)]
public class ProductionBuildingData : BuildingData
{
    [Header("Production")]
    [Tooltip("이 건물에서 생산 가능한 자원 타입 리스트")]
    public List<ResourceType> allowedResourceTypes;
    
    [Tooltip("입력 위치 (건물 기준 상대 좌표)")]
    public Vector2Int inputPosition = new Vector2Int(-1, 1);
    
    [Tooltip("출력 위치 (건물 기준 상대 좌표)")]
    public Vector2Int outputPosition = new Vector2Int(2, 1);
    
    [Tooltip("기본 생산량")]
    public float baseProductionRate = 1.0f;
    
    [Tooltip("처리 시간 (초)")]
    public float processingTime = 1.0f;

    // BuildingData 가상 속성 구현
    public override bool IsProductionBuilding => true;
    public override Vector2Int InputPosition => inputPosition;
    public override Vector2Int OutputPosition => outputPosition;
    public override System.Collections.Generic.List<ResourceType> AllowedResourceTypes => allowedResourceTypes;
}

