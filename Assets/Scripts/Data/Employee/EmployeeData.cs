using UnityEngine;

/// <summary>
/// 직원의 초기 데이터를 정의하는 ScriptableObject
/// </summary>
[CreateAssetMenu(fileName = "NewEmployeeData", menuName = "Game Data/Employee Data")]
public class EmployeeData : ScriptableObject
{
    [Header("Basic Info")]
    public string id;
    public string displayName;
    public EmployeeType type;
    public Sprite icon;
    public Sprite Image;
    [TextArea(3, 10)]
    public string description;

    [Header("Initial Stats")]
    [Range(0f, 2f)]
    public float baseEfficiency;
    [Range(-100f, 100f)]
    public float baseSatisfaction;
    
    [Header("Salary")]
    public long baseSalary;
    public long hiringCost;
    public long firingCost;
}
