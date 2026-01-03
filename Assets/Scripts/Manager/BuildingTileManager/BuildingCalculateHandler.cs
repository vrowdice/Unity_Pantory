using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/// <summary>
/// 건물의 생산 체인, 유지비, 도로 연결성(Pathfinding) 검증을 담당하는 핸들러입니다.
/// </summary>
public class BuildingCalculateHandler
{
    private readonly BuildingTileManager _manager;
    private readonly List<BuildingState> _states;
    private readonly Dictionary<Vector2Int, BuildingState> _gridMap; // 성능 최적화를 위한 좌표 맵

    private DataManager DataManager => _manager.DataManager;

    private static readonly Vector2Int[] Neighbors = {
        Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down
    };

    public BuildingCalculateHandler(BuildingTileManager manager, List<BuildingState> currentBuildingStates)
    {
        _manager = manager;
        _states = currentBuildingStates ?? new List<BuildingState>();
        _gridMap = BuildGridMap(_states);
    }

    /// <summary>
    /// 좌표 기반 탐색 속도를 높이기 위해 그리드 맵을 생성합니다.
    /// </summary>
    private Dictionary<Vector2Int, BuildingState> BuildGridMap(List<BuildingState> states)
    {
        var map = new Dictionary<Vector2Int, BuildingState>();
        foreach (var state in states)
        {
            var data = DataManager.Building.GetBuildingData(state.buildingId);
            if (data == null) continue;

            Vector2Int size = GetRotatedSize(data.size, state.rotation);
            for (int x = 0; x < size.x; x++)
            {
                for (int y = 0; y < size.y; y++)
                {
                    Vector2Int pos = new Vector2Int(state.positionX + x, state.positionY + y);
                    map[pos] = state; // 멀티 타일 영역 전체를 맵에 등록
                }
            }
        }
        return map;
    }

    #region Production & Maintenance Calculations

    /// <summary>
    /// 유효한 연결(도로)을 가진 생산 건물의 총 개수를 계산합니다.
    /// </summary>
    public int CalculateThreadOutputs(string threadId, List<BuildingState> customStates = null)
    {
        if (string.IsNullOrEmpty(threadId) || DataManager == null) return 0;

        var statesToUse = customStates ?? _states;
        int validProducerCount = 0;

        foreach (var state in statesToUse)
        {
            var data = DataManager.Building.GetBuildingData(state.buildingId);
            if (data == null || !data.IsProductionBuilding) continue;

            // 연구 잠금 체크
            if (!state.IsUnlocked(DataManager)) continue;

            // 입출력 포트 좌표 계산
            Vector2Int inPos = GetEndpointPosition(state, data.InputPosition);
            Vector2Int outPos = GetEndpointPosition(state, data.OutputPosition);

            // 하역소 및 상역소 연결 확인
            if (IsConnectedViaRoad(inPos, findUnload: true) && IsConnectedViaRoad(outPos, findLoad: true))
            {
                validProducerCount++;
            }
        }

        return validProducerCount;
    }

    /// <summary>
    /// 총 유지비를 계산합니다. (연구된 건물만 포함)
    /// </summary>
    public int CalculateTotalMaintenanceCost(string threadId, List<BuildingState> customStates = null)
    {
        var statesToUse = customStates ?? _states;
        BuildingCalculationUtility.ExecuteCoreCalculation(
            DataManager, statesToUse,
            out int cost, out _, out _, out _
        );
        return cost;
    }

    #endregion

    #region Pathfinding (BFS)

    private bool IsConnectedViaRoad(Vector2Int startPos, bool findUnload = false, bool findLoad = false)
    {
        return FindPath(startPos, findUnload, findLoad) != null;
    }

