using UnityEngine;

/// <summary>
/// 빌딩 언락을 위한 연구 데이터 클래스
/// ResearchData를 상속받아 특정 건물을 언락하는 연구를 정의합니다.
/// </summary>
public class BuildingUnlockResearchData : ResearchData
{
    [Header("Building Unlock")]
    /// <summary>
    /// 이 연구를 완료하면 언락되는 건물 ID 목록
    /// </summary>
    public string[] unlockableBuildingIds;
    
    /// <summary>
    /// 언락되는 건물의 표시 이름 (UI 표시용)
    /// </summary>
    public string[] unlockableBuildingNames;
    
    /// <summary>
    /// 특정 건물이 이 연구로 언락되는지 확인합니다.
    /// </summary>
    /// <param name="buildingId">확인할 건물 ID</param>
    /// <returns>언락 가능하면 true</returns>
    public bool UnlocksBuilding(string buildingId)
    {
        if (unlockableBuildingIds == null || string.IsNullOrEmpty(buildingId))
            return false;
            
        foreach (var id in unlockableBuildingIds)
        {
            if (id == buildingId)
                return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// 이 연구로 언락되는 모든 건물 ID를 반환합니다.
    /// </summary>
    /// <returns>언락 가능한 건물 ID 배열</returns>
    public string[] GetUnlockableBuildingIds()
    {
        return unlockableBuildingIds != null ? unlockableBuildingIds : new string[0];
    }
}

