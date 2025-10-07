using System.Collections.Generic;
using UnityEngine;

// 게임 데이터를 관리하는 싱글톤 허브 클래스 (실제 데이터 관리는 각 서비스 클래스에 위임)
public class GameDataManager : MonoBehaviour
{
    public static GameDataManager Instance { get; private set; }

    private ResourceService _resourceService;
    public ResourceService Resource => _resourceService;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
        InitializeServices();
    }

    // 모든 서비스를 초기화
    private void InitializeServices()
    {
        _resourceService = new ResourceService();
        Debug.Log("[GameDataManager] All services initialized.");
    }

    // ----------------- 편의 메서드 (ResourceService 직접 호출) -----------------
    
    // 특정 자원의 현재 보유량 반환
    public long GetResource(ResourceType type) => _resourceService.GetResource(type);

    // 모든 자원 정보 반환
    public Dictionary<ResourceType, long> GetAllResources() => _resourceService.GetAllResources();

    // 특정 자원 추가
    public void AddResource(ResourceType type, long amount) => _resourceService.AddResource(type, amount);

    // 특정 자원 제거
    public bool TryRemoveResource(ResourceType type, long amount) => _resourceService.TryRemoveResource(type, amount);

    // 특정 자원 충분 여부 확인
    public bool HasEnoughResource(ResourceType type, long amount) => _resourceService.HasEnoughResource(type, amount);

    // ----------------- 레거시 호환 메서드 -----------------
    
    public long Steel => _resourceService.GetResource(ResourceType.Steel);
    public long Wood => _resourceService.GetResource(ResourceType.Wood);
    public long Labor => _resourceService.GetResource(ResourceType.Labor);
    public long Silver => _resourceService.GetResource(ResourceType.Silver);

    public void AddSteel(long amount) => _resourceService.AddResource(ResourceType.Steel, amount);
    public bool TryRemoveSteel(long amount) => _resourceService.TryRemoveResource(ResourceType.Steel, amount);

    public void AddWood(long amount) => _resourceService.AddResource(ResourceType.Wood, amount);
    public bool TryRemoveWood(long amount) => _resourceService.TryRemoveResource(ResourceType.Wood, amount);

    public void AddLabor(long amount) => _resourceService.AddResource(ResourceType.Labor, amount);
    public bool TryRemoveLabor(long amount) => _resourceService.TryRemoveResource(ResourceType.Labor, amount);

    void Start()
    {
        // 초기화 로직
    }

    void Update()
    {
        // 업데이트 로직
    }
}
