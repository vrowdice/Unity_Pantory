using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 게임 내 직원을 관리하는 서비스 클래스
/// EmployeeData ScriptableObject를 기반으로 직원을 동적으로 관리합니다.
/// </summary>
public class EmployeeDataHandler
{
    // 직원을 저장하는 딕셔너리 (직원 ID -> EmployeeEntry)
    private Dictionary<string, EmployeeEntry> _employees;

    // 직원 변경 이벤트
    public event Action OnEmployeeChanged;

    // 급여 비율 설정
    private InitialEmployeeData _salarySettings;

    /// <summary>
    /// EmployeeService 생성자
    /// </summary>
    public EmployeeDataHandler(GameDataManager gameDataManager)
    {
        _employees = new Dictionary<string, EmployeeEntry>();
        AutoLoadAllEmployees(); // 게임 시작 시 자동으로 모든 직원 데이터 로드
    }

    // ----------------- 초기화 -----------------

    /// <summary>
    /// 지정된 경로에서 모든 EmployeeData를 자동으로 로드하여 등록합니다.
    /// </summary>
    /// <param name="employeePaths">검색할 폴더 경로 배열 (예: "Datas/Employee")</param>
    public void AutoLoadEmployees(string[] employeePaths)
    {
#if UNITY_EDITOR
        int loadedCount = 0;
        
        foreach (string path in employeePaths)
        {
            // AssetDatabase를 사용하여 모든 EmployeeData 찾기
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:EmployeeData", new[] { "Assets/" + path });
            
            foreach (string guid in guids)
            {
                string assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                EmployeeData employeeData = UnityEditor.AssetDatabase.LoadAssetAtPath<EmployeeData>(assetPath);
                
                if (employeeData != null)
                {
                    RegisterEmployee(employeeData);
                    loadedCount++;
                }
            }
        }
        
        Debug.Log($"[EmployeeService] Auto load completed: {loadedCount} employee types registered");
#else
        Debug.LogWarning("[EmployeeService] AutoLoadEmployees is only available in editor mode.");
#endif
    }

    /// <summary>
    /// 모든 EmployeeData를 자동으로 검색하여 등록합니다. (전체 Assets 폴더)
    /// </summary>
    public void AutoLoadAllEmployees()
    {
#if UNITY_EDITOR
        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:EmployeeData");
        int loadedCount = 0;
        
        foreach (string guid in guids)
        {
            string assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            EmployeeData employeeData = UnityEditor.AssetDatabase.LoadAssetAtPath<EmployeeData>(assetPath);
            
            if (employeeData != null)
            {
                RegisterEmployee(employeeData);
                loadedCount++;
            }
        }
        
        Debug.Log($"[EmployeeService] Full auto load completed: {loadedCount} employee types registered");
#else
        Debug.LogWarning("[EmployeeService] AutoLoadAllEmployees is only available in editor mode.");
#endif
    }

    /// <summary>
    /// EmployeeData를 등록하여 관리 대상에 추가합니다.
    /// </summary>
    /// <param name="employeeData">등록할 EmployeeData</param>
    public void RegisterEmployee(EmployeeData employeeData)
    {
        if (employeeData == null)
        {
            Debug.LogWarning("[EmployeeService] EmployeeData is null.");
            return;
        }

        if (string.IsNullOrEmpty(employeeData.id))
        {
            Debug.LogWarning("[EmployeeService] EmployeeData ID is empty.");
            return;
        }

        if (_employees.ContainsKey(employeeData.id))
        {
            Debug.LogWarning($"[EmployeeService] Employee type already registered: {employeeData.id}");
            return;
        }

        _employees[employeeData.id] = new EmployeeEntry(employeeData);
    }

    /// <summary>
    /// 여러 EmployeeData를 한 번에 등록합니다.
    /// </summary>
    /// <param name="employeeDataList">등록할 EmployeeData 배열</param>
    public void RegisterEmployees(EmployeeData[] employeeDataList)
    {
        foreach (var data in employeeDataList)
        {
            RegisterEmployee(data);
        }
    }

