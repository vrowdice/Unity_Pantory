using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 직원 관리 비율 및 일일 상태 업데이트
/// </summary>
public partial class EmployeeDataHandler
{
    /// <summary>
    /// 현재 행정력 비율을 반환합니다. (0.0 ~ 1.0)
    /// 1.0이면 관리가 충분하며, 낮을수록 관리가 부족합니다.
    /// </summary>
    public float GetManagementRatio()
    {
        if (_initialEmployeeData == null) return 1f;

        int managerCount = TryGetEntry(EmployeeType.Manager, out EmployeeEntry managerEntry) ? managerEntry.state.count : 0;
        int totalEmployees = 0;
        foreach (EmployeeEntry entry in _employees.Values)
            totalEmployees += entry.state.count;

        int employeesToManage = totalEmployees - managerCount;
        if (employeesToManage <= 0) return 1.0f;
        int capacity = managerCount * _initialEmployeeData.managerCoverage;
        float ratio = (float)capacity / employeesToManage;
        return Mathf.Clamp01(ratio);
    }

    /// <summary>
    /// 현재 매니저 수와 필요한 매니저 수 정보를 반환합니다.
    /// </summary>
    /// <param name="currentManagers">현재 매니저 수</param>
    /// <param name="requiredManagers">필요한 최소 매니저 수</param>
    public void GetManagementInfo(out int currentManagers, out int requiredManagers)
    {
        currentManagers = 0;
        requiredManagers = 0;

        if (_initialEmployeeData == null)
            return;

        currentManagers = TryGetEntry(EmployeeType.Manager, out EmployeeEntry managerEntry) ? managerEntry.state.count : 0;
        int totalEmployees = 0;
        foreach (EmployeeEntry entry in _employees.Values)
            totalEmployees += entry.state.count;

        int employeesToManage = totalEmployees - currentManagers;
        if (employeesToManage <= 0)
        {
            requiredManagers = 0;
            return;
        }

        requiredManagers = Mathf.CeilToInt((float)employeesToManage / _initialEmployeeData.managerCoverage);
    }

    /// <summary>
    /// 일일 직원 상태 업데이트 (만족도 및 효율성)를 수행합니다.
    /// </summary>
    public void HandleDayChanged()
    {
        float manageRatio = GetManagementRatio();
        float deficit = 1.0f - manageRatio;
        UpdateManagementDeficitEffect(deficit);

        foreach (EmployeeEntry entry in _employees.Values)
        {
            if (entry.state.count == 0)
            {
                continue;
            }

            string instanceId = GetInstanceIdForEmployee(entry);
            List<EffectState> satisfactionEffects = _dataManager.Effect.GetEffectStatEffects(EffectTargetType.Employee, EffectStatType.Employee_Satisfaction, instanceId);
            float totalSatisfactionChange = EffectUtils.ComputeStatFromEffects(0f, satisfactionEffects);

            entry.state.currentSatisfaction = Mathf.Clamp(
                entry.state.currentSatisfaction + totalSatisfactionChange,
                -100f, 100f
            );

            UpdateEfficiencyFromSatisfaction(entry);

            if (deficit > 0.01f && entry.data.type != EmployeeType.Manager)
            {
                float effDrop = _initialEmployeeData.maxEfficiencyPenalty * deficit;
                entry.state.currentEfficiency = Mathf.Max(
                    0.1f,
                    entry.state.currentEfficiency - effDrop
                );
            }
        }

        RefreshAllSalaries();
        SyncAssignedCountsFromThreads(_dataManager.ThreadPlacement);

        OnEmployeeChanged?.Invoke();
    }

    /// <summary>
    /// 만족도 및 기타 이펙트 시스템을 고려하여 최종 효율성을 업데이트합니다.
    /// 또한 만족도에 따른 효율 보너스/패널티를 이펙트로 등록하여 UI에 표시되게 합니다.
    /// </summary>
    private void UpdateEfficiencyFromSatisfaction(EmployeeEntry entry)
    {
        float baseEfficiency = entry.data.baseEfficiency;
        float currentSatisfaction = entry.state.currentSatisfaction;
        float satisfactionEfficiencyBonus = currentSatisfaction * _initialEmployeeData.satisfactionToEfficiencyRatio;

        EffectData efficiencyEffectData = _dataManager.InitialEffectData.satisfactionEfficiencyEffect;
        string instanceId = GetInstanceIdForEffect(efficiencyEffectData, entry);
        _dataManager.Effect.ApplyEffect(efficiencyEffectData, satisfactionEfficiencyBonus, instanceId);

        List<EffectState> efficiencyEffects = _dataManager.Effect.GetEffectStatEffects(EffectTargetType.Employee, EffectStatType.Employee_Efficiency, instanceId);
        float finalEfficiency = EffectUtils.ComputeStatFromEffects(baseEfficiency, efficiencyEffects);
        entry.state.currentEfficiency = Mathf.Clamp(finalEfficiency, 0f, 2f);
    }

    /// <summary>
    /// 매니저 부족 시 만족도 감소 이펙트를 업데이트합니다.
    /// 이펙트는 StatType.SatisfactionChangePerDay 에 영향을 줍니다.
    /// </summary>
    /// <param name="deficit">관리 부족 비율 (0.0 ~ 1.0)</param>
    private void UpdateManagementDeficitEffect(float deficit)
    {
        float satisfactionPenalty = deficit > 0.01f ? _initialEmployeeData.maxSatisfactionPenalty * deficit : 0f;
        if (satisfactionPenalty <= 0f)
        {
            _dataManager.Effect.RemoveEffect(_dataManager.InitialEffectData.managementDeficitEffect);
            return;
        }
        else
        {
            _dataManager.Effect.ApplyEffect(_dataManager.InitialEffectData.managementDeficitEffect, -satisfactionPenalty);
        }
    }
}
