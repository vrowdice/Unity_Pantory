using UnityEngine;

/// <summary>
/// 도로 건물 데이터
/// 자원을 운반하는 도로입니다.
/// </summary>
[CreateAssetMenu(fileName = "NewRoadBuilding", menuName = "Game Data/Building Data/Road", order = 6)]
public class RoadBuildingData : BuildingData
{
    // BuildingData 가상 속성 구현
    public override bool IsRoad => true;
}

