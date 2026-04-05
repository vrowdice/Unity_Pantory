using UnityEngine;

/// <summary>
/// 도로 건물 데이터
/// 자원을 운반하는 도로입니다.
/// </summary>
[CreateAssetMenu(fileName = "NewRoadBuilding", menuName = "Game Data/Building Data/Road")]
public class RoadBuildingData : BuildingData
{
    public override bool IsRoad => true;
}

