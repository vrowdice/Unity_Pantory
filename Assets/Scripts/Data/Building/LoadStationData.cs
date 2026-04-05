using UnityEngine;

/// <summary>
/// 상역소(Load) 건물 데이터
/// 자원을 외부로 내보내는 건물입니다.
/// </summary>
[CreateAssetMenu(fileName = "NewLoadStation", menuName = "Game Data/Building Data/Load Station")]
public class LoadStationData : BuildingData
{
    public override bool IsLoadStation => true;
}

