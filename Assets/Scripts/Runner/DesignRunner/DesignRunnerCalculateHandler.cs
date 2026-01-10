using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/// <summary>
/// 건물의 생산 체인, 유지비, 도로 연결성(Pathfinding) 검증을 담당하는 핸들러 클래스입니다.
/// </summary>
public class DesignRunnerCalculateHandler
{
    private readonly DesignRunner _manager;
    private readonly List<BuildingState> _states;
    private readonly Dictionary<Vector2Int, BuildingState> _gridMap;

    private DataManager DataManager => _manager.DataManager;

    private static readonly Vector2Int[] Neighbors = {
        Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down
    };

    /// <summary>
    /// 핸들러를 초기화하고 리소스 탐색 성능 향상을 위한 그리드 맵을 구축합니다.
    /// </summary>
    /// <param name="manager">빌딩 타일 매니저 참조</param>
    /// <param name="currentBuildingStates">현재 배치된 건물들의 상태 리스트</param>
    public DesignRunnerCalculateHandler(DesignRunner manager, List<BuildingState> currentBuildingStates)
    {
        _manager = manager;
        _states = currentBuildingStates ?? new List<BuildingState>();
        _gridMap = BuildGridMap(_states);
    }

    /// <summary>
    /// 좌표 기반 탐색 속도를 높이기 위해 멀티 타일 영역을 포함한 그리드 맵을 생성합니다.
    /// </summary>
    private Dictionary<Vector2Int, BuildingState> BuildGridMap(List<BuildingState> states)
    {
        Dictionary<Vector2Int, BuildingState> map = new Dictionary<Vector2Int, BuildingState>();
        foreach (BuildingState state in states)
        {
            BuildingData data = DataManager.Building.GetBuildingData(state.buildingId);
            if (data == null) continue;

            Vector2Int size = GetRotatedSize(data.size, state.rotation);
            for (int x = 0; x < size.x; x++)
            {
                for (int y = 0; y < size.y; y++)
                {
                    Vector2Int pos = new Vector2Int(state.positionX + x, state.positionY + y);
                    map[pos] = state;
                }
            }
        }
        return map;
    }

    /// <summary>
    /// 도로와 연결된 유효한 생산 건물의 총 개수를 계산합니다.
    /// </summary>
    public int CalculateThreadOutputs(string threadId, List<BuildingState> customStates = null)
    {
        if (string.IsNullOrEmpty(threadId) || DataManager == null) return 0;

        List<BuildingState> statesToUse = customStates ?? _states;
        int validProducerCount = 0;

        foreach (BuildingState state in statesToUse)
        {
            BuildingData data = DataManager.Building.GetBuildingData(state.buildingId);
            if (data == null || !data.IsProductionBuilding) continue;
            if (!state.IsUnlocked(DataManager)) continue;

            Vector2Int inPos = GetEndpointPosition(state, data.InputPosition);
            Vector2Int outPos = GetEndpointPosition(state, data.OutputPosition);

            if (IsConnectedViaRoad(inPos, true, false) && IsConnectedViaRoad(outPos, false, true))
            {
                validProducerCount++;
            }
        }

        return validProducerCount;
    }

    /// <summary>
    /// 연구된 건물을 기준으로 총 유지비를 계산합니다.
    /// </summary>
    public int CalculateTotalMaintenanceCost(string threadId, List<BuildingState> customStates = null)
    {
        List<BuildingState> statesToUse = customStates ?? _states;

        int cost = 0;
        int requiredEmployees = 0;
        Dictionary<string, int> inputCounts;
        Dictionary<string, int> outputCounts;

        BuildingCalculationUtility.ExecuteCoreCalculation(
            DataManager,
            statesToUse,
            out cost,
            out requiredEmployees,
            out inputCounts,
            out outputCounts
        );

        return cost;
    }

    /// <summary>
    /// 특정 위치에서 도로를 통해 하역소나 상역소까지의 연결 여부를 확인합니다.
    /// </summary>
    private bool IsConnectedViaRoad(Vector2Int startPos, bool findUnload = false, bool findLoad = false)
    {
        return FindPath(startPos, findUnload, findLoad) != null;
    }

