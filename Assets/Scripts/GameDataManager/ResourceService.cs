using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 게임 자원을 관리하는 서비스 클래스
/// Dictionary를 사용하여 자원을 동적으로 관리합니다.
/// </summary>
public class ResourceService
{
    // 자원을 저장하는 딕셔너리 (자원 타입 -> 자원 량)
    private Dictionary<ResourceType, long> _resources;

    /// <summary>
    /// ResourceService 생성자
    /// 모든 자원을 0으로 초기화합니다.
    /// </summary>
    public ResourceService()
    {
        _resources = new Dictionary<ResourceType, long>();
        
        // 모든 ResourceType을 0으로 초기화
        foreach (ResourceType type in System.Enum.GetValues(typeof(ResourceType)))
        {
            _resources[type] = 0;
        }
    }

    // ----------------- Public Getters (읽기 전용) -----------------

    /// <summary>
    /// 특정 자원의 현재 보유량을 반환합니다.
    /// </summary>
    /// <param name="type">조회할 자원 타입</param>
    /// <returns>해당 자원의 보유량</returns>
    public long GetResource(ResourceType type)
    {
        if (_resources.ContainsKey(type))
        {
            return _resources[type];
        }
        
        Debug.LogWarning($"ResourceType {type}이(가) 초기화되지 않았습니다.");
        return 0;
    }

    /// <summary>
    /// 모든 자원 정보를 딕셔너리로 반환합니다 (읽기 전용).
    /// </summary>
    /// <returns>자원 딕셔너리의 복사본</returns>
    public Dictionary<ResourceType, long> GetAllResources()
    {
        return new Dictionary<ResourceType, long>(_resources);
    }

    // ----------------- Public Methods (자원 관리) -----------------

    /// <summary>
    /// 특정 자원을 추가합니다.
    /// </summary>
    /// <param name="type">추가할 자원 타입</param>
    /// <param name="amount">추가할 양 (long)</param>
    public void AddResource(ResourceType type, long amount)
    {
        if (amount <= 0)
        {
            Debug.LogWarning($"추가할 양은 0보다 커야 합니다. (입력값: {amount})");
            return;
        }

        if (!_resources.ContainsKey(type))
        {
            _resources[type] = 0;
        }

        _resources[type] += amount;
        Debug.Log($"[ResourceService] {type} +{amount} (총: {_resources[type]})");
        
        // [추가 로직]: UI 업데이트 이벤트 발생, 사운드 재생 등
    }

    /// <summary>
    /// 특정 자원을 제거합니다. 자원이 부족하면 실패합니다.
    /// </summary>
    /// <param name="type">제거할 자원 타입</param>
    /// <param name="amount">제거할 양 (long)</param>
    /// <returns>성공 시 true, 자원 부족 시 false</returns>
    public bool TryRemoveResource(ResourceType type, long amount)
    {
        if (amount <= 0)
        {
            Debug.LogWarning($"제거할 양은 0보다 커야 합니다. (입력값: {amount})");
            return true;
        }

        if (!_resources.ContainsKey(type))
        {
            Debug.LogWarning($"ResourceType {type}이(가) 초기화되지 않았습니다.");
            return false;
        }

        if (_resources[type] >= amount)
        {
            _resources[type] -= amount;
            Debug.Log($"[ResourceService] {type} -{amount} (총: {_resources[type]})");
            
            // [추가 로직]: 사운드 효과, 이벤트 트리거 등
            return true;
        }
        else
        {
            Debug.LogWarning($"[ResourceService] {type} 부족! (필요: {amount}, 보유: {_resources[type]})");
            return false;
        }
    }

    /// <summary>
    /// 특정 자원을 직접 설정합니다.
    /// </summary>
    /// <param name="type">설정할 자원 타입</param>
    /// <param name="amount">설정할 양 (long)</param>
    public void SetResource(ResourceType type, long amount)
    {
        if (amount < 0)
        {
            Debug.LogWarning($"자원 양은 음수가 될 수 없습니다. (입력값: {amount})");
            return;
        }

        _resources[type] = amount;
        Debug.Log($"[ResourceService] {type} = {amount}");
    }

    /// <summary>
    /// 여러 자원을 한 번에 추가합니다.
    /// </summary>
    /// <param name="resources">추가할 자원 딕셔너리</param>
    public void AddResources(Dictionary<ResourceType, long> resources)
    {
        foreach (var kvp in resources)
        {
            AddResource(kvp.Key, kvp.Value);
        }
    }

    /// <summary>
    /// 여러 자원을 한 번에 제거합니다. 하나라도 부족하면 모두 실패합니다.
    /// </summary>
    /// <param name="resources">제거할 자원 딕셔너리</param>
    /// <returns>성공 시 true, 자원 부족 시 false</returns>
    public bool TryRemoveResources(Dictionary<ResourceType, long> resources)
    {
        // 먼저 모든 자원이 충분한지 확인
        foreach (var kvp in resources)
        {
            if (GetResource(kvp.Key) < kvp.Value)
            {
                Debug.LogWarning($"[ResourceService] 자원 부족으로 거래 실패: {kvp.Key} (필요: {kvp.Value}, 보유: {GetResource(kvp.Key)})");
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
    /// <param name="type">확인할 자원 타입</param>
    /// <param name="amount">필요한 양</param>
    /// <returns>충분하면 true, 부족하면 false</returns>
    public bool HasEnoughResource(ResourceType type, long amount)
    {
        return GetResource(type) >= amount;
    }

    /// <summary>
    /// 모든 자원을 0으로 초기화합니다.
    /// </summary>
    public void ResetAllResources()
    {
        foreach (ResourceType type in System.Enum.GetValues(typeof(ResourceType)))
        {
            _resources[type] = 0;
        }
        Debug.Log("[ResourceService] 모든 자원이 초기화되었습니다.");
    }
}