    private List<Vector2Int> FindPath(Vector2Int startPos, bool findUnload, bool findLoad)
    {
        if (DataManager == null) return null;

        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
        Dictionary<Vector2Int, Vector2Int> parentMap = new Dictionary<Vector2Int, Vector2Int>();

        // 시작점 처리: 도로이거나, 인접한 타일이 도로인 경우 시작 가능
        if (IsRoadAt(startPos))
        {
            EnqueueNode(startPos, startPos);
        }
        else
        {
            foreach (var dir in Neighbors)
            {
                Vector2Int neighbor = startPos + dir;
                if (IsRoadAt(neighbor)) EnqueueNode(neighbor, neighbor);
            }
        }

        void EnqueueNode(Vector2Int node, Vector2Int parent)
        {
            if (visited.Add(node))
            {
                queue.Enqueue(node);
                parentMap[node] = parent;
            }
        }

        Vector2Int targetPos = new Vector2Int(-1, -1);
        bool found = false;

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();

            foreach (var dir in Neighbors)
            {
                Vector2Int next = current + dir;
                if (visited.Contains(next)) continue;

                var nextData = GetBuildingDataAt(next);
                if (nextData == null) continue;

                if (nextData.IsRoad)
                {
                    EnqueueNode(next, current);
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

    #endregion

    #region Production Chain Calculation

    /// <summary>
    /// 생산 체인을 계산합니다. 도로 연결이 확인된 건물만 포함됩니다.
    /// 출력과 입력은 각각 독립적으로 처리됩니다.
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

            // 연구 잠금 체크
            if (!state.IsUnlocked(DataManager)) continue;

            // 출력 경로 (상역소 연결)
            if (IsConnectedViaRoad(GetEndpointPosition(state, data.OutputPosition), findLoad: true))
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

            // 입력 경로 (하역소 연결)
            if (IsConnectedViaRoad(GetEndpointPosition(state, data.InputPosition), findUnload: true))
            {
                ProcessInputs(state, inputCounts);
            }
        }

        inputIds = inputCounts.Keys.ToList();
        outputIds = reachableOutputs.ToList();
    }

    /// <summary>
    /// 입력 자원을 처리합니다. 명시적 입력이 없으면 출력 자원의 요구사항에서 추론합니다.
    /// </summary>
    private void ProcessInputs(BuildingState state, Dictionary<string, int> counts)
    {
        // 1. 명시적 입력 자원 확인
        if (state.inputProductionIds != null && state.inputProductionIds.Count > 0)
        {
            foreach (var id in state.inputProductionIds)
            {
                if (string.IsNullOrEmpty(id)) continue;
                counts[id] = counts.GetValueOrDefault(id, 0) + 1;
            }
        }
        // 2. 출력 자원의 요구사항으로부터 추론
        else if (state.outputProductionIds != null && state.outputProductionIds.Count > 0)
        {
            foreach (var outId in state.outputProductionIds)
            {
                if (string.IsNullOrEmpty(outId)) continue;

                var entry = DataManager.Resource.GetResourceEntry(outId);
                var reqs = entry?.data?.requirements;
                if (reqs == null) continue;

                foreach (var req in reqs)
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

    #endregion

    #region Helpers

    private BuildingData GetBuildingDataAt(Vector2Int pos)
    {
        if (_gridMap.TryGetValue(pos, out var state))
            return DataManager.Building.GetBuildingData(state.buildingId);
        return null;
    }

    private bool IsRoadAt(Vector2Int pos) => GetBuildingDataAt(pos)?.IsRoad ?? false;

    private Vector2Int GetEndpointPosition(BuildingState state, Vector2Int offset)
    {
        Vector2Int basePos = new Vector2Int(state.positionX, state.positionY);
        int rot = state.rotation % 4;

        Vector2Int rotatedOffset = rot switch
        {
            1 => new Vector2Int(offset.y, -offset.x),
            2 => new Vector2Int(-offset.x, -offset.y),
            3 => new Vector2Int(-offset.y, offset.x),
            _ => offset
        };
        return basePos + rotatedOffset;
    }

    private Vector2Int GetRotatedSize(Vector2Int size, int rotation)
    {
        return (rotation % 4 == 1 || rotation % 4 == 3) ? new Vector2Int(size.y, size.x) : size;
    }

    #endregion
}