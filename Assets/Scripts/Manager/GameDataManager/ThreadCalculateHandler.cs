using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 스레드의 생산량, 유지비, 직원 요구사항 등을 계산하는 핸들러.
/// BuildingTileManager와 독립적으로 ThreadDataHandler를 통해 스레드 데이터를 계산합니다.
/// </summary>
public class ThreadCalculateHandler
{
    private readonly dataManager _dataManager;

    public ThreadCalculateHandler(dataManager gameDataManager)
    {
        _dataManager = gameDataManager;

        InitializeAllThreads();
    }

    #region 자원 및 비용 계산

    /// <summary>
    /// Thread의 총 유지비를 계산합니다.
    /// </summary>
    public int CalculateTotalMaintenanceCost(string threadId, List<BuildingState> buildingStates)
    {
        if (string.IsNullOrEmpty(threadId) || _dataManager == null || _dataManager.Building == null)
            return 0;

        if (buildingStates == null || buildingStates.Count == 0)
            return 0;

        int totalMaintenance = 0;

        foreach (var state in buildingStates)
        {
            if (state == null || string.IsNullOrEmpty(state.buildingId))
                continue;

            BuildingData data = _dataManager.Building.GetBuildingData(state.buildingId);
            if (data != null)
            {
                totalMaintenance += data.baseMaintenanceCost;
            }
        }

        return totalMaintenance;
    }

    /// <summary>
    /// Thread의 직원 요구사항을 계산합니다.
    /// </summary>
    public int CalculateRequiredEmployees(string threadId, List<BuildingState> buildingStates)
    {
        if (string.IsNullOrEmpty(threadId) || _dataManager == null || _dataManager.Building == null)
            return 0;

        if (buildingStates == null || buildingStates.Count == 0)
            return 0;

        int totalEmployees = 0;

        foreach (var state in buildingStates)
        {
            if (state == null || string.IsNullOrEmpty(state.buildingId))
                continue;

            BuildingData data = _dataManager.Building.GetBuildingData(state.buildingId);
            if (data != null)
            {
                totalEmployees += data.requiredEmployees;
            }
        }

        return totalEmployees;
    }

    /// <summary>
    /// Thread의 입력 생산 자원 리스트를 수집합니다 (중복 제거).
    /// </summary>
    public List<string> CollectInputProductionIds(string threadId, List<BuildingState> buildingStates)
    {
        List<string> inputIds = new List<string>();

        if (string.IsNullOrEmpty(threadId) || buildingStates == null || buildingStates.Count == 0)
            return inputIds;

        HashSet<string> uniqueIds = new HashSet<string>();

        foreach (var state in buildingStates)
        {
            if (state == null || state.inputProductionIds == null)
                continue;

            foreach (var resourceId in state.inputProductionIds)
            {
                if (!string.IsNullOrEmpty(resourceId) && uniqueIds.Add(resourceId))
                {
                    inputIds.Add(resourceId);
                }
            }
        }
        return inputIds;
    }

    /// <summary>
    /// Thread의 출력 생산 자원 리스트를 수집합니다 (중복 제거).
    /// </summary>
    public List<string> CollectOutputProductionIds(string threadId, List<BuildingState> buildingStates)
    {
        List<string> outputIds = new List<string>();

        if (string.IsNullOrEmpty(threadId) || buildingStates == null || buildingStates.Count == 0)
            return outputIds;

        HashSet<string> uniqueIds = new HashSet<string>();

        foreach (var state in buildingStates)
        {
            if (state == null || state.outputProductionIds == null)
                continue;

            foreach (var resourceId in state.outputProductionIds)
            {
                if (!string.IsNullOrEmpty(resourceId) && uniqueIds.Add(resourceId))
                {
                    outputIds.Add(resourceId);
                }
            }
        }
        return outputIds;
    }

