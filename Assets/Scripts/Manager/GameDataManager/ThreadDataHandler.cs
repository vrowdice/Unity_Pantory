using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Thread(생산 라인)를 관리하는 서비스 클래스
/// </summary>
public class ThreadDataHandler
{
    // Thread를 저장하는 딕셔너리 (Thread ID -> ThreadState)
    private Dictionary<string, ThreadState> _threads;

    // 카테고리를 저장하는 딕셔너리 (Category ID -> ThreadCategory)
    private Dictionary<string, ThreadCategory> _categories;

    // Thread 변경 이벤트
    public event Action OnThreadChanged;

    // Category 변경 이벤트
    public event Action OnCategoryChanged;

    /// <summary>
    /// ThreadService 생성자
    /// </summary>
    public ThreadDataHandler()
    {
        _threads = new Dictionary<string, ThreadState>();
        _categories = new Dictionary<string, ThreadCategory>();
        Debug.Log("[ThreadService] ThreadService initialized.");
    }

    // ----------------- Public Getters (읽기 전용) -----------------

    /// <summary>
    /// 특정 Thread의 ThreadState를 반환합니다.
    /// </summary>
    /// <param name="threadId">Thread ID</param>
    /// <returns>ThreadState 또는 null</returns>
    public ThreadState GetThread(string threadId)
    {
        if (_threads.TryGetValue(threadId, out var thread))
        {
            return thread;
        }
        
        Debug.LogWarning($"[ThreadService] Thread not found: {threadId}");
        return null;
    }

    /// <summary>
    /// 모든 Thread 정보를 딕셔너리로 반환합니다 (읽기 전용).
    /// </summary>
    /// <returns>Thread 딕셔너리의 복사본</returns>
    public Dictionary<string, ThreadState> GetAllThreads()
    {
        return new Dictionary<string, ThreadState>(_threads);
    }

    /// <summary>
    /// 등록된 모든 Thread ID 목록을 반환합니다.
    /// </summary>
    /// <returns>Thread ID 리스트</returns>
    public List<string> GetAllThreadIds()
    {
        return new List<string>(_threads.Keys);
    }

    /// <summary>
    /// 특정 Thread의 건물 리스트를 반환합니다.
    /// </summary>
    /// <param name="threadId">Thread ID</param>
    /// <returns>BuildingState 리스트 또는 null</returns>
    public List<BuildingState> GetBuildingStates(string threadId)
    {
        if (_threads.TryGetValue(threadId, out var thread))
        {
            return thread.buildingStateList;
        }
        
        Debug.LogWarning($"[ThreadService] Thread not found: {threadId}");
        return null;
    }

    /// <summary>
    /// Thread가 존재하는지 확인합니다.
    /// </summary>
    /// <param name="threadId">확인할 Thread ID</param>
    /// <returns>존재하면 true</returns>
    public bool HasThread(string threadId)
    {
        return _threads.ContainsKey(threadId);
    }

    // ----------------- Public Methods (Thread 관리) -----------------

