using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Evo.UI;

public class GoalPopup : PopupBase
{
    [SerializeField] private GameObject _goalBtnPrefab;
    [SerializeField] private Transform _goalBtnScrollViewContentTransform;
    [SerializeField] private Switch _completedGoalSwitch;

    private readonly List<GoalBtn> _goalBtnList = new List<GoalBtn>();
    private readonly List<string> _completedGoalIds = new List<string>();
    private bool _isGoalEventSubscribed;
    private Coroutine _refreshCoroutine;

    public override void Init()
    {
        base.Init();
        SubscribeGoalEvents();
        BindCompletedGoalSwitch();

        Refresh();
        Show();
    }

    public override void Close()
    {
        StaggeredSpawnUtils.Stop(this, ref _refreshCoroutine);
        UnbindCompletedGoalSwitch();
        UnsubscribeGoalEvents();
        base.Close();
    }

    protected override void HandleDayChanged()
    {
        if (gameObject.activeSelf)
            Refresh();
    }

    private void BindCompletedGoalSwitch()
    {
        if (_completedGoalSwitch == null)
            return;

        _completedGoalSwitch.onValueChanged.RemoveListener(OnCompletedGoalSwitchChanged);
        _completedGoalSwitch.onValueChanged.AddListener(OnCompletedGoalSwitchChanged);
    }

    private void UnbindCompletedGoalSwitch()
    {
        if (_completedGoalSwitch == null)
            return;

        _completedGoalSwitch.onValueChanged.RemoveListener(OnCompletedGoalSwitchChanged);
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

    private void OnCompletedGoalSwitchChanged(bool isOn)
    {
        Refresh();
    }

    public void Refresh()
    {
        StaggeredSpawnUtils.Restart(this, ref _refreshCoroutine, RefreshRoutine());
    }

    private bool IsShowingCompletedGoals()
    {
        return _completedGoalSwitch != null && _completedGoalSwitch.IsOn;
    }

    private IEnumerator RefreshRoutine()
    {
        GoalDataHandler goalHandler = _dataManager?.Goal;
        if (goalHandler == null)
            yield break;

        if (IsShowingCompletedGoals())
        {
            goalHandler.FillCompletedGoalIds(_completedGoalIds);
            TrimGoalButtons(_completedGoalIds.Count);

            yield return StaggeredSpawnUtils.ForEachFrame(_completedGoalIds.Count, i =>
            {
                GoalData goalData = goalHandler.GetGoalData(_completedGoalIds[i]);
                GoalBtn goalBtn = GetOrCreateGoalBtn(i);
                if (goalBtn != null)
                    goalBtn.RefreshCompleted(goalData);
            });
            yield break;
        }

        if (goalHandler.ActiveGoals.Count == 0)
        {
            ClearGoalButtons();
            yield break;
        }

        IReadOnlyList<GoalState> activeGoals = goalHandler.ActiveGoals;
        TrimGoalButtons(activeGoals.Count);

        yield return StaggeredSpawnUtils.ForEachFrame(activeGoals.Count, i =>
        {
            GoalState goalState = activeGoals[i];
            GoalData goalData = goalHandler.GetGoalData(goalState.goalId);
            GoalBtn goalBtn = GetOrCreateGoalBtn(i);
            if (goalBtn != null)
                goalBtn.Refresh(goalState, goalData);
        });
    }

    private GoalBtn GetOrCreateGoalBtn(int index)
    {
        if (index < _goalBtnList.Count)
            return _goalBtnList[index];

        GameObject btnObj = Instantiate(_goalBtnPrefab, _goalBtnScrollViewContentTransform);
        GoalBtn goalBtn = btnObj.GetComponent<GoalBtn>();
        if (goalBtn == null)
            return null;

        _goalBtnList.Add(goalBtn);
        return goalBtn;
    }

    private void TrimGoalButtons(int targetCount)
    {
        while (_goalBtnList.Count > targetCount)
        {
            int lastIndex = _goalBtnList.Count - 1;
            GoalBtn removedBtn = _goalBtnList[lastIndex];
            _goalBtnList.RemoveAt(lastIndex);
            if (removedBtn != null)
                Destroy(removedBtn.gameObject);
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
}
