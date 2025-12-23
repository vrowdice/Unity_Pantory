using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Thread(생산 라인)를 관리하는 서비스 클래스.
/// 데이터 변경 시 자동으로 저장(Save)을 트리거합니다.
/// </summary>
public class ThreadDataHandler
{
    private readonly DataManager _dataManager = null;

    #region Private Data Containers

    private Dictionary<string, ThreadState> _threads;
    private Dictionary<string, ThreadCategory> _categories;

    #endregion

    #region Events

    public event Action OnThreadChanged;
    public event Action OnCategoryChanged;

    #endregion

    #region Constructor & Save Logic

    public ThreadDataHandler(DataManager gameDataManager)
    {
        _threads = new Dictionary<string, ThreadState>();
        _categories = new Dictionary<string, ThreadCategory>();
        _dataManager = gameDataManager;
    }

    /// <summary> Thread 데이터를 저장합니다. </summary>
    private void SaveThreadData()
    {
        if (_dataManager?.SaveLoad != null)
        {
            // SaveLoadHandler의 SaveThreadData(this) 호출
            _dataManager.SaveLoad.SaveThreadData(this);
        }
    }

    /// <summary> Thread 데이터를 명시적으로 저장합니다. (외부에서 호출 가능) </summary>
    public void Save()
    {
        SaveThreadData();
    }

    #endregion

    #region Utility & Data Initialization

    /// <summary> Thread 데이터를 초기화하고 저장합니다. </summary>
    public void ResetThreadData()
    {
        _threads.Clear();
        _categories.Clear();
        Debug.Log("[ThreadService] All threads and categories reset to empty.");

        OnThreadChanged?.Invoke();
        OnCategoryChanged?.Invoke();
        SaveThreadData();
    }

    /// <summary> 모든 Thread를 초기화합니다 (로드 전용). </summary>
    public void ClearAllThreads()
    {
        _threads.Clear();
        _categories.Clear();
    }

    /// <summary> 등록된 Thread 개수를 반환합니다. </summary>
    public int GetThreadCount()
    {
        return _threads.Count;
    }

    /// <summary> 등록된 카테고리 개수를 반환합니다. </summary>
    public int GetCategoryCount()
    {
        return _categories.Count;
    }

    #endregion

    #region Thread Getters (Read Operations)

    /// <summary> 특정 Thread의 ThreadState를 반환합니다. </summary>
    public ThreadState GetThread(string threadId)
    {
        if (_threads.TryGetValue(threadId, out var thread)) return thread;
        Debug.LogWarning($"[ThreadService] Thread not found: {threadId}");
        return null;
    }

    /// <summary> 모든 Thread 정보를 딕셔너리로 반환합니다. </summary>
    public Dictionary<string, ThreadState> GetAllThreads()
    {
        return new Dictionary<string, ThreadState>(_threads);
    }

    /// <summary> 모든 Thread 정보를 리스트로 반환합니다. </summary>
    public List<ThreadState> GetAllThreadList()
    {
        return new List<ThreadState>(_threads.Values);
    }

    /// <summary> 등록된 모든 Thread ID 목록을 반환합니다. </summary>
    public List<string> GetAllThreadIds()
    {
        return new List<string>(_threads.Keys);
    }

    /// <summary> Thread가 존재하는지 확인합니다. </summary>
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
            Debug.LogWarning($"[ThreadService] Thread not found when aggregating resources: {threadId}");
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

    #endregion

    #region Thread CRUD (Write Operations)

    /// <summary> 새로운 Thread를 추가합니다. </summary>
    public bool AddThread(ThreadState threadState)
    {
        if (threadState == null || string.IsNullOrEmpty(threadState.threadId) || _threads.ContainsKey(threadState.threadId))
        {
            Debug.LogWarning($"[ThreadService] Cannot add thread. ID invalid or already exists: {threadState?.threadId}");
            return false;
        }

        _threads[threadState.threadId] = threadState;
        Debug.Log($"[ThreadService] Thread added: {threadState.threadName} ({threadState.threadId})");

        OnThreadChanged?.Invoke();
        SaveThreadData();
        return true;
    }

    /// <summary> Thread를 생성하거나, 이미 존재하면 이름을 업데이트합니다. </summary>
    public ThreadState CreateThread(string threadId, string threadName)
    {
        if (_threads.TryGetValue(threadId, out var existingThread))
        {
            if (existingThread.threadName != threadName)
            {
                existingThread.threadName = threadName;
                Debug.Log($"[ThreadService] Thread updated: {threadName} ({threadId})");
                OnThreadChanged?.Invoke();
                SaveThreadData();
            }
            return existingThread;
        }

        var newThread = new ThreadState(threadId, threadName);
        if (AddThread(newThread)) return newThread;

        return null;
    }

