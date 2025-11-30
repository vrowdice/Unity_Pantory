using UnityEngine;

/// <summary>
/// 직원 관리 비율 및 일일 상태 업데이트
/// </summary>
public partial class EmployeeDataHandler
{
    /// <summary>
    /// 현재 행정력 비율을 반환합니다. (0.0 ~ 1.0)
    /// 1.0이면 관리 충분, 낮을수록 관리 부족.
    /// </summary>
    public float GetManagementRatio()
    {
        if (_salarySettings == null) return 1f;

        int managerCount = GetEmployeeCount("manager");
        int totalEmployees = 0;
        foreach (var entry in _employees.Values)
        {
            if (entry?.employeeState != null)
            {
                totalEmployees += entry.employeeState.count;
            }
        }

        // 관리 대상 = 전체 직원 - 매니저 본인들
        int employeesToManage = totalEmployees - managerCount;

        // 관리 대상이 없으면 완벽한 상태 (1.0)
        if (employeesToManage <= 0) return 1.0f;

        // 수용 능력
        int capacity = managerCount * _salarySettings.managerCoverage;

        // 비율 계산 (Capacity / Load)
        float ratio = (float)capacity / employeesToManage;
        
        // 1.0을 넘어가면(초과 달성) 그냥 1.0으로 고정 (슬라이더용)
        return Mathf.Clamp01(ratio);
    }

    /// <summary>
    /// 현재 매니저 수와 필요한 매니저 수 정보를 반환합니다.
    /// </summary>
    /// <param name="currentManagers">현재 매니저 수</param>
    /// <param name="requiredManagers">필요한 매니저 수</param>
    public void GetManagementInfo(out int currentManagers, out int requiredManagers)
    {
        currentManagers = 0;
        requiredManagers = 0;

        if (_salarySettings == null)
        {
            return;
        }

        currentManagers = GetEmployeeCount("manager");
        
        int totalEmployees = 0;
        foreach (var entry in _employees.Values)
        {
            if (entry?.employeeState != null)
            {
                totalEmployees += entry.employeeState.count;
            }
        }

        // 관리 대상 = 전체 직원 - 매니저 본인들
        int employeesToManage = totalEmployees - currentManagers;

        // 관리 대상이 없으면 필요 없음
        if (employeesToManage <= 0)
        {
            requiredManagers = 0;
            return;
        }

        // 필요한 매니저 수 계산 (올림 처리)
        requiredManagers = Mathf.CeilToInt((float)employeesToManage / _salarySettings.managerCoverage);
    }

    /// <summary>
    /// 일일 직원 상태 업데이트 (만족도 및 효율성)
    /// </summary>
    public void UpdateDailyEmployeeStatus()
    {
        if (_salarySettings == null)
        {
            Debug.LogWarning("[EmployeeDataHandler] Salary settings not initialized. Skipping daily update.");
            return;
        }

        // 1. 행정력 비율 계산
        float manageRatio = GetManagementRatio();
        
        // 관리 부족 비율 (1.0에서 모자란 만큼)
        // 예: 비율이 0.8이면 -> 부족분 0.2 (20% 부족)
        float deficit = 1.0f - manageRatio;

        foreach (var entry in _employees.Values)
        {
            if (entry == null || entry.employeeData == null || entry.employeeState == null)
            {
                continue;
            }

            // 각 직원의 개별 급여 레벨에 따른 만족도 변화
            int salaryLevel = entry.employeeState.salaryLevel;
            float satisfactionChange = _salarySettings.GetSatisfactionChangePerDay(salaryLevel);

            // 만족도 업데이트 (-100~100 범위로 클램프)
            entry.employeeState.currentSatisfaction = Mathf.Clamp(
                entry.employeeState.currentSatisfaction + satisfactionChange,
                -100f, 100f
            );

            // 만족도에 따른 효율성 계산 (기존 로직 - 먼저 계산)
            UpdateEfficiencyFromSatisfaction(entry);
            
            // [추가] 관리 부족 패널티 (비율이 1.0보다 작을 때만)
            if (deficit > 0.01f)
            {
                // 매니저는 패널티 면제
                if (entry.employeeData.id != "manager")
                {
                    // 부족분에 비례해서 패널티 적용
                    // 예: 50% 부족하면(0.5) -> 최대 패널티의 50% 적용
                    float satDrop = _salarySettings.maxSatisfactionPenalty * deficit;
                    float effDrop = _salarySettings.maxEfficiencyPenalty * deficit;

                    entry.employeeState.currentSatisfaction = Mathf.Clamp(
                        entry.employeeState.currentSatisfaction - satDrop,
                        -100f, 100f
                    );
                    
                    // 효율성은 만족도 기반 계산 후 추가로 감소 (최소 0.1 보장)
                    entry.employeeState.currentEfficiency = Mathf.Max(
                        0.1f,
                        entry.employeeState.currentEfficiency - effDrop
                    );
                }
            }
        }

        // 급여 재계산 (만족도 변화로 인한 효율성 변화는 급여에 직접 영향 없음)
        RefreshAllSalaries();
        
        OnEmployeeChanged?.Invoke();
    }

    /// <summary>
    /// 만족도에 따른 효율성을 업데이트합니다.
    /// </summary>
    private void UpdateEfficiencyFromSatisfaction(EmployeeEntry entry)
    {
        if (_salarySettings == null || entry == null || entry.employeeData == null || entry.employeeState == null)
        {
            return;
        }

        // 효율성 = 기본 효율성 + (만족도 * 계수)
        // 만족도는 -100~100 범위, 효율성은 0~2 범위 (0~200%)
        float baseEfficiency = entry.employeeData.baseEfficiency;
        float satisfactionEffect = entry.employeeState.currentSatisfaction * _salarySettings.satisfactionToEfficiencyRatio;
        
        // 최종 효율성 = 기본 효율성 + 만족도 영향
        // baseEfficiencyFromSatisfaction은 만족도가 0일 때의 기준값이지만, 
        // 여기서는 각 직원의 baseEfficiency를 기준으로 사용
        entry.employeeState.currentEfficiency = Mathf.Clamp(
            baseEfficiency + satisfactionEffect,
            0f, 2f
        );
    }
}