    /// <summary>
    /// BFS 알고리즘을 사용하여 목적지(하역소/상역소)까지의 최단 경로를 탐색합니다.
    /// </summary>
    private List<Vector2Int> FindPath(Vector2Int startPos, bool findUnload, bool findLoad)
    {
        if (DataManager == null) return null;

        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
        Dictionary<Vector2Int, Vector2Int> parentMap = new Dictionary<Vector2Int, Vector2Int>();

        if (IsRoadAt(startPos))
        {
            EnqueueNode(startPos, startPos, queue, visited, parentMap);
        }
        else
        {
            foreach (Vector2Int dir in Neighbors)
            {
                Vector2Int neighbor = startPos + dir;
                if (IsRoadAt(neighbor))
                    EnqueueNode(neighbor, neighbor, queue, visited, parentMap);
            }
        }

        Vector2Int targetPos = new Vector2Int(-1, -1);
        bool found = false;

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();

            foreach (Vector2Int dir in Neighbors)
            {
                Vector2Int next = current + dir;
                if (visited.Contains(next)) continue;

                BuildingData nextData = GetBuildingDataAt(next);
                if (nextData == null) continue;

                if (nextData.IsRoad)
                {
                    EnqueueNode(next, current, queue, visited, parentMap);
                }
                else if ((findUnload && nextData.IsUnloadStation) || (findLoad && nextData.IsLoadStation))
                {
                    parentMap[next] = current;
                    targetPos = next;
                    found = true;
                    break;
                }
            }
            if (found) break;
        }

        return found ? ReconstructPath(parentMap, targetPos) : null;
    }

    private void EnqueueNode(Vector2Int node, Vector2Int parent, Queue<Vector2Int> queue, HashSet<Vector2Int> visited, Dictionary<Vector2Int, Vector2Int> parentMap)
    {
        if (visited.Add(node))
        {
            queue.Enqueue(node);
            parentMap[node] = parent;
        }
    }

    private List<Vector2Int> ReconstructPath(Dictionary<Vector2Int, Vector2Int> parentMap, Vector2Int target)
    {
        List<Vector2Int> path = new List<Vector2Int>();
        Vector2Int curr = target;
        while (parentMap.ContainsKey(curr) && parentMap[curr] != curr)
        {
            path.Add(curr);
            curr = parentMap[curr];
        }
        path.Add(curr);
        path.Reverse();
        return path;
    }

    /// <summary>
    /// 생산 체인을 계산하여 입력 및 출력 자원의 종류와 개수를 반환합니다.
    /// </summary>
    public void CalculateProductionChain(string threadId, List<BuildingState> states,
        out List<string> inputIds, out Dictionary<string, int> inputCounts,
        out List<string> outputIds, out Dictionary<string, int> outputCounts)
    {
        inputCounts = new Dictionary<string, int>();
        outputCounts = new Dictionary<string, int>();
        HashSet<string> reachableOutputs = new HashSet<string>();

        List<BuildingState> statesToUse = states ?? _states;

        foreach (BuildingState state in statesToUse)
        {
            BuildingData data = DataManager.Building.GetBuildingData(state.buildingId);
            if (data == null || !data.IsProductionBuilding) continue;
            if (!state.IsUnlocked(DataManager)) continue;

            if (IsConnectedViaRoad(GetEndpointPosition(state, data.OutputPosition), false, true))
            {
                if (state.outputProductionIds != null)
                {
                    foreach (string id in state.outputProductionIds)
                    {
                        if (string.IsNullOrEmpty(id)) continue;
                        reachableOutputs.Add(id);
                        outputCounts[id] = outputCounts.GetValueOrDefault(id, 0) + 1;
                    }
                }
            }

            if (IsConnectedViaRoad(GetEndpointPosition(state, data.InputPosition), true, false))
            {
                ProcessInputs(state, inputCounts);
            }
        }

        inputIds = inputCounts.Keys.ToList();
        outputIds = reachableOutputs.ToList();
    }

    /// <summary>
    /// 입력 자원을 처리하며, 명시적 설정이 없을 경우 결과물의 요구사항에서 추론합니다.
    /// </summary>
    private void ProcessInputs(BuildingState state, Dictionary<string, int> counts)
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

    private BuildingData GetBuildingDataAt(Vector2Int pos)
    {
        BuildingState state;
        if (_gridMap.TryGetValue(pos, out state))
        {
            return DataManager.Building.GetBuildingData(state.buildingId);
        }
        return null;
    }

    private bool IsRoadAt(Vector2Int pos)
    {
        BuildingData data = GetBuildingDataAt(pos);
        return data != null && data.IsRoad;
    }

    private Vector2Int GetEndpointPosition(BuildingState state, Vector2Int offset)
    {
        Vector2Int basePos = new Vector2Int(state.positionX, state.positionY);
        int rot = state.rotation % 4;

        Vector2Int rotatedOffset;
        switch (rot)
        {
            case 1: rotatedOffset = new Vector2Int(offset.y, -offset.x); break;
            case 2: rotatedOffset = new Vector2Int(-offset.x, -offset.y); break;
            case 3: rotatedOffset = new Vector2Int(-offset.y, offset.x); break;
            default: rotatedOffset = offset; break;
        }
        return basePos + rotatedOffset;
    }

    private Vector2Int GetRotatedSize(Vector2Int size, int rotation)
    {
        int rot = rotation % 4;
        return (rot == 1 || rot == 3) ? new Vector2Int(size.y, size.x) : size;
    }
}