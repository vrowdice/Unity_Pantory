using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GoalBtn : BtnBase
{
    [SerializeField] private GameObject _activeGoalContent;
    [SerializeField] private TextMeshProUGUI _descriptionText;
    [SerializeField] private TextMeshProUGUI _progressText;
    [SerializeField] private TextMeshProUGUI _rewardText;
    [SerializeField] private Slider _progressSlider;
    [SerializeField] private TextMeshProUGUI _completedText;

    public void Init(GoalState goalState, GoalData goalData)
    {
        Refresh(goalState, goalData);
    }

    public void Refresh(GoalState goalState, GoalData goalData)
    {
        if (goalState == null || goalData == null)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);

        if (_completedText != null)
            _completedText.gameObject.SetActive(false);

        if (_activeGoalContent != null)
            _activeGoalContent.SetActive(true);

        _descriptionText.text = GetTitle(goalData);
        _progressText.text = FormatProgress(goalState);
        _rewardText.text = FormatReward(goalState.rewardCredit);

        _progressSlider.minValue = 0f;
        _progressSlider.maxValue = 1f;
        _progressSlider.value = goalState.ProgressRatio;
    }

    public static string GetTitle(GoalData goalData)
    {
        if (goalData == null)
            return string.Empty;

        return goalData.id.Localize(LocalizationUtils.TABLE_GOAL);
    }

    public static string FormatProgress(GoalState state)
    {
        return $"{ReplaceUtils.FormatNumberWithCommas(state.currentProgress)} / {ReplaceUtils.FormatNumberWithCommas(state.targetValue)}";
    }

    public static string FormatReward(long rewardCredit)
    {
        if (rewardCredit <= 0)
            return string.Empty;

        return "GoalReward".LocalizeFormat(
            LocalizationUtils.TABLE_COMMON,
            ReplaceUtils.FormatNumberWithCommas(rewardCredit));
    }

    protected override void HandleClick()
    {
    }
}
