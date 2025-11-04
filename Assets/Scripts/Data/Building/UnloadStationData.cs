using UnityEngine;

/// <summary>
/// 하역소(Unload) 건물 데이터
/// 외부에서 자원을 받아오는 건물입니다.
/// </summary>
[CreateAssetMenu(fileName = "NewUnloadStation", menuName = "Game Data/Building Data/Unload Station", order = 5)]
public class UnloadStationData : BuildingData
{
    [Header("Station Properties")]
    [Tooltip("출력 위치 (건물 기준 상대 좌표) - 자원을 내보내는 위치")]
    public Vector2Int outputPosition = new Vector2Int(0, 0);
    
    // BuildingData 가상 속성 구현
    public override bool IsUnloadStation => true;
    public override Vector2Int OutputPosition => outputPosition;
    // UnloadStation은 입력이 없으므로 InputPosition은 zero 유지
}

