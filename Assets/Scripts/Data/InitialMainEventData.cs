using UnityEngine;

[CreateAssetMenu(fileName = "InitialMainEventData", menuName = "Init Game Data/Initial Main Event Data")]
public class InitialMainEventData : ScriptableObject
{
    [Header("Union")]
    [Min(1)]
    public int unionEmployeeCountToStart = 500;

    [Tooltip("연합 이벤트 진행 중 매일 차감되는 크레딧 (0이면 없음)")]
    public long unionDailyCreditCost;
}
