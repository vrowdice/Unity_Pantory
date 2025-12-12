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
    /// <summary>
    /// EmployeeDataHandler 관리 파트 (Management)
    /// </summary>
    public float GetManagementRatio()
    {
        if (_salarySettings == null) return 1f;

        int managerCount = GetEmployeeCount("manager");
        int totalEmployees = 0;

        // 1. 전체 직원 수 계산
        foreach (var entry in _employees.Values)
        {
            if (entry?.employeeState != null)
            {
                totalEmployees += entry.employeeState.count;
            }
        }

        // 2. 관리 대상 직원 수 계산 (전체 직원 - 매니저 본인들)
        int employeesToManage = totalEmployees - managerCount;

        // 관리 대상이 없으면 완벽한 상태 (1.0)
        if (employeesToManage <= 0) return 1.0f;

        // 3. 현재 관리 수용 능력 계산
        int capacity = managerCount * _salarySettings.managerCoverage;

        // 4. 비율 계산 (Capacity / Load)
        float ratio = (float)capacity / employeesToManage;

        // 5. 1.0을 넘어가면 1.0으로 고정
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

        // 관리 대상이 없거나 0 이하면 필요 없음
        if (employeesToManage <= 0)
        {
            requiredManagers = 0;
            return;
        }

        // 필요한 매니저 수 계산 (올림 처리)
        // requiredManagers = CEIL(employeesToManage / managerCoverage)
        requiredManagers = Mathf.CeilToInt((float)employeesToManage / _salarySettings.managerCoverage);
    }

    /// <summary>
    /// 일일 직원 상태 업데이트 (만족도 및 효율성)를 수행합니다.
    /// </summary>
    /// <summary>
    /// 일일 직원 상태 업데이트 (만족도 및 효율성)를 수행합니다.
    /// </summary>
    public void UpdateDailyEmployeeStatus()
    {
        if (_salarySettings == null)
        {
            Debug.LogWarning("[EmployeeDataHandler] Salary settings not initialized. Skipping daily update.");
            return;
        }

        // 1. 행정력 비율 계산 및 관리 부족 비율(deficit) 확인
        float manageRatio = GetManagementRatio();
        float deficit = 1.0f - manageRatio; // 관리 부족 비율 (0.0 ~ 1.0)

        // 2. 매니저 부족 시 만족도 감소 이펙트 적용/제거
        UpdateManagementDeficitEffect(deficit);

        foreach (var entry in _employees.Values)
        {
            if (entry == null || entry.employeeData == null || entry.employeeState == null)
            {
                continue;
            }

            // [추가] 직원이 0명이면 만족도와 효율성을 기본값으로 리셋하고 스킵
            if (entry.employeeState.count == 0)
            {
                entry.employeeState.currentSatisfaction = entry.employeeData.baseSatisfaction;
                entry.employeeState.currentEfficiency = Mathf.Clamp(entry.employeeData.baseEfficiency, 0f, 2f);
                // 효과도 제거해야 할 수 있지만, 일단 기본값 복구만 수행
                continue;
            }

            // A. 만족도 변화 계산 (행정력 부족 등 이펙트 시스템만 사용)
            float totalSatisfactionChange = 0f;
            
            // 1) 전역 이펙트 계산
            if (_gameDataManager?.Effect != null)
            {
                totalSatisfactionChange = _gameDataManager.Effect.CalculateStat(
                    StatType.SatisfactionChangePerDay,
                    0f,
                    null
                );
            }

            // 2) 개별 이펙트 계산 (전역 값을 베이스로)
            totalSatisfactionChange = entry.CalculateStat(StatType.SatisfactionChangePerDay, totalSatisfactionChange);

            // B. 만족도 업데이트 (-100 ~ 100 범위로 클램프)
            entry.employeeState.currentSatisfaction = Mathf.Clamp(
                entry.employeeState.currentSatisfaction + totalSatisfactionChange,
                -100f, 100f
            );

            // C. 효율성 계산 (만족도 영향 및 기타 이펙트 통합)
            UpdateEfficiencyFromSatisfaction(entry);

            // D. [추가] 관리 부족 효율성 패널티 적용 (매니저 제외)
            if (deficit > 0.01f && entry.employeeData.id != "manager")
            {
                // 부족분에 비례해서 효율성 패널티 적용
                float effDrop = _salarySettings.maxEfficiencyPenalty * deficit;

                // 효율성은 계산된 값에서 추가로 감소 (최소 0.1 보장)
                entry.employeeState.currentEfficiency = Mathf.Max(
                    0.1f,
                    entry.employeeState.currentEfficiency - effDrop
                );
            }
        }

        // 3. 급여 재계산
        RefreshAllSalaries();

        OnEmployeeChanged?.Invoke();
    }

    /// <summary>
    /// 만족도 및 기타 이펙트 시스템을 고려하여 최종 효율성을 업데이트합니다.
    /// 또한 만족도에 따른 효율 보너스/패널티를 이펙트로 등록하여 UI에 표시되게 합니다.
    /// </summary>
    private void UpdateEfficiencyFromSatisfaction(EmployeeEntry entry)
    {
        if (_salarySettings == null || entry == null || entry.employeeData == null || entry.employeeState == null)
        {
            return;
        }

        float baseEfficiency = entry.employeeData.baseEfficiency;

        // 1. 만족도에 따른 효율성 변화 계산
        float currentSatisfaction = entry.employeeState.currentSatisfaction;
        float satisfactionEfficiencyBonus = currentSatisfaction * _salarySettings.satisfactionToEfficiencyRatio;

        // 2. 만족도 효율 보너스를 이펙트로 변환하여 등록 (UI 표시 및 통합 계산용)
        const string satEffectId = "Satisfaction_Efficiency";
        entry.RemoveEffectById(satEffectId); // 기존 값 제거

        if (Mathf.Abs(satisfactionEfficiencyBonus) > 0.001f) // 유의미한 값일 때만 등록
        {
            EffectData satEffect = new EffectData
            {
                id = satEffectId,
                displayName = satisfactionEfficiencyBonus >= 0 ? "Satisfaction Bonus" : "Satisfaction Penalty",
                statType = StatType.EfficiencyBonus,
                type = ModifierType.PercentAdd, // [변경] 퍼센트 연산으로 변경하여 UI에 %로 표시되게 함 (-0.05 -> -5%)
                value = satisfactionEfficiencyBonus,
                targetCategory = null,
                durationDays = 0f // 영구 (매일 갱신됨)
            };
            entry.AddEffect(satEffect);
        }

        // 3. 이펙트 시스템을 통한 효율성 계산 (기본 효율성에서 시작하여 이펙트 적용)
        float currentEfficiency = baseEfficiency;
        
        // 3-1) 전역 이펙트 적용
        if (_gameDataManager?.Effect != null)
        {
            currentEfficiency = _gameDataManager.Effect.CalculateStat(
                StatType.EfficiencyBonus,
                currentEfficiency, // [변경] 기본값 0에서 currentEfficiency(base)로 변경하여 Percent 이펙트 적용 가능하게 함
                null
            );
        }
        
        // 3-2) 개별 이펙트 적용 (전역 계산 결과를 베이스로)
        currentEfficiency = entry.CalculateStat(StatType.EfficiencyBonus, currentEfficiency);
        
        // 4. 최종 효율성 클램프 (0f ~ 2f)
        float finalEfficiency = Mathf.Clamp(
            currentEfficiency,
            0f, 2f
        );

        // [Debug] 효율성이 200%에 도달했을 때 상세 로그 출력 (원인 분석용)
        if (finalEfficiency >= 1.99f && entry.employeeState.currentEfficiency < 1.99f) // 변경되는 시점에만 로그
        {
            Debug.Log($"[Efficiency Maxed] ID: {entry.employeeData.id} | Base: {baseEfficiency} | Sat: {currentSatisfaction} (Bonus: {satisfactionEfficiencyBonus}) | Total Calc: {currentEfficiency}");
        }

        entry.employeeState.currentEfficiency = finalEfficiency;
    }

    /// <summary>
    /// 매니저 부족 시 만족도 감소 이펙트를 업데이트합니다.
    /// 이펙트는 StatType.SatisfactionChangePerDay 에 영향을 줍니다.
    /// </summary>
    /// <param name="deficit">관리 부족 비율 (0.0 ~ 1.0)</param>
    private void UpdateManagementDeficitEffect(float deficit)
    {
        if (_gameDataManager?.Effect == null || _salarySettings == null)
        {
            return;
        }

        const string effectId = "Management_Deficit_Satisfaction";

        // 관리가 충분하면 이펙트 제거
        if (deficit <= 0.01f)
        {
            _gameDataManager.Effect.RemoveEffectById(effectId);
            return;
        }

        // 부족분에 비례해서 만족도 감소 이펙트 계산 (최대 페널티에 부족 비율 곱)
        float satisfactionPenalty = _salarySettings.maxSatisfactionPenalty * deficit;

        // 기존 이펙트가 있으면 제거 후 재생성 (값 업데이트)
        _gameDataManager.Effect.RemoveEffectById(effectId);

        // 매니저 부족 만족도 감소 이펙트 생성
        EffectData deficitEffect = new EffectData
        {
            id = effectId,
            displayName = "Management Deficit",
            statType = StatType.SatisfactionChangePerDay,
            type = ModifierType.Flat,
            value = -satisfactionPenalty, // 음수 값으로 감소 (매일 만족도 감소)
            targetCategory = null, // 전역 적용
            durationDays = 0f // 영구 효과 (관리 비율이 개선될 때까지)
        };

        _gameDataManager.Effect.AddEffect(deficitEffect);
    }
}

