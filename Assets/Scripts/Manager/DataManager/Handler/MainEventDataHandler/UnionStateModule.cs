/// <summary>
/// 연합 메인 이벤트. 직원 수·비용은 <see cref="InitialUnionMainEventData"/>에서 읽는다. <see cref="ActiveTime"/>은 기록용.
/// </summary>
public class UnionStateModule : IMainEventStateModule
{
    private readonly InitialUnionMainEventData _init;
    private readonly DataManager _dataManager;

    private int _activeTime;
    private bool _isComplete;

    public bool IsComplete => _isComplete;
    public int ActiveTime => _activeTime;

    public UnionStateModule(InitialUnionMainEventData init, DataManager dataManager)
    {
        _init = init;
        _dataManager = dataManager;
    }

    public void RestoreFromSave(int daysActive, bool isComplete)
    {
        _activeTime = daysActive < 0 ? 0 : daysActive;
        _isComplete = isComplete;
    }

    public void OnDayChanged()
    {
        if (_isComplete)
        {
            return;
        }

        ApplyDailyUnionCost();

        _activeTime++;
    }

    private void ApplyDailyUnionCost()
    {
        if (_init == null || _init.unionDailyCreditCost == 0)
        {
            return;
        }

        if (_dataManager?.Finances == null)
        {
            return;
        }

        _dataManager.Finances.ModifyCredit(-_init.unionDailyCreditCost);
    }
}
