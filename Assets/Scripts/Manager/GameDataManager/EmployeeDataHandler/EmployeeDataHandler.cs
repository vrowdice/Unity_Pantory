using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 게임 내 직원을 관리하는 서비스 클래스
/// EmployeeData ScriptableObject를 기반으로 직원을 동적으로 관리합니다.
/// </summary>
public partial class EmployeeDataHandler
{
    // 직원을 저장하는 딕셔너리 (직원 ID -> EmployeeEntry)
    protected Dictionary<string, EmployeeEntry> _employees;

    // 직원 변경 이벤트
    public event Action OnEmployeeChanged;

    // 급여 비율 설정
    protected InitialEmployeeData _salarySettings;

    // GameDataManager 참조 (이펙트 시스템 접근용)
    protected GameDataManager _gameDataManager;

    /// <summary>
    /// EmployeeService 생성자
    /// </summary>
    public EmployeeDataHandler(GameDataManager gameDataManager, List<EmployeeData> employeeDataList = null)
    {
        _gameDataManager = gameDataManager;
        _employees = new Dictionary<string, EmployeeEntry>();
        
        if (employeeDataList != null && employeeDataList.Count > 0)
        {
            // 리스트에서 딕셔너리로 변환
            RegisterEmployees(employeeDataList.ToArray());
            Debug.Log($"[EmployeeService] Initialized with {employeeDataList.Count} employees from list.");
        }
        else
        {
            // 리스트가 없으면 기존 방식으로 자동 로드
            AutoLoadAllEmployees();
        }
    }

    /// <summary>
    /// 지정된 경로에서 모든 EmployeeData를 자동으로 로드하여 등록합니다.
    /// 에디터에서는 AssetDatabase를 사용하고, 빌드된 게임에서는 Resources 폴더를 사용합니다.
    /// </summary>
    /// <param name="employeePaths">검색할 폴더 경로 배열 (예: "Datas/Employee")</param>
    public void AutoLoadEmployees(string[] employeePaths)
    {
#if UNITY_EDITOR
        // 에디터 모드: AssetDatabase를 사용하여 지정된 경로에서 EmployeeData 찾기
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
        string resourcePath = employeePaths != null && employeePaths.Length > 0 ? employeePaths[0] : "Datas/Employee";
        EmployeeData[] employeeDataList = Resources.LoadAll<EmployeeData>(resourcePath);
        if (employeeDataList != null && employeeDataList.Length > 0)
        {
            RegisterEmployees(employeeDataList);
            Debug.Log($"[EmployeeService] Runtime load completed: {employeeDataList.Length} employee types registered from {resourcePath}.");
        }
        else
        {
            Debug.LogWarning($"[EmployeeService] No EmployeeData found in Resources/{resourcePath}. Make sure EmployeeData files are placed in the Resources folder.");
        }
#endif
    }

    /// <summary>
    /// 모든 EmployeeData를 자동으로 검색하여 등록합니다. (전체 Assets 폴더)
    /// 에디터에서는 AssetDatabase를 사용하고, 빌드된 게임에서는 Resources 폴더를 사용합니다.
    /// </summary>
    public void AutoLoadAllEmployees()
    {
#if UNITY_EDITOR
        // 에디터 모드: AssetDatabase를 사용하여 모든 EmployeeData 찾기
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
        // 빌드 모드: Resources 폴더에서 로드
        // 주의: EmployeeData 파일들이 Resources/Datas/Employee 폴더에 있어야 합니다.
        EmployeeData[] employeeDataList = Resources.LoadAll<EmployeeData>("Datas/Employee");
        if (employeeDataList != null && employeeDataList.Length > 0)
        {
            RegisterEmployees(employeeDataList);
            Debug.Log($"[EmployeeService] Runtime load completed: {employeeDataList.Length} employee types registered.");
        }
        else
        {
            Debug.LogWarning("[EmployeeService] No EmployeeData found in Resources/Datas/Employee. Make sure EmployeeData files are placed in the Resources folder.");
        }
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

    // ----------------- Salary Management -----------------

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
        
        // 이전 급여 레벨 저장
        int previousSalaryLevel = entry.employeeState.salaryLevel;
        
        // 급여 레벨 변경
        entry.employeeState.salaryLevel = salaryLevel;
        
        // 해당 직원의 급여 재계산
        UpdateSalary(entry);
        
        // 급여 레벨에 따른 만족도 이펙트 부여
        ApplySalaryLevelSatisfactionEffect(employeeId, salaryLevel, previousSalaryLevel);
        
        string levelName = _salarySettings.GetSalaryLevelName(salaryLevel);
        
        OnEmployeeChanged?.Invoke();
    }

    /// <summary>
    /// 급여 레벨에 따른 만족도 이펙트를 부여합니다.
    /// </summary>

    /// <param name="employeeId">직원 유형 ID</param>
    /// <param name="newSalaryLevel">새 급여 레벨</param>
    /// <param name="previousSalaryLevel">이전 급여 레벨</param>
    private void ApplySalaryLevelSatisfactionEffect(string employeeId, int newSalaryLevel, int previousSalaryLevel)
    {
        if (_gameDataManager?.Effect == null || _salarySettings == null)
        {
            return;
        }

        var entry = GetEmployeeEntry(employeeId);
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
            Debug.Log($"[EmployeeDataHandler] Applied local satisfaction effect for {employeeId}: {satisfactionChange:F1} per day");
        }
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
    /// 급여를 업데이트합니다.
    /// </summary>
    protected void UpdateSalary(EmployeeEntry entry)
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
    protected void RefreshAllSalaries()
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
