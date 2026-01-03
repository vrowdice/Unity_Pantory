using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 건물 계산 로직을 공통으로 처리하는 유틸리티 클래스입니다.
/// 연구 잠금 체크, 유지비 계산, 직원 수 계산, 자원 추론 로직을 한 곳에서 관리합니다.
/// </summary>
public static class BuildingCalculationUtility
{
    /// <summary>
    /// 연구가 완료된 건물들만 필터링하여 유지비, 직원 수, 자원 등을 계산하는 핵심 로직입니다.
    /// </summary>
    /// <param name="dataManager">데이터 매니저</param>
    /// <param name="buildingStates">건물 상태 리스트</param>
    /// <param name="totalMaintenanceCost">총 유지비 (출력)</param>
    /// <param name="totalRequiredEmployees">총 필요 직원 수 (출력)</param>
    /// <param name="inputResourceCounts">입력 자원 카운트 딕셔너리 (출력)</param>
    /// <param name="outputResourceCounts">출력 자원 카운트 딕셔너리 (출력)</param>
    /// <param name="additionalValidation">추가 검증 함수 (도로 연결 확인 등, null 가능)</param>
    public static void ExecuteCoreCalculation(
        DataManager dataManager,
        List<BuildingState> buildingStates,
        out int totalMaintenanceCost,
        out int totalRequiredEmployees,
        out Dictionary<string, int> inputResourceCounts,
        out Dictionary<string, int> outputResourceCounts,
        System.Func<BuildingState, BuildingData, bool> additionalValidation = null)
    {
        totalMaintenanceCost = 0;
        totalRequiredEmployees = 0;
        inputResourceCounts = new Dictionary<string, int>();
        outputResourceCounts = new Dictionary<string, int>();

        if (buildingStates == null || dataManager == null || dataManager.Building == null)
        {
            return;
        }

        foreach (BuildingState buildingState in buildingStates)
        {
            if (buildingState == null || string.IsNullOrEmpty(buildingState.buildingId))
            {
                continue;
            }

            BuildingData buildingData = dataManager.Building.GetBuildingData(buildingState.buildingId);
            if (buildingData == null)
            {
                continue;
            }

            // 1. 공통 필터: 연구 잠금 체크
            if (!buildingState.IsUnlocked(dataManager))
            {
                continue;
            }

            // 2. 추가 필터: 도로 연결 확인 (핸들러에서 넘겨준 로직 수행)
            if (additionalValidation != null && !additionalValidation(buildingState, buildingData))
            {
                continue;
            }

            // 3. 비용 및 인원 합산
            totalMaintenanceCost += buildingData.baseMaintenanceCost;
            totalRequiredEmployees += buildingData.requiredEmployees;

            // 4. 자원 생산/소비 집계 (생산 건물만)
            if (buildingData.IsProductionBuilding)
            {
                AggregateResources(dataManager, buildingState, buildingData, inputResourceCounts, outputResourceCounts);
            }
        }
    }

    /// <summary>
    /// 건물의 입력/출력 자원을 집계합니다. 입력 자원이 없으면 출력 자원의 요구사항에서 추론합니다.
    /// </summary>
    private static void AggregateResources(
        DataManager dataManager,
        BuildingState buildingState,
        BuildingData buildingData,
        Dictionary<string, int> inputCounts,
        Dictionary<string, int> outputCounts)
    {
        // 출력 자원 추가
        if (buildingState.outputProductionIds != null)
        {
            foreach (string outputIdentifier in buildingState.outputProductionIds)
            {
                if (string.IsNullOrEmpty(outputIdentifier))
                {
                    continue;
                }
                outputCounts[outputIdentifier] = outputCounts.GetValueOrDefault(outputIdentifier, 0) + 1;
            }
        }

        // 입력 자원 추가 (추론 로직 포함)
        if (buildingState.inputProductionIds != null && buildingState.inputProductionIds.Count > 0)
        {
            // 명시적 입력 자원이 있는 경우
            foreach (string inputIdentifier in buildingState.inputProductionIds)
            {
                if (string.IsNullOrEmpty(inputIdentifier))
                {
                    continue;
                }
                inputCounts[inputIdentifier] = inputCounts.GetValueOrDefault(inputIdentifier, 0) + 1;
            }
        }
        else if (buildingState.outputProductionIds != null && dataManager.Resource != null)
        {
            // 출력 자원의 요구사항에서 추론
            foreach (string outputIdentifier in buildingState.outputProductionIds)
            {
                if (string.IsNullOrEmpty(outputIdentifier))
                {
                    continue;
                }

                ResourceEntry entry = dataManager.Resource.GetResourceEntry(outputIdentifier);
                if (entry?.data?.requirements == null)
                {
                    continue;
                }

                foreach (ResourceRequirement requirement in entry.data.requirements)
                {
                    if (requirement.resource != null && !string.IsNullOrEmpty(requirement.resource.id))
                    {
                        int amount = Mathf.Max(1, requirement.count);
                        inputCounts[requirement.resource.id] = inputCounts.GetValueOrDefault(requirement.resource.id, 0) + amount;
                    }
                }
            }
        }
    }
}

