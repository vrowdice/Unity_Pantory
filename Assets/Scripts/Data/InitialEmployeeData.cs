using UnityEngine;

/// <summary>
/// 직원 급여 비율 초기화 데이터를 저장하는 ScriptableObject
/// Inspector를 통해 급여 비율을 조정할 수 있습니다.
/// </summary>
[CreateAssetMenu(fileName = "InitialEmployeeData", menuName = "Game Data/Initial Employee Data")]
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

    [Header("Satisfaction Penalty from Unassignment")]
    [Tooltip("직원 할당 해제 시 기본 만족도 패널티 (해제당)")]
    [Range(-50f, 0f)]
    public float baseUnassignmentSatisfactionPenalty = -5f;
    
    [Tooltip("고용된 직원 수에 따른 만족도 패널티 계수 (인원당 추가 패널티)")]
    [Range(0f, 5f)]
    public float employeeCountSatisfactionPenaltyRatio = 0.1f;
    
    [Tooltip("최대 만족도 패널티 (너무 큰 패널티 방지)")]
    [Range(-100f, 0f)]
    public float maxUnassignmentSatisfactionPenalty = -30f;

    [Header("Satisfaction Penalty from Firing")]
    [Tooltip("최대 공포치 (전원 해고 시 만족도 패널티, 비율 기반 계산의 최대값)")]
    [Range(0f, 200f)]
    public float maxFirePanic = 100f;
    
    [Tooltip("최소 패널티 (아무리 적은 비율의 해고라도 최소 이만큼은 깎임)")]
    [Range(0f, 20f)]
    public float minFireSatisfactionPenalty = 1f;
    
    [Tooltip("다른 직군으로의 만족도 전파 비율 (0.3 = 30% 영향)")]
    [Range(0f, 1f)]
    public float crossEmployeeTypeSatisfactionPenaltyRatio = 0.3f;
    
    [Header("Management System")]
    [Tooltip("매니저 1명이 커버할 수 있는 직원 수 (예: 20명)")]
    [Range(1, 100)]
    public int managerCoverage = 20;
    
    [Tooltip("관리 비율 1.0 미만일 때 효율 감소 최대치 (0.2 = 최대 20% 감소)")]
    [Range(0f, 1f)]
    public float maxEfficiencyPenalty = 0.2f;
    
    [Tooltip("관리 비율 1.0 미만일 때 만족도 하락 최대치")]
    [Range(0f, 50f)]
    public float maxSatisfactionPenalty = 5.0f;
    
    [Header("Manager Mitigation (Optional - for Firing Penalty)")]
    [Tooltip("관리자에 의한 해고 패널티 완화 활성화 여부")]
    public bool enableManagerMitigation = true;
    
    [Tooltip("충분한 관리자 커버 시 해고 패널티 감소율 (0.5 = 50% 감소)")]
    [Range(0f, 1f)]
    public float managerMitigationRatio = 0.5f;

    [Header("Research System")]
    [Tooltip("연구자명당 연구 포인트 증가 수치")]
    public int researchPointsPerResearcher = 5;

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
        
        baseUnassignmentSatisfactionPenalty = Mathf.Clamp(baseUnassignmentSatisfactionPenalty, -50f, 0f);
        employeeCountSatisfactionPenaltyRatio = Mathf.Clamp(employeeCountSatisfactionPenaltyRatio, 0f, 5f);
        maxUnassignmentSatisfactionPenalty = Mathf.Clamp(maxUnassignmentSatisfactionPenalty, -100f, 0f);
        
        maxFirePanic = Mathf.Clamp(maxFirePanic, 0f, 200f);
        minFireSatisfactionPenalty = Mathf.Clamp(minFireSatisfactionPenalty, 0f, 20f);
        crossEmployeeTypeSatisfactionPenaltyRatio = Mathf.Clamp(crossEmployeeTypeSatisfactionPenaltyRatio, 0f, 1f);
        
        managerCoverage = Mathf.Clamp(managerCoverage, 1, 100);
        maxEfficiencyPenalty = Mathf.Clamp(maxEfficiencyPenalty, 0f, 1f);
        maxSatisfactionPenalty = Mathf.Clamp(maxSatisfactionPenalty, 0f, 50f);
        managerMitigationRatio = Mathf.Clamp(managerMitigationRatio, 0f, 1f);
    }
}

