using UnityEngine;

public static class GoalDisplayUtils
{
    public static string GetGoalTitle(GoalDataHandler goalHandler)
    {
        if (goalHandler == null)
            return string.Empty;

        GoalData goalData = goalHandler.ActiveGoalData;
        if (goalData == null)
            return string.Empty;

        if (!string.IsNullOrEmpty(goalData.displayName))
            return goalData.displayName;

        return string.IsNullOrEmpty(goalData.id) ? string.Empty : goalData.id;
    }

    public static string FormatProgress(GoalState state)
    {
        if (state == null)
            return string.Empty;

        return $"{ReplaceUtils.FormatNumberWithCommas(state.currentProgress)} / {ReplaceUtils.FormatNumberWithCommas(state.targetValue)}";
    }

    public static string FormatReward(long rewardCredit)
    {
        if (rewardCredit <= 0)
            return string.Empty;

        return "GoalReward".LocalizeFormat(LocalizationUtils.TABLE_COMMON, ReplaceUtils.FormatNumberWithCommas(rewardCredit));
    }
}
