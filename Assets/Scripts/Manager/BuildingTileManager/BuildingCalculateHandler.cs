using System.Collections.Generic;
using UnityEngine;

public class BuildingCalculateHandler
{
    private readonly BuildingTileManager _buildingTileManager;

    // 고정 ID (데이터 자산과 일치해야 함)
    private const string LoadBuildingId = "load";
    private const string UnloadBuildingId = "unload";
    private const string RoadBuildingId = "road";

    public BuildingCalculateHandler(BuildingTileManager buildingTileManager)
    {
        _buildingTileManager = buildingTileManager;
    }

    /// <summary>
    /// 현재 스레드의 빌딩 상태를 기반으로 간단한 산출량을 계산합니다.
    /// 유효한 입출력 연결(도로 경로) 여부를 검증하여, 연결이 유효한 생산 건물 수를 반환합니다.
    /// </summary>
    public int CalculateThreadOutputs(string threadId)
    {
        if (string.IsNullOrEmpty(threadId))
            return 0;

        var dataManager = _buildingTileManager.DataManager;
        if (dataManager == null)
            return 0;

        var buildingStates = dataManager.GetBuildingStates(threadId);
        if (buildingStates == null || buildingStates.Count == 0)
            return 0;

        int validProducerCount = 0;

        foreach (var state in buildingStates)
        {
            BuildingData data = dataManager.GetBuildingData(state.buildingId);
            if (data == null)
                continue;

            // 생산 건물만 대상 (ProductionBuildingData 또는 그 파생 클래스)
            if (!data.IsProductionBuilding)
                continue;

            // 입/출력 엔드포인트 계산 (회전 적용)
            Vector2Int inputEndpoint = GetEndpointGridPosition(state.position, data.InputPosition, state.rotation);
            Vector2Int outputEndpoint = GetEndpointGridPosition(state.position, data.OutputPosition, state.rotation);

            // BuildingData의 특화 속성을 사용하여 하역소/상역소 찾기
            bool inputOk = IsConnectedViaRoadToTarget(inputEndpoint, threadId, isUnloadStation: true);
            bool outputOk = IsConnectedViaRoadToTarget(outputEndpoint, threadId, isLoadStation: true);

            if (inputOk && outputOk)
            {
                validProducerCount++;
            }
        }

        Debug.Log($"[BuildingCalculateHandler] Thread '{threadId}' valid producers: {validProducerCount}");
        return validProducerCount;
    }

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
    /// 시작 지점에서 도로(Road)만 따라 탐색하여 목표 건물(Load/Unload)에 도달 가능한지 검사합니다.
    /// Input/Output 마커가 맞닿아 있어도 빈 땅이면 연결되지 않고, 반드시 도로를 통해서만 연결됩니다.
    /// </summary>
    private bool IsConnectedViaRoadToTarget(Vector2Int startPos, string threadId, bool isUnloadStation = false, bool isLoadStation = false)
    {
        var dataManager = _buildingTileManager.DataManager;
        if (dataManager == null)
            return false;

        var buildingStates = dataManager.GetBuildingStates(threadId);
        if (buildingStates == null)
            return false;

        // BFS 탐색
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

        // 시작점 자체가 도로인지 확인
        BuildingData buildingDataAtStart = GetBuildingDataAt(startPos, buildingStates, dataManager);
        if (buildingDataAtStart != null && buildingDataAtStart.IsRoad)
        {
            queue.Enqueue(startPos);
            visited.Add(startPos);
        }
        else
        {
            // 시작점이 도로가 아니면, 인접한 도로에서만 시작
            // 빈 땅이거나 다른 건물이면 연결 불가
            foreach (var dir in _neighbors)
            {
                Vector2Int next = startPos + dir;
                BuildingData buildingDataAtNext = GetBuildingDataAt(next, buildingStates, dataManager);
                
                if (buildingDataAtNext != null && buildingDataAtNext.IsRoad)
                {
                    queue.Enqueue(next);
                    visited.Add(next);
                }
                // 빈 땅이거나 다른 건물이면 무시 (도로가 아니면 연결 불가)
            }
        }

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();

            foreach (var dir in _neighbors)
            {
                Vector2Int next = current + dir;
                if (visited.Contains(next))
                    continue;

                BuildingData buildingData = GetBuildingDataAt(next, buildingStates, dataManager);
                if (buildingData != null && buildingData.IsRoad)
                {
                    queue.Enqueue(next);
                    visited.Add(next);
                }
                else if (buildingData != null)
                {
                    // 목표 건물 타입 확인 (도로를 통해 도달한 경우만)
                    if ((isUnloadStation && buildingData.IsUnloadStation) ||
                        (isLoadStation && buildingData.IsLoadStation))
                    {
                        return true;
                    }
                }
                // 빈 땅이면 무시 (연결 불가)
            }
        }

