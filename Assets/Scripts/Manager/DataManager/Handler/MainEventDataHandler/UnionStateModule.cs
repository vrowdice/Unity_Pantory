/// <summary>
/// 연합 메인 이벤트. 직원 수·비용은 <see cref="InitialUnionMainEventData"/>에서 읽는다. <see cref="ActiveTime"/>은 기록용.
/// </summary>
public class UnionStateModule : MainEventStateModuleBase
{
    private readonly InitialUnionMainEventData _unionInit;
    private readonly DataManager _dataManager;

    public UnionStateModule(InitialUnionMainEventData init, MainEventDataHandler mainEventDataHandler) : base(init, mainEventDataHandler)
    {
        _unionInit = init;
        _dataManager = mainEventDataHandler.DataManager;
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
}
