using System;
using UnityEngine;

/// <summary>
/// 직원 데이터와 상태를 포함하는 엔트리.
/// 이펙트는 EffectDataHandler에서 instanceId = data.type.ToString() 으로 관리합니다.
/// </summary>
[Serializable]
public class EmployeeEntry
{
    [Tooltip("직군 ScriptableObject")]
    public EmployeeData data;
    [Tooltip("현재 인원·만족도·급여 등 런타임 상태")]
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
