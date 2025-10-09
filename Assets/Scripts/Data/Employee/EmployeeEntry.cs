using System;

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
            employeeState.currentSkill = data.baseSkill;
            employeeState.currentFatigue = data.baseFatigue;
            employeeState.currentLoyalty = data.baseLoyalty;
        }
    }
}
