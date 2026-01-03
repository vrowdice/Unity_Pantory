using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 게임 자원을 관리하는 서비스 클래스
/// ResourceData ScriptableObject를 기반으로 자원을 동적으로 관리합니다.
/// </summary>
public class ResourceDataHandler
{
    // 자원을 저장하는 딕셔너리 (자원 ID -> ResourceEntry)
    private Dictionary<string, ResourceEntry> _resources;
    private readonly DataManager _dataManager;
    public event Action OnResourceChanged;

    /// <summary>
    /// ResourceService 생성자
    /// </summary>
    public ResourceDataHandler(DataManager gameDataManager, List<ResourceData> resourceDataList = null)
    {
        _dataManager = gameDataManager;
        _resources = new Dictionary<string, ResourceEntry>();
        
        if (resourceDataList != null && resourceDataList.Count > 0)
        {
            foreach (var data in resourceDataList)
            {
                if (data == null || string.IsNullOrEmpty(data.id)) continue;
                if (_resources.ContainsKey(data.id))
                {
                    Debug.LogWarning($"[ResourceService] Resource already registered: {data.id}");
                    continue;
                }
                _resources[data.id] = new ResourceEntry(data);
            }
        }
    }

    public int GetResourceQuantity(string resourceId)
    {
        if (_resources.TryGetValue(resourceId, out var entry))
        {
            return entry.state.count;
        }

        return 0;
    }

    public float GetResourcePrice(string resourceId)
    {
        if (_resources.TryGetValue(resourceId, out var entry))
        {
            return entry.state.value;
        }

        return 0f;
    }

    public ResourceEntry GetResourceEntry(string resourceId)
    {
        if (_resources.TryGetValue(resourceId, out var entry))
        {
            return entry;
        }

        return null;
    }

    public Dictionary<string, ResourceEntry> GetAllResources()
    {
        return new Dictionary<string, ResourceEntry>(_resources);
    }

    /// <summary>
    /// 플레이어 창고를 수정합니다 (생산, 구매, 판매 시 사용).
    /// </summary>
    /// <param name="resourceId">자원 ID</param>
    /// <param name="amount">변경할 수량 (양수: 증가, 음수: 감소)</param>
    public void ModifyStorage(string resourceId, int amount)
    {
        if (!_resources.TryGetValue(resourceId, out var entry)) return;

        if (entry.state.count < amount)
        {
            return;
        }

        entry.state.count += amount;
        entry.state.threadDeltaCount += amount;

        if (entry.state.count < 0)
        {
            entry.state.count = 0;
        }

        OnResourceChanged?.Invoke();
    }

    public long CalculateResourceDeltaChange()
    {
        long credit = 0;

        return credit;
    }

    /// <summary>
    /// 누적된 deltaCount를 실제 자원 수량에 반영하고 초기화합니다.
    /// </summary>
    public void DayResourceChange()
    {
        ApplyDeltaChange();
        ApplyValueChange();

        OnResourceChanged?.Invoke();
    }

    private void ApplyDeltaChange()
    {
        foreach (ResourceEntry entry in _resources.Values)
        {
            entry.state.count += entry.state.threadDeltaCount;
            entry.state.count += entry.state.marketDeltaCount;
        }
    }

    private void ApplyValueChange()
    {

    }

    /// <summary>
    /// 모든 이벤트 구독을 초기화합니다.
    /// </summary>
    public void ClearAllSubscriptions()
    {
        OnResourceChanged = null;
    }
}
