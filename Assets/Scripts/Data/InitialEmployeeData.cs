using UnityEngine;

/// <summary>
/// 직원 급여 비율 초기화 데이터를 저장하는 ScriptableObject
/// Inspector를 통해 급여 비율을 조정할 수 있습니다.
/// </summary>
[CreateAssetMenu(fileName = "InitialEmployeeData", menuName = "Game Data/Initial Employee Data", order = 3)]
public class InitialEmployeeData : ScriptableObject
{
    [Header("Salary Level Multipliers")]
    [Tooltip("급여 비율: 매우 적음 (0.5 = 50% of base salary)")]
    [Range(0f, 2f)]
    public float veryLowSalaryMultiplier = 0.5f;
    
    [Tooltip("급여 비율: 적음 (0.75 = 75% of base salary)")]
    [Range(0f, 2f)]
    public float lowSalaryMultiplier = 0.75f;
    
    [Tooltip("급여 비율: 보통 (1.0 = 100% of base salary)")]
    [Range(0f, 2f)]
    public float normalSalaryMultiplier = 1.0f;
    
    [Tooltip("급여 비율: 많음 (1.25 = 125% of base salary)")]
    [Range(0f, 2f)]
    public float highSalaryMultiplier = 1.25f;
    
    [Tooltip("급여 비율: 매우 많음 (1.5 = 150% of base salary)")]
    [Range(0f, 2f)]
    public float veryHighSalaryMultiplier = 1.5f;

    [Header("Satisfaction Change Per Day")]
    [Tooltip("매우 적은 급여 시 일일 만족도 변화량 (음수 = 감소)")]
    [Range(-10f, 0f)]
    public float veryLowSalarySatisfactionChange = -2f;
    
    [Tooltip("적은 급여 시 일일 만족도 변화량 (음수 = 감소)")]
    [Range(-10f, 0f)]
    public float lowSalarySatisfactionChange = -1f;
    
    [Tooltip("보통 급여 시 일일 만족도 변화량 (0 = 변화 없음)")]
    [Range(-5f, 5f)]
    public float normalSalarySatisfactionChange = 0f;
    
    [Tooltip("많은 급여 시 일일 만족도 변화량 (양수 = 증가)")]
    [Range(0f, 10f)]
    public float highSalarySatisfactionChange = 1f;
    
    [Tooltip("매우 많은 급여 시 일일 만족도 변화량 (양수 = 증가)")]
    [Range(0f, 10f)]
    public float veryHighSalarySatisfactionChange = 2f;

    [Header("Efficiency from Satisfaction")]
    [Tooltip("만족도가 효율성에 미치는 영향 계수 (0.0 = 영향 없음, 0.01 = 만족도 1당 효율성 0.01 변화)")]
    [Range(0f, 0.1f)]
    public float satisfactionToEfficiencyRatio = 0.01f;
    
    [Tooltip("기본 효율성 (만족도가 0일 때의 효율성)")]
    [Range(0f, 2f)]
    public float baseEfficiencyFromSatisfaction = 1f;

    /// <summary>
    /// 급여 레벨에 따른 일일 만족도 변화량을 반환합니다.
    /// </summary>
    /// <param name="salaryLevel">급여 레벨 (0=매우 적음, 1=적음, 2=보통, 3=많음, 4=매우 많음)</param>
    /// <returns>일일 만족도 변화량</returns>
    public float GetSatisfactionChangePerDay(int salaryLevel)
    {
        switch (salaryLevel)
        {
            case 0: return veryLowSalarySatisfactionChange;      // 매우 적음
            case 1: return lowSalarySatisfactionChange;           // 적음
            case 2: return normalSalarySatisfactionChange;        // 보통
            case 3: return highSalarySatisfactionChange;          // 많음
            case 4: return veryHighSalarySatisfactionChange;     // 매우 많음
            default: return normalSalarySatisfactionChange;       // 기본값: 보통
        }
    }

    /// <summary>
    /// 급여 레벨에 따른 비율을 반환합니다.
    /// </summary>
    /// <param name="salaryLevel">급여 레벨 (0=매우 적음, 1=적음, 2=보통, 3=많음, 4=매우 많음)</param>
    /// <returns>급여 비율</returns>
    public float GetSalaryMultiplier(int salaryLevel)
    {
        switch (salaryLevel)
        {
            case 0: return veryLowSalaryMultiplier;      // 매우 적음
            case 1: return lowSalaryMultiplier;           // 적음
            case 2: return normalSalaryMultiplier;        // 보통
            case 3: return highSalaryMultiplier;          // 많음
            case 4: return veryHighSalaryMultiplier;      // 매우 많음
            default: return normalSalaryMultiplier;       // 기본값: 보통
        }
    }

    /// <summary>
    /// 급여 레벨 이름을 반환합니다.
    /// </summary>
    /// <param name="salaryLevel">급여 레벨</param>
    /// <returns>급여 레벨 이름</returns>
    public string GetSalaryLevelName(int salaryLevel)
    {
        switch (salaryLevel)
        {
            case 0: return "Very Low";      // 매우 적음
            case 1: return "Low";           // 적음
            case 2: return "Normal";         // 보통
            case 3: return "High";           // 많음
            case 4: return "Very High";      // 매우 많음
            default: return "Normal";
        }
    }

    /// <summary>
    /// EmployeeDataHandler에 초기 데이터를 적용합니다.
    /// </summary>
    /// <param name="employeeHandler">EmployeeDataHandler to apply to</param>
    public void ApplyToEmployeeHandler(EmployeeDataHandler employeeHandler)
    {
        if (employeeHandler == null)
        {
            Debug.LogError("[InitialEmployeeData] EmployeeDataHandler is null.");
            return;
        }

        employeeHandler.SetSalaryMultipliers(this);
    }

    /// <summary>
    /// Editor에서 값 검증 (유효하지 않은 값 방지)
    /// </summary>
    private void OnValidate()
    {
        veryLowSalaryMultiplier = Mathf.Clamp(veryLowSalaryMultiplier, 0f, 2f);
        lowSalaryMultiplier = Mathf.Clamp(lowSalaryMultiplier, 0f, 2f);
        normalSalaryMultiplier = Mathf.Clamp(normalSalaryMultiplier, 0f, 2f);
        highSalaryMultiplier = Mathf.Clamp(highSalaryMultiplier, 0f, 2f);
        veryHighSalaryMultiplier = Mathf.Clamp(veryHighSalaryMultiplier, 0f, 2f);
        
        veryLowSalarySatisfactionChange = Mathf.Clamp(veryLowSalarySatisfactionChange, -10f, 0f);
        lowSalarySatisfactionChange = Mathf.Clamp(lowSalarySatisfactionChange, -10f, 0f);
        normalSalarySatisfactionChange = Mathf.Clamp(normalSalarySatisfactionChange, -5f, 5f);
        highSalarySatisfactionChange = Mathf.Clamp(highSalarySatisfactionChange, 0f, 10f);
        veryHighSalarySatisfactionChange = Mathf.Clamp(veryHighSalarySatisfactionChange, 0f, 10f);
        
        satisfactionToEfficiencyRatio = Mathf.Clamp(satisfactionToEfficiencyRatio, 0f, 0.1f);
        baseEfficiencyFromSatisfaction = Mathf.Clamp(baseEfficiencyFromSatisfaction, 0f, 2f);
    }
}

