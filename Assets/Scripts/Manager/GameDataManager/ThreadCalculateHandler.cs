using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/// <summary>
/// 스레드의 생산량, 유지비, 직원 요구사항 등을 계산하는 핸들러입니다.
/// 연구되지 않은 건물은 모든 계산에서 제외됩니다.
/// </summary>
public class ThreadCalculateHandler
{
    private readonly DataManager _dataManager;

    public ThreadCalculateHandler(DataManager dataManager)
    {
        _dataManager = dataManager;

        InitializeAllThreads();
    }

    /// <summary>
    /// 스레드의 총 유지비를 계산합니다. (연구된 건물만 포함)
    /// </summary>
    public int CalculateTotalMaintenanceCost(string threadIdentifier, List<BuildingState> buildingStates)
    {
        if (buildingStates == null || buildingStates.Count == 0)
        {
            return 0;
        }

        BuildingCalculationUtility.ExecuteCoreCalculation(
            _dataManager, buildingStates,
            out int cost, out _, out _, out _
        );
        return cost;
    }

    /// <summary>
    /// 스레드의 직원 요구사항을 계산합니다. (연구된 건물만 포함)
    /// </summary>
    public int CalculateRequiredEmployees(string threadIdentifier, List<BuildingState> buildingStates)
    {
        if (buildingStates == null || buildingStates.Count == 0)
        {
            return 0;
        }

        BuildingCalculationUtility.ExecuteCoreCalculation(
            _dataManager, buildingStates,
            out _, out int employees, out _, out _
        );
        return employees;
    }

    /// <summary>
    /// 스레드의 입력 생산 자원 식별자 리스트를 수집합니다. (연구된 건물만 포함)
    /// </summary>
    public List<string> CollectInputProductionIdentifiers(string threadIdentifier, List<BuildingState> buildingStates)
    {
        List<string> inputIdentifiers = new List<string>();

        if (string.IsNullOrEmpty(threadIdentifier) || buildingStates == null || buildingStates.Count == 0)
        {
            return inputIdentifiers;
        }

        HashSet<string> uniqueIdentifiers = new HashSet<string>();

        foreach (BuildingState buildingState in buildingStates)
        {
            if (buildingState == null || buildingState.inputProductionIds == null || !buildingState.IsUnlocked(_dataManager))
            {
                continue;
            }

            foreach (string resourceIdentifier in buildingState.inputProductionIds)
            {
                if (!string.IsNullOrEmpty(resourceIdentifier) && uniqueIdentifiers.Add(resourceIdentifier))
                {
                    inputIdentifiers.Add(resourceIdentifier);
                }
            }
        }
        return inputIdentifiers;
    }

    /// <summary>
    /// 스레드의 출력 생산 자원 식별자 리스트를 수집합니다. (연구된 건물만 포함)
    /// </summary>
    public List<string> CollectOutputProductionIdentifiers(string threadIdentifier, List<BuildingState> buildingStates)
    {
        List<string> outputIdentifiers = new List<string>();

        if (string.IsNullOrEmpty(threadIdentifier) || buildingStates == null || buildingStates.Count == 0)
        {
            return outputIdentifiers;
        }

        HashSet<string> uniqueIdentifiers = new HashSet<string>();

        foreach (BuildingState buildingState in buildingStates)
        {
            if (buildingState == null || buildingState.outputProductionIds == null || !buildingState.IsUnlocked(_dataManager))
            {
                continue;
            }

            foreach (string resourceIdentifier in buildingState.outputProductionIds)
            {
                if (!string.IsNullOrEmpty(resourceIdentifier) && uniqueIdentifiers.Add(resourceIdentifier))
                {
                    outputIdentifiers.Add(resourceIdentifier);
                }
            }
        }
        return outputIdentifiers;
    }

    /// <summary>
    /// 스레드의 생산 체인을 계산하여 자원을 집계합니다. (연구된 건물만 포함)
    /// </summary>
    public void CalculateProductionChain(
        string threadIdentifier,
        List<BuildingState> buildingStates,
        out List<string> inputResourceIdentifiers,
        out Dictionary<string, int> inputResourceCounts,
        out List<string> outputResourceIdentifiers,
        out Dictionary<string, int> outputResourceCounts)
    {
        if (string.IsNullOrEmpty(threadIdentifier) || buildingStates == null || buildingStates.Count == 0)
        {
            inputResourceIdentifiers = new List<string>();
            inputResourceCounts = new Dictionary<string, int>();
            outputResourceIdentifiers = new List<string>();
            outputResourceCounts = new Dictionary<string, int>();
            return;
        }

        if (_dataManager == null || _dataManager.Building == null)
        {
            inputResourceIdentifiers = new List<string>();
            inputResourceCounts = new Dictionary<string, int>();
            outputResourceIdentifiers = new List<string>();
            outputResourceCounts = new Dictionary<string, int>();
            return;
        }

        BuildingCalculationUtility.ExecuteCoreCalculation(
            _dataManager, buildingStates,
            out int maintenance, out int employees, out inputResourceCounts, out outputResourceCounts
        );

        inputResourceIdentifiers = new List<string>(inputResourceCounts.Keys);
        outputResourceIdentifiers = new List<string>(outputResourceCounts.Keys);
    }

    public void InitializeAllThreads()
    {
        Dictionary<string, ThreadState> allThreads = _dataManager.Thread.GetAllThreads();
        if (allThreads == null || allThreads.Count == 0)
        {
            return;
        }

        foreach (ThreadState threadState in allThreads.Values)
        {
            if (threadState == null || string.IsNullOrEmpty(threadState.threadId))
            {
                continue;
            }

            InitializeThread(threadState, _dataManager.Thread);
        }
    }

    public void InitializeThread(ThreadState threadState, ThreadDataHandler threadDataHandler)
    {
        if (threadState == null || threadDataHandler == null)
        {
            return;
        }

        List<BuildingState> buildingStates = threadState.buildingStateList;
        if (buildingStates == null || buildingStates.Count == 0)
        {
            threadState.requiredEmployees = 0;
            threadState.totalMaintenanceCost = 0;
            return;
        }

        threadState.totalMaintenanceCost = CalculateTotalMaintenanceCost(threadState.threadId, buildingStates);
        threadState.requiredEmployees = CalculateRequiredEmployees(threadState.threadId, buildingStates);

        List<string> inputResourceIdentifiers;
        Dictionary<string, int> inputResourceCounts;
        List<string> outputResourceIdentifiers;
        Dictionary<string, int> outputResourceCounts;

        CalculateProductionChain(
            threadState.threadId,
            buildingStates,
            out inputResourceIdentifiers,
            out inputResourceCounts,
            out outputResourceIdentifiers,
            out outputResourceCounts
        );
    }

    /// <summary>
    /// 배치된 모든 스레드의 총 유지비를 계산합니다.
    /// </summary>
    public int CalculateTotalMaintenanceCost(ThreadPlacementDataHandler threadPlacement)
    {
        if (threadPlacement == null)
        {
            return 0;
        }

        int totalCost = 0;
        Dictionary<Vector2Int, ThreadPlacementState> allPlacedThreads = threadPlacement.GetAllPlacedThreads();
        
        if (allPlacedThreads == null)
        {
            return 0;
        }

        foreach (ThreadPlacementState placement in allPlacedThreads.Values)
        {
            if (placement?.RuntimeState != null)
            {
                totalCost += placement.RuntimeState.totalMaintenanceCost;
            }
        }

        return totalCost;
    }
}