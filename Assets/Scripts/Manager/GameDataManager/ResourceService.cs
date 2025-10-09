using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 게임 자원을 관리하는 서비스 클래스
/// ResourceData ScriptableObject를 기반으로 자원을 동적으로 관리합니다.
/// </summary>
public class ResourceService
{
    // 자원을 저장하는 딕셔너리 (자원 ID -> ResourceEntry)
    private Dictionary<string, ResourceEntry> _resources;

    // 자원 변경 이벤트 (자원이 추가/제거/설정될 때 발생)
    public event Action OnResourceChanged;

    /// <summary>
    /// ResourceService 생성자
    /// </summary>
    public ResourceService()
    {
        _resources = new Dictionary<string, ResourceEntry>();
    }

    // ----------------- 초기화 -----------------

    /// <summary>
    /// ResourceData를 등록하여 관리 대상에 추가합니다.
    /// </summary>
    /// <param name="resourceData">등록할 ResourceData</param>
    public void RegisterResource(ResourceData resourceData)
    {
        if (resourceData == null)
        {
            Debug.LogWarning("[ResourceService] ResourceData가 null입니다.");
            return;
        }

        if (string.IsNullOrEmpty(resourceData.id))
        {
            Debug.LogWarning("[ResourceService] ResourceData의 ID가 비어있습니다.");
            return;
        }

        if (_resources.ContainsKey(resourceData.id))
        {
            Debug.LogWarning($"[ResourceService] 이미 등록된 자원입니다: {resourceData.id}");
            return;
        }

        _resources[resourceData.id] = new ResourceEntry(resourceData);
        Debug.Log($"[ResourceService] 자원 등록: {resourceData.displayName} ({resourceData.id})");
    }

    /// <summary>
    /// 여러 ResourceData를 한 번에 등록합니다.
    /// </summary>
    /// <param name="resourceDataList">등록할 ResourceData 배열</param>
    public void RegisterResources(ResourceData[] resourceDataList)
    {
        foreach (var data in resourceDataList)
        {
            RegisterResource(data);
        }
    }

    // ----------------- Public Getters (읽기 전용) -----------------

    /// <summary>
    /// 특정 자원의 보유량을 반환합니다.
    /// </summary>
    /// <param name="resourceId">자원 ID</param>
    /// <returns>해당 자원의 보유량</returns>
    public long GetResourceQuantity(string resourceId)
    {
        if (_resources.TryGetValue(resourceId, out var entry))
        {
            return entry.resourceState.quantity;
        }
        
        Debug.LogWarning($"[ResourceService] 등록되지 않은 자원입니다: {resourceId}");
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
        
        Debug.LogWarning($"[ResourceService] 등록되지 않은 자원입니다: {resourceId}");
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
        
        Debug.LogWarning($"[ResourceService] 등록되지 않은 자원입니다: {resourceId}");
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

    // ----------------- Public Methods (자원 수량 관리) -----------------

    /// <summary>
    /// 특정 자원의 수량을 추가합니다.
    /// </summary>
    /// <param name="resourceId">추가할 자원 ID</param>
    /// <param name="amount">추가할 수량</param>
    public void AddResource(string resourceId, long amount)
    {
        if (amount <= 0)
        {
            Debug.LogWarning($"[ResourceService] 추가할 수량은 0보다 커야 합니다. (입력값: {amount})");
            return;
        }

        if (!_resources.TryGetValue(resourceId, out var entry))
        {
            Debug.LogWarning($"[ResourceService] 등록되지 않은 자원입니다: {resourceId}");
            return;
        }

        entry.resourceState.quantity += amount;
        Debug.Log($"[ResourceService] {entry.resourceData.displayName} +{amount} (총: {entry.resourceState.quantity})");
        
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
            Debug.LogWarning($"[ResourceService] 제거할 수량은 0보다 커야 합니다. (입력값: {amount})");
            return true;
        }

        if (!_resources.TryGetValue(resourceId, out var entry))
        {
            Debug.LogWarning($"[ResourceService] 등록되지 않은 자원입니다: {resourceId}");
            return false;
        }

        if (entry.resourceState.quantity >= amount)
        {
            entry.resourceState.quantity -= amount;
            Debug.Log($"[ResourceService] {entry.resourceData.displayName} -{amount} (총: {entry.resourceState.quantity})");
            
            OnResourceChanged?.Invoke();
            return true;
        }
        else
        {
            Debug.LogWarning($"[ResourceService] {entry.resourceData.displayName} 부족! (필요: {amount}, 보유: {entry.resourceState.quantity})");
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
            Debug.LogWarning($"[ResourceService] 자원 수량은 음수가 될 수 없습니다. (입력값: {amount})");
            return;
        }

        if (!_resources.TryGetValue(resourceId, out var entry))
        {
            Debug.LogWarning($"[ResourceService] 등록되지 않은 자원입니다: {resourceId}");
            return;
        }

        entry.resourceState.quantity = amount;
        Debug.Log($"[ResourceService] {entry.resourceData.displayName} = {amount}");
        
        OnResourceChanged?.Invoke();
    }

