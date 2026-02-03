using System;

/// <summary>
/// 직원의 현재 상태를 저장하는 클래스 (데이터 저장용).
/// 이펙트는 EffectDataHandler에서 인스턴스별로 관리합니다.
/// </summary>
[Serializable]
public class EmployeeState
{
    public int count;
    public float currentSatisfaction;
    public float currentEfficiency;
    public int assignedCount;
    public long totalSalary;
    public int salaryLevel;

    public EmployeeState()
    {
        count = 0;
        currentSatisfaction = 0f;
        currentEfficiency = 1f;
        assignedCount = 0;
        totalSalary = 0;
        salaryLevel = 2;
    }
}
