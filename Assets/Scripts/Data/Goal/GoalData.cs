using UnityEngine;

[CreateAssetMenu(fileName = "NewGoalData", menuName = "Game Data/Goal Data")]
public class GoalData : ScriptableObject
{
    [Tooltip("목표 고유 ID")]
    public string id;

    [Tooltip("UI 표시용 이름(로컬라이제이션 키 또는 직접 텍스트)")]
    public string displayName;

    public GoalConditionType conditionType;

    [Tooltip("resourceId, buildingId, researchId 등")]
    public string targetId;

    [Tooltip("달성에 필요한 값")]
    public long targetValue = 1;

    [Tooltip("완료 시 지급 크레딧")]
    public long rewardCredit;

    public bool isDefaultUnlocked;

    [Tooltip("완료 후 활성화할 다음 목표. 비우면 목표 라인 종료")]
    public GoalData nextGoal;
}