    /// <summary>
    /// 새로운 Thread를 추가합니다.
    /// </summary>
    /// <param name="threadState">추가할 ThreadState</param>
    /// <returns>성공 시 true</returns>
    public bool AddThread(ThreadState threadState)
    {
        if (threadState == null)
        {
            Debug.LogWarning("[ThreadService] ThreadState is null.");
            return false;
        }

        if (string.IsNullOrEmpty(threadState.threadId))
        {
            Debug.LogWarning("[ThreadService] Thread ID is empty.");
            return false;
        }

        if (_threads.ContainsKey(threadState.threadId))
        {
            Debug.LogWarning($"[ThreadService] Thread already exists: {threadState.threadId}");
            return false;
        }

        _threads[threadState.threadId] = threadState;
        Debug.Log($"[ThreadService] Thread added: {threadState.threadName} ({threadState.threadId})");
        
        OnThreadChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// Thread를 생성하고 추가합니다.
    /// </summary>
    /// <param name="threadId">Thread ID</param>
    /// <param name="threadName">Thread 이름</param>
    /// <param name="division">부서/사업부</param>
    /// <returns>생성된 ThreadState</returns>
    public ThreadState CreateThread(string threadId, string threadName, string division = "")
    {
        var newThread = new ThreadState(threadId, threadName, division);
        
        if (AddThread(newThread))
        {
            return newThread;
        }
        
        return null;
    }

    /// <summary>
    /// Thread를 제거합니다.
    /// </summary>
    /// <param name="threadId">제거할 Thread ID</param>
    /// <returns>성공 시 true</returns>
    public bool RemoveThread(string threadId)
    {
        if (!_threads.ContainsKey(threadId))
        {
            Debug.LogWarning($"[ThreadService] Thread not found: {threadId}");
            return false;
        }

        var thread = _threads[threadId];
        _threads.Remove(threadId);
        Debug.Log($"[ThreadService] Thread removed: {thread.threadName} ({threadId})");
        
        OnThreadChanged?.Invoke();
        return true;
    }

    // ----------------- Public Methods (건물 관리) -----------------

    /// <summary>
    /// 특정 Thread에 건물을 추가합니다.
    /// </summary>
    /// <param name="threadId">Thread ID</param>
    /// <param name="buildingState">추가할 BuildingState</param>
    /// <returns>성공 시 true</returns>
    public bool AddBuilding(string threadId, BuildingState buildingState)
    {
        if (!_threads.TryGetValue(threadId, out var thread))
        {
            Debug.LogWarning($"[ThreadService] Thread not found: {threadId}");
            return false;
        }

        if (buildingState == null)
        {
            Debug.LogWarning("[ThreadService] BuildingState is null.");
            return false;
        }

        thread.buildingStateList.Add(buildingState);
        Debug.Log($"[ThreadService] Building added to thread {thread.threadName}: {buildingState.buildingId}");
        
        OnThreadChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// 특정 Thread에서 위치로 건물을 제거합니다.
    /// </summary>
    /// <param name="threadId">Thread ID</param>
    /// <param name="position">건물 위치</param>
    /// <returns>성공 시 true</returns>
    public bool RemoveBuilding(string threadId, Vector2Int position)
    {
        if (!_threads.TryGetValue(threadId, out var thread))
        {
            Debug.LogWarning($"[ThreadService] Thread not found: {threadId}");
            return false;
        }

        var building = thread.buildingStateList.Find(b => b.position == position);
        if (building == null)
        {
            Debug.LogWarning($"[ThreadService] Building not found at position {position} in thread {threadId}");
            return false;
        }

        thread.buildingStateList.Remove(building);
        Debug.Log($"[ThreadService] Building removed from thread {thread.threadName}: {building.buildingId} at {position}");
        
        OnThreadChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// 특정 위치에 건물이 있는지 확인합니다.
    /// </summary>
    /// <param name="threadId">Thread ID</param>
    /// <param name="position">확인할 위치</param>
    /// <returns>건물이 있으면 BuildingState, 없으면 null</returns>
    public BuildingState GetBuildingAt(string threadId, Vector2Int position)
    {
        if (!_threads.TryGetValue(threadId, out var thread))
        {
            return null;
        }

        return thread.buildingStateList.Find(b => b.position == position);
    }

    // ----------------- Utility Methods -----------------

    /// <summary>
    /// 모든 Thread를 초기화합니다.
    /// </summary>
    public void ResetAllThreads()
    {
        _threads.Clear();
        Debug.Log("[ThreadService] All threads have been reset.");
        
        OnThreadChanged?.Invoke();
    }

    /// <summary>
    /// 등록된 Thread 개수를 반환합니다.
    /// </summary>
    /// <returns>Thread 개수</returns>
    public int GetThreadCount()
    {
        return _threads.Count;
    }

    // ----------------- Category Management Methods -----------------

    /// <summary>
    /// 카테고리를 추가합니다.
    /// </summary>
    /// <param name="category">추가할 ThreadCategory</param>
    /// <returns>성공 시 true</returns>
    public bool AddCategory(ThreadCategory category)
    {
        if (category == null)
        {
            Debug.LogWarning("[ThreadService] ThreadCategory is null.");
            return false;
        }

        if (string.IsNullOrEmpty(category.categoryId))
        {
            Debug.LogWarning("[ThreadService] Category ID is empty.");
            return false;
        }

        if (_categories.ContainsKey(category.categoryId))
        {
            Debug.LogWarning($"[ThreadService] Category already exists: {category.categoryId}");
            return false;
        }

        _categories[category.categoryId] = category;
        Debug.Log($"[ThreadService] Category added: {category.categoryName} ({category.categoryId})");
        
        OnCategoryChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// 카테고리를 생성하고 추가합니다.
    /// </summary>
    /// <param name="categoryId">카테고리 ID</param>
    /// <param name="categoryName">카테고리 이름</param>
    /// <returns>생성된 ThreadCategory</returns>
    public ThreadCategory CreateCategory(string categoryId, string categoryName)
    {
        var newCategory = new ThreadCategory(categoryId, categoryName);
        
        if (AddCategory(newCategory))
        {
            return newCategory;
        }
        
        return null;
    }

    /// <summary>
    /// 카테고리를 제거합니다.
    /// </summary>
    /// <param name="categoryId">제거할 카테고리 ID</param>
    /// <returns>성공 시 true</returns>
    public bool RemoveCategory(string categoryId)
    {
        if (!_categories.ContainsKey(categoryId))
        {
            Debug.LogWarning($"[ThreadService] Category not found: {categoryId}");
            return false;
        }

        var category = _categories[categoryId];
        
        // 카테고리에 속한 모든 스레드의 categoryId 초기화
        if (category.threadIds != null)
        {
            foreach (var threadId in category.threadIds)
            {
                if (_threads.TryGetValue(threadId, out var thread))
                {
                    thread.categoryId = string.Empty;
                }
            }
        }

        _categories.Remove(categoryId);
        Debug.Log($"[ThreadService] Category removed: {category.categoryName} ({categoryId})");
        
        OnCategoryChanged?.Invoke();
        OnThreadChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// 카테고리 이름을 변경합니다.
    /// </summary>
    /// <param name="categoryId">카테고리 ID</param>
    /// <param name="newName">새 이름</param>
    /// <returns>성공 시 true</returns>
    public bool RenameCategory(string categoryId, string newName)
    {
        if (!_categories.TryGetValue(categoryId, out var category))
        {
            Debug.LogWarning($"[ThreadService] Category not found: {categoryId}");
            return false;
        }

        category.categoryName = newName;
        Debug.Log($"[ThreadService] Category renamed: {categoryId} -> {newName}");
        
        OnCategoryChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// 특정 카테고리를 반환합니다.
    /// </summary>
    /// <param name="categoryId">카테고리 ID</param>
    /// <returns>ThreadCategory 또는 null</returns>
    public ThreadCategory GetCategory(string categoryId)
    {
        if (_categories.TryGetValue(categoryId, out var category))
        {
            return category;
        }
        
        Debug.LogWarning($"[ThreadService] Category not found: {categoryId}");
        return null;
    }

    /// <summary>
    /// 모든 카테고리를 반환합니다.
    /// </summary>
    /// <returns>카테고리 딕셔너리의 복사본</returns>
    public Dictionary<string, ThreadCategory> GetAllCategories()
    {
        return new Dictionary<string, ThreadCategory>(_categories);
    }

    /// <summary>
    /// 모든 카테고리 ID 목록을 반환합니다.
    /// </summary>
    /// <returns>카테고리 ID 리스트</returns>
    public List<string> GetAllCategoryIds()
    {
        return new List<string>(_categories.Keys);
    }

    /// <summary>
    /// 카테고리가 존재하는지 확인합니다.
    /// </summary>
    /// <param name="categoryId">확인할 카테고리 ID</param>
    /// <returns>존재하면 true</returns>
    public bool HasCategory(string categoryId)
    {
        return _categories.ContainsKey(categoryId);
    }

    /// <summary>
    /// 등록된 카테고리 개수를 반환합니다.
    /// </summary>
    /// <returns>카테고리 개수</returns>
    public int GetCategoryCount()
    {
        return _categories.Count;
    }

    /// <summary>
    /// 카테고리에 스레드를 추가합니다.
    /// </summary>
    /// <param name="categoryId">카테고리 ID</param>
    /// <param name="threadId">추가할 스레드 ID</param>
    /// <returns>성공 시 true</returns>
    public bool AddThreadToCategory(string categoryId, string threadId)
    {
        if (!_categories.TryGetValue(categoryId, out var category))
        {
            Debug.LogWarning($"[ThreadService] Category not found: {categoryId}");
            return false;
        }

        if (!_threads.TryGetValue(threadId, out var thread))
        {
            Debug.LogWarning($"[ThreadService] Thread not found: {threadId}");
            return false;
        }

        // 기존 카테고리에서 제거
        if (!string.IsNullOrEmpty(thread.categoryId) && _categories.TryGetValue(thread.categoryId, out var oldCategory))
        {
            oldCategory.threadIds.Remove(threadId);
        }

        // 새 카테고리에 추가
        if (!category.threadIds.Contains(threadId))
        {
            category.threadIds.Add(threadId);
        }
        
        thread.categoryId = categoryId;
        Debug.Log($"[ThreadService] Thread {thread.threadName} added to category {category.categoryName}");
        
        OnCategoryChanged?.Invoke();
        OnThreadChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// 카테고리에서 스레드를 제거합니다.
    /// </summary>
    /// <param name="categoryId">카테고리 ID</param>
    /// <param name="threadId">제거할 스레드 ID</param>
    /// <returns>성공 시 true</returns>
    public bool RemoveThreadFromCategory(string categoryId, string threadId)
    {
        if (!_categories.TryGetValue(categoryId, out var category))
        {
            Debug.LogWarning($"[ThreadService] Category not found: {categoryId}");
            return false;
        }

        if (!_threads.TryGetValue(threadId, out var thread))
        {
            Debug.LogWarning($"[ThreadService] Thread not found: {threadId}");
            return false;
        }

        category.threadIds.Remove(threadId);
        thread.categoryId = string.Empty;
        Debug.Log($"[ThreadService] Thread {thread.threadName} removed from category {category.categoryName}");
        
        OnCategoryChanged?.Invoke();
        OnThreadChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// 특정 카테고리에 속한 스레드 목록을 반환합니다.
    /// </summary>
    /// <param name="categoryId">카테고리 ID</param>
    /// <returns>스레드 ID 리스트</returns>
    public List<string> GetThreadIdsInCategory(string categoryId)
    {
        if (_categories.TryGetValue(categoryId, out var category))
        {
            return new List<string>(category.threadIds);
        }
        
        Debug.LogWarning($"[ThreadService] Category not found: {categoryId}");
        return new List<string>();
    }

    /// <summary>
    /// 특정 카테고리에 속한 스레드 상태 목록을 반환합니다.
    /// </summary>
    /// <param name="categoryId">카테고리 ID</param>
    /// <returns>ThreadState 리스트</returns>
    public List<ThreadState> GetThreadsInCategory(string categoryId)
    {
        var result = new List<ThreadState>();
        
        if (_categories.TryGetValue(categoryId, out var category))
        {
            foreach (var threadId in category.threadIds)
            {
                if (_threads.TryGetValue(threadId, out var thread))
                {
                    result.Add(thread);
                }
            }
        }
        
        return result;
    }
}
