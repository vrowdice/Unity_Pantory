using System.Collections.Generic;
using UnityEngine;

public static class BuildingCalculationUtility
{
    /// <summary>
    /// 연구가 완료된 건물들을 필터링하여 경제/자원 수치를 계산합니다.
    /// </summary>
    public static ThreadCalculationResult CalculateProductionStats(
        DataManager dataManager,
        List<BuildingState> buildingStates,
        System.Func<BuildingState, BuildingData, bool> additionalValidation = null)
    {
        ThreadCalculationResult result = new ThreadCalculationResult();

        foreach (BuildingState buildingState in buildingStates)
        {
            BuildingData buildingData;
            if (!IsValidBuilding(buildingState, dataManager, out buildingData))
                continue;

            // 추가 검증 (외부 조건)
            if (additionalValidation != null && !additionalValidation(buildingState, buildingData))
                continue;

            // 경제 수치 합산
            result.TotalBuildCost += buildingData.buildCost;
            result.TotalMaintenanceCost += buildingData.maintenanceCost;
            result.TotalRequiredEmployees += buildingData.requiredEmployees;
            result.RequiredTechnicians += buildingData.isProfessional ? 1 : 0;

            // 자원 생산/소비 집계
            if (buildingData.IsProductionBuilding)
            {
                ProcessBuildingResources(dataManager, buildingState, result);
            }
        }

        return result;
    }

    /// <summary>
    /// 건물의 유효성 및 잠금 해제 여부를 확인합니다.
    /// </summary>
    private static bool IsValidBuilding(BuildingState state, DataManager data, out BuildingData dataEntry)
    {
        dataEntry = null;
        if (state == null || string.IsNullOrEmpty(state.buildingId)) return false;

        dataEntry = data.Building.GetBuildingData(state.buildingId);
        if (dataEntry == null) return false;

        return state.IsUnlocked(data);
    }

    private static void ProcessBuildingResources(
        DataManager dataManager,
        BuildingState state,
        ThreadCalculationResult result)
    {
        // 입력 자원 집계
        if (state.inputProductionIds != null)
        {
            foreach (string id in state.inputProductionIds)
            {
                if (string.IsNullOrEmpty(id)) continue;
                result.InputResourceCounts[id] = result.InputResourceCounts.GetValueOrDefault(id, 0) + 1;
            }
        }

        // 출력 자원 집계
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