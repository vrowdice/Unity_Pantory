using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 도로 네트워크에서 하역소 및 생산 건물로부터 자원이 전파되는 것을 관리하는 핸들러입니다.
/// 하역소와 생산 건물에서 출력된 자원이 도로를 따라 전파되어 각 도로 타일에 표시됩니다.
/// </summary>
public class DesignRunnerRoadHandler
{
    private readonly DesignRunner _manager;
    private readonly DesignRunnerGridHandler _gridHandler;
    private readonly Dictionary<Vector2Int, Dictionary<string, int>> _roadResources = new Dictionary<Vector2Int, Dictionary<string, int>>();
    private static readonly Vector2Int[] Neighbors = { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down };
    private DataManager DataManager => _manager.DataManager;
    
    public DesignRunnerRoadHandler(DesignRunner manager)
    {
        _manager = manager;
        _gridHandler = manager.GridGenHandler;
    }
    
    /// <summary>
    /// 건물 배치가 변경될 때마다 도로 자원 전파를 다시 계산합니다.
    /// </summary>
    public void RefreshRoadResources(List<BuildingState> buildingStates)
    {
        _roadResources.Clear();
        
        if (buildingStates == null || buildingStates.Count == 0)
        {
            UpdateRoadVisuals(buildingStates);
            return;
        }

        Dictionary<Vector2Int, BuildingState> gridMap = _gridHandler.BuildGridMap(buildingStates);
        foreach (BuildingState state in buildingStates)
        {
            BuildingData data = DataManager.Building.GetBuildingData(state.Id);
            if (data == null) continue;

            if (data.IsUnloadStation)
            {
                Vector2Int outputPos = new Vector2Int(state.positionX, state.positionY) + 
                    GridMathUtils.GetRotatedOffset(data.OutputPosition, state.rotation);

                if (!IsRoadAt(outputPos, gridMap)) continue;
                
                if (state.outputProductionIds != null && state.outputProductionIds.Count > 0)
                {
                    foreach (string resourceId in state.outputProductionIds)
                    {
                        if (string.IsNullOrEmpty(resourceId)) continue;
                        PropagateResourceFromBuilding(outputPos, resourceId, gridMap, 1);
                    }
                }
            }
        }

        HashSet<(int px, int py, string resourceId)> propagated = new HashSet<(int, int, string)>();
        bool changed = true;
        int maxIterations = buildingStates.Count;
        int iteration = 0;

        while (changed && iteration < maxIterations)
        {
            changed = false;
            iteration++;

            foreach (BuildingState state in buildingStates)
            {
                BuildingData data = DataManager.Building.GetBuildingData(state.Id);
                if (data == null || !data.IsProductionBuilding) continue;

                Vector2Int basePos = new Vector2Int(state.positionX, state.positionY);
                Vector2Int inputPos = basePos + GridMathUtils.GetRotatedOffset(data.InputPosition, state.rotation);
                Vector2Int outputPos = basePos + GridMathUtils.GetRotatedOffset(data.OutputPosition, state.rotation);

                if (!IsRoadAt(outputPos, gridMap))
                    continue;

                bool isRawMaterialFactory = data is RawMaterialFactoryData;

                if (!isRawMaterialFactory)
                {
                    if (!IsRoadAt(inputPos, gridMap))
                        continue;
                    if (!RoadNetworkAnalyzer.IsConnected(inputPos, true, false, gridMap, DataManager))
                        continue;
                    if (!AreAllInputResourcesAvailable(inputPos, state, gridMap))
                        continue;
                }

                if (state.outputProductionIds != null && state.outputProductionIds.Count > 0)
                {
                    foreach (string resourceId in state.outputProductionIds)
                    {
                        if (string.IsNullOrEmpty(resourceId)) continue;
                        (int, int, string) key = (state.positionX, state.positionY, resourceId);
                        if (propagated.Contains(key)) continue;
                        propagated.Add(key);
                        PropagateResourceFromBuilding(outputPos, resourceId, gridMap, 1);
                        changed = true;
                    }
                }
            }
        }
        
        UpdateRoadVisuals(buildingStates);
    }
    
