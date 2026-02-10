using System.Collections.Generic;
using UnityEngine;

public static class BuildingCalculationUtils
{
    /// <summary>
    /// 연구가 완료된 건물들을 필터링하여 경제/자원 수치를 계산합니다.
    /// </summary>
    public static ThreadCalculationResult CalculateProductionStats(
        DataManager dataManager,
        List<BuildingState> buildingStates)
    {
        ThreadCalculationResult result = new ThreadCalculationResult();
        foreach (BuildingState buildingState in buildingStates)
        {
            BuildingData buildingData = dataManager.Building.GetBuildingData(buildingState.Id);

            result.TotalBuildCost += buildingData.buildCost;
            result.TotalMaintenanceCost += buildingData.maintenanceCost;
            result.TotalRequiredEmployees += buildingData.requiredEmployees;
            result.RequiredTechnicians += buildingData.isProfessional ? buildingData.requiredEmployees : 0;
            if (buildingData.IsProductionBuilding)
            {
                ProcessBuildingResources(dataManager, buildingState, result);
            }
        }

        return result;
    }

    private static void ProcessBuildingResources(
        DataManager dataManager,
        BuildingState state,
        ThreadCalculationResult result)
    {
        if (state.inputProductionIds != null)
        {
            foreach (string id in state.inputProductionIds)
            {
                if (string.IsNullOrEmpty(id)) continue;
                result.InputResourceCounts[id] = result.InputResourceCounts.GetValueOrDefault(id, 0) + 1;
            }
        }

        if (state.outputProductionIds != null)
        {
            foreach (string id in state.outputProductionIds)
            {
                if (string.IsNullOrEmpty(id)) continue;
                result.OutputResourceCounts[id] = result.OutputResourceCounts.GetValueOrDefault(id, 0) + 1;
            }
        }
    }
}