    /// <summary>
    /// Thread의 생산 체인을 계산하여 입력/출력 자원을 집계합니다.
    /// 경로 검증 없이 BuildingState의 inputProductionIds와 outputProductionIds를 직접 집계합니다.
    /// </summary>
    public void CalculateProductionChain(string threadId, List<BuildingState> buildingStates, 
        out List<string> inputResourceIds, 
        out Dictionary<string, int> inputResourceCounts, 
        out List<string> outputResourceIds, 
        out Dictionary<string, int> outputResourceCounts)
    {
        inputResourceIds = new List<string>();
        inputResourceCounts = new Dictionary<string, int>();
        outputResourceIds = new List<string>();
        outputResourceCounts = new Dictionary<string, int>();

        if (string.IsNullOrEmpty(threadId) || buildingStates == null || buildingStates.Count == 0)
            return;

        if (_dataManager == null || _dataManager.Building == null)
            return;

        Dictionary<string, int> requiredInputResources = new Dictionary<string, int>();
        Dictionary<string, int> resourceCounts = new Dictionary<string, int>();

        foreach (var state in buildingStates)
        {
            if (state == null || string.IsNullOrEmpty(state.buildingId))
                continue;

            BuildingData data = _dataManager.Building.GetBuildingData(state.buildingId);
            if (data == null || !data.IsProductionBuilding)
                continue;

            // 출력 자원 집계
            if (state.outputProductionIds != null)
            {
                foreach (var outputId in state.outputProductionIds)
                {
                    if (!string.IsNullOrEmpty(outputId))
                    {
                        AddResourceCount(resourceCounts, outputId, 1);
                    }
                }
            }

            // 입력 자원 추적 (BuildingState에 설정된 값 우선 사용)
            var currentInputIds = state.inputProductionIds;

            // inputProductionIds가 없으면, 출력 자원의 요구사항(requirements)에서 추론
            if (currentInputIds == null || currentInputIds.Count == 0)
            {
                if (state.outputProductionIds != null && _dataManager.Resource != null)
                {
                    foreach (var outputId in state.outputProductionIds)
                    {
                        ResourceEntry outputResource = _dataManager.Resource.GetResourceEntry(outputId);
                        if (outputResource != null && outputResource.resourceData?.requirements != null)
                        {
                            foreach (var requirement in outputResource.resourceData.requirements)
                            {
                                if (requirement.resource != null && !string.IsNullOrEmpty(requirement.resource.id))
                                {
                                    int requirementCount = Mathf.Max(1, requirement.count);
                                    AddResourceCount(requiredInputResources, requirement.resource.id, requirementCount);
                                }
                            }
                        }
                    }
                }
            }
            else // BuildingState에 명시된 입력 자원이 있으면 그것을 사용
            {
                foreach (var inputId in currentInputIds)
                {
                    if (!string.IsNullOrEmpty(inputId))
                    {
                        AddResourceCount(requiredInputResources, inputId, 1);
                    }
                }
            }
        }

        inputResourceIds = new List<string>(requiredInputResources.Keys);
        inputResourceCounts = requiredInputResources;
        outputResourceIds = new List<string>(resourceCounts.Keys);
        outputResourceCounts = resourceCounts;
    }

    private void AddResourceCount(Dictionary<string, int> counts, string resourceId, int amount)
    {
        if (string.IsNullOrEmpty(resourceId))
            return;

        if (counts.ContainsKey(resourceId))
        {
            counts[resourceId] += amount;
        }
        else
        {
            counts[resourceId] = amount;
        }
    }

    /// <summary>
    /// 모든 스레드의 생산량 등을 초기화합니다.
    /// </summary>
    public void InitializeAllThreads()
    {
        var allThreads = _dataManager.Thread.GetAllThreads();
        if (allThreads == null || allThreads.Count == 0)
        {
            Debug.Log("[ThreadCalculateHandler] No threads to initialize.");
            return;
        }

        int initializedCount = 0;

        foreach (var threadState in allThreads.Values)
        {
            if (threadState == null || string.IsNullOrEmpty(threadState.threadId))
                continue;

            InitializeThread(threadState, _dataManager.Thread);
            initializedCount++;
        }
    }