    /// <summary>
    /// 여러 자원을 한 번에 추가합니다.
    /// </summary>
    /// <param name="resources">추가할 자원 딕셔너리 (ID -> 수량)</param>
    public void AddResources(Dictionary<string, long> resources)
    {
        foreach (var kvp in resources)
        {
            AddResource(kvp.Key, kvp.Value);
        }
    }

    /// <summary>
    /// 여러 자원을 한 번에 제거합니다. 하나라도 부족하면 모두 실패합니다.
    /// </summary>
    /// <param name="resources">제거할 자원 딕셔너리 (ID -> 수량)</param>
    /// <returns>성공 시 true, 자원 부족 시 false</returns>
    public bool TryRemoveResources(Dictionary<string, long> resources)
    {
        // 먼저 모든 자원이 충분한지 확인
        foreach (var kvp in resources)
        {
            if (GetResourceQuantity(kvp.Key) < kvp.Value)
            {
                var entry = GetResourceEntry(kvp.Key);
                string displayName = entry != null ? entry.resourceData.displayName : kvp.Key;
                Debug.LogWarning($"[ResourceService] 자원 부족으로 거래 실패: {displayName} (필요: {kvp.Value}, 보유: {GetResourceQuantity(kvp.Key)})");
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
    /// 특정 자원이 충분한지 확인합니다.
    /// </summary>
    /// <param name="resourceId">확인할 자원 ID</param>
    /// <param name="amount">필요한 수량</param>
    /// <returns>충분하면 true, 부족하면 false</returns>
    public bool HasEnoughResource(string resourceId, long amount)
    {
        return GetResourceQuantity(resourceId) >= amount;
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
            Debug.LogWarning($"[ResourceService] 가격은 음수가 될 수 없습니다. (입력값: {price})");
            return;
        }

        if (!_resources.TryGetValue(resourceId, out var entry))
        {
            Debug.LogWarning($"[ResourceService] 등록되지 않은 자원입니다: {resourceId}");
            return;
        }

        entry.resourceState.currentValue = price;
        Debug.Log($"[ResourceService] {entry.resourceData.displayName} 가격 = {price}");
        
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
            Debug.LogWarning($"[ResourceService] 등록되지 않은 자원입니다: {resourceId}");
            return;
        }

        entry.resourceState.priceChangeRate = rate;
    }

    /// <summary>
    /// 모든 자원의 가격을 업데이트합니다 (가격 변동률 적용).
    /// </summary>
    public void UpdateAllPrices()
    {
        foreach (var entry in _resources.Values)
        {
            float newPrice = entry.resourceState.currentValue * (1 + entry.resourceState.priceChangeRate);
            
            // minValue와 maxValue 범위 내로 제한
            newPrice = Mathf.Clamp(newPrice, entry.resourceData.minValue, entry.resourceData.maxValue);
            
            entry.resourceState.currentValue = newPrice;
        }
        
        OnResourceChanged?.Invoke();
    }

    // ----------------- Utility Methods -----------------

    /// <summary>
    /// 모든 자원의 수량을 0으로 초기화합니다.
    /// </summary>
    public void ResetAllResources()
    {
        foreach (var entry in _resources.Values)
        {
            entry.resourceState.quantity = 0;
            entry.resourceState.currentValue = entry.resourceData.baseValue;
            entry.resourceState.priceChangeRate = 0f;
        }
        Debug.Log("[ResourceService] 모든 자원이 초기화되었습니다.");
        
        OnResourceChanged?.Invoke();
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
}
