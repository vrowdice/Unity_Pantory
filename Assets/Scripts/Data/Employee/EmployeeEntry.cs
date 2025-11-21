using System;
using UnityEngine;

/// <summary>
/// 직원 데이터와 상태를 포함하는 엔트리
/// </summary>
[Serializable]
public class EmployeeEntry
{
    public EmployeeData employeeData;      // 직원의 정적 데이터 (ScriptableObject)
    public EmployeeState employeeState;    // 직원의 동적 상태

    public EmployeeEntry(EmployeeData data)
    {
        employeeData = data;
        employeeState = new EmployeeState();
        
        // 초기 상태를 baseData로 설정
        if (data != null)
        {
            employeeState.currentSatisfaction = data.baseSatisfaction;
            // 효율성은 0~200% 범위 (0.0~2.0)
            employeeState.currentEfficiency = Mathf.Clamp(data.baseEfficiency, 0f, 2f);
            // 급여 레벨 기본값: 보통 (2)
            employeeState.salaryLevel = 2;
        }
    }
}
