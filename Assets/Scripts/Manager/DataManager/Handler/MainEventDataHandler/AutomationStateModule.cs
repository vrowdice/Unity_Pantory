public class AutomationStateModule : IMainEventStateModule
{
    private int _activeTime;
    private bool _isComplete;

    public bool IsComplete => _isComplete;

    public int ActiveTime => _activeTime;

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
