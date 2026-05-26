using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GoalPopup : PopupBase
{
    [SerializeField] private GameObject _activeGoalContent;
    [SerializeField] private TextMeshProUGUI _titleText;
    [SerializeField] private TextMeshProUGUI _descriptionText;
    [SerializeField] private TextMeshProUGUI _progressText;
    [SerializeField] private TextMeshProUGUI _rewardText;
    [SerializeField] private Slider _progressSlider;
    [SerializeField] private TextMeshProUGUI _completedText;

    private bool _isGoalEventSubscribed;

    public override void Init()
    {
        base.Init();
        SubscribeGoalEvents();
        Refresh();
        Show();
    }

    public override void Close()
    {
        UnsubscribeGoalEvents();
        base.Close();
    }

    protected override void HandleDayChanged()
    {
        if (gameObject.activeSelf)
        {
            Refresh();
        }
    }

    private void SubscribeGoalEvents()
    {
        if (_isGoalEventSubscribed || _dataManager?.Goal == null)
            return;

        _dataManager.Goal.OnActiveGoalChanged += HandleGoalDataChanged;
        _dataManager.Goal.OnGoalCompleted += HandleGoalDataChanged;
        _dataManager.Goal.OnAllGoalsCompleted += HandleAllGoalsCompleted;
        _isGoalEventSubscribed = true;
    }

    private void UnsubscribeGoalEvents()
    {
        if (!_isGoalEventSubscribed || _dataManager?.Goal == null)
            return;

        _dataManager.Goal.OnActiveGoalChanged -= HandleGoalDataChanged;
        _dataManager.Goal.OnGoalCompleted -= HandleGoalDataChanged;
        _dataManager.Goal.OnAllGoalsCompleted -= HandleAllGoalsCompleted;
        _isGoalEventSubscribed = false;
    }

    private void HandleGoalDataChanged(GoalState goalState)
    {
        Refresh();
    }

    private void HandleAllGoalsCompleted()
    {
        Refresh();
    }

    public void Refresh()
    {
        GoalDataHandler goalHandler = _dataManager?.Goal;
        if (goalHandler == null)
            return;

        if (goalHandler.AllGoalsCompleted)
        {
            ShowCompletedState();
            return;
        }

        if (goalHandler.ActiveGoal == null)
        {
            ShowEmptyState();
            return;
        }

        ShowActiveState(goalHandler);
    }

    private void ShowCompletedState()
    {
        SetActiveGoalContentVisible(false);

        if (_completedText != null)
        {
            _completedText.gameObject.SetActive(true);
            _completedText.text = "GoalChainComplete".Localize(LocalizationUtils.TABLE_COMMON);
        }
    }

    private void ShowEmptyState()
    {
        SetActiveGoalContentVisible(false);

        if (_completedText != null)
        {
            _completedText.gameObject.SetActive(true);
            _completedText.text = string.Empty;
        }
    }

    private void ShowActiveState(GoalDataHandler goalHandler)
    {
        SetActiveGoalContentVisible(true);

        if (_completedText != null)
            _completedText.gameObject.SetActive(false);

        GoalState activeGoal = goalHandler.ActiveGoal;

        if (_titleText != null)
            _titleText.text = "GoalPanelTitle".Localize(LocalizationUtils.TABLE_COMMON);

        if (_descriptionText != null)
            _descriptionText.text = GoalDisplayUtils.GetGoalTitle(goalHandler);

        if (_progressText != null)
            _progressText.text = GoalDisplayUtils.FormatProgress(activeGoal);

        if (_rewardText != null)
            _rewardText.text = GoalDisplayUtils.FormatReward(activeGoal.rewardCredit);

        if (_progressSlider != null)
        {
            _progressSlider.minValue = 0f;
            _progressSlider.maxValue = 1f;
            _progressSlider.value = activeGoal.ProgressRatio;
        }
    }

    private void SetActiveGoalContentVisible(bool visible)
    {
        if (_activeGoalContent != null)
        {
            _activeGoalContent.SetActive(visible);
            return;
        }

        if (_titleText != null)
            _titleText.gameObject.SetActive(visible);
        if (_descriptionText != null)
            _descriptionText.gameObject.SetActive(visible);
        if (_progressText != null)
            _progressText.gameObject.SetActive(visible);
        if (_rewardText != null)
            _rewardText.gameObject.SetActive(visible);
        if (_progressSlider != null)
            _progressSlider.gameObject.SetActive(visible);
    }
}
