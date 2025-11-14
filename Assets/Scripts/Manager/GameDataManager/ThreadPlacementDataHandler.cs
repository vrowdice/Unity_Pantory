using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Thread 설치 위치 데이터를 관리하고 GameDataManager를 통해 저장/로드에 연계하기 위한 핸들러.
/// </summary>
public class ThreadPlacementDataHandler
{
    private readonly GameDataManager _gameDataManager;
    private readonly Dictionary<Vector2Int, ThreadPlacementState> _placedThreads = new Dictionary<Vector2Int, ThreadPlacementState>();

    public event Action OnPlacementChanged;

    public ThreadPlacementDataHandler(GameDataManager gameDataManager)
    {
        _gameDataManager = gameDataManager;
    }

    public bool HasPlacedThread(Vector2Int gridPosition)
    {
        return _placedThreads.ContainsKey(gridPosition);
    }

    public bool TryGetPlacedThread(Vector2Int gridPosition, out ThreadPlacementState placementState)
    {
        return _placedThreads.TryGetValue(gridPosition, out placementState);
    }

    public IReadOnlyDictionary<Vector2Int, ThreadPlacementState> GetAllPlacedThreads()
    {
        return _placedThreads;
    }

    public void SetPlacedThread(Vector2Int gridPosition, string threadId)
    {
        if (string.IsNullOrEmpty(threadId))
        {
            return;
        }

        _placedThreads[gridPosition] = new ThreadPlacementState(threadId);
        RaisePlacementChanged();
    }

    public bool RemovePlacedThread(Vector2Int gridPosition)
    {
        bool removed = _placedThreads.Remove(gridPosition);
        if (removed)
        {
            RaisePlacementChanged();
        }

        return removed;
    }

    public void ClearAll()
    {
        _placedThreads.Clear();
        RaisePlacementChanged();
    }

    private void RaisePlacementChanged()
    {
        OnPlacementChanged?.Invoke();
    }
}

public class ThreadPlacementState
{
    public string ThreadId { get; private set; }

    public ThreadPlacementState(string threadId)
    {
        ThreadId = threadId;
    }

    public void UpdateThreadId(string threadId)
    {
        ThreadId = threadId;
    }
}

