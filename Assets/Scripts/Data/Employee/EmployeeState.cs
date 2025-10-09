using System;

/// <summary>
/// 직원의 현재 상태를 저장하는 클래스
/// </summary>
[Serializable]
public class EmployeeState
{
    public int count;                   // 고용된 직원 수
    public float currentSkill;          // 현재 숙련도
    public float currentFatigue;        // 현재 피로도
    public float currentLoyalty;        // 현재 충성도
    public float currentEfficiency;     // 현재 생산 효율
    public int assignedCount;           // 배치된 인원 수
    public long totalSalary;            // 총 급여

    public EmployeeState()
    {
        count = 0;
        currentSkill = 0f;
        currentFatigue = 0f;
        currentLoyalty = 0f;
        currentEfficiency = 1f;
        assignedCount = 0;
        totalSalary = 0;
    }
}
