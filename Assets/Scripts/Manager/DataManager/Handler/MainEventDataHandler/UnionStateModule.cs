using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 연합 메인 이벤트. <see cref="UnionCohesionProgress"/>는 종합 만족도에 따라 매일 변하며 0~100으로 유지됩니다.
/// </summary>
public class UnionStateModule : MainEventStateModuleBase
{
    private const float MinCohesionProgress = 0f;
    private const float MaxCohesionProgress = 100f;

    private readonly InitialUnionMainEventData _unionInit;
    private readonly DataManager _dataManager;

    private int _remainingDays = -1;
    private float _unionCohesionProgress;

    public int RemainingDays => _remainingDays;
    public float UnionCohesionProgress => _unionCohesionProgress;

    public List<UnionRequestState> GetActiveUnionRequests()
    {
        if (_dataManager?.UnionRequest == null)
        {
            return new List<UnionRequestState>();
        }

        return _dataManager.UnionRequest.GetActiveUnionRequestList();
    }

    public bool TryFulfillUnionRequest(UnionRequestState request)
    {
        return _dataManager?.UnionRequest != null && _dataManager.UnionRequest.TryFulfillUnionRequest(request);
    }

    public float GetWorkforceSatisfaction()
    {
        return _dataManager?.Employee?.GetWeightedAverageSatisfaction() ?? 0f;
    }

    /// <summary>
    /// 현재 종합 만족도 기준, 다음 일일 틱에서 적용될 결합도 증가량(%). 기준 이하이면 0.
    /// </summary>
    public float GetDailyCohesionGainFromWorkforceSatisfaction()
    {
        if (_unionInit == null || _unionCohesionProgress >= MaxCohesionProgress)
        {
            return 0f;
        }

        float deltaFromBaseline = GetWorkforceSatisfaction() - _unionInit.cohesionSatisfactionBaseline;
        if (deltaFromBaseline <= 0f)
        {
            return 0f;
        }

        float dailyGain = deltaFromBaseline * _unionInit.cohesionProgressPerSatisfactionPointPerDay;
        if (_unionInit.maxCohesionProgressGainPerDay > 0f)
        {
            dailyGain = Mathf.Min(dailyGain, _unionInit.maxCohesionProgressGainPerDay);
        }

        return Mathf.Max(0f, dailyGain);
    }

    /// <summary>
    /// 현재 종합 만족도 기준, 다음 일일 틱에서 적용될 결합도 감소량(%). 기준 이상이면 0.
    /// </summary>
    public float GetDailyCohesionLossFromWorkforceSatisfaction()
    {
        if (_unionInit == null || _unionCohesionProgress <= MinCohesionProgress)
        {
            return 0f;
        }

        float deltaFromBaseline = GetWorkforceSatisfaction() - _unionInit.cohesionSatisfactionBaseline;
        if (deltaFromBaseline >= 0f)
        {
            return 0f;
        }

        float dailyLoss = -deltaFromBaseline * _unionInit.cohesionLossPerSatisfactionPointBelowBaselinePerDay;
        if (_unionInit.maxCohesionProgressLossPerDay > 0f)
        {
            dailyLoss = Mathf.Min(dailyLoss, _unionInit.maxCohesionProgressLossPerDay);
        }

        return Mathf.Max(0f, dailyLoss);
    }

    public float GetMaxDailyCohesionGainFromWorkforceSatisfaction()
    {
        return _unionInit != null ? _unionInit.maxCohesionProgressGainPerDay : 0f;
    }

    public float GetMaxDailyCohesionLossFromWorkforceSatisfaction()
    {
        return _unionInit != null ? _unionInit.maxCohesionProgressLossPerDay : 0f;
    }

    public UnionStateModule(InitialUnionMainEventData init, MainEventDataHandler mainEventDataHandler) : base(init, mainEventDataHandler)
    {
        _unionInit = init;
        _dataManager = mainEventDataHandler.DataManager;
    }

    public void InitializeForNewChapter()
    {
        int total = GetDurationDays();
        _remainingDays = total > 0 ? total : -1;
        _unionCohesionProgress = MinCohesionProgress;
        SetComplete(false);
        _dataManager?.UnionRequest?.ResetForNewUnionChapter();
    }

    public void RestoreUnionState(int remainingDays, float unionCohesionProgress, bool isComplete)
    {
        _remainingDays = remainingDays;
        _unionCohesionProgress = ClampCohesionProgress(unionCohesionProgress);
        SetComplete(isComplete);
    }

    public void AddCohesionProgress(float amount)
    {
        if (amount <= 0f || IsComplete)
        {
            return;
        }

        _unionCohesionProgress = ClampCohesionProgress(_unionCohesionProgress + amount);
    }

    public override void OnDayChanged()
    {
        if (IsComplete)
        {
            return;
        }

        OnDailyTick();
        _dataManager?.UnionRequest?.HandleUnionDayChanged();
        ApplyDailyCohesionProgressFromWorkforceSatisfaction();

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

    private void ApplyDailyCohesionProgressFromWorkforceSatisfaction()
    {
        float dailyGain = GetDailyCohesionGainFromWorkforceSatisfaction();
        if (dailyGain > 0f)
        {
            _unionCohesionProgress = ClampCohesionProgress(_unionCohesionProgress + dailyGain);
            return;
        }

        float dailyLoss = GetDailyCohesionLossFromWorkforceSatisfaction();
        if (dailyLoss > 0f)
        {
            _unionCohesionProgress = ClampCohesionProgress(_unionCohesionProgress - dailyLoss);
        }
    }

    private static float ClampCohesionProgress(float progress)
    {
        return Mathf.Clamp(progress, MinCohesionProgress, MaxCohesionProgress);
    }
}
