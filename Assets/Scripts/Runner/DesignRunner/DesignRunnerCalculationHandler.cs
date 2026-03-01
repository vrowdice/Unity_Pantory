using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/// <summary>
/// 생산 체인 및 건물 관련 계산을 담당하는 핸들러입니다.
/// </summary>
public class DesignRunnerCalculationHandler
{
    private readonly DesignRunner _manager;
    private readonly DesignRunnerGridHandler _gridHandler;
    
    private DataManager DataManager => _manager.DataManager;
    
    public DesignRunnerCalculationHandler(DesignRunner manager)
    {
        _manager = manager;
        _gridHandler = manager.GridGenHandler;
    }
    
    /// <summary>
    /// 생산 체인을 계산합니다.
    /// ThreadPlacementDataHandler와 동일하게 BuildingCalculationUtils 기반으로 집계합니다.
    /// (도로 연결/하역소 특수 로직 없이, 순수하게 건물 리스트만 기준으로 사용/생성 자원을 계산)
    /// </summary>
    public void CalculateProductionChain(
        string threadName,
        List<BuildingState> states,
        Dictionary<Vector2Int, BuildingState> gridMap,
        out List<string> inputIds,
        out Dictionary<string, int> inputCounts,
        out List<string> outputIds,
        out Dictionary<string, int> outputCounts)
    {
        inputIds = new List<string>();
        outputIds = new List<string>();
        inputCounts = new Dictionary<string, int>();
        outputCounts = new Dictionary<string, int>();

        if (states == null || states.Count == 0)
        {
            return;
        }

        ThreadCalculationResult stats = BuildingCalculationUtils.CalculateProductionStats(DataManager, states);
        inputCounts = stats.InputResourceCounts;
        outputCounts = stats.OutputResourceCounts;

        inputIds.AddRange(inputCounts.Keys);
        outputIds.AddRange(outputCounts.Keys);
    }
    
    /// <summary>
    /// 생산 건물의 모든 입력 자원이 도로에 충족되는지 확인합니다.
    /// </summary>
    public bool AreAllInputResourcesAvailable(BuildingState state, Vector2Int inputPos, Dictionary<Vector2Int, BuildingState> gridMap)
    {
        BuildingData data = DataManager.Building.GetBuildingData(state.Id);
        if (data is RawMaterialFactoryData) return true;
        
        if (!_gridHandler.IsRoadAtPosition(inputPos, gridMap)) return false;
        
        HashSet<string> roadResources = _manager.RoadHandler.GetResourcesAtRoad(inputPos);
        
        if (state.inputProductionIds != null && state.inputProductionIds.Count > 0)
        {
            foreach (string id in state.inputProductionIds)
            {
                if (string.IsNullOrEmpty(id)) continue;
                if (!roadResources.Contains(id)) return false;
            }
            return true;
        }
        
        if (state.outputProductionIds != null && state.outputProductionIds.Count > 0)
        {
            foreach (string outId in state.outputProductionIds)
            {
                if (string.IsNullOrEmpty(outId)) continue;
                
                ResourceEntry entry = DataManager.Resource.GetResourceEntry(outId);
                if (entry?.data?.requirements == null) continue;
                
                foreach (ResourceRequirement req in entry.data.requirements)
                {
                    if (req.resource == null || string.IsNullOrEmpty(req.resource.id)) continue;
                    if (!roadResources.Contains(req.resource.id)) return false;
                }
            }
        }
        
        return true;
    }
    
    /// <summary>
    /// 건물의 입력 자원을 처리하여 카운트에 추가합니다.
    /// </summary>
    public void ProcessInputs(BuildingState state, Dictionary<string, int> counts)
    {
        if (state.inputProductionIds != null && state.inputProductionIds.Count > 0)
        {
            foreach (string id in state.inputProductionIds)
            {
                if (string.IsNullOrEmpty(id)) continue;
                counts[id] = counts.GetValueOrDefault(id, 0) + 1;
            }
        }
        else if (state.outputProductionIds != null && state.outputProductionIds.Count > 0)
        {
            foreach (string outId in state.outputProductionIds)
            {
                if (string.IsNullOrEmpty(outId)) continue;

                ResourceEntry entry = DataManager.Resource.GetResourceEntry(outId);
                if (entry?.data?.requirements == null) continue;

                foreach (ResourceRequirement req in entry.data.requirements)
                {
                    if (req.resource != null && !string.IsNullOrEmpty(req.resource.id))
                    {
                        int amount = Mathf.Max(1, req.count);
                        counts[req.resource.id] = counts.GetValueOrDefault(req.resource.id, 0) + amount;
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// 스레드 출력 건물 수를 계산합니다.
    /// </summary>
    public int CalculateThreadOutputs(string threadName, List<BuildingState> states, Dictionary<Vector2Int, BuildingState> gridMap)
    {
        int count = 0;

        foreach (BuildingState state in states ?? new List<BuildingState>())
        {
            BuildingData data = DataManager.Building.GetBuildingData(state.Id);
            if (data == null || !data.IsProductionBuilding) continue;

            Vector2Int basePos = new Vector2Int(state.positionX, state.positionY);
            Vector2Int inPos = basePos + GridMathUtils.GetRotatedOffset(data.InputPosition, state.rotation);
            Vector2Int outPos = basePos + GridMathUtils.GetRotatedOffset(data.OutputPosition, state.rotation);

            bool canProduce = false;
            if (data is RawMaterialFactoryData)
            {
                canProduce = RoadNetworkAnalyzer.IsConnected(outPos, false, true, gridMap, DataManager);
            }
            else
            {
                canProduce = RoadNetworkAnalyzer.IsConnected(inPos, true, false, gridMap, DataManager) &&
                             RoadNetworkAnalyzer.IsConnected(outPos, false, true, gridMap, DataManager) &&
                             AreAllInputResourcesAvailable(state, inPos, gridMap);
            }
            
            if (canProduce) count++;
        }
        return count;
    }
    
    /// <summary>
    /// 총 유지비를 계산합니다.
    /// </summary>
    public int CalculateTotalMaintenanceCost(string threadName, List<BuildingState> states)
    {
        ThreadCalculationResult stats = BuildingCalculationUtils.CalculateProductionStats(DataManager, states ?? new List<BuildingState>());
        return stats.TotalMaintenanceCost;
    }
}
