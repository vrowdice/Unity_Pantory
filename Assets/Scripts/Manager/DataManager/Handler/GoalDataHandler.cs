using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어 목표(퀘스트) 진행을 관리합니다. 한 번에 활성 목표 1개, nextGoal로 순서 연결.
/// </summary>
public class GoalDataHandler : IDataHandlerEvents, ICrossHandlerEvents, IGameSaveHandler
{
    private readonly DataManager _dataManager;
    private readonly InitialGoalData _initialGoalData;
    private readonly Dictionary<string, GoalData> _goalDict = new Dictionary<string, GoalData>();

    private GoalData _activeGoalData;
    private GoalState _activeGoal;
    private bool _allGoalsCompleted;

    private long _produceProgress;
    private int _produceResourceSnapshot;

    private MainBuildingGridHandler _boundGridHandler;

    public GoalState ActiveGoal => _activeGoal;
    public GoalData ActiveGoalData => _activeGoalData;
    public bool AllGoalsCompleted => _allGoalsCompleted;

    public event Action<GoalState> OnActiveGoalChanged;
    public event Action<GoalState> OnGoalCompleted;
    public event Action OnAllGoalsCompleted;

    public GoalDataHandler(DataManager dataManager, List<GoalData> goalDataList, InitialGoalData initialGoalData)
    {
        _dataManager = dataManager;
        _initialGoalData = initialGoalData;

        if (goalDataList != null)
        {
            foreach (GoalData goalData in goalDataList)
            {
                if (goalData == null || string.IsNullOrEmpty(goalData.id))
                    continue;

                if (_goalDict.ContainsKey(goalData.id))
                {
                    Debug.LogWarning($"[GoalDataHandler] Duplicate goal ID: {goalData.id}");
                    continue;
                }

                _goalDict[goalData.id] = goalData;
            }
        }

        SubscribeCrossHandlerEvents();
    }

    public void TryStartInitialGoalForNewGame()
    {
        if (_allGoalsCompleted || _activeGoal != null || _activeGoalData != null)
            return;

        if (_initialGoalData == null
            || !_initialGoalData.autoStartOnNewGame
            || _initialGoalData.startingGoal == null)
        {
            return;
        }

        StartGoal(_initialGoalData.startingGoal.id);
    }

    public GoalData GetGoalData(string goalId)
    {
        if (string.IsNullOrEmpty(goalId))
            return null;

        return _goalDict.TryGetValue(goalId, out GoalData goalData) ? goalData : null;
    }

    public bool StartGoal(string goalId)
    {
        GoalData goalData = GetGoalData(goalId);
        if (goalData == null)
            return false;

        _activeGoalData = goalData;
        _allGoalsCompleted = false;
        ActivateGoal(resetProduceProgress: true);
        return true;
    }

    public void BindSceneGrid(MainBuildingGridHandler gridHandler)
    {
        if (gridHandler == null)
            return;

        UnbindSceneGrid();
        _boundGridHandler = gridHandler;
        _boundGridHandler.OnBuildingInstanceLayoutChanged -= EvaluateActiveGoal;
        _boundGridHandler.OnBuildingInstanceLayoutChanged += EvaluateActiveGoal;
        EvaluateActiveGoal();
    }

    public void UnbindSceneGrid()
    {
        if (_boundGridHandler == null)
            return;

        _boundGridHandler.OnBuildingInstanceLayoutChanged -= EvaluateActiveGoal;
        _boundGridHandler = null;
    }

    public void SubscribeCrossHandlerEvents()
    {
        if (_dataManager?.Finances != null)
        {
            _dataManager.Finances.OnCreditChanged -= EvaluateActiveGoal;
            _dataManager.Finances.OnCreditChanged += EvaluateActiveGoal;
        }

        if (_dataManager?.Resource != null)
        {
            _dataManager.Resource.OnResourceChanged -= HandleResourceChanged;
            _dataManager.Resource.OnResourceChanged += HandleResourceChanged;
        }

        if (_dataManager?.Research != null)
        {
            _dataManager.Research.OnResearchCompleted -= HandleResearchCompleted;
            _dataManager.Research.OnResearchCompleted += HandleResearchCompleted;
        }
    }

    public void ClearAllSubscriptions()
    {
        UnbindSceneGrid();

        if (_dataManager?.Finances != null)
            _dataManager.Finances.OnCreditChanged -= EvaluateActiveGoal;

        if (_dataManager?.Resource != null)
            _dataManager.Resource.OnResourceChanged -= HandleResourceChanged;

        if (_dataManager?.Research != null)
            _dataManager.Research.OnResearchCompleted -= HandleResearchCompleted;

        OnActiveGoalChanged = null;
        OnGoalCompleted = null;
        OnAllGoalsCompleted = null;
    }

    public void CaptureTo(GameSaveData saveData)
    {
        if (saveData == null)
            return;

        saveData.activeGoalId = _activeGoalData != null ? _activeGoalData.id : string.Empty;
        saveData.activeGoalProgress = _produceProgress;
        saveData.allGoalsCompleted = _allGoalsCompleted;
    }

