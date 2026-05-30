using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어 목표(퀘스트) 진행을 관리합니다. isDefaultUnlocked 목표는 생성 시 자동 활성화되며,
/// 완료 시 nextGoal을 해제합니다.
/// </summary>
public class GoalDataHandler : IDataHandlerEvents, ICrossHandlerEvents, IGameSaveHandler
{
    private readonly DataManager _dataManager;
    private readonly Dictionary<string, GoalData> _goalDict = new Dictionary<string, GoalData>();
    private readonly List<GoalState> _activeGoals = new List<GoalState>();
    private readonly Dictionary<string, long> _produceProgressByGoalId = new Dictionary<string, long>();
    private readonly Dictionary<string, int> _produceResourceSnapshotByGoalId = new Dictionary<string, int>();
    private readonly HashSet<string> _completedGoalIds = new HashSet<string>();

    private bool _allGoalsCompleted;
    private MainBuildingGridHandler _boundGridHandler;

    public IReadOnlyList<GoalState> ActiveGoals => _activeGoals;
    public bool AllGoalsCompleted => _allGoalsCompleted;

    public void FillCompletedGoalIds(List<string> results)
    {
        if (results == null)
            return;

        results.Clear();
        foreach (string goalId in _completedGoalIds)
            results.Add(goalId);
    }

    public event Action OnActiveGoalsChanged;
    public event Action<GoalState> OnGoalCompleted;
    public event Action OnAllGoalsCompleted;

    public GoalDataHandler(DataManager dataManager, List<GoalData> goalDataList)
    {
        _dataManager = dataManager;

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
        UnlockDefaultGoals();
    }

    public GoalData GetGoalData(string goalId)
    {
        if (string.IsNullOrEmpty(goalId))
            return null;

        return _goalDict.TryGetValue(goalId, out GoalData goalData) ? goalData : null;
    }

    public void BindSceneGrid(MainBuildingGridHandler gridHandler)
    {
        if (gridHandler == null)
            return;

        UnbindSceneGrid();
        _boundGridHandler = gridHandler;
        _boundGridHandler.OnBuildingInstanceLayoutChanged -= EvaluateAllActiveGoals;
        _boundGridHandler.OnBuildingInstanceLayoutChanged += EvaluateAllActiveGoals;
        EvaluateAllActiveGoals();
    }

    public void UnbindSceneGrid()
    {
        if (_boundGridHandler == null)
            return;

        _boundGridHandler.OnBuildingInstanceLayoutChanged -= EvaluateAllActiveGoals;
        _boundGridHandler = null;
    }

