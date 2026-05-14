using UnityEngine;

[CreateAssetMenu(fileName = "InitialUnionMainEventData", menuName = "Init Game Data/Main Event/Initial Union Main Event Data")]
public class InitialUnionMainEventData : InitialMainEventModuleData
{
    [Header("Union")]
    [Min(1)]
    public int unionEmployeeCountToStart = 500;
    public int unionDaysToComplete = 3650;

    [Tooltip("연합 이벤트 진행 중 매일 차감되는 크레딧 (0이면 없음)")]
    public long unionDailyCreditCost;
}
