using System;
using UnityEngine;

/// <summary>
/// 직원의 현재 상태를 저장하는 클래스 (데이터 저장용).
/// 이펙트는 EffectDataHandler에서 인스턴스별로 관리합니다.
/// </summary>
[Serializable]
public class EmployeeState
{
    [Tooltip("현재 고용 인원")]
    public int count;
    [Tooltip("현재 만족도")]
    public float currentSatisfaction;
    [Tooltip("현재 효율(0~1)")]
    public float currentEfficiency;
    [Tooltip("건물 등에 배치된 인원")]
    public int assignedCount;
    [Tooltip("일일 총 급여 지출(크레딧)")]
    public long totalSalary;
    [Tooltip("급여 레벨(0=매우 적음 ~ 4=매우 많음). InitialEmployeeData 참조")]
    public int salaryLevel;

    public EmployeeState()
    {
        count = 0;
        currentSatisfaction = 0f;
        currentEfficiency = 0.5f;
        assignedCount = 0;
        totalSalary = 0;
        salaryLevel = 2;
    }
}