    public void SubscribeCrossHandlerEvents()
    {
        if (_dataManager?.Finances != null)
        {
            _dataManager.Finances.OnCreditChanged -= EvaluateAllActiveGoals;
            _dataManager.Finances.OnCreditChanged += EvaluateAllActiveGoals;
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
            _dataManager.Finances.OnCreditChanged -= EvaluateAllActiveGoals;

        if (_dataManager?.Resource != null)
            _dataManager.Resource.OnResourceChanged -= HandleResourceChanged;

        if (_dataManager?.Research != null)
            _dataManager.Research.OnResearchCompleted -= HandleResearchCompleted;

        OnActiveGoalsChanged = null;
        OnGoalCompleted = null;
        OnAllGoalsCompleted = null;
    }

    public void CaptureTo(GameSaveData saveData)
    {
        if (saveData == null)
            return;

        saveData.activeGoals ??= new List<GoalActiveSaveData>();
        saveData.activeGoals.Clear();

        for (int i = 0; i < _activeGoals.Count; i++)
        {
            GoalState state = _activeGoals[i];
            saveData.activeGoals.Add(new GoalActiveSaveData
            {
                goalId = state.goalId,
                produceProgress = _produceProgressByGoalId.TryGetValue(state.goalId, out long progress) ? progress : 0
            });
        }

        saveData.completedGoalIds ??= new List<string>();
        saveData.completedGoalIds.Clear();
        saveData.completedGoalIds.AddRange(_completedGoalIds);
        saveData.allGoalsCompleted = _allGoalsCompleted;
    }

    public void ApplyFromSave(GameSaveData saveData)
    {
        if (saveData == null)
            return;

        ClearActiveGoalRuntime();
        _completedGoalIds.Clear();
        _allGoalsCompleted = saveData.allGoalsCompleted;

        if (saveData.completedGoalIds != null)
        {
            for (int i = 0; i < saveData.completedGoalIds.Count; i++)
            {
                string goalId = saveData.completedGoalIds[i];
                if (!string.IsNullOrEmpty(goalId))
                    _completedGoalIds.Add(goalId);
            }
        }

        if (_allGoalsCompleted)
            return;

        if (RestoreActiveGoalsFromSave(saveData))
            EvaluateAllActiveGoals();
        else
            UnlockDefaultGoals();
    }

    private void UnlockDefaultGoals()
    {
        if (_allGoalsCompleted)
            return;

        foreach (GoalData goalData in _goalDict.Values)
        {
            if (goalData == null || !goalData.isDefaultUnlocked)
                continue;

            TryUnlockGoal(goalData.id, resetProduceProgress: true);
        }

        if (_activeGoals.Count == 0)
            return;

        NotifyActiveGoalsChanged();
        EvaluateAllActiveGoals();
    }

    private bool RestoreActiveGoalsFromSave(GameSaveData saveData)
    {
        if (saveData.activeGoals == null || saveData.activeGoals.Count == 0)
            return false;

        for (int i = 0; i < saveData.activeGoals.Count; i++)
        {
            GoalActiveSaveData entry = saveData.activeGoals[i];
            if (entry == null || string.IsNullOrEmpty(entry.goalId))
                continue;

            _produceProgressByGoalId[entry.goalId] = entry.produceProgress;
            TryUnlockGoal(entry.goalId, resetProduceProgress: false);
        }

        return _activeGoals.Count > 0;
    }

    private bool TryUnlockGoal(string goalId, bool resetProduceProgress)
    {
        if (string.IsNullOrEmpty(goalId)
            || _completedGoalIds.Contains(goalId)
            || ContainsActiveGoal(goalId))
        {
            return false;
        }

        GoalData goalData = GetGoalData(goalId);
        if (goalData == null)
            return false;

        _allGoalsCompleted = false;

        if (resetProduceProgress || !_produceProgressByGoalId.ContainsKey(goalId))
            _produceProgressByGoalId[goalId] = 0;

        if (goalData.conditionType == GoalConditionType.ProduceResource)
            _produceResourceSnapshotByGoalId[goalId] = GetResourceCount(goalData.targetId);

        long progress = GetCurrentProgress(goalData, goalId);
        _activeGoals.Add(new GoalState(goalData, progress));
        return true;
    }

    private bool ContainsActiveGoal(string goalId)
    {
        for (int i = 0; i < _activeGoals.Count; i++)
        {
            if (_activeGoals[i].goalId == goalId)
                return true;
        }

        return false;
    }

    private int IndexOfActiveGoal(string goalId)
    {
        for (int i = 0; i < _activeGoals.Count; i++)
        {
            if (_activeGoals[i].goalId == goalId)
                return i;
        }

        return -1;
    }

    /// <summary>
    /// 채굴·벌목 등 플레이어 보유량에 직접 반영되는 생산(원자재 공장) 진행을 갱신합니다.
    /// </summary>
    private void HandleResourceChanged()
    {
        if (_allGoalsCompleted || _activeGoals.Count == 0)
            return;

        for (int i = 0; i < _activeGoals.Count; i++)
        {
            string goalId = _activeGoals[i].goalId;
            GoalData goalData = GetGoalData(goalId);
            if (goalData == null || goalData.conditionType != GoalConditionType.ProduceResource)
                continue;

            int current = GetResourceCount(goalData.targetId);
            if (!_produceResourceSnapshotByGoalId.TryGetValue(goalId, out int snapshot))
                snapshot = current;

            if (current > snapshot)
                AddProduceProgress(goalId, current - snapshot);

            _produceResourceSnapshotByGoalId[goalId] = current;
        }

        EvaluateAllActiveGoals();
    }

    /// <summary>
    /// 가공 건물 등 도로로 배출되는 생산량을 목표 진행에 반영합니다.
    /// </summary>
    public void NotifyResourceProduced(string resourceId, int amount)
    {
        if (_allGoalsCompleted || _activeGoals.Count == 0 || string.IsNullOrEmpty(resourceId) || amount <= 0)
            return;

        bool anyUpdated = false;

        for (int i = 0; i < _activeGoals.Count; i++)
        {
            string goalId = _activeGoals[i].goalId;
            GoalData goalData = GetGoalData(goalId);
            if (goalData == null
                || goalData.conditionType != GoalConditionType.ProduceResource
                || goalData.targetId != resourceId)
            {
                continue;
            }

            AddProduceProgress(goalId, amount);
            anyUpdated = true;
        }

        if (anyUpdated)
            EvaluateAllActiveGoals();
    }

    private void AddProduceProgress(string goalId, long amount)
    {
        if (amount <= 0)
            return;

        long progress = _produceProgressByGoalId.TryGetValue(goalId, out long existingProgress)
            ? existingProgress
            : 0;
        _produceProgressByGoalId[goalId] = progress + amount;
    }

    private void HandleResearchCompleted(string researchId)
    {
        EvaluateAllActiveGoals();
    }

    private void EvaluateAllActiveGoals()
    {
        if (_allGoalsCompleted || _activeGoals.Count == 0)
            return;

        bool progressChanged = false;

        for (int i = 0; i < _activeGoals.Count; i++)
        {
            GoalState state = _activeGoals[i];
            GoalData goalData = GetGoalData(state.goalId);
            if (goalData == null)
                continue;

            long progress = GetCurrentProgress(goalData, state.goalId);
            if (state.currentProgress != progress)
            {
                state.currentProgress = progress;
                progressChanged = true;
            }

            if (state.IsComplete)
            {
                CompleteGoal(state.goalId);
                return;
            }
        }

        if (progressChanged)
            NotifyActiveGoalsChanged();
    }

    private long GetCurrentProgress(GoalData goalData, string goalId)
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
                return _produceProgressByGoalId.TryGetValue(goalId, out long progress) ? progress : 0;
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

    private void CompleteGoal(string goalId)
    {
        int index = IndexOfActiveGoal(goalId);
        if (index < 0)
            return;

        GoalState completed = _activeGoals[index];
        GoalData goalData = GetGoalData(goalId);
        if (goalData == null)
            return;

        _activeGoals.RemoveAt(index);
        _produceProgressByGoalId.Remove(goalId);
        _produceResourceSnapshotByGoalId.Remove(goalId);
        _completedGoalIds.Add(goalId);

        if (completed.rewardCredit > 0 && _dataManager.Finances != null)
            _dataManager.Finances.ModifyCredit(completed.rewardCredit);

        OnGoalCompleted?.Invoke(completed);
        ShowGoalCompletedWarning(goalData);

        string nextGoalId = ResolveNextGoalId(goalData);
        if (!string.IsNullOrEmpty(nextGoalId))
            TryUnlockGoal(nextGoalId, resetProduceProgress: true);

        if (AreAllGoalsCompleted())
        {
            CompleteAllGoals();
            return;
        }

        NotifyActiveGoalsChanged();
        EvaluateAllActiveGoals();
    }

    private bool AreAllGoalsCompleted()
    {
        if (_goalDict.Count == 0)
            return false;

        foreach (KeyValuePair<string, GoalData> pair in _goalDict)
        {
            if (!_completedGoalIds.Contains(pair.Key))
                return false;
        }

        return true;
    }

    private void CompleteAllGoals()
    {
        _allGoalsCompleted = true;
        ClearActiveGoalRuntime();
        OnAllGoalsCompleted?.Invoke();
        UIManager.Instance?.ShowWarningPopup("GoalChainComplete");
        NotifyActiveGoalsChanged();
    }

    private void ClearActiveGoalRuntime()
    {
        _activeGoals.Clear();
        _produceProgressByGoalId.Clear();
        _produceResourceSnapshotByGoalId.Clear();
    }

    private void NotifyActiveGoalsChanged()
    {
        OnActiveGoalsChanged?.Invoke();
    }

    private static string ResolveNextGoalId(GoalData goalData)
    {
        if (goalData == null)
            return string.Empty;

        if (goalData.nextGoal != null && !string.IsNullOrEmpty(goalData.nextGoal.id))
            return goalData.nextGoal.id;

        return goalData.nextGoalId;
    }

    private static void ShowGoalCompletedWarning(GoalData goalData)
    {
        if (goalData == null || UIManager.Instance == null)
            return;

        string title = goalData.id.Localize(LocalizationUtils.TABLE_GOAL);
        UIManager.Instance.ShowWarningPopup("GoalComplete", title);
    }
}