    // ----------------- Public Getters (읽기 전용) -----------------

    /// <summary>
    /// 특정 직원 유형의 인원 수를 반환합니다.
    /// </summary>
    /// <param name="employeeId">직원 유형 ID</param>
    /// <returns>해당 직원 유형의 인원 수</returns>
    public int GetEmployeeCount(string employeeId)
    {
        if (_employees.TryGetValue(employeeId, out var entry))
        {
            return entry.employeeState.count;
        }
        
        Debug.LogWarning($"[EmployeeService] Unregistered employee type: {employeeId}");
        return 0;
    }

    /// <summary>
    /// 특정 직원 유형의 총 급여를 반환합니다.
    /// </summary>
    /// <param name="employeeId">직원 유형 ID</param>
    /// <returns>총 급여</returns>
    public long GetEmployeeTotalSalary(string employeeId)
    {
        if (_employees.TryGetValue(employeeId, out var entry))
        {
            return entry.employeeState.totalSalary;
        }
        
        Debug.LogWarning($"[EmployeeService] Unregistered employee type: {employeeId}");
        return 0;
    }

    /// <summary>
    /// 특정 직원 유형의 EmployeeEntry를 반환합니다.
    /// </summary>
    /// <param name="employeeId">직원 유형 ID</param>
    /// <returns>EmployeeEntry 또는 null</returns>
    public EmployeeEntry GetEmployeeEntry(string employeeId)
    {
        if (_employees.TryGetValue(employeeId, out var entry))
        {
            return entry;
        }
        
        Debug.LogWarning($"[EmployeeService] Unregistered employee type: {employeeId}");
        return null;
    }

    /// <summary>
    /// 모든 직원 정보를 딕셔너리로 반환합니다 (읽기 전용).
    /// </summary>
    /// <returns>직원 딕셔너리의 복사본</returns>
    public Dictionary<string, EmployeeEntry> GetAllEmployees()
    {
        return new Dictionary<string, EmployeeEntry>(_employees);
    }

