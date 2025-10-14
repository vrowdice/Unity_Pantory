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
    public int baseWorkValue;              // 기본 일의 진행도
    [Range(0f, 100f)]
    public float baseSatisfaction;       // 기본 만족도
    
    [Header("Salary")]
    public long baseSalary;              // 기본 급여
}