    /// <summary> Thread를 제거합니다. </summary>
    public bool RemoveThread(string threadId)
    {
        if (!_threads.ContainsKey(threadId))
        {
            Debug.LogWarning($"[ThreadService] Thread not found: {threadId}");
            return false;
        }

        var thread = _threads[threadId];

        // 1. 모든 카테고리에서 해당 스레드 제거
        foreach (var category in _categories.Values)
        {
            category.threadIds.Remove(threadId); // RemoveAll 대신 Remove 사용
        }

        // 2. 스레드 삭제
        _threads.Remove(threadId);
        Debug.Log($"[ThreadService] Thread removed: {thread.threadName} ({threadId})");

        OnCategoryChanged?.Invoke();
        OnThreadChanged?.Invoke();
        SaveThreadData();
        return true;
    }

    #endregion

    #region Building Management

    /// <summary> 특정 Thread의 건물 리스트를 반환합니다. </summary>
    public List<BuildingState> GetBuildingStates(string threadId)
    {
        if (_threads.TryGetValue(threadId, out var thread)) return thread.buildingStateList;
        Debug.LogWarning($"[ThreadService] Thread not found: {threadId}");
        return null;
    }

    /// <summary>
    /// 특정 Thread의 건물 목록을 새로운 리스트로 덮어씁니다. (핵심 저장 로직)
    /// BuildingTileManager에서 임시 데이터 적용 시 호출됩니다.
    /// </summary>
    /// <param name="threadId">대상 스레드 ID</param>
    /// <param name="newBuildingStates">덮어쓸 새 건물 상태 리스트</param>
    /// <returns>성공 시 true</returns>
    public bool OverwriteBuildings(string threadId, List<BuildingState> newBuildingStates)
    {
        if (!_threads.TryGetValue(threadId, out var thread))
        {
            Debug.LogWarning($"[ThreadService] Cannot overwrite buildings: Thread not found: {threadId}");
            return false;
        }

        thread.buildingStateList = newBuildingStates ?? new List<BuildingState>();

        Debug.Log($"[ThreadService] Overwrote {thread.buildingStateList.Count} buildings in thread: {threadId}");

        OnThreadChanged?.Invoke();
        SaveThreadData();
        return true;
    }

    /// <summary> 특정 Thread에 건물을 추가합니다. </summary>
    public bool AddBuilding(string threadId, BuildingState buildingState)
    {
        if (!_threads.TryGetValue(threadId, out var thread) || buildingState == null) return false;

        thread.buildingStateList.RemoveAll(b => b.positionX == buildingState.positionX && b.positionY == buildingState.positionY);
        thread.buildingStateList.Add(buildingState);

        Debug.Log($"[ThreadService] Building added/replaced in thread {thread.threadName}: {buildingState.buildingId}");

        OnThreadChanged?.Invoke();
        return true;
    }

    /// <summary> 특정 Thread에서 위치로 건물을 제거합니다. </summary>
    public bool RemoveBuilding(string threadId, Vector2Int position)
    {
        if (!_threads.TryGetValue(threadId, out var thread)) return false;

        int removedCount = thread.buildingStateList.RemoveAll(b => b.positionX == position.x && b.positionY == position.y);
        if (removedCount == 0)
        {
            Debug.LogWarning($"[ThreadService] Building not found at position {position} in thread {threadId}");
            return false;
        }

        Debug.Log($"[ThreadService] Building removed from thread {thread.threadName} at {position}");

        OnThreadChanged?.Invoke();
        return true;
    }

    /// <summary> 특정 위치에 건물이 있는지 확인합니다. </summary>
    public BuildingState GetBuildingAt(string threadId, Vector2Int position)
    {
        if (!_threads.TryGetValue(threadId, out var thread)) return null;
        return thread.buildingStateList.Find(b => b.positionX == position.x && b.positionY == position.y);
    }

    #endregion

    #region Category Getters (Read Operations)

    /// <summary> 특정 카테고리를 반환합니다. </summary>
    public ThreadCategory GetCategory(string categoryId)
    {
        if (_categories.TryGetValue(categoryId, out var category)) return category;
        Debug.LogWarning($"[ThreadService] Category not found: {categoryId}");
        return null;
    }

    /// <summary> 모든 카테고리를 반환합니다. </summary>
    public Dictionary<string, ThreadCategory> GetAllCategories()
    {
        return new Dictionary<string, ThreadCategory>(_categories);
    }

    /// <summary> 모든 카테고리 ID 목록을 반환합니다. </summary>
    public List<string> GetAllCategoryIds()
    {
        return new List<string>(_categories.Keys);
    }

    /// <summary> 카테고리가 존재하는지 확인합니다. </summary>
    public bool HasCategory(string categoryId)
    {
        return _categories.ContainsKey(categoryId);
    }

    /// <summary> 특정 카테고리에 속한 스레드 ID 목록을 반환합니다. </summary>
    public List<string> GetThreadIdsInCategory(string categoryId)
    {
        if (_categories.TryGetValue(categoryId, out var category)) return new List<string>(category.threadIds);
        Debug.LogWarning($"[ThreadService] Category not found: {categoryId}");
        return new List<string>();
    }

    /// <summary> 특정 카테고리에 속한 스레드 상태 목록을 반환합니다. </summary>
    public List<ThreadState> GetThreadsInCategory(string categoryId)
    {
        var result = new List<ThreadState>();
        foreach (var thread in _threads.Values)
        {
            if (thread.categoryId == categoryId) result.Add(thread);
        }
        return result;
    }

