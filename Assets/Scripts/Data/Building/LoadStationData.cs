using UnityEngine;

/// <summary>
/// 상역소(Load) 건물 데이터
/// 자원을 외부로 내보내는 건물입니다.
/// </summary>
[CreateAssetMenu(fileName = "NewLoadStation", menuName = "Game Data/Building Data/Load Station", order = 4)]
public class LoadStationData : BuildingData
{
    [Header("Station Properties")]
    [Tooltip("입력 위치 (건물 기준 상대 좌표) - 외부에서 자원을 받아오는 위치")]
    public Vector2Int inputPosition = new Vector2Int(0, 0);
    
    // BuildingData 가상 속성 구현
    public override bool IsLoadStation => true;
    public override Vector2Int InputPosition => inputPosition;
    // LoadStation은 출력이 없으므로 OutputPosition은 zero 유지
}

