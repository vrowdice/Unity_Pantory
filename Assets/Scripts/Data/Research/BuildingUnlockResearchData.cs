using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 빌딩 언락을 위한 연구 데이터 클래스
/// ResearchData를 상속받아 특정 건물을 언락하는 연구를 정의합니다.
/// </summary>
[CreateAssetMenu(fileName = "NewResearch", menuName = "Game Data/Research Data/Building Unlock")]
public class BuildingUnlockResearchData : ResearchData
{
    [Header("Building Unlock")]
    /// <summary>
    /// 이 연구를 완료하면 언락되는 건물 ID 목록
    /// </summary>
    public List<BuildingData> unlockableBuildings;
    
    /// <summary>
    /// 특정 건물이 이 연구로 언락되는지 확인합니다.
    /// </summary>
    /// <param name="buildingId">확인할 건물 ID</param>
    /// <returns>언락 가능하면 true</returns>
    public bool UnlocksBuilding(BuildingData buildingData)
    {
        if (unlockableBuildings == null)
            return false;
            
        foreach (BuildingData item in unlockableBuildings)
        {
            if (item == buildingData)
                return true;
        }
        
        return false;
    }
}

