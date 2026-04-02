using UnityEngine;

/// <summary>
/// 하역소(Unload) 건물 데이터
/// 외부에서 자원을 받아오는 건물입니다.
/// </summary>
[CreateAssetMenu(fileName = "NewUnloadStation", menuName = "Game Data/Building Data/Unload Station")]
public class UnloadStationData : BuildingData
{
    [Header("Station Properties")]
    [Tooltip("출력 위치 (건물 기준 상대 좌표) - 자원을 내보내는 위치")]
    public Vector2Int outputPosition = new Vector2Int(0, 0);

    [Header("Simulation")]
    [Tooltip("시간 틱마다 창고에서 꺼내 도로로 보낼 개수 (어떤 자원인지는 BuildingObject에서 플레이어가 선택)")]
    public int pullPerHour = 1;
    
    public override bool IsUnloadStation => true;
    public override Vector2Int OutputPosition => outputPosition;
}

