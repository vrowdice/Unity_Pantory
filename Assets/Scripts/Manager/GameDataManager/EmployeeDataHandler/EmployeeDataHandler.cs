using System;
using System.Collections.Generic;
using UnityEngine;

public partial class EmployeeDataHandler
{
    protected Dictionary<EmployeeType, EmployeeEntry> _employees;
    public event Action OnEmployeeChanged;
    protected DataManager _dataManager;
    InitialEmployeeData _initialEmployeeData;

    /// <summary>
    /// 모든 이벤트 구독을 초기화합니다.
    /// </summary>
    public void ClearAllSubscriptions()
    {
        OnEmployeeChanged = null;
    }

    /// <summary>
    /// EmployeeService 생성자
    /// </summary>
    public EmployeeDataHandler(DataManager gameDataManager, List<EmployeeData> employeeDataList, InitialEmployeeData initData)
    {
        _dataManager = gameDataManager;
        _employees = new Dictionary<EmployeeType, EmployeeEntry>();
        _initialEmployeeData = initData;

        if (employeeDataList != null && employeeDataList.Count > 0)
        {
            foreach (EmployeeData data in employeeDataList)
            {
                if (data == null) continue;
                EmployeeEntry entry = new EmployeeEntry(data);
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
        if (_employees.TryGetValue(type, out EmployeeEntry entry))
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
    public long CalculateTotalSalary()
    {
        long total = 0;
        foreach (EmployeeEntry entry in _employees.Values)
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

        if (!_employees.TryGetValue(type, out EmployeeEntry entry))
        {
            Debug.LogWarning($"[EmployeeService] Unregistered employee type: {type}");
            return;
        }

        entry.state.count += count;
        UpdateSalary(entry);

        OnEmployeeChanged.Invoke();
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

        if (!_employees.TryGetValue(type, out EmployeeEntry entry))
        {
            Debug.LogWarning($"[EmployeeService] Unregistered employee type: {type}");
            return;
        }

        entry.state.count = count;
        UpdateSalary(entry);
        Debug.Log($"[EmployeeService] {entry.data.displayName} count = {count}");

        OnEmployeeChanged.Invoke();
    }

    /// <summary>
    /// 특정 직원 유형의 숙련도를 업데이트합니다.
    /// </summary>
    /// <param name="type">직원 유형</param>
    /// <param name="skill">새로운 숙련도</param>
    public void SetEmployeeSkill(EmployeeType type)
    {
        if (!_employees.TryGetValue(type, out EmployeeEntry entry))
        {
            Debug.LogWarning($"[EmployeeService] Unregistered employee type: {type}");
            return;
        }

        OnEmployeeChanged.Invoke();
    }

    /// <summary>
    /// 특정 직원 유형의 피로도를 업데이트합니다.
    /// </summary>
    /// <param name="type">직원 유형</param>
    /// <param name="fatigue">새로운 피로도</param>
    public void SetEmployeeFatigue(EmployeeType type)
    {
        if (!_employees.TryGetValue(type, out EmployeeEntry entry))
        {
            Debug.LogWarning($"[EmployeeService] Unregistered employee type: {type}");
            return;
        }

        OnEmployeeChanged.Invoke();
    }

    /// <summary>
    /// 특정 직원 유형의 충성도를 업데이트합니다.
    /// </summary>
    /// <param name="type">직원 유형</param>
    /// <param name="satisfaction">새로운 만족도</param>
    public void SetEmployeeSatisfaction(EmployeeType type, float satisfaction)
    {
        if (!_employees.TryGetValue(type, out EmployeeEntry entry))
        {
            Debug.LogWarning($"[EmployeeService] Unregistered employee type: {type}");
            return;
        }

        entry.state.currentSatisfaction = Mathf.Clamp(satisfaction, 0f, 100f);

        OnEmployeeChanged.Invoke();
    }

    /// <summary>
    /// 특정 직원 유형의 급여 레벨을 설정합니다.
    /// </summary>
    /// <param name="type">직원 유형</param>
    /// <param name="salaryLevel">급여 레벨 (0=매우 적음, 1=적음, 2=보통, 3=많음, 4=매우 많음)</param>
    public void SetEmployeeSalaryLevel(EmployeeType type, int salaryLevel)
    {
        if (!_employees.TryGetValue(type, out EmployeeEntry entry))
        {
            Debug.LogWarning($"[EmployeeDataHandler] Unregistered employee type: {type}");
            return;
        }

        salaryLevel = Mathf.Clamp(salaryLevel, 0, 4);
        int previousSalaryLevel = entry.state.salaryLevel;
        entry.state.salaryLevel = salaryLevel;
        UpdateSalary(entry);
        ApplySalaryLevelSatisfactionEffect(type, salaryLevel, previousSalaryLevel);
        string levelName = _initialEmployeeData.GetSalaryLevelName(salaryLevel);

        OnEmployeeChanged.Invoke();
    }

    /// <summary>
    /// 급여 레벨에 따른 만족도 이펙트를 부여합니다.
    /// </summary>
    /// <param name="type">직원 유형</param>
    /// <param name="newSalaryLevel">새 급여 레벨</param>
    /// <param name="previousSalaryLevel">이전 급여 레벨</param>
    private void ApplySalaryLevelSatisfactionEffect(EmployeeType type, int newSalaryLevel, int previousSalaryLevel)
    {
        EmployeeEntry entry = GetEmployeeEntry(type);
        if (entry == null) return;

        float satisfactionChange = _initialEmployeeData.GetSatisfactionChangePerDay(newSalaryLevel);
        entry.ApplyEffect(_initialEmployeeData.salarySatisfactionEffect, satisfactionChange);
    }

    /// <summary>
    /// 특정 직원 유형의 현재 급여 레벨을 반환합니다.
    /// </summary>
    /// <param name="type">직원 유형</param>
    /// <returns>급여 레벨 (0~4), 직원이 없으면 -1</returns>
    public int GetEmployeeSalaryLevel(EmployeeType type)
    {
        if (!_employees.TryGetValue(type, out EmployeeEntry entry))
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
        float salaryMultiplier = _initialEmployeeData.GetSalaryMultiplier(entry.state.salaryLevel);
        entry.state.totalSalary = (long)(entry.data.baseSalary * salaryMultiplier * entry.state.count);
    }

    /// <summary>
    /// 모든 직원의 급여를 재계산합니다.
    /// </summary>
    protected void RefreshAllSalaries()
    {
        foreach (EmployeeEntry entry in _employees.Values)
        {
            UpdateSalary(entry);
        }

        OnEmployeeChanged?.Invoke();
    }
}