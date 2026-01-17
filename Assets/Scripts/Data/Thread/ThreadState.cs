using System;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Thread(생산 라인)의 상태를 나타내는 클래스
/// </summary>
[Serializable]
public class ThreadState
{
    public string threadId = string.Empty;         // 고유 식별자 (플레이어가 저장할 때 사용)
    public string threadName = string.Empty;       // 표시 이름
    public string categoryId = string.Empty;       // 속한 카테고리 ID
    public List<BuildingState> buildingStateList = new List<BuildingState>();
    public string previewImagePath = string.Empty; // 건물 레이아웃 미리보기 이미지 경로
    public int totalMaintenanceCost = 0;           // 스레드의 총 유지비 (월간)
    public int requiredBuildCost = 0;      // 스레드 건설 시 필요한 금액
    
    [Header("Production Status")]
    public float currentProductionProgress = 0f;   // 현재 생산 진행도 (0.0 ~ 1.0)
    public float currentProductionEfficiency = 0f; // 현재 생산 효율 퍼센테이지 (0.0 ~ 1.0)
    
    [Header("Employee Requirements")]
    public int requiredEmployees = 0;               // 필요한 Employee 직원 수
    public int currentWorkers = 0;                // 현재 일하고 있는 Worker 직원 수
    public int currentTechnicians = 0;            // 현재 일하고 있는 Technician 직원 수
    
    public ThreadState()
    {

    }
    
    public ThreadState(string id, string name, string div = "", string catId = "")
    {
        threadId = id;
        threadName = name;
        categoryId = catId;
    }

    public bool TryGetAggregatedResourceCounts(out Dictionary<string, int> consumptionCounts, out Dictionary<string, int> productionCounts)
    {
        consumptionCounts = new Dictionary<string, int>();
        productionCounts = new Dictionary<string, int>();

        if (buildingStateList == null)
            return false;

        foreach (var buildingState in buildingStateList)
        {
            if (buildingState == null)
                continue;

            AccumulateResourceCounts(consumptionCounts, buildingState.inputProductionIds);
            AccumulateResourceCounts(productionCounts, buildingState.outputProductionIds);
        }

        return consumptionCounts.Count > 0 || productionCounts.Count > 0;
    }

    private void AccumulateResourceCounts(Dictionary<string, int> counts, List<string> resourceIds)
    {
        if (resourceIds == null)
            return;

        foreach (var resourceId in resourceIds)
        {
            if (string.IsNullOrEmpty(resourceId))
                continue;

            if (counts.TryGetValue(resourceId, out int current))
            {
                counts[resourceId] = current + 1;
            }
            else
            {
                counts[resourceId] = 1;
            }
        }
    }
}
