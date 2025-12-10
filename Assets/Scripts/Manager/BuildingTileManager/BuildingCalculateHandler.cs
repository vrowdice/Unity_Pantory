using System.Collections.Generic;
using UnityEngine;
using System;

/// <summary>
/// 건물의 생산 체인, 유지비, 유효성 등 복잡한 계산을 처리하는 핸들러.
/// 핵심 로직은 도로를 통한 연결성(Pathfinding) 검증에 있습니다.
/// </summary>
public class BuildingCalculateHandler
{
    private readonly BuildingTileManager _buildingTileManager;
    private GameDataManager DataManager => _buildingTileManager.DataManager;

    // 인접 타일 탐색을 위한 방향 벡터
    private static readonly Vector2Int[] _neighbors = new Vector2Int[]
    {
        new Vector2Int(1, 0),
        new Vector2Int(-1, 0),
        new Vector2Int(0, 1),
        new Vector2Int(0, -1)
    };

    public BuildingCalculateHandler(BuildingTileManager buildingTileManager)
    {
        _buildingTileManager = buildingTileManager;
    }

    //---------------------------------------------------------

    #region 생산 유효성 및 간단 산출량 계산

    /// <summary>
    /// 현재 스레드의 빌딩 상태를 기반으로 간단한 산출량을 계산합니다.
    /// 유효한 입출력 연결(도로 경로) 여부를 검증하여, 연결이 유효한 생산 건물 수를 반환합니다.
    /// </summary>
    public int CalculateThreadOutputs(string threadId, List<BuildingState> buildingStates = null)
    {
        if (string.IsNullOrEmpty(threadId) || DataManager == null)
            return 0;

        // buildingStates가 null이면 현재 편집 중인 데이터 가져오기 (Temp-First)
        if (buildingStates == null)
        {
            buildingStates = _buildingTileManager.GetCurrentBuildingStates();
        }

        if (buildingStates == null || buildingStates.Count == 0)
            return 0;

        int validProducerCount = 0;

        foreach (var state in buildingStates)
        {
            BuildingData data = DataManager.Building.GetBuildingData(state.buildingId);
            if (data == null || !data.IsProductionBuilding)
                continue;

            // 입/출력 엔드포인트 그리드 좌표 계산 (회전 적용)
            Vector2Int inputEndpoint = GetEndpointGridPosition(new Vector2Int(state.positionX, state.positionY), data.InputPosition, state.rotation);
            Vector2Int outputEndpoint = GetEndpointGridPosition(new Vector2Int(state.positionX, state.positionY), data.OutputPosition, state.rotation);

            // 입력 엔드포인트가 하역소까지 도로로 연결되었는지 검증
            bool inputOk = IsConnectedViaRoadToTarget(inputEndpoint, isUnloadStation: true);
            // 출력 엔드포인트가 상역소까지 도로로 연결되었는지 검증
            bool outputOk = IsConnectedViaRoadToTarget(outputEndpoint, isLoadStation: true);

            if (inputOk && outputOk)
            {
                validProducerCount++;
            }
        }

        Debug.Log($"[BuildingCalculateHandler] Thread '{threadId}' valid producers: {validProducerCount}");
        return validProducerCount;
    }

    #endregion

    //---------------------------------------------------------

    #region 경로 탐색 (Pathfinding) 유틸리티

    /// <summary>
    /// 시작 지점에서 도로(Road)만 따라 탐색하여 목표 건물(Load/Unload)에 도달 가능한지 검사합니다.
    /// 이 메서드는 BFS(Breadth-First Search)를 사용하여 최단 경로 탐색을 수행합니다.
    /// </summary>
    private bool IsConnectedViaRoadToTarget(Vector2Int startPos, bool isUnloadStation = false, bool isLoadStation = false)
    {
        // 경로를 찾으면 true, 못 찾으면 false
        // 성능 최적화: 경로 리스트 생성을 피하고 싶다면 별도 로직이 필요하지만, 
        // 코드 중복 제거를 우선하여 FindPath를 재사용합니다.
        return FindPath(startPos, isUnloadStation, isLoadStation) != null;
    }

    /// <summary>
    /// 실제 경로 리스트가 필요할 때 사용합니다. (기존 GetPathViaRoad 대체)
    /// </summary>
    public List<Vector2Int> GetPathViaRoad(Vector2Int startPos, bool isUnloadStation, bool isLoadStation)
    {
        return FindPath(startPos, isUnloadStation, isLoadStation);
    }

