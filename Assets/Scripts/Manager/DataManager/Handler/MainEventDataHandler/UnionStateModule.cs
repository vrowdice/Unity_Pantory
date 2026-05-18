using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 연합 메인 이벤트. 기한은 <see cref="RemainingDays"/>만 사용하며, 직원 만족도 가중 평균이 <see cref="UnionMood"/>다.
/// </summary>
public class UnionStateModule : MainEventStateModuleBase
{
    private readonly InitialUnionMainEventData _unionInit;
    private readonly DataManager _dataManager;

    private int _remainingDays = -1;
    private float _unionMood;

    /// <summary>남은 일수. -1이면 기한 없음.</summary>
    public int RemainingDays => _remainingDays;

    public float UnionMood => _unionMood;

    public UnionStateModule(InitialUnionMainEventData init, MainEventDataHandler mainEventDataHandler) : base(init, mainEventDataHandler)
    {
        _unionInit = init;
        _dataManager = mainEventDataHandler.DataManager;
    }

    public int GetChapterDurationDays() => GetDurationDays();

    public void InitializeForNewChapter()
    {
        int total = GetDurationDays();
        _remainingDays = total > 0 ? total : -1;
        RecalculateUnionMood();
        SetComplete(false);
    }

    public void RestoreUnionState(int remainingDays, float unionMood, bool isComplete)
    {
        _remainingDays = remainingDays;
        _unionMood = unionMood;
        SetComplete(isComplete);
    }

    public override void OnDayChanged()
    {
        if (IsComplete)
        {
            return;
        }

        OnDailyTick();
        RecalculateUnionMood();

        if (_remainingDays < 0)
        {
            return;
        }

        _remainingDays--;
        if (_remainingDays <= 0)
        {
            MarkComplete();
        }
    }

    protected override void OnDailyTick()
    {
        if (_unionInit == null || _unionInit.unionDailyCreditCost == 0) return;
        if (_dataManager?.Finances == null) return;

        _dataManager.Finances.ModifyCredit(-_unionInit.unionDailyCreditCost);
    }

    protected override int GetDurationDays()
    {
        if (_unionInit == null) return 0;
        if (_unionInit.eventOverDate > 0) return _unionInit.eventOverDate;

        return _unionInit.unionDaysToComplete > 0 ? _unionInit.unionDaysToComplete : 0;
    }

    private void RecalculateUnionMood()
    {
        if (_dataManager?.Employee == null)
        {
            _unionMood = 0f;
            return;
        }

        Dictionary<EmployeeType, EmployeeEntry> employees = _dataManager.Employee.GetAllEmployees();
        int totalCount = 0;
        float weightedSum = 0f;

        foreach (EmployeeEntry entry in employees.Values)
        {
            if (entry?.state == null || entry.state.count <= 0)
            {
                continue;
            }

            int count = entry.state.count;
            totalCount += count;
            weightedSum += entry.state.currentSatisfaction * count;
        }

        _unionMood = totalCount > 0 ? weightedSum / totalCount : 0f;
    }
}