        return false;
    }

    private static readonly Vector2Int[] _neighbors = new Vector2Int[]
    {
        new Vector2Int(1, 0),
        new Vector2Int(-1, 0),
        new Vector2Int(0, 1),
        new Vector2Int(0, -1)
    };

    /// <summary>
    /// 해당 그리드 좌표에 배치된 건물 데이터를 반환합니다. 없으면 null.
    /// </summary>
    private BuildingData GetBuildingDataAt(Vector2Int gridPos, List<BuildingState> buildingStates, GameDataManager dataManager)
    {
        // 정확히 해당 좌표를 포함하는 빌딩 상태를 찾음 (회전 고려한 영역 포함 검사)
        foreach (var state in buildingStates)
        {
            BuildingData data = dataManager.GetBuildingData(state.buildingId);
            if (data == null)
                continue;

            Vector2Int size = GetRotatedSize(data.size, state.rotation);
            if (gridPos.x >= state.position.x && gridPos.x < state.position.x + size.x &&
                gridPos.y >= state.position.y && gridPos.y < state.position.y + size.y)
            {
                return data;
            }
        }

        return null;
    }

    /// <summary>
    /// 회전에 따라 건물 크기를 계산합니다. (TileManager의 로직과 동일하게 유지)
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

    /// <summary>
    /// Thread의 총 유지비를 계산합니다.
    /// </summary>
    public int CalculateTotalMaintenanceCost(string threadId)
    {
        if (string.IsNullOrEmpty(threadId))
            return 0;

        var dataManager = _buildingTileManager.DataManager;
        if (dataManager == null)
            return 0;

        var buildingStates = dataManager.GetBuildingStates(threadId);
        if (buildingStates == null || buildingStates.Count == 0)
            return 0;

        int totalMaintenance = 0;

        foreach (var state in buildingStates)
        {
            BuildingData data = dataManager.GetBuildingData(state.buildingId);
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
    public List<string> CollectInputProductionIds(string threadId)
    {
        List<string> inputIds = new List<string>();

        if (string.IsNullOrEmpty(threadId))
            return inputIds;

        var dataManager = _buildingTileManager.DataManager;
        if (dataManager == null)
            return inputIds;

        var buildingStates = dataManager.GetBuildingStates(threadId);
        if (buildingStates == null || buildingStates.Count == 0)
            return inputIds;

        HashSet<string> uniqueIds = new HashSet<string>();

        foreach (var state in buildingStates)
        {
            if (state.inputProductionIds != null && state.inputProductionIds.Count > 0)
            {
                foreach (var resourceId in state.inputProductionIds)
                {
                    if (!string.IsNullOrEmpty(resourceId) && !uniqueIds.Contains(resourceId))
                    {
                        uniqueIds.Add(resourceId);
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
    public List<string> CollectOutputProductionIds(string threadId)
    {
        List<string> outputIds = new List<string>();

        if (string.IsNullOrEmpty(threadId))
            return outputIds;

        var dataManager = _buildingTileManager.DataManager;
        if (dataManager == null)
            return outputIds;

        var buildingStates = dataManager.GetBuildingStates(threadId);
        if (buildingStates == null || buildingStates.Count == 0)
            return outputIds;

        HashSet<string> uniqueIds = new HashSet<string>();

        foreach (var state in buildingStates)
        {
            if (state.outputProductionIds != null && state.outputProductionIds.Count > 0)
            {
                foreach (var resourceId in state.outputProductionIds)
                {
                    if (!string.IsNullOrEmpty(resourceId) && !uniqueIds.Contains(resourceId))
                    {
                        uniqueIds.Add(resourceId);
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
    public void CalculateProductionChain(string threadId, out List<string> inputResourceIds, out List<string> outputResourceIds, out Dictionary<string, int> outputResourceCounts)
    {
        inputResourceIds = new List<string>();
        outputResourceIds = new List<string>();
        outputResourceCounts = new Dictionary<string, int>();

        if (string.IsNullOrEmpty(threadId))
            return;

        var dataManager = _buildingTileManager.DataManager;
        if (dataManager == null)
            return;

        var buildingStates = dataManager.GetBuildingStates(threadId);
        if (buildingStates == null || buildingStates.Count == 0)
            return;

        // 하역소 위치 찾기
        List<Vector2Int> unloadStations = new List<Vector2Int>();
        List<Vector2Int> loadStations = new List<Vector2Int>();
        Dictionary<Vector2Int, BuildingState> positionToState = new Dictionary<Vector2Int, BuildingState>();

        foreach (var state in buildingStates)
        {
            BuildingData data = dataManager.GetBuildingData(state.buildingId);
            if (data == null)
                continue;

            // 건물이 차지하는 모든 타일 기록
            Vector2Int size = GetRotatedSize(data.size, state.rotation);
            for (int x = 0; x < size.x; x++)
            {
                for (int y = 0; y < size.y; y++)
                {
                    Vector2Int pos = state.position + new Vector2Int(x, y);
                    if (!positionToState.ContainsKey(pos))
                    {
                        positionToState[pos] = state;
                    }
                }
            }

            if (data.IsUnloadStation)
            {
                unloadStations.Add(state.position);
            }
            else if (data.IsLoadStation)
            {
                loadStations.Add(state.position);
            }
        }

        if (unloadStations.Count == 0 || loadStations.Count == 0)
            return;

        // 각 생산 건물에서 시작하여 상역소까지 도달 가능한지 확인하고, 하역소까지 역으로 추적
        HashSet<string> reachableOutputResources = new HashSet<string>();
        HashSet<string> requiredInputResources = new HashSet<string>();
        Dictionary<string, int> resourceCounts = new Dictionary<string, int>(); // 자원별 생산 건물 개수

        foreach (var state in buildingStates)
        {
            BuildingData data = dataManager.GetBuildingData(state.buildingId);
            if (data == null || !data.IsProductionBuilding)
                continue;

            // 생산 건물의 출력이 상역소까지 도달 가능한지 확인
            Vector2Int outputEndpoint = GetEndpointGridPosition(state.position, data.OutputPosition, state.rotation);
            bool canReachLoadStation = CanReachLoadStationFrom(outputEndpoint, threadId, buildingStates, positionToState, dataManager);

            if (canReachLoadStation)
            {
                // 출력 자원 추가 및 생산 건물 개수 카운트
                if (state.outputProductionIds != null)
                {
                    foreach (var outputId in state.outputProductionIds)
                    {
                        if (!string.IsNullOrEmpty(outputId))
                        {
                            reachableOutputResources.Add(outputId);
                            // 해당 자원을 생산하는 건물 개수 증가
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

                // 입력 자원 추적
                Vector2Int inputEndpoint = GetEndpointGridPosition(state.position, data.InputPosition, state.rotation);
                bool canReachUnloadStation = CanReachUnloadStationFrom(inputEndpoint, threadId, buildingStates, positionToState, dataManager);

                if (canReachUnloadStation)
                {
                    // 입력 자원이 설정되어 있으면 추가
                    if (state.inputProductionIds != null && state.inputProductionIds.Count > 0)
                    {
                        foreach (var inputId in state.inputProductionIds)
                        {
                            if (!string.IsNullOrEmpty(inputId))
                            {
                                requiredInputResources.Add(inputId);
                            }
                        }
                    }
                    else
                    {
                        // 입력 자원이 설정되지 않았으면, 출력 자원의 requirements에서 추출
                        if (state.outputProductionIds != null)
                        {
                            foreach (var outputId in state.outputProductionIds)
                            {
                                ResourceEntry outputResource = dataManager.GetResourceEntry(outputId);
                                if (outputResource != null && outputResource.resourceData != null)
                                {
                                    if (outputResource.resourceData.requirements != null)
                                    {
                                        foreach (var requirement in outputResource.resourceData.requirements)
                                        {
                                            if (requirement.resource != null && !string.IsNullOrEmpty(requirement.resource.id))
                                            {
                                                requiredInputResources.Add(requirement.resource.id);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        inputResourceIds = new List<string>(requiredInputResources);
        outputResourceIds = new List<string>(reachableOutputResources);
        outputResourceCounts = resourceCounts;
    }

    /// <summary>
    /// 특정 위치에서 상역소까지 도로를 통해 도달 가능한지 확인합니다.
    /// </summary>
    private bool CanReachLoadStationFrom(Vector2Int startPos, string threadId, List<BuildingState> buildingStates, 
        Dictionary<Vector2Int, BuildingState> positionToState, GameDataManager dataManager)
    {
        return IsConnectedViaRoadToTarget(startPos, threadId, isUnloadStation: false, isLoadStation: true);
    }

    /// <summary>
    /// 특정 위치에서 하역소까지 도로를 통해 도달 가능한지 확인합니다.
    /// </summary>
    private bool CanReachUnloadStationFrom(Vector2Int startPos, string threadId, List<BuildingState> buildingStates,
        Dictionary<Vector2Int, BuildingState> positionToState, GameDataManager dataManager)
    {
        return IsConnectedViaRoadToTarget(startPos, threadId, isUnloadStation: true, isLoadStation: false);
    }
}