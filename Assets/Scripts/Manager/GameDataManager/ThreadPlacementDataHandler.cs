using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Thread 설치 위치 데이터를 관리하고 GameDataManager를 통해 저장/로드에 연계하기 위한 핸들러.
/// 실제 배치된 스레드 인스턴스의 독립적인 상태를 관리합니다.
/// </summary>
public class ThreadPlacementDataHandler
{
    private readonly DataManager _dataManager;
    private readonly Dictionary<Vector2Int, ThreadPlacementState> _placedThreads = new Dictionary<Vector2Int, ThreadPlacementState>();

    public event Action OnPlacementChanged;

    public ThreadPlacementDataHandler(DataManager gameDataManager)
    {
        _dataManager = gameDataManager;
    }

    public Dictionary<Vector2Int, ThreadPlacementState> GetAllPlacedThreads()
    {
        return new Dictionary<Vector2Int, ThreadPlacementState>(_placedThreads);
    }

    /// <summary>
    /// 템플릿 스레드를 복사하여 새로운 인스턴스를 배치합니다.
    /// 각 배치마다 독립적인 ThreadState를 가지도록 합니다.
    /// </summary>
    public ThreadState PlaceThread(Vector2Int gridPosition, string templateId)
    {
        ThreadState template = _dataManager.Thread.GetThread(templateId);

        string json = JsonUtility.ToJson(template);
        ThreadState newState = JsonUtility.FromJson<ThreadState>(json);

        string uniqueInstanceId = $"{templateId}_{gridPosition.x}_{gridPosition.y}_{System.Guid.NewGuid().ToString().Substring(0, 8)}";
        newState.threadId = uniqueInstanceId;
        newState.threadName = $"{template.threadName} ({gridPosition.x}, {gridPosition.y})";
        newState.currentWorkers = 0;
        newState.currentTechnicians = 0;
        newState.currentProductionProgress = 0f;
        newState.currentProductionEfficiency = 0f;

        _dataManager.ThreadCalculate.InitializeThread(newState, _dataManager.Thread);

        ThreadPlacementState placement = new ThreadPlacementState(gridPosition, templateId, newState);
        _placedThreads[gridPosition] = placement;
        RaisePlacementChanged();
        
        return newState;
    }

    /// <summary>
    /// 특정 위치의 스레드 런타임 상태를 가져옵니다.
    /// </summary>
    public ThreadState GetThreadStateAt(Vector2Int gridPosition)
    {
        if (_placedThreads.TryGetValue(gridPosition, out var placement))
        {
            return placement.RuntimeState;
        }
        return null;
    }

    /// <summary>
    /// ThreadId로 배치를 설정합니다
    /// </summary>
    public void SetPlacedThread(Vector2Int gridPosition, string threadId)
    {
        if (!_placedThreads.ContainsKey(gridPosition))
        {
            var threadState = _dataManager?.Thread?.GetThread(threadId);
            if (threadState != null)
            {
                var placement = new ThreadPlacementState(gridPosition, threadId, threadState);
                _placedThreads[gridPosition] = placement;
                RaisePlacementChanged();
            }
        }
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

    /// <summary>
    /// 모든 이벤트 구독을 초기화합니다.
    /// </summary>
    public void ClearAllSubscriptions()
    {
        OnPlacementChanged = null;
    }
}