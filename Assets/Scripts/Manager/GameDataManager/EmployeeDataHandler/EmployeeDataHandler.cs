using System;
using System.Collections.Generic;
using UnityEngine;

public partial class EmployeeDataHandler
{
    protected Dictionary<EmployeeType, EmployeeEntry> _employees;
    public event Action OnEmployeeChanged;
    protected GameDataManager _gameDataManager;
    InitialEmployeeData _salarySettings;

    /// <summary>
    /// EmployeeService 생성자
    /// </summary>
    public EmployeeDataHandler(GameDataManager gameDataManager, List<EmployeeData> employeeDataList, InitialEmployeeData initData)
    {
        _gameDataManager = gameDataManager;
        _employees = new Dictionary<EmployeeType, EmployeeEntry>();
        _salarySettings = initData;

        if (employeeDataList != null && employeeDataList.Count > 0)
        {
            foreach(EmployeeData data in employeeDataList)
            {
                if (data == null) continue;
                var entry = new EmployeeEntry(data);
                _employees[data.type] = entry;
            }
        }
        else
        {
            Debug.LogWarning("[EmployeeService] EmployeeDataList is null or empty. No employees registered.");
        }

        RefreshAllSalaries();
    }

    /// <summary>
    /// 특정 직원 유형의 EmployeeEntry를 반환합니다.
    /// </summary>
    /// <param name="employeeId">직원 유형 ID</param>
    /// <returns>EmployeeEntry 또는 null</returns>
    public EmployeeEntry GetEmployeeEntry(EmployeeType type)
    {
        if (_employees.TryGetValue(type, out var entry))
        {
            return entry;
        }
        
        Debug.LogWarning($"[EmployeeService] Unregistered employee type: {type}");
        return null;
    }

    /// <summary>
    /// 모든 직원 정보를 딕셔너리로 반환합니다 (읽기 전용).
    /// </summary>
    /// <returns>직원 딕셔너리의 복사본</returns>
    public Dictionary<EmployeeType, EmployeeEntry> GetAllEmployees()
    {
        return new Dictionary<EmployeeType, EmployeeEntry>(_employees);
    }

    /// <summary>
    /// 모든 직원의 총 급여를 반환합니다.
    /// </summary>
    /// <returns>총 급여</returns>
    public long GetTotalSalary()
    {
        long total = 0;
        foreach (var entry in _employees.Values)
        {
            total += entry.state.totalSalary;
        }
        return total;
    }

    /// <summary>
    /// 특정 직원 유형을 고용합니다.
    /// </summary>
    /// <param name="type">고용할 직원 유형</param>
    /// <param name="count">고용할 인원 수</param>
    public void HireEmployee(EmployeeType type, int count)
    {
        if (count <= 0)
        {
            Debug.LogWarning($"[EmployeeService] Hire count must be greater than 0. (input: {count})");
            return;
        }

        if (!_employees.TryGetValue(type, out var entry))
        {
            Debug.LogWarning($"[EmployeeService] Unregistered employee type: {type}");
            return;
        }

        entry.state.count += count;
        UpdateSalary(entry);
        
        OnEmployeeChanged?.Invoke();
    }

    /// <summary>
    /// 특정 직원 유형의 인원 수를 직접 설정합니다.
    /// </summary>
    /// <param name="type">설정할 직원 유형</param>
    /// <param name="count">설정할 인원 수</param>
    public void SetEmployeeCount(EmployeeType type, int count)
    {
        if (count < 0)
        {
            Debug.LogWarning($"[EmployeeService] Employee count cannot be negative. (input: {count})");
            return;
        }

        if (!_employees.TryGetValue(type, out var entry))
        {
            Debug.LogWarning($"[EmployeeService] Unregistered employee type: {type}");
            return;
        }

        entry.state.count = count;
        UpdateSalary(entry);
        Debug.Log($"[EmployeeService] {entry.employeeData.displayName} count = {count}");
        
        OnEmployeeChanged?.Invoke();
    }

    /// <summary>
    /// 특정 직원 유형의 숙련도를 업데이트합니다.
    /// </summary>
    /// <param name="type">직원 유형</param>
    /// <param name="skill">새로운 숙련도</param>
    public void SetEmployeeSkill(EmployeeType type)
    {
        if (!_employees.TryGetValue(type, out var entry))
        {
            Debug.LogWarning($"[EmployeeService] Unregistered employee type: {type}");
            return;
        }
        
        OnEmployeeChanged?.Invoke();
    }

    /// <summary>
    /// 특정 직원 유형의 피로도를 업데이트합니다.
    /// </summary>
    /// <param name="type">직원 유형</param>
    /// <param name="fatigue">새로운 피로도</param>
    public void SetEmployeeFatigue(EmployeeType type)
    {
        if (!_employees.TryGetValue(type, out var entry))
        {
            Debug.LogWarning($"[EmployeeService] Unregistered employee type: {type}");
            return;
        }
        
        OnEmployeeChanged?.Invoke();
    }

    /// <summary>
    /// 특정 직원 유형의 충성도를 업데이트합니다.
    /// </summary>
    /// <param name="type">직원 유형</param>
    /// <param name="satisfaction">새로운 만족도</param>
    public void SetEmployeeSatisfaction(EmployeeType type, float satisfaction)
    {
        if (!_employees.TryGetValue(type, out var entry))
        {
            Debug.LogWarning($"[EmployeeService] Unregistered employee type: {type}");
            return;
        }

        entry.state.currentSatisfaction = Mathf.Clamp(satisfaction, 0f, 100f);
        
        OnEmployeeChanged?.Invoke();
    }