    #endregion

    #region Category CRUD (Write Operations)

    /// <summary> 카테고리를 추가합니다. </summary>
    public bool AddCategory(ThreadCategory category)
    {
        if (category == null || string.IsNullOrEmpty(category.categoryId) || _categories.ContainsKey(category.categoryId))
        {
            Debug.LogWarning($"[ThreadService] Invalid category or ID exists: {category?.categoryId}");
            return false;
        }

        _categories[category.categoryId] = category;
        Debug.Log($"[ThreadService] Category added: {category.categoryName}");

        OnCategoryChanged?.Invoke();
        SaveThreadData();
        return true;
    }

    /// <summary> 카테고리를 생성하고 추가합니다. </summary>
    public ThreadCategory CreateCategory(string categoryId, string categoryName)
    {
        var newCategory = new ThreadCategory(categoryId, categoryName);
        if (AddCategory(newCategory)) return newCategory;
        return null;
    }

    /// <summary> 카테고리를 제거합니다. </summary>
    public bool RemoveCategory(string categoryId)
    {
        if (!_categories.ContainsKey(categoryId)) return false;

        var category = _categories[categoryId];
        // 카테고리에 속한 스레드 ID 초기화
        if (category.threadIds != null)
        {
            foreach (var threadId in category.threadIds)
            {
                if (_threads.TryGetValue(threadId, out var thread)) thread.categoryId = string.Empty;
            }
        }

        _categories.Remove(categoryId);
        Debug.Log($"[ThreadService] Category removed: {category.categoryName}");

        OnCategoryChanged?.Invoke();
        OnThreadChanged?.Invoke();
        SaveThreadData();
        return true;
    }

    /// <summary> 카테고리 이름을 변경합니다. </summary>
    public bool RenameCategory(string categoryId, string newName)
    {
        if (!_categories.TryGetValue(categoryId, out var category)) return false;

        category.categoryName = newName;
        Debug.Log($"[ThreadService] Category renamed: {categoryId} -> {newName}");

        OnCategoryChanged?.Invoke();
        SaveThreadData();
        return true;
    }

    /// <summary> 카테고리에 스레드를 추가합니다. </summary>
    public bool AddThreadToCategory(string categoryId, string threadId)
    {
        if (!_categories.TryGetValue(categoryId, out var category) || !_threads.TryGetValue(threadId, out var thread)) return false;

        // 기존 카테고리에서 제거
        if (!string.IsNullOrEmpty(thread.categoryId) && _categories.TryGetValue(thread.categoryId, out var oldCategory))
        {
            oldCategory.threadIds.Remove(threadId);
        }

        // 새 카테고리에 추가
        if (!category.threadIds.Contains(threadId)) category.threadIds.Add(threadId);

        thread.categoryId = categoryId;
        Debug.Log($"[ThreadService] Thread {thread.threadName} added to category {category.categoryName}");

        OnCategoryChanged?.Invoke();
        OnThreadChanged?.Invoke();
        SaveThreadData();
        return true;
    }

    /// <summary> 카테고리에서 스레드를 제거합니다. </summary>
    public bool RemoveThreadFromCategory(string categoryId, string threadId)
    {
        if (!_categories.TryGetValue(categoryId, out var category) || !_threads.TryGetValue(threadId, out var thread)) return false;

        category.threadIds.Remove(threadId);
        thread.categoryId = string.Empty;
        Debug.Log($"[ThreadService] Thread {thread.threadName} removed from category {category.categoryName}");

        OnCategoryChanged?.Invoke();
        OnThreadChanged?.Invoke();
        SaveThreadData();
        return true;
    }

    #endregion

    #region Save/Load Data Access

    /// <summary> 저장용 Thread 리스트를 반환합니다. </summary>
    public List<ThreadState> GetThreadListForSave()
    {
        return new List<ThreadState>(_threads.Values);
    }

    /// <summary> 저장용 Category 리스트를 반환합니다. </summary>
    public List<ThreadCategory> GetCategoryListForSave()
    {
        return new List<ThreadCategory>(_categories.Values);
    }

    /// <summary> 저장된 Thread 리스트를 로드합니다. </summary>
    public void LoadThreads(List<ThreadState> threads)
    {
        if (threads == null) return;

        // 기존 로직 유지 (LoadThreads는 AddThread를 사용하지 않고 직접 _threads를 채움)

        int loadedCount = 0;
        int skippedCount = 0;

        foreach (var thread in threads)
        {
            if (thread == null || string.IsNullOrEmpty(thread.threadId)) continue;
            if (_threads.ContainsKey(thread.threadId))
            {
                Debug.LogWarning($"[ThreadService] Skipping duplicate thread during load: {thread.threadId}");
                skippedCount++;
                continue;
            }
            _threads[thread.threadId] = thread;
            loadedCount++;
        }

        OnThreadChanged?.Invoke();
    }

    /// <summary> 저장된 Category 리스트를 로드합니다. </summary>
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

    #endregion
}