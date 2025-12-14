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
    private readonly GameDataManager _gameDataManager;

    // 자원 변경 이벤트 (자원이 추가/제거/설정될 때 발생)
    public event Action OnResourceChanged;

    /// <summary>
    /// ResourceService 생성자
    /// </summary>
    public ResourceDataHandler(GameDataManager gameDataManager, List<ResourceData> resourceDataList = null)
    {
        _gameDataManager = gameDataManager;
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

    /// <summary>
    /// 특정 자원의 시장 재고량을 반환합니다. (deltaCount를 고려한 실제 사용 가능한 수량)
    /// </summary>
    /// <param name="resourceId">자원 ID</param>
    /// <returns>해당 자원의 시장 재고량 (count + deltaCount)</returns>
    public long GetMarketResourceQuantity(string resourceId)
    {
        if (_resources.TryGetValue(resourceId, out var entry))
        {
            // deltaCount를 고려한 실제 사용 가능한 시장 재고량 반환
            return entry.resourceState.count + entry.resourceState.deltaCount;
        }
        
        Debug.LogWarning($"[ResourceService] Unregistered resource: {resourceId}");
        return 0;
    }

    /// <summary>
    /// 특정 자원의 플레이어 보유량을 반환합니다.
    /// </summary>
    /// <param name="resourceId">자원 ID</param>
    /// <returns>해당 자원의 플레이어 보유량</returns>
    public long GetPlayerResourceQuantity(string resourceId)
    {
        if (_resources.TryGetValue(resourceId, out var entry))
        {
            return entry.resourceState.playerInventory;
        }
        
        Debug.LogWarning($"[ResourceService] Unregistered resource: {resourceId}");
        return 0;
    }

    /// <summary>
    /// 특정 자원의 현재 가격을 반환합니다.
    /// </summary>
    /// <param name="resourceId">자원 ID</param>
    /// <returns>해당 자원의 현재 가격</returns>
    public float GetResourcePrice(string resourceId)
    {
        if (_resources.TryGetValue(resourceId, out var entry))
        {
            return entry.resourceState.currentValue;
        }
        
        Debug.LogWarning($"[ResourceService] Unregistered resource: {resourceId}");
        return 0f;
    }

    /// <summary>
    /// 특정 자원의 ResourceEntry를 반환합니다.
    /// </summary>
    /// <param name="resourceId">자원 ID</param>
    /// <returns>ResourceEntry 또는 null</returns>
    public ResourceEntry GetResourceEntry(string resourceId)
    {
        if (_resources.TryGetValue(resourceId, out var entry))
        {
            return entry;
        }
        
        Debug.LogWarning($"[ResourceService] Unregistered resource: {resourceId}");
        return null;
    }

    /// <summary>
    /// 모든 자원 정보를 딕셔너리로 반환합니다 (읽기 전용).
    /// </summary>
    /// <returns>자원 딕셔너리의 복사본</returns>
    public Dictionary<string, ResourceEntry> GetAllResources()
    {
        return new Dictionary<string, ResourceEntry>(_resources);
    }

    /// <summary>
    /// 등록된 모든 자원 ID 목록을 반환합니다.
    /// </summary>
    /// <returns>자원 ID 리스트</returns>
    public List<string> GetAllResourceIds()
    {
        return new List<string>(_resources.Keys);
    }

    /// <summary>
    /// 시장 재고를 수정합니다 (시장 액터 생산/소비용).
    /// </summary>
    /// <param name="resourceId">자원 ID</param>
    /// <param name="amount">변경할 수량 (양수: 증가, 음수: 감소)</param>
    public void ModifyMarketInventory(string resourceId, long amount)
    {
        if (!_resources.TryGetValue(resourceId, out var entry)) return;
        entry.resourceState.deltaCount += amount;
        OnResourceChanged?.Invoke();
    }

    /// <summary>
    /// 플레이어 창고를 수정합니다 (생산, 구매, 판매 시 사용).
    /// 시장 재고(count + deltaCount)와 달리, 플레이어 재고는 게임 진행 중 즉시 사용 가능해야 하므로
    /// 델타 시스템을 사용하지 않고 즉시 반영합니다.
    /// </summary>
    /// <param name="resourceId">자원 ID</param>
    /// <param name="amount">변경할 수량 (양수: 증가, 음수: 감소)</param>
    public void ModifyPlayerInventory(string resourceId, long amount)
    {
        if (!_resources.TryGetValue(resourceId, out var entry)) return;

        if (amount < 0)
        {
            if (entry.resourceState.playerInventory < -amount)
            {
                Debug.LogWarning($"[Resource] Insufficient player inventory for {resourceId}.");
                return;
            }
        }

        entry.resourceState.playerInventory += amount;
        entry.resourceState.playerInventoryDelta += amount;

        if (entry.resourceState.playerInventory < 0) entry.resourceState.playerInventory = 0;

        OnResourceChanged?.Invoke();
    }

    /// <summary>
    /// 특정 자원의 수량을 추가합니다 (시장 재고용 - 하위 호환성 유지).
    /// </summary>
    /// <param name="resourceId">추가할 자원 ID</param>
    /// <param name="amount">추가할 수량</param>
    public void AddResource(string resourceId, long amount)
    {
        if (amount <= 0)
        {
            Debug.LogWarning($"[ResourceService] Amount to add must be greater than 0. (input: {amount})");
            return;
        }

        if (!_resources.TryGetValue(resourceId, out var entry))
        {
            Debug.LogWarning($"[ResourceService] Unregistered resource: {resourceId}");
            return;
        }

        // deltaCount만 누적 (count는 ApplyResourceDeltas에서 반영)
        entry.resourceState.deltaCount += amount;
        Debug.Log($"[ResourceService] {entry.resourceData.displayName} +{amount} (delta: {entry.resourceState.deltaCount}, current: {entry.resourceState.count})");
        
        OnResourceChanged?.Invoke();
    }

    /// <summary>
    /// 특정 자원의 수량을 제거합니다. 자원이 부족하면 실패합니다.
    /// </summary>
    /// <param name="resourceId">제거할 자원 ID</param>
    /// <param name="amount">제거할 수량</param>
    /// <returns>성공 시 true, 자원 부족 시 false</returns>
    public bool TryRemoveResource(string resourceId, long amount)
    {
        if (amount <= 0)
        {
            Debug.LogWarning($"[ResourceService] Amount to remove must be greater than 0. (input: {amount})");
            return true;
        }

        if (!_resources.TryGetValue(resourceId, out var entry))
        {
            Debug.LogWarning($"[ResourceService] Unregistered resource: {resourceId}");
            return false;
        }

        // 현재 count + deltaCount를 고려한 실제 사용 가능한 자원량 계산
        long availableCount = entry.resourceState.count + entry.resourceState.deltaCount;
        
        if (availableCount >= amount)
        {
            // deltaCount만 누적 (count는 ApplyResourceDeltas에서 반영)
            entry.resourceState.deltaCount -= amount;
            Debug.Log($"[ResourceService] {entry.resourceData.displayName} -{amount} (delta: {entry.resourceState.deltaCount}, current: {entry.resourceState.count})");
            
            OnResourceChanged?.Invoke();
            return true;
        }
        else
        {
            Debug.LogWarning($"[ResourceService] {entry.resourceData.displayName} not enough! (required: {amount}, available: {availableCount})");
            return false;
        }
    }

    /// <summary>
    /// 특정 자원의 수량을 직접 설정합니다.
    /// </summary>
    /// <param name="resourceId">설정할 자원 ID</param>
    /// <param name="amount">설정할 수량</param>
    public void SetResource(string resourceId, long amount)
    {
        if (amount < 0)
        {
            Debug.LogWarning($"[ResourceService] Resource quantity cannot be negative. (input: {amount})");
            return;
        }

        if (!_resources.TryGetValue(resourceId, out var entry))
        {
            Debug.LogWarning($"[ResourceService] Unregistered resource: {resourceId}");
            return;
        }

        // 목표 수량과 현재 수량의 차이를 deltaCount로 설정
        entry.resourceState.deltaCount = amount - entry.resourceState.count;
        // count는 ApplyResourceDeltas에서 반영되므로 여기서는 변경하지 않음
        Debug.Log($"[ResourceService] {entry.resourceData.displayName} = {amount} (delta: {entry.resourceState.deltaCount}, current: {entry.resourceState.count})");
        
        OnResourceChanged?.Invoke();
    }

    /// <summary>
    /// 여러 자원을 한 번에 추가합니다.
    /// </summary>
    /// <param name="resources">추가할 자원 딕셔너리 (ID -> 수량)</param>
    public void AddResources(Dictionary<string, long> resources)
    {
        bool changed = false;
        foreach (var kvp in resources)
        {
            if (_resources.TryGetValue(kvp.Key, out var entry))
            {
                entry.resourceState.deltaCount += kvp.Value;
                changed = true;
            }
        }

        if (changed) OnResourceChanged?.Invoke();
    }

    /// <summary>
    /// 여러 자원을 한 번에 제거합니다. 하나라도 부족하면 모두 실패합니다.
    /// </summary>
    /// <param name="resources">제거할 자원 딕셔너리 (ID -> 수량)</param>
    /// <returns>성공 시 true, 자원 부족 시 false</returns>
    public bool TryRemoveResources(Dictionary<string, long> resources)
    {
        // 먼저 모든 자원이 충분한지 확인 (deltaCount 고려)
        foreach (var kvp in resources)
        {
            if (!HasEnoughResource(kvp.Key, kvp.Value))
            {
                var entry = GetResourceEntry(kvp.Key);
                string displayName = entry != null ? entry.resourceData.displayName : kvp.Key;
                long available = GetMarketResourceQuantity(kvp.Key);
                Debug.LogWarning($"[ResourceService] Transaction failed due to insufficient resources: {displayName} (required: {kvp.Value}, available: {available})");
                return false;
            }
        }

        // 모두 충분하면 제거
        foreach (var kvp in resources)
        {
            TryRemoveResource(kvp.Key, kvp.Value);
        }

        return true;
    }

    /// <summary>
    /// 특정 자원의 시장 재고가 충분한지 확인합니다.
    /// </summary>
    /// <param name="resourceId">확인할 자원 ID</param>
    /// <param name="amount">필요한 수량</param>
    /// <returns>충분하면 true, 부족하면 false</returns>
    public bool HasEnoughResource(string resourceId, long amount)
    {
        if (!_resources.TryGetValue(resourceId, out var entry))
        {
            return false;
        }
        
        // deltaCount를 고려한 실제 사용 가능한 시장 재고량 반환
        long availableCount = entry.resourceState.count + entry.resourceState.deltaCount;
        return availableCount >= amount;
    }

    /// <summary>
    /// 플레이어 재고가 충분한지 확인합니다.
    /// </summary>
    /// <param name="resourceId">확인할 자원 ID</param>
    /// <param name="amount">필요한 수량</param>
    /// <returns>충분하면 true, 부족하면 false</returns>
    public bool HasEnoughPlayerResource(string resourceId, long amount)
    {
        if (!_resources.TryGetValue(resourceId, out var entry))
        {
            return false;
        }
        
        return entry.resourceState.playerInventory >= amount;
    }

    /// <summary>
    /// 시장 재고가 충분한지 확인합니다 (플레이어가 구매 가능한지 체크용).
    /// </summary>
    /// <param name="resourceId">확인할 자원 ID</param>
    /// <param name="amount">필요한 수량</param>
    /// <returns>충분하면 true, 부족하면 false</returns>
    public bool HasEnoughMarketInventory(string resourceId, long amount)
    {
        if (!_resources.TryGetValue(resourceId, out var entry))
        {
            return false;
        }
        
        // deltaCount를 고려한 실제 사용 가능한 시장 재고량 반환
        long availableCount = entry.resourceState.count + entry.resourceState.deltaCount;
        return availableCount >= amount;
    }

    // ----------------- Public Methods (가격 관리) -----------------

    /// <summary>
    /// 특정 자원의 가격을 설정합니다.
    /// </summary>
    /// <param name="resourceId">자원 ID</param>
    /// <param name="price">설정할 가격</param>
    public void SetResourcePrice(string resourceId, float price)
    {
        if (price < 0)
        {
            Debug.LogWarning($"[ResourceService] Price cannot be negative. (input: {price})");
            return;
        }

        if (!_resources.TryGetValue(resourceId, out var entry))
        {
            Debug.LogWarning($"[ResourceService] Unregistered resource: {resourceId}");
            return;
        }

        entry.resourceState.currentValue = price;
        entry.resourceState.RecordPrice(entry.resourceState.currentValue);
        Debug.Log($"[ResourceService] {entry.resourceData.displayName} price = {price}");
        
        OnResourceChanged?.Invoke();
    }

    /// <summary>
    /// 특정 자원의 가격 변동률을 설정합니다.
    /// </summary>
    /// <param name="resourceId">자원 ID</param>
    /// <param name="rate">가격 변동률</param>
    public void SetPriceChangeRate(string resourceId, float rate)
    {
        if (!_resources.TryGetValue(resourceId, out var entry))
        {
            Debug.LogWarning($"[ResourceService] Unregistered resource: {resourceId}");
            return;
        }

        entry.resourceState.priceChangeRate = rate;
    }

    /// <summary>
    /// 모든 자원의 가격을 업데이트합니다 (가격 변동률 적용).
    /// </summary>
    public void UpdateAllPrices()
    {
        bool changed = false;
        foreach (var entry in _resources.Values)
        {
            if (entry?.resourceState == null) continue;

            float multiplier = 1f + entry.resourceState.priceChangeRate;

            // 가격 변화가 거의 없으면 스킵 (부동소수점 오차 고려)
            if (Mathf.Abs(multiplier - 1f) < 0.0001f) continue;

            float newPrice = Mathf.Max(0.01f, entry.resourceState.currentValue * multiplier);

            if (Mathf.Abs(newPrice - entry.resourceState.currentValue) > 0.001f)
            {
                entry.resourceState.currentValue = newPrice;
                entry.resourceState.RecordPrice(newPrice);
                changed = true;
            }
        }

        if (changed) OnResourceChanged?.Invoke();
    }

    // ----------------- Utility Methods -----------------

    /// <summary>
    /// 모든 자원의 수량을 0으로 초기화합니다.
    /// </summary>
    public void ResetAllResources()
    {
        foreach (var entry in _resources.Values)
        {
            long previousCount = entry.resourceState.count;
            entry.resourceState.InitializeFromData(entry.resourceData);
            entry.resourceState.deltaCount = -previousCount;
        }
        Debug.Log("[ResourceService] All resources have been reset.");
        
        OnResourceChanged?.Invoke();
    }

    /// <summary>
    /// 누적된 deltaCount를 실제 자원 수량에 반영하고 초기화합니다.
    /// </summary>
    public void ApplyResourceDeltas()
    {
        bool hasChanges = false;

        foreach (var entry in _resources.Values)
        {
            if (entry?.resourceState == null)
            {
                continue;
            }

            // 플레이어 재고 델타는 항상 초기화 (일일 업데이트 시점)
            entry.resourceState.playerInventoryDelta = 0;

            long delta = entry.resourceState.deltaCount;
            if (delta == 0)
            {
                continue;
            }

            long newCount = entry.resourceState.count + delta;
            if (newCount < 0)
            {
                long deficit = -newCount;
                ApplyMarketDemand(entry, deficit);
                newCount = 0;
            }

            entry.resourceState.count = newCount;
            entry.resourceState.deltaCount = 0;
            hasChanges = true;
        }

        if (hasChanges)
        {
            OnResourceChanged?.Invoke();
        }
    }

    /// <summary>
    /// 특정 자원이 등록되어 있는지 확인합니다.
    /// </summary>
    /// <param name="resourceId">확인할 자원 ID</param>
    /// <returns>등록되어 있으면 true</returns>
    public bool IsResourceRegistered(string resourceId)
    {
        return _resources.ContainsKey(resourceId);
    }

    /// <summary>
    /// 자원 부족 시 시장 수요에 반영합니다 (자원 추가 없이 수요만 증가).
    /// </summary>
    private void ApplyMarketDemand(ResourceEntry entry, long amount)
    {
        if (entry?.resourceState == null)
        {
            return;
        }

        // 시장 수요 증가
        entry.resourceState.lastDemand += amount;

        // 가격에 소폭 영향 (플레이어 구매와 동일한 로직)
        float demandImpact = amount * 0.1f;
        float currentPrice = entry.resourceState.currentValue;
        float priceAdjustment = demandImpact * entry.resourceData.marketSensitivity * 0.01f;
        entry.resourceState.currentValue = Mathf.Max(0.01f, currentPrice * (1f + priceAdjustment));
        entry.resourceState.RecordPrice(entry.resourceState.currentValue);
    }
}