    /// <summary>
    /// 특정 직원 유형의 급여 레벨을 설정합니다.
    /// </summary>
    /// <param name="type">직원 유형</param>
    /// <param name="salaryLevel">급여 레벨 (0=매우 적음, 1=적음, 2=보통, 3=많음, 4=매우 많음)</param>
    public void SetEmployeeSalaryLevel(EmployeeType type, int salaryLevel)
    {
        if (!_employees.TryGetValue(type, out var entry))
        {
            Debug.LogWarning($"[EmployeeDataHandler] Unregistered employee type: {type}");
            return;
        }

        // 급여 레벨 범위 검증 (0~4)
        salaryLevel = Mathf.Clamp(salaryLevel, 0, 4);
        
        // 이전 급여 레벨 저장
        int previousSalaryLevel = entry.state.salaryLevel;
        
        // 급여 레벨 변경
        entry.state.salaryLevel = salaryLevel;
        
        // 해당 직원의 급여 재계산
        UpdateSalary(entry);
        
        // 급여 레벨에 따른 만족도 이펙트 부여
        ApplySalaryLevelSatisfactionEffect(type, salaryLevel, previousSalaryLevel);
        
        string levelName = _salarySettings.GetSalaryLevelName(salaryLevel);
        
        OnEmployeeChanged?.Invoke();
    }

    /// <summary>
    /// 급여 레벨에 따른 만족도 이펙트를 부여합니다.
    /// </summary>
    /// <param name="type">직원 유형</param>
    /// <param name="newSalaryLevel">새 급여 레벨</param>
    /// <param name="previousSalaryLevel">이전 급여 레벨</param>
    private void ApplySalaryLevelSatisfactionEffect(EmployeeType type, int newSalaryLevel, int previousSalaryLevel)
    {
        var entry = GetEmployeeEntry(type);
        if (entry == null) return;

        // 이전 급여 이펙트 제거 (ID를 "Salary_Satisfaction" 으로 통일하여 관리)
        entry.RemoveEffectById("Salary_Satisfaction");

        // 새 급여 레벨에 따른 만족도 변화량 가져오기
        float satisfactionChange = _salarySettings.GetSatisfactionChangePerDay(newSalaryLevel);

        // 만족도 변화가 0이 아니면 이펙트 부여
        if (Mathf.Abs(satisfactionChange) > 0.001f)
        {
            // 급여 레벨별 만족도 이펙트 생성
            EffectData salaryEffect = new EffectData
            {
                id = "Salary_Satisfaction", // 직원의 지역 이펙트이므로 단순화된 ID 사용
                displayName = $"Salary Level Satisfaction ({_salarySettings.GetSalaryLevelName(newSalaryLevel)})",
                statType = StatType.SatisfactionChangePerDay,
                type = ModifierType.Flat,
                value = satisfactionChange,
                targetCategory = null, 
                durationDays = 0f // 영구 효과
            };

            entry.AddEffect(salaryEffect);
            Debug.Log($"[EmployeeDataHandler] Applied local satisfaction effect for {type}: {satisfactionChange:F1} per day");
        }
    }

    /// <summary>
    /// 특정 직원 유형의 현재 급여 레벨을 반환합니다.
    /// </summary>
    /// <param name="type">직원 유형</param>
    /// <returns>급여 레벨 (0~4), 직원이 없으면 -1</returns>
    public int GetEmployeeSalaryLevel(EmployeeType type)
    {
        if (!_employees.TryGetValue(type, out var entry))
        {
            Debug.LogWarning($"[EmployeeDataHandler] Unregistered employee type: {type}");
            return -1;
        }

        return entry.state.salaryLevel;
    }

    /// <summary>
    /// 급여를 업데이트합니다.
    /// </summary>
    protected void UpdateSalary(EmployeeEntry entry)
    {
        float salaryMultiplier = _salarySettings.GetSalaryMultiplier(entry.state.salaryLevel);
        entry.state.totalSalary = (long)(entry.employeeData.baseSalary * salaryMultiplier * entry.state.count);
    }

    /// <summary>
    /// 모든 직원의 급여를 재계산합니다.
    /// </summary>
    protected void RefreshAllSalaries()
    {
        foreach (var entry in _employees.Values)
        {
            UpdateSalary(entry);
        }
        OnEmployeeChanged?.Invoke();
    }

    /// <summary>
    /// 모든 직원을 초기화합니다.
    /// </summary>
    public void ResetAllEmployees()
    {
        foreach (var entry in _employees.Values)
        {
            entry.state.count = 0;
            entry.state.currentSatisfaction = entry.employeeData.baseSatisfaction;
            entry.state.currentEfficiency = Mathf.Clamp(entry.employeeData.baseEfficiency, 0f, 2f);
            entry.state.assignedCount = 0;
            entry.state.totalSalary = 0;
            entry.state.salaryLevel = 2; // 기본값: 보통
        }
        Debug.Log("[EmployeeService] All employees have been reset.");
        
        OnEmployeeChanged?.Invoke();
    }

    /// <summary>
    /// 특정 직원 유형이 등록되어 있는지 확인합니다.
    /// </summary>
    /// <param name="type">확인할 직원 유형</param>
    /// <returns>등록되어 있으면 true</returns>
    public bool IsEmployeeRegistered(EmployeeType type)
    {
        return _employees.ContainsKey(type);
    }
}
