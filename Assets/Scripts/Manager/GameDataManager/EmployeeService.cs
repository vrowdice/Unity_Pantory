using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 게임 내 직원을 관리하는 서비스 클래스
/// EmployeeData ScriptableObject를 기반으로 직원을 동적으로 관리합니다.
/// </summary>
public class EmployeeService
{
    // 직원을 저장하는 딕셔너리 (직원 ID -> EmployeeEntry)
    private Dictionary<string, EmployeeEntry> _employees;

    // 직원 변경 이벤트
    public event Action OnEmployeeChanged;

    /// <summary>
    /// EmployeeService 생성자
    /// </summary>
    public EmployeeService()
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
        
        Debug.Log($"[EmployeeService] 자동 로드 완료: {loadedCount}개의 직원 유형 등록됨");
#else
        Debug.LogWarning("[EmployeeService] AutoLoadEmployees는 에디터에서만 사용 가능합니다.");
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
        
        Debug.Log($"[EmployeeService] 전체 자동 로드 완료: {loadedCount}개의 직원 유형 등록됨");
#else
        Debug.LogWarning("[EmployeeService] AutoLoadAllEmployees는 에디터에서만 사용 가능합니다.");
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
            Debug.LogWarning("[EmployeeService] EmployeeData가 null입니다.");
            return;
        }

        if (string.IsNullOrEmpty(employeeData.id))
        {
            Debug.LogWarning("[EmployeeService] EmployeeData의 ID가 비어있습니다.");
            return;
        }

        if (_employees.ContainsKey(employeeData.id))
        {
            Debug.LogWarning($"[EmployeeService] 이미 등록된 직원 유형입니다: {employeeData.id}");
            return;
        }

        _employees[employeeData.id] = new EmployeeEntry(employeeData);
        Debug.Log($"[EmployeeService] 직원 유형 등록: {employeeData.displayName} ({employeeData.id})");
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
        
        Debug.LogWarning($"[EmployeeService] 등록되지 않은 직원 유형입니다: {employeeId}");
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
        
        Debug.LogWarning($"[EmployeeService] 등록되지 않은 직원 유형입니다: {employeeId}");
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
        
        Debug.LogWarning($"[EmployeeService] 등록되지 않은 직원 유형입니다: {employeeId}");
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
            Debug.LogWarning($"[EmployeeService] 고용 인원은 0보다 커야 합니다. (입력값: {count})");
            return;
        }

        if (!_employees.TryGetValue(employeeId, out var entry))
        {
            Debug.LogWarning($"[EmployeeService] 등록되지 않은 직원 유형입니다: {employeeId}");
            return;
        }

        entry.employeeState.count += count;
        UpdateSalary(entry);
        Debug.Log($"[EmployeeService] {entry.employeeData.displayName} 고용 +{count} (총: {entry.employeeState.count})");
        
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
            Debug.LogWarning($"[EmployeeService] 해고 인원은 0보다 커야 합니다. (입력값: {count})");
            return true;
        }

        if (!_employees.TryGetValue(employeeId, out var entry))
        {
            Debug.LogWarning($"[EmployeeService] 등록되지 않은 직원 유형입니다: {employeeId}");
            return false;
        }

        if (entry.employeeState.count >= count)
        {
            entry.employeeState.count -= count;
            UpdateSalary(entry);
            Debug.Log($"[EmployeeService] {entry.employeeData.displayName} 해고 -{count} (총: {entry.employeeState.count})");
            
            OnEmployeeChanged?.Invoke();
            return true;
        }
        else
        {
            Debug.LogWarning($"[EmployeeService] {entry.employeeData.displayName} 인원 부족! (필요: {count}, 보유: {entry.employeeState.count})");
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
            Debug.LogWarning($"[EmployeeService] 인원 수는 음수가 될 수 없습니다. (입력값: {count})");
            return;
        }

        if (!_employees.TryGetValue(employeeId, out var entry))
        {
            Debug.LogWarning($"[EmployeeService] 등록되지 않은 직원 유형입니다: {employeeId}");
            return;
        }

        entry.employeeState.count = count;
        UpdateSalary(entry);
        Debug.Log($"[EmployeeService] {entry.employeeData.displayName} 인원 = {count}");
        
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
            Debug.LogWarning($"[EmployeeService] 등록되지 않은 직원 유형입니다: {employeeId}");
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
            Debug.LogWarning($"[EmployeeService] 등록되지 않은 직원 유형입니다: {employeeId}");
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
            Debug.LogWarning($"[EmployeeService] 등록되지 않은 직원 유형입니다: {employeeId}");
            return;
        }

        entry.employeeState.currentSatisfaction = Mathf.Clamp(satisfaction, 0f, 100f);
        
        OnEmployeeChanged?.Invoke();
    }

    // ----------------- Private Helper Methods -----------------

    /// <summary>
    /// 급여를 업데이트합니다.
    /// </summary>
    private void UpdateSalary(EmployeeEntry entry)
    {
        entry.employeeState.totalSalary = entry.employeeData.baseSalary * entry.employeeState.count;
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
            entry.employeeState.currentEfficiency = 1f;
            entry.employeeState.assignedCount = 0;
            entry.employeeState.totalSalary = 0;
        }
        Debug.Log("[EmployeeService] 모든 직원이 초기화되었습니다.");
        
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

