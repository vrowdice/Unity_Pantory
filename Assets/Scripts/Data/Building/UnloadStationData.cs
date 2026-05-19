using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 하역소(Unload) 건물 데이터
/// 외부에서 자원을 받아오는 건물입니다.
/// </summary>
[CreateAssetMenu(fileName = "NewUnloadStation", menuName = "Game Data/Building Data/Unload Station")]
public class UnloadStationData : BuildingData
{
    [Header("Production")]
    [Tooltip("하역·생산에 사용할 수 있는 자원 카테고리")]
    public List<ResourceType> allowedResourceTypes;

    [Header("Simulation")]
    [Tooltip("시간 1회마다 창고에서 끌어와 출력 버퍼에 넣는 최대 수량")]
    public int pullPerHour = 1;
    [Tooltip("하역소 전용 출력 버퍼 용량(부모 출력 버퍼와 별도)")]
    public int outputBufferCapacity = 16;

    public override bool IsUnloadStation => true;
    public override List<ResourceType> AllowedResourceTypes => allowedResourceTypes;
}
