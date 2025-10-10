using UnityEngine;

/// <summary>
/// 직원의 초기 데이터를 정의하는 ScriptableObject
/// </summary>
[CreateAssetMenu(fileName = "NewEmployeeData", menuName = "Game Data/Employee Data", order = 2)]
public class EmployeeData : ScriptableObject
{
    [Header("Basic Info")]
    public string id;                    // 직원 유형 ID (예: "worker")
    public string displayName;           // 표시 이름 (예: "Worker")
    public EmployeeType role;            // 직원 역할
    public Sprite icon;
    [TextArea(3, 10)]
    public string description;

    [Header("Initial Stats")]
    [Range(0f, 100f)]
    public float baseSkill;              // 기본 숙련도
    [Range(0f, 100f)]
    public float baseFatigue;            // 기본 피로도
    [Range(0f, 100f)]
    public float baseSatisfaction;       // 기본 만족도
    
    [Header("Salary")]
    public long baseSalary;              // 기본 급여
}
