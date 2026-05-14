public class WarStateModule : IMainEventStateModule
{
    private readonly InitialWarMainEventData _init;

    private int _activeTime;
    private bool _isComplete;

    public bool IsComplete => _isComplete;

    public int ActiveTime => _activeTime;

    public WarStateModule(InitialWarMainEventData init)
    {
        _init = init;
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

        _activeTime++;
    }
}
