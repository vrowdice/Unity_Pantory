using System;

/// <summary>
/// 직원의 현재 상태를 저장하는 클래스
/// </summary>
[Serializable]
public class EmployeeState
{
    public int count;                   // 고용된 직원 수
    public float currentSatisfaction;   // 현재 만족도
    public float currentEfficiency;     // 현재 생산 효율
    public int assignedCount;           // 배치된 인원 수
    public int workValue;            // 추가되는 일의 진행도
    public long totalSalary;            // 총 급여
    public bool hasUnion;                // 노조 가입 여부

    public EmployeeState()
    {
        count = 0;
        currentSatisfaction = 0f;
        currentEfficiency = 1f;
        assignedCount = 0;
        totalSalary = 0;
        hasUnion = false;
    }
}
