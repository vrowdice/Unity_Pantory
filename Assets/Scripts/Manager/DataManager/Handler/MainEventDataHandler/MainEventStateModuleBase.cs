/// <summary>
/// 메인 이벤트(연합/전쟁/자동화) 챕터 공통 일 단위 스테이트.
/// </summary>
public abstract class MainEventStateModuleBase
{
    protected readonly InitialMainEventModuleData Init;
    protected readonly MainEventDataHandler MainEventDataHandler;

    private int _activeTime;
    private bool _isComplete;

    public bool IsComplete => _isComplete;

    public int ActiveTime => _activeTime;

    protected MainEventStateModuleBase(InitialMainEventModuleData init, MainEventDataHandler mainEventDataHandler)
    {
        Init = init;
        MainEventDataHandler = mainEventDataHandler;
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

        OnDailyTick();

        _activeTime++;

        int durationDays = GetDurationDays();
        if (durationDays <= 0 || _activeTime < durationDays)
        {
            return;
        }

        MarkComplete();
    }

    /// <summary>일일 진행 직전·당일 비용 등 챕터별 처리.</summary>
    protected virtual void OnDailyTick() { }

    /// <summary>챕터 종료까지 필요한 일 수. 0 이하면 기한 종료 없음.</summary>
    protected virtual int GetDurationDays()
    {
        if (Init == null || Init.eventOverDate <= 0) return 0;
        return Init.eventOverDate;
    }

    protected void MarkComplete()
    {
        if (_isComplete) return;
        _isComplete = true;
    }
}