    /// <summary>
    /// 생산 건물의 입력 위치에서 필요한 모든 자원이 도로에 있는지 확인합니다.
    /// </summary>
    private bool AreAllInputResourcesAvailable(Vector2Int inputPos, BuildingState state, Dictionary<Vector2Int, BuildingState> gridMap)
    {
        return _manager.CalculationHandler.AreAllInputResourcesAvailable(state, inputPos, gridMap);
    }
    
    /// <summary>
    /// 특정 위치의 도로에 특정 자원이 있는지 확인합니다.
    /// </summary>
    private bool IsResourceAvailableOnRoad(Vector2Int inputPos, string resourceId, Dictionary<Vector2Int, BuildingState> gridMap)
    {
        if (!IsRoadAt(inputPos, gridMap)) return false;
        return _roadResources.TryGetValue(inputPos, out Dictionary<string, int> counts) && counts.ContainsKey(resourceId) && counts[resourceId] > 0;
    }
    
    /// <summary>
    /// 건물 출력 위치에서 시작하여 도로를 따라 자원을 전파합니다.
    /// </summary>
    private void PropagateResourceFromBuilding(Vector2Int startPos, string resourceId, Dictionary<Vector2Int, BuildingState> gridMap, int amount = 1)
    {
        if (!IsRoadAt(startPos, gridMap)) return;
        
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
        
        queue.Enqueue(startPos);
        visited.Add(startPos);
        
        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            
            if (!_roadResources.ContainsKey(current))
            {
                _roadResources[current] = new Dictionary<string, int>();
            }
            if (!_roadResources[current].ContainsKey(resourceId))
            {
                _roadResources[current][resourceId] = 0;
            }
            _roadResources[current][resourceId] += amount;
            
            foreach (Vector2Int dir in Neighbors)
            {
                Vector2Int next = current + dir;
                if (visited.Contains(next)) continue;
                if (IsRoadAt(next, gridMap))
                {
                    visited.Add(next);
                    queue.Enqueue(next);
                }
            }
        }
    }
    
    /// <summary>
    /// 특정 위치에 도로가 있는지 확인합니다.
    /// </summary>
    private bool IsRoadAt(Vector2Int pos, Dictionary<Vector2Int, BuildingState> gridMap)
    {
        return gridMap.TryGetValue(pos, out BuildingState state) &&
               (DataManager.Building.GetBuildingData(state.Id)?.IsRoad ?? false);
    }
    
    /// <summary>
    /// 특정 도로 위치에 전파된 자원 ID 집합을 가져옵니다.
    /// </summary>
    public HashSet<string> GetResourcesAtRoad(Vector2Int roadPosition)
    {
        if (!_roadResources.TryGetValue(roadPosition, out Dictionary<string, int> counts) || counts == null)
            return new HashSet<string>();
        HashSet<string> set = new HashSet<string>();
        foreach (string id in counts.Keys)
        {
            if (counts[id] > 0) set.Add(id);
        }
        return set;
    }

    /// <summary>
    /// 특정 도로 위치에 전파된 자원별 수량을 가져옵니다.
    /// </summary>
    public Dictionary<string, int> GetResourceCountsAtRoad(Vector2Int roadPosition)
    {
        if (!_roadResources.TryGetValue(roadPosition, out Dictionary<string, int> counts) || counts == null)
            return new Dictionary<string, int>();
        return new Dictionary<string, int>(counts);
    }
    
    /// <summary>
    /// 도로 건물 오브젝트의 시각적 표현을 업데이트합니다.
    /// </summary>
    private void UpdateRoadVisuals(List<BuildingState> buildingStates)
    {
        foreach (BuildingState state in buildingStates)
        {
            BuildingData data = DataManager.Building.GetBuildingData(state.Id);
            if (!data.IsRoad) continue;
            
            GameObject buildingObj = _gridHandler.GetBuildingAtOrigin(new Vector2Int(state.positionX, state.positionY));
            if (buildingObj != null && buildingObj.TryGetComponent(out BuildingObject buildingComponent))
            {
                buildingComponent.SetupRoadResources(this);
            }
        }
    }
}
