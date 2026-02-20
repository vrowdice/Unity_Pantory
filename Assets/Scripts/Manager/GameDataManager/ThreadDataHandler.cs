using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Thread(생산 라인)를 관리하는 서비스 클래스.
/// 데이터 변경 시 자동으로 SaveLoadManager를 통해 저장을 트리거합니다.
/// </summary>
public class ThreadDataHandler : IDataHandlerEvents
{
    private readonly DataManager _dataManager = null;
    private Dictionary<string, ThreadState> _threads;
    private Dictionary<string, ThreadCategory> _categories;

    public event Action OnThreadChanged;
    public event Action OnCategoryChanged;

    public ThreadDataHandler(DataManager gameDataManager)
    {
        _threads = new Dictionary<string, ThreadState>();
        _categories = new Dictionary<string, ThreadCategory>();
        _dataManager = gameDataManager;
    }

    private void SaveThreadData()
    {
        if (SaveLoadManager.Instance != null && SaveLoadManager.Instance.Thread != null)
        {
            SaveLoadManager.Instance.Thread.SaveThreadData(this);
        }
    }

    public void Save()
    {
        SaveThreadData();
    }

    public void ResetThreadData()
    {
        _threads.Clear();
        _categories.Clear();
        Debug.Log("[ThreadDataHandler] All threads and categories reset to empty.");

        OnThreadChanged?.Invoke();
        OnCategoryChanged?.Invoke();
        SaveThreadData();
    }

    public void ClearAllThreads()
    {
        _threads.Clear();
        _categories.Clear();
    }

    public int GetThreadCount()
    {
        return _threads.Count;
    }

    public int GetCategoryCount()
    {
        return _categories.Count;
    }

    public ThreadState GetThread(string threadId)
    {
        if (_threads.TryGetValue(threadId, out var thread)) return thread;
        Debug.LogWarning($"[ThreadDataHandler] Thread not found: {threadId}");
        return null;
    }

    public Dictionary<string, ThreadState> GetAllThreads()
    {
        return new Dictionary<string, ThreadState>(_threads);
    }

    public List<ThreadState> GetAllThreadList()
    {
        return new List<ThreadState>(_threads.Values);
    }

    public List<string> GetAllThreadIds()
    {
        return new List<string>(_threads.Keys);
    }

    public bool HasThread(string threadId)
    {
        return _threads.ContainsKey(threadId);
    }

    /// <summary>
    /// 특정 Thread의 자원 소비/생산량을 집계한 Summary를 반환합니다.
    /// </summary>
    public ThreadResourceSummary GetThreadResourceSummary(string threadId)
    {
        if (!_threads.TryGetValue(threadId, out var thread))
        {
            Debug.LogWarning($"[ThreadDataHandler] Thread not found when aggregating resources: {threadId}");
            return null;
        }

        thread.TryGetAggregatedResourceCounts(out var consumptionCounts, out var productionCounts);
        return new ThreadResourceSummary(thread.threadId, consumptionCounts, productionCounts);
    }

    /// <summary>
    /// 특정 Thread의 자원 Summary를 반환 시도합니다.
    /// </summary>
    public bool TryGetThreadResourceSummary(string threadId, out ThreadResourceSummary summary)
    {
        summary = GetThreadResourceSummary(threadId);
        return summary != null;
    }

    /// <summary>
    /// 모든 Thread의 자원 Summary를 리스트로 반환합니다.
    /// </summary>
    public List<ThreadResourceSummary> GetAllThreadResourceSummaries()
    {
        var result = new List<ThreadResourceSummary>();

        foreach (var thread in _threads.Values)
        {
            thread.TryGetAggregatedResourceCounts(out var consumptionCounts, out var productionCounts);
            result.Add(new ThreadResourceSummary(thread.threadId, consumptionCounts, productionCounts));
        }

        return result;
    }

    public bool AddThread(ThreadState threadState)
    {
        if (threadState == null || string.IsNullOrEmpty(threadState.threadId) || _threads.ContainsKey(threadState.threadId))
        {
            Debug.LogWarning($"[ThreadDataHandler] Cannot add thread. ID invalid or already exists: {threadState?.threadId}");
            return false;
        }

        _threads[threadState.threadId] = threadState;
        Debug.Log($"[ThreadDataHandler] Thread added: {threadState.threadName} ({threadState.threadId})");

        OnThreadChanged?.Invoke();
        SaveThreadData();
        return true;
    }

    public ThreadState CreateThread(string threadId, string threadName)
    {
        if (_threads.TryGetValue(threadId, out var existingThread))
        {
            if (existingThread.threadName != threadName)
            {
                existingThread.threadName = threadName;
                Debug.Log($"[ThreadDataHandler] Thread updated: {threadName} ({threadId})");
                OnThreadChanged?.Invoke();
                SaveThreadData();
            }
            return existingThread;
        }

        var newThread = new ThreadState(threadId, threadName);
        if (AddThread(newThread)) return newThread;

        return null;
    }