    public void ApplyFromSave(GameSaveData saveData)
    {
        if (saveData == null)
            return;

        _allGoalsCompleted = saveData.allGoalsCompleted || saveData.goalChainCompleted;
        _produceProgress = saveData.activeGoalProgress;

        if (_allGoalsCompleted)
        {
            _activeGoalData = null;
            _activeGoal = null;
            return;
        }

        string goalId = saveData.activeGoalId;
        if (string.IsNullOrEmpty(goalId) && !string.IsNullOrEmpty(saveData.activeGoalChainId))
        {
            TryStartInitialGoalForNewGame();
            return;
        }

        if (string.IsNullOrEmpty(goalId))
        {
            _activeGoalData = null;
            _activeGoal = null;
            TryStartInitialGoalForNewGame();
            return;
        }

        GoalData goalData = GetGoalData(goalId);
        if (goalData == null)
        {
            _activeGoalData = null;
            _activeGoal = null;
            return;
        }

        _activeGoalData = goalData;
        if (goalData.conditionType == GoalConditionType.ProduceResource)
            _produceResourceSnapshot = GetResourceCount(goalData.targetId);

        BuildActiveGoalState(resetProduceSnapshot: false);
        EvaluateActiveGoal();
    }

    private void ActivateGoal(bool resetProduceProgress)
    {
        if (_activeGoalData == null)
        {
            CompleteAllGoals();
            return;
        }

        if (resetProduceProgress)
            _produceProgress = 0;

        BuildActiveGoalState(resetProduceSnapshot: resetProduceProgress);
        EvaluateActiveGoal();
    }

    private void BuildActiveGoalState(bool resetProduceSnapshot)
    {
        if (_activeGoalData == null)
        {
            _activeGoal = null;
            return;
        }

        if (resetProduceSnapshot && _activeGoalData.conditionType == GoalConditionType.ProduceResource)
            _produceResourceSnapshot = GetResourceCount(_activeGoalData.targetId);

        long progress = GetCurrentProgress(_activeGoalData);
        _activeGoal = new GoalState(_activeGoalData, progress);
    }

    private void HandleResourceChanged()
    {
        if (_activeGoal == null || _allGoalsCompleted || _activeGoalData == null)
            return;

        if (_activeGoalData.conditionType == GoalConditionType.ProduceResource)
        {
            int current = GetResourceCount(_activeGoalData.targetId);
            if (current > _produceResourceSnapshot)
                _produceProgress += current - _produceResourceSnapshot;

            _produceResourceSnapshot = current;
        }

        EvaluateActiveGoal();
    }

    private void HandleResearchCompleted(string researchId)
    {
        if (_activeGoal == null || _allGoalsCompleted || _activeGoalData == null)
            return;

        if (_activeGoalData.conditionType != GoalConditionType.CompleteResearch
            || _activeGoalData.targetId != researchId)
        {
            return;
        }

        EvaluateActiveGoal();
    }

    private void EvaluateActiveGoal()
    {
        if (_activeGoal == null || _allGoalsCompleted || _activeGoalData == null)
            return;

        long progress = GetCurrentProgress(_activeGoalData);
        _activeGoal.currentProgress = progress;

        if (_activeGoal.IsComplete)
        {
            CompleteCurrentGoal();
            return;
        }

        OnActiveGoalChanged?.Invoke(_activeGoal);
    }

    private long GetCurrentProgress(GoalData goalData)
    {
        if (goalData == null)
            return 0;

        switch (goalData.conditionType)
        {
            case GoalConditionType.ReachCredit:
                return _dataManager.Finances.Credit;
            case GoalConditionType.ReachWealth:
                return _dataManager.Finances.Wealth;
            case GoalConditionType.HaveResource:
                return GetResourceCount(goalData.targetId);
            case GoalConditionType.ProduceResource:
                return _produceProgress;
            case GoalConditionType.PlaceBuilding:
                return GetPlacedBuildingCount(goalData.targetId);
            case GoalConditionType.AssignBuildingOutput:
                return HasBuildingOutputAssigned(goalData.targetId) ? 1 : 0;
            case GoalConditionType.CompleteResearch:
                return _dataManager.Research.IsResearchCompleted(goalData.targetId) ? 1 : 0;
            default:
                return 0;
        }
    }

    private int GetResourceCount(string resourceId)
    {
        if (string.IsNullOrEmpty(resourceId) || _dataManager.Resource == null)
            return 0;

        ResourceEntry entry = _dataManager.Resource.GetResourceEntry(resourceId);
        return entry != null ? entry.state.count : 0;
    }

    private int GetPlacedBuildingCount(string buildingId)
    {
        if (string.IsNullOrEmpty(buildingId))
            return 0;

        if (_boundGridHandler != null)
            return _boundGridHandler.CountPlacedLayoutEntries(buildingId);

        return _dataManager.PlacedLayout != null
            ? _dataManager.PlacedLayout.CountPlacedBuildings(buildingId)
            : 0;
    }

    private bool HasBuildingOutputAssigned(string buildingId)
    {
        if (string.IsNullOrEmpty(buildingId))
            return false;

        if (_boundGridHandler != null)
            return _boundGridHandler.AnyPlacedBuildingHasConfiguredOutputResource(buildingId);

        return _dataManager.PlacedLayout != null
            && _dataManager.PlacedLayout.AnyPlacedBuildingHasConfiguredOutputResource(buildingId);
    }

    private void CompleteCurrentGoal()
    {
        GoalState completed = _activeGoal;
        if (completed == null || _activeGoalData == null)
            return;

        if (completed.rewardCredit > 0 && _dataManager.Finances != null)
            _dataManager.Finances.ModifyCredit(completed.rewardCredit);

        OnGoalCompleted?.Invoke(completed);

        GoalData nextGoal = _activeGoalData.nextGoal;
        if (nextGoal == null)
        {
            CompleteAllGoals();
            return;
        }

        _activeGoalData = nextGoal;
        ActivateGoal(resetProduceProgress: true);
    }

    private void CompleteAllGoals()
    {
        _allGoalsCompleted = true;
        _activeGoal = null;
        _activeGoalData = null;
        _produceProgress = 0;
        _produceResourceSnapshot = 0;
        OnAllGoalsCompleted?.Invoke();
    }
}
