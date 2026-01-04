using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

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

        var managerEntry = GetEmployeeEntry(EmployeeType.Manager);
        int managerCount = managerEntry != null && managerEntry.state != null ? managerEntry.state.count : 0;
        int totalEmployees = 0;

        foreach (var entry in _employees.Values)
        {
            if (entry?.state != null)
            {
                totalEmployees += entry.state.count;
            }
        }

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
        {
            return;
        }

        var managerEntry = GetEmployeeEntry(EmployeeType.Manager);
        currentManagers = managerEntry != null && managerEntry.state != null ? managerEntry.state.count : 0;

        int totalEmployees = 0;
        foreach (var entry in _employees.Values)
        {
            if (entry?.state != null)
            {
                totalEmployees += entry.state.count;
            }
        }

        // 관리 대상 = 전체 직원 - 매니저 본인들
        int employeesToManage = totalEmployees - currentManagers;

        // 관리 대상이 없거나 0 이하면 필요 없음
        if (employeesToManage <= 0)
        {
            requiredManagers = 0;
            return;
        }

        // 필요한 매니저 수 계산 (올림 처리)
        requiredManagers = Mathf.CeilToInt((float)employeesToManage / _initialEmployeeData.managerCoverage);
    }

    /// <summary>
    /// 일일 직원 상태 업데이트 (만족도 및 효율성)를 수행합니다.
    /// </summary>
    public void HandleDayChanged()
    {
        // 1. 행정력 비율 계산 및 관리 부족 비율(deficit) 확인
        float manageRatio = GetManagementRatio();
        float deficit = 1.0f - manageRatio;

        // 2. 매니저 부족 시 만족도 감소 이펙트 적용/제거
        UpdateManagementDeficitEffect(deficit);

        foreach (EmployeeEntry entry in _employees.Values)
        {
            // 직원이 0명이면 만족도와 효율성을 기본값으로 리셋하고 스킵
            if (entry.state.count == 0)
            {
                entry.state.currentSatisfaction = entry.data.baseSatisfaction;
                entry.state.currentEfficiency = Mathf.Clamp(entry.data.baseEfficiency, 0f, 2f);
                continue;
            }

            // 3. 만족도 변화 계산 (모든 SatisfactionChangePerDay 이펙트 합산)
            float totalSatisfactionChange = 0f;
            List<EffectState> globalEffects = _dataManager.Effect.GetEffectStatEffects(EffectTargetType.Employee, EffectStatType.Employee_Satisfaction_Per);
            if (globalEffects != null)
            {
                foreach (var effect in globalEffects)
                {
                    if (effect != null)
                    {
                        totalSatisfactionChange += effect.value;
                    }
                }
            }

            List<EffectState> employeeSatisfactionEffects = _dataManager.Effect.GetEffectStatEffects(entry.data.type, EffectStatType.Employee_Satisfaction_Per);
            if (employeeSatisfactionEffects != null)
            {
                foreach (var effect in employeeSatisfactionEffects)
                {
                    if (effect != null)
                    {
                        totalSatisfactionChange += effect.value;
                    }
                }
            }

            // 4. 만족도 업데이트 (-100 ~ 100 범위로 클램프)
            entry.state.currentSatisfaction = Mathf.Clamp(
                entry.state.currentSatisfaction + totalSatisfactionChange,
                -100f, 100f
            );

            // 5. 효율성 계산 (만족도 영향 및 기타 이펙트 통합)
            UpdateEfficiencyFromSatisfaction(entry);

            // 6. 관리 부족 효율성 패널티 적용 (매니저 제외)
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

        _dataManager.Effect.ApplyEffect(_initialEmployeeData.satisfactionEfficiencyEffect, entry.data.type, satisfactionEfficiencyBonus);

        // 3. 이펙트 시스템을 통한 효율성 계산 (기본 효율성에서 시작하여 이펙트 적용)
        float currentEfficiency = baseEfficiency + satisfactionEfficiencyBonus;

        // 4. 최종 효율성 클램프 (0f ~ 2f)
        float finalEfficiency = Mathf.Clamp(
            currentEfficiency,
            0f, 2f
        );

        entry.state.currentEfficiency = finalEfficiency;
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
            _dataManager.Effect.RemoveEffect(_initialEmployeeData.managementDeficitEffect);
            return;
        }
        else
        {
            _dataManager.Effect.ApplyEffect(_initialEmployeeData.managementDeficitEffect, -satisfactionPenalty);
        }
    }
}
