using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Thread 설치 위치 데이터를 관리하고 GameDataManager를 통해 저장/로드에 연계하기 위한 핸들러.
/// 실제 배치된 스레드 인스턴스의 독립적인 상태를 관리합니다.
/// </summary>
public class ThreadPlacementDataHandler
{
    private readonly dataManager _gameDataManager;
    private readonly Dictionary<Vector2Int, ThreadPlacementState> _placedThreads = new Dictionary<Vector2Int, ThreadPlacementState>();

    public event Action OnPlacementChanged;

    public ThreadPlacementDataHandler(dataManager gameDataManager)
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

    /// <summary>
    /// 템플릿 스레드를 복사하여 새로운 인스턴스를 배치합니다.
    /// 각 배치마다 독립적인 ThreadState를 가지도록 합니다.
    /// </summary>
    public ThreadState PlaceThread(Vector2Int gridPosition, string templateId)
    {
        if (string.IsNullOrEmpty(templateId) || _gameDataManager?.Thread == null)
        {
            Debug.LogWarning($"[ThreadPlacementDataHandler] Invalid template ID or Thread handler is null: {templateId}");
            return null;
        }

        // 1. 원본 템플릿 가져오기
        var template = _gameDataManager.Thread.GetThread(templateId);
        if (template == null)
        {
            Debug.LogWarning($"[ThreadPlacementDataHandler] Template thread not found: {templateId}");
            return null;
        }

        // 2. 상태 복제 (Deep Copy)
        string json = JsonUtility.ToJson(template);
        ThreadState newState = JsonUtility.FromJson<ThreadState>(json);
        
        // 3. 고유 인스턴스 ID 부여
        string uniqueInstanceId = $"{templateId}_{gridPosition.x}_{gridPosition.y}_{System.Guid.NewGuid().ToString().Substring(0, 8)}";
        newState.threadId = uniqueInstanceId;
        newState.threadName = $"{template.threadName} ({gridPosition.x}, {gridPosition.y})";
        
        // 4. 런타임 상태 초기화 (각 인스턴스는 독립적으로 관리)
        newState.currentWorkers = 0;
        newState.currentTechnicians = 0;
        newState.currentProductionProgress = 0f;
        newState.currentProductionEfficiency = 0f;

        // 5. 새로 생성된 인스턴스 초기화 (유지비, 직원 요구사항, 생산 체인 계산)
        if (_gameDataManager.ThreadCalculate != null)
        {
            _gameDataManager.ThreadCalculate.InitializeThread(newState, _gameDataManager.Thread);
        }

        // 6. 배치 상태 생성 및 저장
        var placement = new ThreadPlacementState(gridPosition, templateId, newState);
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
    /// [레거시 호환] ThreadId로 배치를 설정합니다. (기존 코드 호환용)
    /// </summary>
    public void SetPlacedThread(Vector2Int gridPosition, string threadId)
    {
        // 레거시 호환: ThreadId가 이미 인스턴스 ID인 경우를 처리
        if (string.IsNullOrEmpty(threadId))
        {
            return;
        }

        // 기존 배치가 있으면 그대로 사용, 없으면 새로 생성
        if (!_placedThreads.ContainsKey(gridPosition))
        {
            // ThreadId가 템플릿 ID인지 인스턴스 ID인지 확인
            // 인스턴스 ID는 보통 "{templateId}_{x}_{y}_{guid}" 형식
            var threadState = _gameDataManager?.Thread?.GetThread(threadId);
            if (threadState != null)
            {
                // 이미 ThreadDataHandler에 있는 경우 (레거시 데이터)
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
}

/// <summary>
/// 배치된 스레드의 상태를 나타내는 클래스.
/// 템플릿 ID와 독립적인 런타임 상태를 보유합니다.
/// </summary>
[Serializable]
public class ThreadPlacementState
{
    public Vector2Int GridPosition;      // 위치 정보
    public string TemplateId;            // 원본 템플릿 ID (예: "iron_mine")
    public ThreadState RuntimeState;     // [핵심] 독립적인 런타임 상태 (직원수, 진행도 등)

    public ThreadPlacementState(Vector2Int pos, string templateId, ThreadState initialState)
    {
        GridPosition = pos;
        TemplateId = templateId;
        RuntimeState = initialState; // 복사본을 받아야 함
    }

    // [레거시 호환] 기존 코드를 위한 속성
    public string ThreadId => RuntimeState?.threadId ?? string.Empty;
}

