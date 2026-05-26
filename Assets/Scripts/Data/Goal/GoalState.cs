using System;
using UnityEngine;

[Serializable]
public class GoalState
{
    public string goalId;
    public GoalConditionType conditionType;
    public string targetId;
    public long targetValue;
    public long rewardCredit;
    public long currentProgress;

    public GoalState() { }

    public GoalState(GoalData goalData, long currentProgress = 0)
    {
        goalId = goalData != null ? goalData.id : string.Empty;
        conditionType = goalData != null ? goalData.conditionType : GoalConditionType.ReachCredit;
        targetId = goalData != null ? goalData.targetId : string.Empty;
        targetValue = goalData != null ? goalData.targetValue : 0;
        rewardCredit = goalData != null ? goalData.rewardCredit : 0;
        this.currentProgress = currentProgress;
    }

    public float ProgressRatio =>
        targetValue <= 0 ? 1f : Mathf.Clamp01((float)currentProgress / targetValue);

    public bool IsComplete => currentProgress >= targetValue;
}
