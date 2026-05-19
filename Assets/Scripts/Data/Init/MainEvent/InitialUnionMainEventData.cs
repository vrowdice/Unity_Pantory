using UnityEngine;

[CreateAssetMenu(fileName = "InitialUnionMainEventData", menuName = "Init Game Data/Main Event/Initial Union Main Event Data")]
public class InitialUnionMainEventData : InitialMainEventModuleData
{
    [Header("Union")]
    [Tooltip("연합 이벤트가 시작되기 위한 최소 직원 수")]
    [Min(1)]
    public int unionEmployeeCountToStart = 500;

    [Tooltip("연합 이벤트 전체 기한(일). eventOverDate가 0일 때 대체값으로도 사용")]
    public int unionDaysToComplete = 3650;

    [Tooltip("연합 이벤트 진행 중 매일 차감되는 크레딧 (0이면 없음)")]
    public long unionDailyCreditCost;

    [Header("Cohesion Progress")]
    [Tooltip("가중 평균 만족도가 이 값 미만이면 결합도가 오르지 않음")]
    public float cohesionSatisfactionBaseline;

    [Tooltip("기준 만족도 대비 1포인트당 하루에 증가하는 결합도(%)")]
    [Min(0f)]
    public float cohesionProgressPerSatisfactionPointPerDay = 0.05f;

    [Tooltip("만족도로 인한 하루 최대 결합도 증가량(%)")]
    [Min(0f)]
    public float maxCohesionProgressGainPerDay = 2f;

    [Header("Union Request Generation")]
    [Tooltip("동시에 활성화될 수 있는 노조 요구 최대 개수")]
    public int maxActiveUnionRequests = 3;

    [Tooltip("매일 새 노조 요구가 생길 기본 확률")]
    public float baseUnionRequestChance = 0.02f;

    [Tooltip("요구가 없을 때 매일 누적되는 추가 확률(피티)")]
    public float unionRequestChanceIncrement = 0.01f;

    [Tooltip("이 일수 동안 요구가 없으면 100% 확률로 생성")]
    public int guaranteedUnionRequestDay = 60;

    [Tooltip("생성된 요구의 최소 마감 일수")]
    public int minUnionRequestDeadlineDays = 30;

    [Tooltip("생성된 요구의 최대 마감 일수")]
    public int maxUnionRequestDeadlineDays = 90;
}
