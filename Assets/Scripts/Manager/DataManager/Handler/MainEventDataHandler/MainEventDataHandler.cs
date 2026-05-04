public class MainEventDataHandler : ITimeChangeHandler
{
    private readonly DataManager _dataManager;

    private MainEventType _currentEventType;
    private IMainEventStateModule _activeStateModule;

    public MainEventType CurrentEventType => _currentEventType;

    public MainEventDataHandler(DataManager dataManager)
    {
        _dataManager = dataManager;
        SetMainEventType(MainEventType.Union);
    }

    /// <summary>
    /// 메인 이벤트 타입을 바꾸고, 타입이 바뀐 경우에만 해당 스테이트 모듈을 새로 만든다.
    /// </summary>
    public void SetMainEventType(MainEventType mainEventType)
    {
        if (_currentEventType == mainEventType && _activeStateModule != null)
        {
            return;
        }

        _currentEventType = mainEventType;
        _activeStateModule = CreateStateModule(mainEventType);
    }

    public IMainEventStateModule GetNowModuleState()
    {
        return _activeStateModule;
    }

    public void HandleDayChanged()
    {
        _activeStateModule?.OnDayChanged();
    }

    public void CaptureTo(GameSaveData saveData)
    {
        if (saveData == null)
        {
            return;
        }

        saveData.mainEventType = _currentEventType;
    }

    public void ApplyFromSave(GameSaveData saveData)
    {
        if (saveData == null)
        {
            return;
        }

        SetMainEventType(saveData.mainEventType);
    }

    private static IMainEventStateModule CreateStateModule(MainEventType mainEventType)
    {
        switch (mainEventType)
        {
            case MainEventType.Union:
                return new UnionStateModule();
            case MainEventType.War:
                return new WarStateModule();
            case MainEventType.Automation:
                return new AutomationStateModule();
            default:
                throw new System.ArgumentOutOfRangeException(nameof(mainEventType), mainEventType, null);
        }
    }
}