    /// <summary>
    /// 특정 스레드의 생산량 등을 초기화합니다.
    /// </summary>
    public void InitializeThread(ThreadState threadState, ThreadDataHandler threadDataHandler)
    {
        if (threadState == null || threadDataHandler == null)
            return;

        var buildingStates = threadState.buildingStateList;
        if (buildingStates == null || buildingStates.Count == 0)
        {
            // 건물이 없어도 기본값 설정
            threadState.requiredEmployees = 0;
            threadState.totalMaintenanceCost = 0;
            return;
        }

        // 1. 유지비 계산
        int totalMaintenance = CalculateTotalMaintenanceCost(threadState.threadId, buildingStates);
        threadState.totalMaintenanceCost = totalMaintenance;

        // 2. 직원 요구사항 계산
        int requiredEmployees = CalculateRequiredEmployees(threadState.threadId, buildingStates);
        threadState.requiredEmployees = requiredEmployees;

        // 3. 생산 체인 계산 (BuildingState의 inputProductionIds와 outputProductionIds 집계)
        CalculateProductionChain(threadState.threadId, buildingStates,
            out List<string> inputResourceIds,
            out Dictionary<string, int> inputResourceCounts,
            out List<string> outputResourceIds,
            out Dictionary<string, int> outputResourceCounts);

        // 4. BuildingState의 inputProductionIds와 outputProductionIds 업데이트
        // (이미 BuildingState에 설정되어 있다면 그대로 사용, 없으면 계산된 값으로 설정)
        foreach (var buildingState in buildingStates)
        {
            if (buildingState == null)
                continue;

            // BuildingState에 inputProductionIds가 없으면 계산된 값으로 설정
            if (buildingState.inputProductionIds == null || buildingState.inputProductionIds.Count == 0)
            {
                buildingState.inputProductionIds = new List<string>();
                if (_dataManager?.Building != null)
                {
                    BuildingData data = _dataManager.Building.GetBuildingData(buildingState.buildingId);
                    if (data != null && data.IsProductionBuilding && buildingState.outputProductionIds != null)
                    {
                        // 출력 자원의 requirements에서 입력 자원 추론
                        foreach (var outputId in buildingState.outputProductionIds)
                        {
                            if (_dataManager.Resource != null)
                            {
                                ResourceEntry outputResource = _dataManager.Resource.GetResourceEntry(outputId);
                                if (outputResource != null && outputResource.resourceData?.requirements != null)
                                {
                                    foreach (var requirement in outputResource.resourceData.requirements)
                                    {
                                        if (requirement.resource != null && !string.IsNullOrEmpty(requirement.resource.id))
                                        {
                                            if (!buildingState.inputProductionIds.Contains(requirement.resource.id))
                                            {
                                                buildingState.inputProductionIds.Add(requirement.resource.id);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // BuildingState에 outputProductionIds가 없으면 건물 데이터에서 가져오기
            if (buildingState.outputProductionIds == null || buildingState.outputProductionIds.Count == 0)
            {
                buildingState.outputProductionIds = new List<string>();
                if (_dataManager?.Building != null)
                {
                    BuildingData data = _dataManager.Building.GetBuildingData(buildingState.buildingId);
                    if (data != null && data.IsProductionBuilding)
                    {
                        // ProductionBuildingData에서 producibleResources 가져오기
                        if (data is ProductionBuildingData productionData && productionData.ProducibleResources != null)
                        {
                            foreach (var resourceData in productionData.ProducibleResources)
                            {
                                if (resourceData != null && !string.IsNullOrEmpty(resourceData.id))
                                {
                                    if (!buildingState.outputProductionIds.Contains(resourceData.id))
                                    {
                                        buildingState.outputProductionIds.Add(resourceData.id);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    #endregion
}