    /// <summary>
    /// 등록된 모든 직원 유형 ID 목록을 반환합니다.
    /// </summary>
    /// <returns>직원 유형 ID 리스트</returns>
    public List<string> GetAllEmployeeIds()
    {
        return new List<string>(_employees.Keys);
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
            total += entry.employeeState.totalSalary;
        }
        return total;
    }

    // ----------------- Public Methods (직원 고용/해고) -----------------

    /// <summary>
    /// 특정 직원 유형을 고용합니다.
    /// </summary>
    /// <param name="employeeId">고용할 직원 유형 ID</param>
    /// <param name="count">고용할 인원 수</param>
    public void HireEmployee(string employeeId, int count)
    {
        if (count <= 0)
        {
            Debug.LogWarning($"[EmployeeService] Hire count must be greater than 0. (input: {count})");
            return;
        }

        if (!_employees.TryGetValue(employeeId, out var entry))
        {
            Debug.LogWarning($"[EmployeeService] Unregistered employee type: {employeeId}");
            return;
        }

        entry.employeeState.count += count;
        UpdateSalary(entry);
        
        OnEmployeeChanged?.Invoke();
    }

    /// <summary>
    /// 특정 직원 유형을 해고합니다. 인원이 부족하면 실패합니다.
    /// </summary>
    /// <param name="employeeId">해고할 직원 유형 ID</param>
    /// <param name="count">해고할 인원 수</param>
    /// <returns>성공 시 true, 인원 부족 시 false</returns>
    public bool TryFireEmployee(string employeeId, int count)
    {
        if (count <= 0)
        {
            Debug.LogWarning($"[EmployeeService] Fire count must be greater than 0. (input: {count})");
            return true;
        }

        if (!_employees.TryGetValue(employeeId, out var entry))
        {
            Debug.LogWarning($"[EmployeeService] Unregistered employee type: {employeeId}");
            return false;
        }

        if (entry.employeeState.count >= count)
        {
            entry.employeeState.count -= count;
            UpdateSalary(entry);
            OnEmployeeChanged?.Invoke();
            return true;
        }
        else
        {
            Debug.LogWarning($"[EmployeeService] {entry.employeeData.displayName} not enough employees! (required: {count}, available: {entry.employeeState.count})");
            return false;
        }
    }

    /// <summary>
    /// 특정 직원 유형의 인원 수를 직접 설정합니다.
    /// </summary>
    /// <param name="employeeId">설정할 직원 유형 ID</param>
    /// <param name="count">설정할 인원 수</param>
    public void SetEmployeeCount(string employeeId, int count)
    {
        if (count < 0)
        {
            Debug.LogWarning($"[EmployeeService] Employee count cannot be negative. (input: {count})");
            return;
        }

        if (!_employees.TryGetValue(employeeId, out var entry))
        {
            Debug.LogWarning($"[EmployeeService] Unregistered employee type: {employeeId}");
            return;
        }

        entry.employeeState.count = count;
        UpdateSalary(entry);
        Debug.Log($"[EmployeeService] {entry.employeeData.displayName} count = {count}");
        
        OnEmployeeChanged?.Invoke();
    }

    // ----------------- Public Methods (스탯 관리) -----------------

    /// <summary>
    /// 특정 직원 유형의 숙련도를 업데이트합니다.
    /// </summary>
    /// <param name="employeeId">직원 유형 ID</param>
    /// <param name="skill">새로운 숙련도</param>
    public void SetEmployeeSkill(string employeeId)
    {
        if (!_employees.TryGetValue(employeeId, out var entry))
        {
            Debug.LogWarning($"[EmployeeService] Unregistered employee type: {employeeId}");
            return;
        }
        
        OnEmployeeChanged?.Invoke();
    }

    /// <summary>
    /// 특정 직원 유형의 피로도를 업데이트합니다.
    /// </summary>
    /// <param name="employeeId">직원 유형 ID</param>
    /// <param name="fatigue">새로운 피로도</param>
    public void SetEmployeeFatigue(string employeeId)
    {
        if (!_employees.TryGetValue(employeeId, out var entry))
        {
            Debug.LogWarning($"[EmployeeService] Unregistered employee type: {employeeId}");
            return;
        }
        
        OnEmployeeChanged?.Invoke();
    }

    /// <summary>
    /// 특정 직원 유형의 충성도를 업데이트합니다.
    /// </summary>
    /// <param name="employeeId">직원 유형 ID</param>
    /// <param name="satisfaction">새로운 만족도</param>
    public void SetEmployeeSatisfaction(string employeeId, float satisfaction)
    {
        if (!_employees.TryGetValue(employeeId, out var entry))
        {
            Debug.LogWarning($"[EmployeeService] Unregistered employee type: {employeeId}");
            return;
        }

        entry.employeeState.currentSatisfaction = Mathf.Clamp(satisfaction, 0f, 100f);
        
        OnEmployeeChanged?.Invoke();
    }

    // ----------------- Private Helper Methods -----------------

    /// <summary>
    /// 급여 비율 설정을 적용합니다.
    /// </summary>
    /// <param name="initialEmployeeData">InitialEmployeeData 설정</param>
    public void SetSalaryMultipliers(InitialEmployeeData initialEmployeeData)
    {
        if (initialEmployeeData == null)
        {
            Debug.LogWarning("[EmployeeDataHandler] InitialEmployeeData is null.");
            _salarySettings = null;
            return;
        }

        _salarySettings = initialEmployeeData;
        
        // 모든 직원의 급여 재계산
        RefreshAllSalaries();
    }

    /// <summary>
    /// 급여 레벨에 따른 비율을 반환합니다.
    /// </summary>
    /// <param name="salaryLevel">급여 레벨 (0=매우 적음, 1=적음, 2=보통, 3=많음, 4=매우 많음)</param>
    /// <returns>급여 비율</returns>
    public float GetSalaryMultiplier(int salaryLevel)
    {
        if (_salarySettings == null)
        {
            return 1.0f; // 기본값
        }
        return _salarySettings.GetSalaryMultiplier(salaryLevel);
    }

    /// <summary>
    /// 특정 직원 유형의 급여 레벨을 설정합니다.
    /// </summary>
    /// <param name="employeeId">직원 유형 ID</param>
    /// <param name="salaryLevel">급여 레벨 (0=매우 적음, 1=적음, 2=보통, 3=많음, 4=매우 많음)</param>
    public void SetEmployeeSalaryLevel(string employeeId, int salaryLevel)
    {
        if (_salarySettings == null)
        {
            Debug.LogWarning("[EmployeeDataHandler] Salary settings not initialized. Cannot set salary level.");
            return;
        }

        if (!_employees.TryGetValue(employeeId, out var entry))
        {
            Debug.LogWarning($"[EmployeeDataHandler] Unregistered employee type: {employeeId}");
            return;
        }

        // 급여 레벨 범위 검증 (0~4)
        salaryLevel = Mathf.Clamp(salaryLevel, 0, 4);
        entry.employeeState.salaryLevel = salaryLevel;
        
        // 해당 직원의 급여 재계산
        UpdateSalary(entry);
        
        string levelName = _salarySettings.GetSalaryLevelName(salaryLevel);
        Debug.Log($"[EmployeeDataHandler] {entry.employeeData.displayName} salary level set to: {levelName}");
        
        OnEmployeeChanged?.Invoke();
    }

    /// <summary>
    /// 특정 직원 유형의 현재 급여 레벨을 반환합니다.
    /// </summary>
    /// <param name="employeeId">직원 유형 ID</param>
    /// <returns>급여 레벨 (0~4), 직원이 없으면 -1</returns>
    public int GetEmployeeSalaryLevel(string employeeId)
    {
        if (!_employees.TryGetValue(employeeId, out var entry))
        {
            Debug.LogWarning($"[EmployeeDataHandler] Unregistered employee type: {employeeId}");
            return -1;
        }

        return entry.employeeState.salaryLevel;
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

            // 만족도에 따른 효율성 계산
            UpdateEfficiencyFromSatisfaction(entry);
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

    /// <summary>
    /// 급여를 업데이트합니다.
    /// </summary>
    private void UpdateSalary(EmployeeEntry entry)
    {
        if (_salarySettings == null)
        {
            // 급여 설정이 없으면 기본 급여 사용
            entry.employeeState.totalSalary = entry.employeeData.baseSalary * entry.employeeState.count;
            return;
        }

        // 각 직원의 개별 급여 레벨에 따른 비율 적용
        float salaryMultiplier = _salarySettings.GetSalaryMultiplier(entry.employeeState.salaryLevel);
        entry.employeeState.totalSalary = (long)(entry.employeeData.baseSalary * salaryMultiplier * entry.employeeState.count);
    }

    /// <summary>
    /// 모든 직원의 급여를 재계산합니다.
    /// </summary>
    private void RefreshAllSalaries()
    {
        foreach (var entry in _employees.Values)
        {
            UpdateSalary(entry);
        }
        OnEmployeeChanged?.Invoke();
    }

    // ----------------- Utility Methods -----------------

    /// <summary>
    /// 모든 직원을 초기화합니다.
    /// </summary>
    public void ResetAllEmployees()
    {
        foreach (var entry in _employees.Values)
        {
            entry.employeeState.count = 0;
            entry.employeeState.currentSatisfaction = entry.employeeData.baseSatisfaction;
            entry.employeeState.currentEfficiency = Mathf.Clamp(entry.employeeData.baseEfficiency, 0f, 2f);
            entry.employeeState.assignedCount = 0;
            entry.employeeState.totalSalary = 0;
            entry.employeeState.salaryLevel = 2; // 기본값: 보통
        }
        Debug.Log("[EmployeeService] All employees have been reset.");
        
        OnEmployeeChanged?.Invoke();
    }

    /// <summary>
    /// 특정 직원 유형이 등록되어 있는지 확인합니다.
    /// </summary>
    /// <param name="employeeId">확인할 직원 유형 ID</param>
    /// <returns>등록되어 있으면 true</returns>
    public bool IsEmployeeRegistered(string employeeId)
    {
        return _employees.ContainsKey(employeeId);
    }
}