    public bool RemoveThread(string threadId)
    {
        if (!_threads.ContainsKey(threadId))
        {
            Debug.LogWarning($"[ThreadDataHandler] Thread not found: {threadId}");
            return false;
        }

        var thread = _threads[threadId];

        foreach (var category in _categories.Values)
        {
            category.threadIds.Remove(threadId);
        }

        _threads.Remove(threadId);
        Debug.Log($"[ThreadDataHandler] Thread removed: {thread.threadName} ({threadId})");

        OnCategoryChanged?.Invoke();
        OnThreadChanged?.Invoke();
        SaveThreadData();
        return true;
    }

    public List<BuildingState> GetBuildingStates(string threadId)
    {
        if (_threads.TryGetValue(threadId, out var thread)) return thread.buildingStateList;
        Debug.LogWarning($"[ThreadDataHandler] Thread not found: {threadId}");
        return null;
    }

    public bool OverwriteBuildings(string threadId, List<BuildingState> newBuildingStates)
    {
        if (!_threads.TryGetValue(threadId, out var thread))
        {
            Debug.LogWarning($"[ThreadDataHandler] Cannot overwrite buildings: Thread not found: {threadId}");
            return false;
        }

        if (thread.buildingStateList != null)
        {
            thread.buildingStateList.Clear();
        }
        else
        {
            thread.buildingStateList = new List<BuildingState>();
        }

        if (newBuildingStates != null && newBuildingStates.Count > 0)
        {
            thread.buildingStateList.AddRange(newBuildingStates);
        }

        Debug.Log($"[ThreadDataHandler] Overwrote {thread.buildingStateList.Count} buildings in thread: {threadId} (was {newBuildingStates?.Count ?? 0} buildings)");

        OnThreadChanged?.Invoke();
        SaveThreadData();
        return true;
    }

    public bool AddBuilding(string threadId, BuildingState buildingState)
    {
        if (!_threads.TryGetValue(threadId, out var thread) || buildingState == null) return false;

        thread.buildingStateList.RemoveAll(b => b.positionX == buildingState.positionX && b.positionY == buildingState.positionY);
        thread.buildingStateList.Add(buildingState);

        Debug.Log($"[ThreadDataHandler] Building added/replaced in thread {thread.threadName}: {buildingState.Id}");

        OnThreadChanged?.Invoke();
        return true;
    }

    public bool RemoveBuilding(string threadId, Vector2Int position)
    {
        if (!_threads.TryGetValue(threadId, out var thread)) return false;

        int removedCount = thread.buildingStateList.RemoveAll(b => b.positionX == position.x && b.positionY == position.y);
        if (removedCount == 0)
        {
            Debug.LogWarning($"[ThreadDataHandler] Building not found at position {position} in thread {threadId}");
            return false;
        }

        Debug.Log($"[ThreadDataHandler] Building removed from thread {thread.threadName} at {position}");

        OnThreadChanged?.Invoke();
        return true;
    }

    public BuildingState GetBuildingAt(string threadId, Vector2Int position)
    {
        if (!_threads.TryGetValue(threadId, out var thread)) return null;
        return thread.buildingStateList.Find(b => b.positionX == position.x && b.positionY == position.y);
    }

    public ThreadCategory GetCategory(string categoryId)
    {
        if (_categories.TryGetValue(categoryId, out var category)) return category;
        Debug.LogWarning($"[ThreadDataHandler] Category not found: {categoryId}");
        return null;
    }

    public Dictionary<string, ThreadCategory> GetAllCategories()
    {
        return new Dictionary<string, ThreadCategory>(_categories);
    }

    public List<string> GetAllCategoryIds()
    {
        return new List<string>(_categories.Keys);
    }

    public bool HasCategory(string categoryId)
    {
        return _categories.ContainsKey(categoryId);
    }

    public List<string> GetThreadIdsInCategory(string categoryId)
    {
        if (_categories.TryGetValue(categoryId, out var category)) return new List<string>(category.threadIds);
        Debug.LogWarning($"[ThreadDataHandler] Category not found: {categoryId}");
        return new List<string>();
    }

    public List<ThreadState> GetThreadsInCategory(string categoryId)
    {
        var result = new List<ThreadState>();
        foreach (var thread in _threads.Values)
        {
            if (thread.categoryId == categoryId) result.Add(thread);
        }
        return result;
    }