    /// <summary>
    /// [핵심 엔진] BFS를 사용하여 도로를 따라 목표까지의 경로를 탐색합니다.
    /// </summary>
    private List<Vector2Int> FindPath(Vector2Int startPos, bool findUnloadStation, bool findLoadStation)
    {
        if (DataManager == null) return null;

        var buildingStates = _buildingTileManager.GetCurrentBuildingStates();
        if (buildingStates == null) return null;

        // BFS 초기화
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
        Dictionary<Vector2Int, Vector2Int> parentMap = new Dictionary<Vector2Int, Vector2Int>();

        // 시작점 찾기 (기존 IsConnectedViaRoadToTarget의 더 꼼꼼한 로직을 채택)
        // 시작점이 바로 도로라면 큐에 넣고, 아니라면 인접한 도로들을 큐에 넣습니다.
        BuildingData startData = GetBuildingDataAt(startPos, buildingStates, DataManager);
        if (startData != null && startData.IsRoad)
        {
            queue.Enqueue(startPos);
            visited.Add(startPos);
            parentMap[startPos] = startPos;
        }
        else
        {
            // 시작점이 건물의 입출력 포트(빈 땅)일 수 있으므로 인접한 도로를 찾습니다.
            foreach (var dir in _neighbors)
            {
                Vector2Int next = startPos + dir;
                BuildingData nextData = GetBuildingDataAt(next, buildingStates, DataManager);
                if (nextData != null && nextData.IsRoad)
                {
                    if (!visited.Contains(next))
                    {
                        queue.Enqueue(next);
                        visited.Add(next);
                        parentMap[next] = next; // 시작점의 대리인으로 설정
                    }
                }
            }
        }

        Vector2Int targetPos = new Vector2Int(-1, -1);
        bool found = false;

        // BFS 루프
        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();

            foreach (var dir in _neighbors)
            {
                Vector2Int next = current + dir;
                if (visited.Contains(next)) continue;

                BuildingData nextData = GetBuildingDataAt(next, buildingStates, DataManager);
                if (nextData == null) continue;

                // 1. 도로인 경우: 계속 이동
                if (nextData.IsRoad)
                {
                    visited.Add(next);
                    queue.Enqueue(next);
                    parentMap[next] = current;
                }
                // 2. 목표 건물인 경우: 탐색 종료
                else if ((findUnloadStation && nextData.IsUnloadStation) || 
                         (findLoadStation && nextData.IsLoadStation))
                {
                    visited.Add(next);
                    parentMap[next] = current;
                    targetPos = next;
                    found = true;
                    break; 
                }
            }
            if (found) break;
        }

        if (!found) return null;

        // 경로 역추적 (Backtracking)
        List<Vector2Int> path = new List<Vector2Int>();
        Vector2Int curr = targetPos;

        // 시작점의 대리인(혹은 시작점 자체)에 도달할 때까지 추적
        while (parentMap.ContainsKey(curr) && parentMap[curr] != curr)
        {
            path.Add(curr);
            curr = parentMap[curr];
        }
        path.Add(curr); // 루프 빠져나온 마지막 지점(도로 시작점) 추가

        // 만약 startPos가 도로가 아니어서 인접 도로에서 시작했다면,
        // path에는 인접 도로까지만 들어있으므로 startPos는 포함되지 않을 수 있음.
        // 필요하다면 path.Add(startPos); 를 할 수도 있지만, 
        // 보통 도로 위의 이동 경로만 필요하므로 여기서는 유지합니다.
        
