using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 건물 계산 결과를 담는 데이터 구조체입니다.
/// </summary>
public class CalculationResult
{
    public int TotalMaintenanceCost { get; set; }
    public int TotalRequiredEmployees { get; set; }
    public Dictionary<string, int> InputResourceCounts { get; } = new Dictionary<string, int>();
    public Dictionary<string, int> OutputResourceCounts { get; } = new Dictionary<string, int>();

    public void AddInput(string id, int amount)
    {
        AddToDict(InputResourceCounts, id, amount);
    }

    public void AddOutput(string id, int amount)
    {
        AddToDict(OutputResourceCounts, id, amount);
    }

    private void AddToDict(Dictionary<string, int> dict, string id, int amount)
    {
        if (string.IsNullOrEmpty(id)) return;
        dict[id] = dict.GetValueOrDefault(id, 0) + amount;
    }
}

public static class BuildingCalculationUtility
{
    /// <summary>
    /// 연구가 완료된 건물들을 필터링하여 경제/자원 수치를 계산합니다.
    /// </summary>
    public static CalculationResult CalculateProductionStats(
        DataManager dataManager,
        List<BuildingState> buildingStates,
        System.Func<BuildingState, BuildingData, bool> additionalValidation = null)
    {
        CalculationResult result = new CalculationResult();

        if (dataManager?.Building == null || buildingStates == null)
            return result;

        foreach (BuildingState buildingState in buildingStates)
        {
            BuildingData buildingData;
            if (!IsValidBuilding(buildingState, dataManager, out buildingData))
                continue;

            // 추가 검증 (외부 조건)
            if (additionalValidation != null && !additionalValidation(buildingState, buildingData))
                continue;

            // 경제 수치 합산
            result.TotalMaintenanceCost += buildingData.baseMaintenanceCost;
            result.TotalRequiredEmployees += buildingData.requiredEmployees;

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
        CalculationResult result)
    {
        // 출력 자원 집계
        if (state.outputProductionIds != null)
        {
            foreach (string id in state.outputProductionIds)
            {
                result.AddOutput(id, 1);
            }
        }

        // 입력 자원 집계 (명시적 입력 vs 레시피 추론)
        if (HasExplicitInputs(state))
        {
            // 명시적 입력 자원이 설정된 경우
            foreach (string id in state.inputProductionIds)
            {
                result.AddInput(id, 1);
            }
        }
        else
        {
            // 입력 정보가 없어 출력물의 레시피로 역산해야 하는 경우
            InferInputsFromRecipe(dataManager, state.outputProductionIds, result);
        }
    }

    private static bool HasExplicitInputs(BuildingState state)
    {
        return state.inputProductionIds != null && state.inputProductionIds.Count > 0;
    }

    /// <summary>
    /// 출력물의 제작 레시피(Requirements)를 기반으로 필요한 입력 자원을 추론합니다.
    /// </summary>
    private static void InferInputsFromRecipe(
        DataManager dataManager,
        List<string> outputIds,
        CalculationResult result)
    {
        if (outputIds == null || dataManager.Resource == null) return;

        foreach (string outputId in outputIds)
        {
            if (string.IsNullOrEmpty(outputId)) continue;

            ResourceEntry entry = dataManager.Resource.GetResourceEntry(outputId);
            // entry 유효성 및 requirement 존재 여부 체크
            if (entry?.data?.requirements == null) continue;

            foreach (ResourceRequirement req in entry.data.requirements)
            {
                if (req.resource == null || string.IsNullOrEmpty(req.resource.id)) continue;

                // 최소 1개 보장 로직 유지
                int amount = Mathf.Max(1, req.count);
                result.AddInput(req.resource.id, amount);
            }
        }
    }
}