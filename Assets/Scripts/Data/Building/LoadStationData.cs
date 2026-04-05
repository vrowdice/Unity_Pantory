using UnityEngine;

/// <summary>
/// 상역소(Load) 건물 데이터 — 도로에서 받은 자원을 창고로 옮깁니다.
/// </summary>
[CreateAssetMenu(fileName = "NewLoadStation", menuName = "Game Data/Building Data/Load Station")]
public class LoadStationData : BuildingData
{
    [Header("Simulation")]
    [Tooltip("진행 1회 완료 시 맨 앞 입력 패킷에서 창고로 넣는 최대 수량(패킷보다 작으면 일부만 넣고 나머지는 큐 앞에 유지).")]
    public int pushPerHour = 1;

    public override bool IsLoadStation => true;
}
