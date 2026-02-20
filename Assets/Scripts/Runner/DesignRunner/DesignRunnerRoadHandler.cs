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
    private readonly Dictionary<Vector2Int, HashSet<string>> _roadResources = new Dictionary<Vector2Int, HashSet<string>>();
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
        
        // GridHandler의 그리드 맵 재사용
        Dictionary<Vector2Int, BuildingState> gridMap = _gridHandler.BuildGridMap(buildingStates);
        
        // 1단계: 하역소에서 자원 전파 (입력 자원 없음)
        foreach (BuildingState state in buildingStates)
        {
            BuildingData data = DataManager.Building.GetBuildingData(state.Id);
            if (data == null || !state.IsUnlocked(DataManager)) continue;
            
            if (data.IsUnloadStation)
            {
                Vector2Int outputPos = new Vector2Int(state.positionX, state.positionY) + 
                    GridMathUtils.GetRotatedOffset(data.OutputPosition, state.rotation);
                
                // 출력 위치가 정확히 도로인 경우에만 전파
                if (!IsRoadAt(outputPos, gridMap)) continue;
                
                if (state.outputProductionIds != null && state.outputProductionIds.Count > 0)
                {
                    foreach (string resourceId in state.outputProductionIds)
                    {
                        if (string.IsNullOrEmpty(resourceId)) continue;
                        PropagateResourceFromBuilding(outputPos, resourceId, gridMap);
                    }
                }
            }
        }
        
        // 2단계: 생산 건물에서 자원 전파 (입력 자원이 충족된 경우만)
        // 순환 의존성을 처리하기 위해 여러 번 반복
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
                if (data == null || !data.IsProductionBuilding || !state.IsUnlocked(DataManager)) continue;
                
                Vector2Int basePos = new Vector2Int(state.positionX, state.positionY);
                Vector2Int inputPos = basePos + GridMathUtils.GetRotatedOffset(data.InputPosition, state.rotation);
                Vector2Int outputPos = basePos + GridMathUtils.GetRotatedOffset(data.OutputPosition, state.rotation);
                
                // 출력 위치가 정확히 도로인지 확인
                if (!IsRoadAt(outputPos, gridMap))
                    continue;
                
                // 원자재 생산 건물인지 확인
                bool isRawMaterialFactory = data is RawMaterialFactoryData;
                
                if (!isRawMaterialFactory)
                {
                    // 원자재 생산 건물이 아닌 경우 입력 위치도 도로여야 함
                    if (!IsRoadAt(inputPos, gridMap))
                        continue;
                    
                    // 입력이 하역소로 연결되어 있고, 모든 입력 자원이 도로에 있는지 확인
                    if (!RoadNetworkAnalyzer.IsConnected(inputPos, true, false, gridMap, DataManager))
                        continue;
                    
                    if (!AreAllInputResourcesAvailable(inputPos, state, gridMap))
                        continue;
                }
                
                // 출력 위치가 상역소로 연결되어 있는 경우에만 전파 (원자재 생산 건물 포함)
                if (RoadNetworkAnalyzer.IsConnected(outputPos, false, true, gridMap, DataManager))
                {
                    if (state.outputProductionIds != null && state.outputProductionIds.Count > 0)
                    {
                        bool hasNewOutput = false;
                        foreach (string resourceId in state.outputProductionIds)
                        {
                            if (string.IsNullOrEmpty(resourceId)) continue;
                            
                            // 이미 전파된 자원인지 확인
                            if (!_roadResources.ContainsKey(outputPos) || 
                                !_roadResources[outputPos].Contains(resourceId))
                            {
                                PropagateResourceFromBuilding(outputPos, resourceId, gridMap);
                                hasNewOutput = true;
                            }
                        }
                        if (hasNewOutput) changed = true;
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
        // 입력 위치가 정확히 도로인지 확인
        if (!IsRoadAt(inputPos, gridMap)) return false;
        
        return _roadResources.ContainsKey(inputPos) && 
               _roadResources[inputPos].Contains(resourceId);
    }
    
    /// <summary>
    /// 건물 출력 위치에서 시작하여 도로를 따라 자원을 전파합니다.
    /// </summary>
    private void PropagateResourceFromBuilding(Vector2Int startPos, string resourceId, Dictionary<Vector2Int, BuildingState> gridMap)
    {
        // 시작 위치가 정확히 도로인지 확인
        if (!IsRoadAt(startPos, gridMap)) return;
        
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
        
        queue.Enqueue(startPos);
        visited.Add(startPos);
        
        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            
            // 현재 위치에 자원 추가
            if (!_roadResources.ContainsKey(current))
            {
                _roadResources[current] = new HashSet<string>();
            }
            _roadResources[current].Add(resourceId);
            
            // 인접한 도로로 전파
            foreach (Vector2Int dir in Neighbors)
            {
                Vector2Int next = current + dir;
                
                if (visited.Contains(next)) continue;
                
                // 다음 위치가 도로인지 확인
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
    /// 특정 도로 위치에 전파된 자원 리스트를 가져옵니다.
    /// </summary>
    public HashSet<string> GetResourcesAtRoad(Vector2Int roadPosition)
    {
        return _roadResources.TryGetValue(roadPosition, out HashSet<string> resources) 
            ? resources 
            : new HashSet<string>();
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
