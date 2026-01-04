using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 건물 계산 로직을 공통으로 처리하는 유틸리티 클래스입니다.
/// 연구 잠금 체크, 유지비 계산, 직원 수 계산, 자원 추론 로직을 통합 관리합니다.
/// </summary>
public static class BuildingCalculationUtility
{
    /// <summary>
    /// 연구가 완료된 건물들을 필터링하여 유지비, 직원 수, 자원 소비/생산량을 계산하는 핵심 로직을 실행합니다.
    /// </summary>
    /// <param name="dataManager">데이터 매니저 참조</param>
    /// <param name="buildingStates">계산 대상 건물 상태 리스트</param>
    /// <param name="totalMaintenanceCost">계산된 총 유지비 (출력)</param>
    /// <param name="totalRequiredEmployees">계산된 총 필요 직원 수 (출력)</param>
    /// <param name="inputResourceCounts">계산된 입력 자원 ID별 수량 딕셔너리 (출력)</param>
    /// <param name="outputResourceCounts">계산된 출력 자원 ID별 수량 딕셔너리 (출력)</param>
    /// <param name="additionalValidation">도로 연결 등 추가 검증용 델리게이트 (선택 사항)</param>
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

            // 1. 연구 잠금 상태 체크
            if (!buildingState.IsUnlocked(dataManager))
            {
                continue;
            }

            // 2. 외부에서 주입된 추가 검증 로직 수행 (예: 도로 연결성 확인)
            if (additionalValidation != null && !additionalValidation(buildingState, buildingData))
            {
                continue;
            }

            // 3. 경제적 수치 합산
            totalMaintenanceCost += buildingData.baseMaintenanceCost;
            totalRequiredEmployees += buildingData.requiredEmployees;

            // 4. 자원 생산 및 소비 집계
            if (buildingData.IsProductionBuilding)
            {
                AggregateResources(dataManager, buildingState, buildingData, inputResourceCounts, outputResourceCounts);
            }
        }
    }

    /// <summary>
    /// 특정 건물의 입/출력 자원을 집계합니다. 명시적 입력 정보가 없을 경우 출력물의 필요 요구사항에서 자원을 추론합니다.
    /// </summary>
    private static void AggregateResources(
        DataManager dataManager,
        BuildingState buildingState,
        BuildingData buildingData,
        Dictionary<string, int> inputCounts,
        Dictionary<string, int> outputCounts)
    {
        // 출력 자원 집계
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

        // 입력 자원 집계 및 추론
        if (buildingState.inputProductionIds != null && buildingState.inputProductionIds.Count > 0)
        {
            // 건물 상태에 명시적인 입력 자원 ID가 등록되어 있는 경우
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
            // 명시적 입력 정보가 없을 때: 결과물(Output)의 제작 레시피(Requirements)를 기반으로 역산
            foreach (string outputIdentifier in buildingState.outputProductionIds)
            {
                if (string.IsNullOrEmpty(outputIdentifier))
                {
                    continue;
                }

                ResourceEntry entry = dataManager.Resource.GetResourceEntry(outputIdentifier);
                if (entry == null || entry.data == null || entry.data.requirements == null)
                {
                    continue;
                }

                foreach (ResourceRequirement requirement in entry.data.requirements)
                {
                    if (requirement.resource != null && !string.IsNullOrEmpty(requirement.resource.id))
                    {
                        // 수량이 설정되지 않았더라도 최소 1개는 소비하는 것으로 처리
                        int amount = Mathf.Max(1, requirement.count);
                        string resourceId = requirement.resource.id;
                        inputCounts[resourceId] = inputCounts.GetValueOrDefault(resourceId, 0) + amount;
                    }
                }
            }
        }
    }
}