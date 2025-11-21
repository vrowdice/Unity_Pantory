using System;

/// <summary>
/// 직원의 현재 상태를 저장하는 클래스
/// </summary>
[Serializable]
public class EmployeeState
{
    public int count;                   // 고용된 직원 수
    public float currentSatisfaction;   // 현재 만족도
    public float currentEfficiency;     // 현재 생산 효율 (0.0 = 0%, 1.0 = 100%, 2.0 = 200%)
    public int assignedCount;           // 배치된 인원 수
    public long totalSalary;            // 총 급여
    public int salaryLevel;            // 급여 레벨 (0=매우 적음, 1=적음, 2=보통, 3=많음, 4=매우 많음)

    public EmployeeState()
    {
        count = 0;
        currentSatisfaction = 0f;
        currentEfficiency = 1f; // 기본값 100% (EmployeeEntry 생성자에서 baseEfficiency로 덮어씀)
        assignedCount = 0;
        totalSalary = 0;
        salaryLevel = 2; // 기본값: 보통
    }
}
