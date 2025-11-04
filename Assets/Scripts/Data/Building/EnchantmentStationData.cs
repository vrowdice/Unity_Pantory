using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 마법 부여 건물 데이터
/// 도구나 무기에 마법을 부여하는 건물입니다.
/// </summary>
[CreateAssetMenu(fileName = "NewEnchantmentStation", menuName = "Game Data/Building Data/Enchantment", order = 3)]
public class EnchantmentStationData : ProductionBuildingData
{
    [Header("Enchantment")]
    [Tooltip("마법 부여 가능한 자원 타입 리스트 (tool, weapon 등)")]
    public List<ResourceType> enchantableResourceTypes = new List<ResourceType>();
    
    /// <summary>
    /// 특정 자원 타입에 마법을 부여할 수 있는지 확인합니다.
    /// </summary>
    public bool CanEnchantResourceType(ResourceType resourceType)
    {
        // 비어있으면 모든 타입 가능
        if (enchantableResourceTypes == null || enchantableResourceTypes.Count == 0)
            return true;
        
        return enchantableResourceTypes.Contains(resourceType);
    }
}

