using System;
using UnityEngine;

/// <summary>
/// 직원 데이터와 상태를 포함하는 엔트리.
/// 이펙트는 EffectDataHandler에서 instanceId = data.type.ToString() 으로 관리합니다.
/// </summary>
[Serializable]
public class EmployeeEntry
{
    public EmployeeData data;
    public EmployeeState state;

    public EmployeeEntry(EmployeeData data)
    {
        this.data = data;
        state = new EmployeeState();

        if (data != null)
        {
            state.currentSatisfaction = data.baseSatisfaction;
            state.currentEfficiency = Mathf.Clamp(data.baseEfficiency, 0f, 1f);
            state.salaryLevel = 2;
        }
    }
}