        path.Reverse(); // 정방향으로 뒤집기
        return path;
    }

    /// <summary>
    /// 해당 그리드 좌표에 배치된 건물 데이터를 반환합니다. 없으면 null.
    /// (멀티 타일 건물도 고려하여 점유 영역을 검사합니다.)
    /// </summary>
    private BuildingData GetBuildingDataAt(Vector2Int gridPos, List<BuildingState> buildingStates, GameDataManager dataManager)
    {
        // 정확히 해당 좌표를 포함하는 빌딩 상태를 찾음 (회전 고려한 영역 포함 검사)
        foreach (var state in buildingStates)
        {
            BuildingData data = dataManager.Building.GetBuildingData(state.buildingId);
            if (data == null)
                continue;

            Vector2Int size = GetRotatedSize(data.size, state.rotation);
            if (gridPos.x >= state.positionX && gridPos.x < state.positionX + size.x &&
                gridPos.y >= state.positionY && gridPos.y < state.positionY + size.y)
            {
                return data;
            }
        }
        return null;
    }

    #endregion

    //---------------------------------------------------------

    #region 자원 및 비용 계산

    /// <summary>
    /// Thread의 총 유지비를 계산합니다.
    /// </summary>
    public int CalculateTotalMaintenanceCost(string threadId, List<BuildingState> buildingStates = null)
    {
        if (string.IsNullOrEmpty(threadId) || DataManager == null)
            return 0;

        // 항상 현재 편집 상태를 사용 (ThreadManager에서 처리)
        buildingStates = _buildingTileManager.GetCurrentBuildingStates();

        if (buildingStates == null || buildingStates.Count == 0)
            return 0;

        int totalMaintenance = 0;

        foreach (var state in buildingStates)
        {
            BuildingData data = DataManager.Building.GetBuildingData(state.buildingId);
            if (data != null)
            {
                totalMaintenance += data.baseMaintenanceCost;
            }
        }

        return totalMaintenance;
    }

    /// <summary>
    /// Thread의 입력 생산 자원 리스트를 수집합니다 (중복 제거).
    /// </summary>
    public List<string> CollectInputProductionIds(string threadId, List<BuildingState> buildingStates = null)
    {
        List<string> inputIds = new List<string>();

        if (string.IsNullOrEmpty(threadId) || DataManager == null)
            return inputIds;

        // buildingStates가 null이면 현재 편집 상태를 사용
        if (buildingStates == null)
        {
            buildingStates = _buildingTileManager.GetCurrentBuildingStates();
        }

        if (buildingStates == null || buildingStates.Count == 0)
            return inputIds;

        HashSet<string> uniqueIds = new HashSet<string>();

        foreach (var state in buildingStates)
        {
            if (state.inputProductionIds != null)
            {
                foreach (var resourceId in state.inputProductionIds)
                {
                    if (!string.IsNullOrEmpty(resourceId) && uniqueIds.Add(resourceId))
                    {
                        inputIds.Add(resourceId);
                    }
                }
            }
        }
        return inputIds;
    }

    /// <summary>
    /// Thread의 출력 생산 자원 리스트를 수집합니다 (중복 제거).
    /// </summary>
    public List<string> CollectOutputProductionIds(string threadId, List<BuildingState> buildingStates = null)
    {
        List<string> outputIds = new List<string>();

        if (string.IsNullOrEmpty(threadId) || DataManager == null)
            return outputIds;

        // buildingStates가 null이면 현재 편집 상태를 사용
        if (buildingStates == null)
        {
            buildingStates = _buildingTileManager.GetCurrentBuildingStates();
        }

        if (buildingStates == null || buildingStates.Count == 0)
            return outputIds;

        HashSet<string> uniqueIds = new HashSet<string>();

        foreach (var state in buildingStates)
        {
            if (state.outputProductionIds != null)
            {
                foreach (var resourceId in state.outputProductionIds)
                {
                    if (!string.IsNullOrEmpty(resourceId) && uniqueIds.Add(resourceId))
                    {
                        outputIds.Add(resourceId);
                    }
                }
            }
        }
        return outputIds;
    }

    /// <summary>
    /// 생산 체인을 추적하여 하역소에서 시작하는 입력 자원과 상역소까지 연결된 최종 출력 자원을 계산합니다.
    /// 하역소 → (도로) → 생산건물들 → (도로) → 상역소 경로를 추적합니다.
    /// </summary>
    public void CalculateProductionChain(string threadId, List<BuildingState> buildingStates, out List<string> inputResourceIds, out Dictionary<string, int> inputResourceCounts, out List<string> outputResourceIds, out Dictionary<string, int> outputResourceCounts)
    {
        inputResourceIds = new List<string>();
        inputResourceCounts = new Dictionary<string, int>();
        outputResourceIds = new List<string>();
        outputResourceCounts = new Dictionary<string, int>();

        if (string.IsNullOrEmpty(threadId) || DataManager == null)
            return;

        // buildingStates가 null이면 현재 편집 상태를 사용
        if (buildingStates == null)
        {
            buildingStates = _buildingTileManager.GetCurrentBuildingStates();
        }

        if (buildingStates == null || buildingStates.Count == 0)
            return;

        HashSet<string> reachableOutputResources = new HashSet<string>();
        Dictionary<string, int> requiredInputResources = new Dictionary<string, int>();
        Dictionary<string, int> resourceCounts = new Dictionary<string, int>();

        foreach (var state in buildingStates)
        {
            BuildingData data = DataManager.Building.GetBuildingData(state.buildingId);
            if (data == null || !data.IsProductionBuilding)
                continue;

            // 1. 출력 경로 검증: 생산 건물의 출력이 상역소까지 도로로 연결되었는지 확인
            Vector2Int outputEndpoint = GetEndpointGridPosition(new Vector2Int(state.positionX, state.positionY), data.OutputPosition, state.rotation);
            bool canReachLoadStation = IsConnectedViaRoadToTarget(outputEndpoint, isLoadStation: true);

            if (canReachLoadStation)
            {
                // 유효한 출력 자원 카운트
                if (state.outputProductionIds != null)
                {
                    foreach (var outputId in state.outputProductionIds)
                    {
                        if (!string.IsNullOrEmpty(outputId))
                        {
                            reachableOutputResources.Add(outputId);

                            if (resourceCounts.ContainsKey(outputId))
                            {
                                resourceCounts[outputId]++;
                            }
                            else
                            {
                                resourceCounts[outputId] = 1;
                            }
                        }
                    }
                }
            }

            // 2. 입력 경로 검증: 생산 건물의 입력이 하역소까지 도로로 연결되었는지 확인
            Vector2Int inputEndpoint = GetEndpointGridPosition(new Vector2Int(state.positionX, state.positionY), data.InputPosition, state.rotation);
            bool canReachUnloadStation = IsConnectedViaRoadToTarget(inputEndpoint, isUnloadStation: true);

            if (canReachUnloadStation)
            {
                // 필요한 입력 자원 추적 (BuildingState에 설정된 값 우선 사용)
                var currentInputIds = state.inputProductionIds;

                // inputProductionIds가 없으면, 출력 자원의 요구사항(requirements)에서 추론
                if (currentInputIds == null || currentInputIds.Count == 0)
                {
                    if (state.outputProductionIds != null)
                    {
                        foreach (var outputId in state.outputProductionIds)
                        {
                            ResourceEntry outputResource = DataManager.Resource.GetResourceEntry(outputId);
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
        }

        inputResourceIds = new List<string>(requiredInputResources.Keys);
        inputResourceCounts = requiredInputResources;
        outputResourceIds = new List<string>(reachableOutputResources);
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

    #endregion

    //---------------------------------------------------------

    #region 좌표 및 회전 유틸리티

    /// <summary>
    /// state.position 에서 offset 을 회전에 맞춰 적용하여 IO 엔드포인트 그리드 좌표를 구합니다.
    /// rotation 은 0,1,2,3 (90도 단위) 기준.
    /// </summary>
    private Vector2Int GetEndpointGridPosition(Vector2Int basePos, Vector2Int offset, int rotation)
    {
        rotation = rotation % 4;
        Vector2Int rotated = offset;

        // 90도 회전: (x,y) -> (y, -x)
        if (rotation == 1)
        {
            rotated = new Vector2Int(offset.y, -offset.x);
        }
        // 180도 회전: (x,y) -> (-x, -y)
        else if (rotation == 2)
        {
            rotated = new Vector2Int(-offset.x, -offset.y);
        }
        // 270도 회전: (x,y) -> (-y, x)
        else if (rotation == 3)
        {
            rotated = new Vector2Int(-offset.y, offset.x);
        }

        return basePos + rotated;
    }

    /// <summary>
    /// 회전에 따라 건물 크기를 계산합니다.
    /// </summary>
    private Vector2Int GetRotatedSize(Vector2Int size, int rotation)
    {
        rotation = rotation % 4;
        if (rotation == 1 || rotation == 3)
        {
            return new Vector2Int(size.y, size.x);
        }
        return size;
    }

    // 다음 두 메서드는 IsConnectedViaRoadToTarget의 명확한 호출을 위해 남겨둡니다.
    private bool CanReachLoadStationFrom(Vector2Int startPos, string threadId, List<BuildingState> buildingStates, Dictionary<Vector2Int, BuildingState> positionToState, GameDataManager dataManager)
    {
        return IsConnectedViaRoadToTarget(startPos, isUnloadStation: false, isLoadStation: true);
    }

    private bool CanReachUnloadStationFrom(Vector2Int startPos, string threadId, List<BuildingState> buildingStates, Dictionary<Vector2Int, BuildingState> positionToState, GameDataManager dataManager)
    {
        return IsConnectedViaRoadToTarget(startPos, isUnloadStation: true, isLoadStation: false);
    }

    #endregion
}