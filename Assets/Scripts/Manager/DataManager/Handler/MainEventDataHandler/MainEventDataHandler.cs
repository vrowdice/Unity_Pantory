using UnityEngine;

public class MainEventDataHandler : ITimeChangeHandler, IDataHandlerEvents
{
    private readonly DataManager _dataManager;
    private readonly InitialUnionMainEventData _initialUnionMainEventData;
    private readonly InitialWarMainEventData _initialWarMainEventData;
    private readonly InitialAutomationMainEventData _initialAutomationMainEventData;

    private MainEventType _currentEventType;
    private IMainEventStateModule _activeStateModule;

    private UnionStateModule _unionStateModule;
    private WarStateModule _warStateModule;
    private AutomationStateModule _automationStateModule;

    public MainEventType CurrentEventType => _currentEventType;

    /// <summary>
    /// 활성 메인 이벤트 모듈의 종료 여부. 모듈이 없으면 false.
    /// </summary>
    public bool IsCurrentMainEventComplete =>
        _activeStateModule != null && _activeStateModule.IsComplete;

    public MainEventDataHandler(
        DataManager dataManager,
        InitialUnionMainEventData initialUnionMainEventData,
        InitialWarMainEventData initialWarMainEventData,
        InitialAutomationMainEventData initialAutomationMainEventData)
    {
        _dataManager = dataManager;
        _initialUnionMainEventData = initialUnionMainEventData;
        _initialWarMainEventData = initialWarMainEventData;
        _initialAutomationMainEventData = initialAutomationMainEventData;
        SubscribeCrossHandlerEvents();
        SetMainEventType(MainEventType.None);
    }

    public void ClearAllSubscriptions()
    {
        if (_dataManager?.Employee != null)
        {
            _dataManager.Employee.OnEmployeeChanged -= HandleEmployeeRosterChanged;
        }
    }

    /// <summary>
    /// 다른 핸들러의 Clear 이후에 다시 직원 이벤트를 구독합니다. DataManager.ClearAllEventSubscriptions 끝에서 호출됩니다.
    /// </summary>
    public void SubscribeCrossHandlerEvents()
    {
        if (_dataManager?.Employee == null)
        {
            return;
        }

        _dataManager.Employee.OnEmployeeChanged -= HandleEmployeeRosterChanged;
        _dataManager.Employee.OnEmployeeChanged += HandleEmployeeRosterChanged;
    }

    private void HandleEmployeeRosterChanged()
    {
        TryActivateUnionFromEmployeeCount();
    }

    /// <summary>
    /// 메인 이벤트 타입을 바꾸고, 타입이 바뀐 경우에만 해당 스테이트 모듈을 새로 만든다.
    /// 같은 타입이면 기존 모듈을 유지한다. 세이브 복원은 <see cref="ApplyFromSave"/>에서 모듈에 직접 반영한다.
    /// </summary>
    public void SetMainEventType(MainEventType mainEventType, bool showStartAnnouncement = true)
    {
        if (_currentEventType == mainEventType)
        {
            if (mainEventType == MainEventType.None || _activeStateModule != null)
            {
                return;
            }
        }

        MainEventType previousType = _currentEventType;
        _currentEventType = mainEventType;

        switch (mainEventType)
        {
            case MainEventType.Union:
                _unionStateModule = new UnionStateModule(_initialUnionMainEventData, _dataManager);
                _activeStateModule = _unionStateModule;
                break;
            case MainEventType.War:
                _warStateModule = new WarStateModule(_initialWarMainEventData);
                _activeStateModule = _warStateModule;
                break;
            case MainEventType.Automation:
                _automationStateModule = new AutomationStateModule(_initialAutomationMainEventData);
                _activeStateModule = _automationStateModule;
                break;
            case MainEventType.None:
            default:
                _activeStateModule = null;
                break;
        }

        if (showStartAnnouncement
            && mainEventType != MainEventType.None
            && previousType != mainEventType)
        {
            TryShowMainEventStartAnnouncement(mainEventType);
        }
    }

    private void TryShowMainEventStartAnnouncement(MainEventType mainEventType)
    {
        InitialMainEventModuleData moduleData = null;
        switch (mainEventType)
        {
            case MainEventType.Union:
                moduleData = _initialUnionMainEventData;
                break;
            case MainEventType.War:
                moduleData = _initialWarMainEventData;
                break;
            case MainEventType.Automation:
                moduleData = _initialAutomationMainEventData;
                break;
            default:
                return;
        }

        if (moduleData == null || !moduleData.HasAnnouncementConfigured())
        {
            return;
        }

        UIManager uiManager = UIManager.Instance;
        if (uiManager == null)
        {
            return;
        }

        MainCanvas mainCanvas = Object.FindAnyObjectByType<MainCanvas>();
        if (mainCanvas == null)
        {
            return;
        }

        uiManager.ShowMainEventAnnouncementPopup(moduleData, mainCanvas);
    }

    public void HandleDayChanged()
    {
        TryActivateUnionFromEmployeeCount();
        _activeStateModule?.OnDayChanged();
    }

    /// <summary>
    /// 연합 이벤트가 비활성(None)이고 총 직원 수가 기준 이상이면 연합 이벤트를 켠다.
    /// </summary>
    public void TryActivateUnionFromEmployeeCount()
    {
        if (_currentEventType != MainEventType.None)
        {
            return;
        }

        if (_dataManager?.Employee == null)
        {
            return;
        }

        int threshold = GetUnionEmployeeCountToStart();
        int total = _dataManager.Employee.GetTotalEmployeeCount();
        if (total >= threshold)
        {
            SetMainEventType(MainEventType.Union);
        }
    }

    private int GetUnionEmployeeCountToStart()
    {
        if (_initialUnionMainEventData != null)
        {
            return _initialUnionMainEventData.unionEmployeeCountToStart;
        }

        return 500;
    }

    public void CaptureTo(GameSaveData saveData)
    {
        if (saveData == null)
        {
            return;
        }

        saveData.mainEventType = _currentEventType;
        saveData.mainEventIsComplete = IsCurrentMainEventComplete;
        saveData.mainEventUnionDaysActive = 0;
        if (_activeStateModule != null && _currentEventType != MainEventType.None)
        {
            saveData.mainEventUnionDaysActive = _activeStateModule.ActiveTime;
        }
    }

    public void ApplyFromSave(GameSaveData saveData)
    {
        if (saveData == null)
        {
            return;
        }

        SetMainEventType(saveData.mainEventType, showStartAnnouncement: false);
        switch (saveData.mainEventType)
        {
            case MainEventType.Union:
                if (_unionStateModule != null)
                {
                    _unionStateModule.RestoreFromSave(saveData.mainEventUnionDaysActive, saveData.mainEventIsComplete);
                }

                break;
            case MainEventType.War:
                if (_warStateModule != null)
                {
                    _warStateModule.RestoreFromSave(saveData.mainEventUnionDaysActive, saveData.mainEventIsComplete);
                }

                break;
            case MainEventType.Automation:
                if (_automationStateModule != null)
                {
                    _automationStateModule.RestoreFromSave(saveData.mainEventUnionDaysActive, saveData.mainEventIsComplete);
                }

                break;
        }
    }
}
