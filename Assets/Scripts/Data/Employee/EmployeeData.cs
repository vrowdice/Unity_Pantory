using UnityEngine;

/// <summary>
/// 직원의 초기 데이터를 정의하는 ScriptableObject
/// </summary>
[CreateAssetMenu(fileName = "NewEmployeeData", menuName = "Game Data/Employee Data")]
public class EmployeeData : ScriptableObject
{
    [Header("Basic Info")]
    [Tooltip("직군 고유 ID")]
    public string id;
    [Tooltip("UI에 표시할 직군 이름")]
    public string displayName;
    [Tooltip("직군 분류(노동자·기술자·연구자·관리자)")]
    public EmployeeType type;
    [Tooltip("목록·팝업용 아이콘")]
    public Sprite icon;
    [Tooltip("상세 화면용 일러스트")]
    public Sprite image;
    [TextArea(3, 10)]
    [Tooltip("직군 설명")]
    public string description;

    [Header("Initial Stats")]
    [Range(0f, 1f)]
    [Tooltip("게임 시작·신규 고용 시 기본 효율(0~1)")]
    public float baseEfficiency = 0.5f;
    [Range(-100f, 100f)]
    [Tooltip("게임 시작·신규 고용 시 기본 만족도")]
    public float baseSatisfaction;
    
    [Header("Salary")]
    [Tooltip("급여 레벨 '보통' 기준 일급(크레딧). 다른 레벨은 InitialEmployeeData 배율 적용")]
    public long baseSalary;
    [Tooltip("1명 고용 시 일회성 비용")]
    public long hiringCost;
    [Tooltip("1명 해고 시 일회성 비용")]
    public long firingCost;
}
