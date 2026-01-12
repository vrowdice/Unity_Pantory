using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 도로 연결성 및 경로 탐색을 담당하는 분석기입니다.
/// </summary>
public static class RoadNetworkAnalyzer
{
    private static readonly Vector2Int[] Neighbors = { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down };

    /// <summary>
    /// 특정 위치에서 도로를 통해 하역소나 상역소까지의 연결 여부를 확인합니다.
    /// </summary>
    /// <param name="startPos">시작 위치</param>
    /// <param name="findUnload">하역소 찾기 여부</param>
    /// <param name="findLoad">상역소 찾기 여부</param>
    /// <param name="gridMap">그리드 맵 (좌표 -> BuildingState)</param>
    /// <param name="dataManager">데이터 매니저</param>
    /// <returns>연결되어 있으면 true</returns>
    public static bool IsConnected(
        Vector2Int startPos,
        bool findUnload,
        bool findLoad,
        Dictionary<Vector2Int, BuildingState> gridMap,
        DataManager dataManager)
    {
        return FindPathBFS(startPos, findUnload, findLoad, gridMap, dataManager) != null;
    }

    /// <summary>
    /// BFS 알고리즘을 사용하여 목적지(하역소/상역소)까지의 최단 경로를 탐색합니다.
    /// </summary>
    /// <param name="startPos">시작 위치</param>
    /// <param name="findUnload">하역소 찾기 여부</param>
    /// <param name="findLoad">상역소 찾기 여부</param>
    /// <param name="gridMap">그리드 맵</param>
    /// <param name="dataManager">데이터 매니저</param>
    /// <returns>경로 리스트, 경로가 없으면 null</returns>
    private static List<Vector2Int> FindPathBFS(
        Vector2Int startPos,
        bool findUnload,
        bool findLoad,
        Dictionary<Vector2Int, BuildingState> gridMap,
        DataManager dataManager)
    {
        if (dataManager == null) return null;

        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
        Dictionary<Vector2Int, Vector2Int> parentMap = new Dictionary<Vector2Int, Vector2Int>();

        // 시작점 처리: 도로라면 큐에 넣고, 아니라면 인접 도로 검색
        if (IsRoadAt(startPos, gridMap, dataManager))
        {
            EnqueueNode(startPos, startPos, queue, visited, parentMap);
        }
        else
        {
            foreach (Vector2Int dir in Neighbors)
            {
                Vector2Int neighbor = startPos + dir;
                if (IsRoadAt(neighbor, gridMap, dataManager))
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

                BuildingData nextData = GetBuildingDataAt(next, gridMap, dataManager);
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

    /// <summary>
    /// 노드를 큐에 추가합니다.
    /// </summary>
    private static void EnqueueNode(Vector2Int node, Vector2Int parent, Queue<Vector2Int> queue, HashSet<Vector2Int> visited, Dictionary<Vector2Int, Vector2Int> parentMap)
    {
        if (visited.Add(node))
        {
            queue.Enqueue(node);
            parentMap[node] = parent;
        }
    }

    /// <summary>
    /// 부모 맵을 사용하여 경로를 재구성합니다.
    /// </summary>
    private static List<Vector2Int> ReconstructPath(Dictionary<Vector2Int, Vector2Int> parentMap, Vector2Int target)
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
    /// 특정 위치의 건물 데이터를 가져옵니다.
    /// </summary>
    private static BuildingData GetBuildingDataAt(Vector2Int pos, Dictionary<Vector2Int, BuildingState> gridMap, DataManager manager)
    {
        if (gridMap.TryGetValue(pos, out BuildingState state))
        {
            return manager.Building.GetBuildingData(state.buildingId);
        }
        return null;
    }

    /// <summary>
    /// 특정 위치에 도로가 있는지 확인합니다.
    /// </summary>
    private static bool IsRoadAt(Vector2Int pos, Dictionary<Vector2Int, BuildingState> gridMap, DataManager manager)
    {
        BuildingData data = GetBuildingDataAt(pos, gridMap, manager);
        return data != null && data.IsRoad;
    }
}
