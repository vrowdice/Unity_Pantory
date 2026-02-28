using System;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Thread(생산 라인)의 상태를 나타내는 클래스
/// </summary>
[Serializable]
public class ThreadState
{
    public string threadId = string.Empty;
    public string threadName = string.Empty;
    public string categoryId = string.Empty;
    public List<BuildingState> buildingStateList = new List<BuildingState>();
    public string previewImagePath = string.Empty;
    public int totalMaintenanceCost = 0;
    public int requiredBuildCost = 0;
    
    [Header("Production Status")]
    public float currentProductionProgress = 0f;
    public float currentProductionEfficiency = 0f;
    
    [Header("Employee Requirements")]
    public int requiredTechnicians = 0;
    public int requiredEmployees = 0;
    public int currentWorkers = 0;
    public int currentTechnicians = 0;
    
    [System.NonSerialized]
    public Dictionary<string, int> cachedInputCounts;
    [System.NonSerialized]
    public Dictionary<string, int> cachedOutputCounts;
    
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

        foreach (BuildingState buildingState in buildingStateList)
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

        foreach (string resourceId in resourceIds)
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
