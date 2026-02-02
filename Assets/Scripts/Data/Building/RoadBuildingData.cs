using UnityEngine;

/// <summary>
/// 도로 건물 데이터
/// 자원을 운반하는 도로입니다.
/// </summary>
[CreateAssetMenu(fileName = "NewRoadBuilding", menuName = "Game Data/Building Data/Road")]
public class RoadBuildingData : BuildingData
{
    // BuildingData 가상 속성 구현
    public override bool IsRoad => true;

    [Header("Visualization")]
    [Tooltip("자원 충돌 발생 시 표시할 아이콘 스프라이트")]
    public Sprite conflictIcon;
}

