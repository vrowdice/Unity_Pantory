using System;
using UnityEngine;

public class MainEventDataHandler : ITimeChangeHandler, IDataHandlerEvents
{   
    private readonly DataManager _dataManager;
    private readonly InitialUnionMainEventData _initialUnionMainEventData;
    private readonly InitialWarMainEventData _initialWarMainEventData;
    private readonly InitialAutomationMainEventData _initialAutomationMainEventData;

    private MainEventType _currentEventType;
    private MainEventStateModuleBase _activeStateModule;
    private bool _unionChapterEnded;

    private UnionStateModule _unionStateModule;
    private WarStateModule _warStateModule;
    private AutomationStateModule _automationStateModule;

    public DataManager DataManager => _dataManager;
    public MainEventType CurrentEventType => _currentEventType;
    public bool IsCurrentMainEventComplete =>
        _activeStateModule != null && _activeStateModule.IsComplete;
    public event Action<MainEventType> OnMainEventTypeChanged;

    public UnionStateModule UnionModule => _unionStateModule;
    public WarStateModule WarModule => _warStateModule;
    public AutomationStateModule AutomationModule => _automationStateModule;

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

        OnMainEventTypeChanged = null;
    }

    public void SubscribeCrossHandlerEvents()
    {
        if (_dataManager?.Employee == null)
        {
            return;
        }

        _dataManager.Employee.OnEmployeeChanged -= HandleEmployeeRosterChanged;
        _dataManager.Employee.OnEmployeeChanged += HandleEmployeeRosterChanged;
    }

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
        _activeStateModule = CreateStateModule(mainEventType);

        if (previousType != mainEventType)
        {
            OnMainEventTypeChanged?.Invoke(_currentEventType);
        }

        if (showStartAnnouncement
            && mainEventType != MainEventType.None
            && previousType != mainEventType)
        {
            TryShowMainEventStartAnnouncement(mainEventType);
        }
    }

    public void HandleDayChanged()
    {
        TryActivateUnionFromEmployeeCount();

        if (_activeStateModule == null)
        {
            return;
        }

        _activeStateModule.OnDayChanged();

        if (!_activeStateModule.IsComplete || _currentEventType == MainEventType.None)
        {
            return;
        }

        if (_currentEventType == MainEventType.Union) _unionChapterEnded = true;
        SetMainEventType(MainEventType.None, showStartAnnouncement: false);
    }

    public void TryActivateUnionFromEmployeeCount()
    {
        if (_currentEventType != MainEventType.None || _unionChapterEnded)
        {
            return;
        }

        if (_dataManager?.Employee == null || _initialUnionMainEventData == null)
        {
            return;
        }

        int threshold = _initialUnionMainEventData.unionEmployeeCountToStart;
        int total = _dataManager.Employee.GetTotalEmployeeCount();
        if (total >= threshold)
        {
            SetMainEventType(MainEventType.Union);
        }
    }

    public void CaptureTo(GameSaveData saveData)
    {
        if (saveData == null)
        {
            return;
        }

        saveData.mainEventType = _currentEventType;
        saveData.mainEventIsComplete = IsCurrentMainEventComplete;
        saveData.mainEventUnionChapterEnded = _unionChapterEnded;
        saveData.mainEventUnionDaysActive = 0;
        saveData.mainEventUnionRemainingDays = -1;
        saveData.mainEventUnionMood = 0f;

        if (_currentEventType == MainEventType.Union && _unionStateModule != null)
        {
            saveData.mainEventUnionRemainingDays = _unionStateModule.RemainingDays;
            saveData.mainEventUnionMood = _unionStateModule.UnionMood;
            saveData.mainEventIsComplete = _unionStateModule.IsComplete;
        }
        else if (_activeStateModule != null && _currentEventType != MainEventType.None)
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

        _unionChapterEnded = saveData.mainEventUnionChapterEnded;
        SetMainEventType(saveData.mainEventType, showStartAnnouncement: false);

        if (_currentEventType == MainEventType.Union)
        {
            RestoreUnionFromSave(saveData);
        }
        else
        {
            RestoreActiveModuleFromSave(saveData.mainEventUnionDaysActive, saveData.mainEventIsComplete);
        }

        if (_activeStateModule != null && _activeStateModule.IsComplete)
        {
            if (_currentEventType == MainEventType.Union) _unionChapterEnded = true;
            SetMainEventType(MainEventType.None, showStartAnnouncement: false);
        }
    }

    private MainEventStateModuleBase CreateStateModule(MainEventType mainEventType)
    {
        switch (mainEventType)
        {
            case MainEventType.Union:
                _unionStateModule = new UnionStateModule(_initialUnionMainEventData, this);
                _unionStateModule.InitializeForNewChapter();
                return _unionStateModule;
            case MainEventType.War:
                return _warStateModule = new WarStateModule(_initialWarMainEventData, this);
            case MainEventType.Automation:
                return _automationStateModule = new AutomationStateModule(_initialAutomationMainEventData, this);
            default:
                return null;
        }
    }

    private MainEventStateModuleBase GetStoredModule(MainEventType mainEventType)
    {
        switch (mainEventType)
        {
            case MainEventType.Union: return _unionStateModule;
            case MainEventType.War: return _warStateModule;
            case MainEventType.Automation: return _automationStateModule;
            default: return null;
        }
    }

    private void RestoreActiveModuleFromSave(int daysActive, bool isComplete)
    {
        MainEventStateModuleBase module = GetStoredModule(_currentEventType);
        if (module == null) return;
        module.RestoreFromSave(daysActive, isComplete);
    }

    private void RestoreUnionFromSave(GameSaveData saveData)
    {
        if (_unionStateModule == null || saveData == null)
        {
            return;
        }

        int remaining = saveData.mainEventUnionRemainingDays;
        bool isComplete = saveData.mainEventIsComplete;
        float mood = saveData.mainEventUnionMood;

        if (!isComplete && saveData.mainEventUnionDaysActive > 0 && remaining <= 0)
        {
            int total = _unionStateModule.GetChapterDurationDays();
            remaining = total > 0 ? Mathf.Max(0, total - saveData.mainEventUnionDaysActive) : -1;
        }
        else if (!isComplete && remaining == 0 && saveData.mainEventUnionDaysActive == 0 && mood == 0f)
        {
            remaining = _unionStateModule.RemainingDays;
        }

        _unionStateModule.RestoreUnionState(remaining, mood, isComplete);
    }

    private InitialMainEventModuleData GetInitialData(MainEventType mainEventType)
    {
        switch (mainEventType)
        {
            case MainEventType.Union: return _initialUnionMainEventData;
            case MainEventType.War: return _initialWarMainEventData;
            case MainEventType.Automation: return _initialAutomationMainEventData;
            default: return null;
        }
    }

    private void HandleEmployeeRosterChanged() => TryActivateUnionFromEmployeeCount();

    private void TryShowMainEventStartAnnouncement(MainEventType mainEventType)
    {
        InitialMainEventModuleData moduleData = GetInitialData(mainEventType);
        if (moduleData == null) return;

        UIManager.Instance.ShowMainEventAnnouncementPopup(moduleData);
    }
}