    public bool AddCategory(ThreadCategory category)
    {
        if (category == null || string.IsNullOrEmpty(category.categoryId) || _categories.ContainsKey(category.categoryId))
        {
            Debug.LogWarning($"[ThreadDataHandler] Invalid category or ID exists: {category?.categoryId}");
            return false;
        }

        _categories[category.categoryId] = category;
        Debug.Log($"[ThreadDataHandler] Category added: {category.categoryName}");

        OnCategoryChanged?.Invoke();
        SaveThreadData();
        return true;
    }

    public ThreadCategory CreateCategory(string categoryId, string categoryName)
    {
        var newCategory = new ThreadCategory(categoryId, categoryName);
        if (AddCategory(newCategory)) return newCategory;
        return null;
    }

    public bool RemoveCategory(string categoryId)
    {
        if (!_categories.ContainsKey(categoryId)) return false;

        var category = _categories[categoryId];
        if (category.threadIds != null)
        {
            foreach (var threadId in category.threadIds)
            {
                if (_threads.TryGetValue(threadId, out var thread)) thread.categoryId = string.Empty;
            }
        }

        _categories.Remove(categoryId);
        Debug.Log($"[ThreadDataHandler] Category removed: {category.categoryName}");

        OnCategoryChanged?.Invoke();
        OnThreadChanged?.Invoke();
        SaveThreadData();
        return true;
    }

    public bool RenameCategory(string categoryId, string newName)
    {
        if (!_categories.TryGetValue(categoryId, out var category)) return false;

        category.categoryName = newName;
        Debug.Log($"[ThreadDataHandler] Category renamed: {categoryId} -> {newName}");

        OnCategoryChanged?.Invoke();
        SaveThreadData();
        return true;
    }

    public bool AddThreadToCategory(string categoryId, string threadId)
    {
        if (!_categories.TryGetValue(categoryId, out var category) || !_threads.TryGetValue(threadId, out var thread)) return false;

        if (!string.IsNullOrEmpty(thread.categoryId) && _categories.TryGetValue(thread.categoryId, out var oldCategory))
        {
            oldCategory.threadIds.Remove(threadId);
        }

        if (!category.threadIds.Contains(threadId)) category.threadIds.Add(threadId);

        thread.categoryId = categoryId;
        Debug.Log($"[ThreadDataHandler] Thread {thread.threadName} added to category {category.categoryName}");

        OnCategoryChanged?.Invoke();
        OnThreadChanged?.Invoke();
        SaveThreadData();
        return true;
    }

    public bool RemoveThreadFromCategory(string categoryId, string threadId)
    {
        if (!_categories.TryGetValue(categoryId, out var category) || !_threads.TryGetValue(threadId, out var thread)) return false;

        category.threadIds.Remove(threadId);
        thread.categoryId = string.Empty;
        Debug.Log($"[ThreadDataHandler] Thread {thread.threadName} removed from category {category.categoryName}");

        OnCategoryChanged?.Invoke();
        OnThreadChanged?.Invoke();
        SaveThreadData();
        return true;
    }

    public List<ThreadState> GetThreadListForSave()
    {
        return new List<ThreadState>(_threads.Values);
    }

    public List<ThreadCategory> GetCategoryListForSave()
    {
        return new List<ThreadCategory>(_categories.Values);
    }

    public void LoadThreads(List<ThreadState> threads)
    {
        if (threads == null) return;

        int loadedCount = 0;
        int skippedCount = 0;

        foreach (var thread in threads)
        {
            if (thread == null || string.IsNullOrEmpty(thread.threadId)) continue;
            if (_threads.ContainsKey(thread.threadId))
            {
                Debug.LogWarning($"[ThreadDataHandler] Skipping duplicate thread during load: {thread.threadId}");
                skippedCount++;
                continue;
            }

            if (thread.buildingStateList == null)
            {
                Debug.LogWarning($"[ThreadDataHandler] buildingStateList is null for thread {thread.threadId} after deserialization. Initializing.");
                thread.buildingStateList = new List<BuildingState>();
            }

            _threads[thread.threadId] = thread;
            loadedCount++;
            Debug.Log($"[ThreadDataHandler] Loaded thread: {thread.threadName} ({thread.threadId}) with {thread.buildingStateList.Count} buildings");
        }

        Debug.Log($"[ThreadDataHandler] Loaded {loadedCount} threads, skipped {skippedCount} duplicates");
        OnThreadChanged?.Invoke();
    }

    public void LoadCategories(List<ThreadCategory> categories)
    {
        if (categories == null) return;

        foreach (var category in categories)
        {
            if (category != null && !string.IsNullOrEmpty(category.categoryId))
            {
                _categories[category.categoryId] = category;
            }
        }
        OnCategoryChanged?.Invoke();
    }

    /// <summary>
    /// 모든 이벤트 구독을 초기화합니다.
    /// </summary>
    public void ClearAllSubscriptions()
    {
        OnThreadChanged = null;
        OnCategoryChanged = null;
    }
}