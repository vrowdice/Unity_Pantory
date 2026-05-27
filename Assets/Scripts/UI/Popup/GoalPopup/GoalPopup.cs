using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GoalPopup : PopupBase
{
    [SerializeField] private GameObject _goalBtnPrefab;
    [SerializeField] private Transform _goalBtnScrollViewContentTransform;
    [SerializeField] private TextMeshProUGUI _completedText;

    private readonly List<GoalBtn> _goalBtnList = new List<GoalBtn>();
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
            Refresh();
    }

    private void SubscribeGoalEvents()
    {
        if (_isGoalEventSubscribed || _dataManager?.Goal == null)
            return;

        _dataManager.Goal.OnActiveGoalsChanged += HandleGoalsChanged;
        _dataManager.Goal.OnGoalCompleted += HandleGoalCompleted;
        _dataManager.Goal.OnAllGoalsCompleted += HandleAllGoalsCompleted;
        _isGoalEventSubscribed = true;
    }

    private void UnsubscribeGoalEvents()
    {
        if (!_isGoalEventSubscribed || _dataManager?.Goal == null)
            return;

        _dataManager.Goal.OnActiveGoalsChanged -= HandleGoalsChanged;
        _dataManager.Goal.OnGoalCompleted -= HandleGoalCompleted;
        _dataManager.Goal.OnAllGoalsCompleted -= HandleAllGoalsCompleted;
        _isGoalEventSubscribed = false;
    }

    private void HandleGoalsChanged()
    {
        Refresh();
    }

    private void HandleGoalCompleted(GoalState goalState)
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
            ClearGoalButtons();
            ShowCompletedState();
            return;
        }

        IReadOnlyList<GoalState> activeGoals = goalHandler.ActiveGoals;
        if (activeGoals == null || activeGoals.Count == 0)
        {
            ClearGoalButtons();
            ShowEmptyState();
            return;
        }

        HideStatusText();
        RepopulateGoalButtons(activeGoals, goalHandler);
    }

    private void RepopulateGoalButtons(IReadOnlyList<GoalState> activeGoals, GoalDataHandler goalHandler)
    {
        while (_goalBtnList.Count > activeGoals.Count)
        {
            int lastIndex = _goalBtnList.Count - 1;
            GoalBtn removedBtn = _goalBtnList[lastIndex];
            _goalBtnList.RemoveAt(lastIndex);
            if (removedBtn != null)
                Destroy(removedBtn.gameObject);
        }

        for (int i = 0; i < activeGoals.Count; i++)
        {
            GoalState goalState = activeGoals[i];
            GoalData goalData = goalHandler.GetGoalData(goalState.goalId);

            if (i >= _goalBtnList.Count)
            {
                GameObject btnObj = Instantiate(_goalBtnPrefab, _goalBtnScrollViewContentTransform);
                GoalBtn goalBtn = btnObj.GetComponent<GoalBtn>();
                if (goalBtn == null)
                    continue;

                _goalBtnList.Add(goalBtn);
                goalBtn.Init(goalState, goalData);
                continue;
            }

            _goalBtnList[i].Refresh(goalState, goalData);
        }
    }

    private void ClearGoalButtons()
    {
        for (int i = _goalBtnList.Count - 1; i >= 0; i--)
        {
            if (_goalBtnList[i] != null)
                Destroy(_goalBtnList[i].gameObject);
        }

        _goalBtnList.Clear();

        if (_goalBtnScrollViewContentTransform != null)
            GameObjectUtils.ClearChildren(_goalBtnScrollViewContentTransform);
    }

    private void ShowCompletedState()
    {
        if (_completedText == null)
            return;

        _completedText.gameObject.SetActive(true);
        _completedText.text = "GoalChainComplete".Localize(LocalizationUtils.TABLE_COMMON);
    }

    private void ShowEmptyState()
    {
        if (_completedText == null)
            return;

        _completedText.gameObject.SetActive(true);
        _completedText.text = string.Empty;
    }

    private void HideStatusText()
    {
        if (_completedText != null)
            _completedText.gameObject.SetActive(false);
    }
}
