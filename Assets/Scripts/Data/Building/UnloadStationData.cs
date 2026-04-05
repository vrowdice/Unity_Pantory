using UnityEngine;

/// <summary>
/// 하역소(Unload) 건물 데이터
/// 외부에서 자원을 받아오는 건물입니다.
/// </summary>
[CreateAssetMenu(fileName = "NewUnloadStation", menuName = "Game Data/Building Data/Unload Station")]
public class UnloadStationData : BuildingData
{
    [Header("Simulation")]
    public int pullPerHour = 1;
    public int outputBufferCapacity = 16;

    public override bool IsUnloadStation => true;
}
