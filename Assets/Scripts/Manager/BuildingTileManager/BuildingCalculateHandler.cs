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

            // 생산 건물만 대상 (allowedResourceTypes가 존재하는 건물로 간주)
            bool isProducer = data.allowedResourceTypes != null && data.allowedResourceTypes.Count > 0;
            if (!isProducer)
                continue;

            // 입/출력 엔드포인트 계산 (회전 적용)
            Vector2Int inputEndpoint = GetEndpointGridPosition(state.position, data.inputPosition, state.rotation);
            Vector2Int outputEndpoint = GetEndpointGridPosition(state.position, data.outputPosition, state.rotation);

            bool inputOk = IsConnectedViaRoadToTarget(inputEndpoint, threadId, UnloadBuildingId);
            bool outputOk = IsConnectedViaRoadToTarget(outputEndpoint, threadId, LoadBuildingId);

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
    /// 시작 지점에서 도로(Road)만 따라 탐색하여 목표 건물 ID(Load/Unload)에 도달 가능한지 검사합니다.
    /// </summary>
    private bool IsConnectedViaRoadToTarget(Vector2Int startPos, string threadId, string targetBuildingId)
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

        // 시작점은 건물의 IO 끝점이므로, 인접한 로드에서 시작
        foreach (var dir in _neighbors)
        {
            Vector2Int next = startPos + dir;
            string buildingIdAtNext = GetBuildingIdAt(next, buildingStates, dataManager);
            if (buildingIdAtNext == RoadBuildingId)
            {
                queue.Enqueue(next);
                visited.Add(next);
            }
            else if (buildingIdAtNext == targetBuildingId)
            {
                return true;
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

                string buildingId = GetBuildingIdAt(next, buildingStates, dataManager);
                if (buildingId == RoadBuildingId)
                {
                    queue.Enqueue(next);
                    visited.Add(next);
                }
                else if (buildingId == targetBuildingId)
                {
                    return true;
                }
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
    /// 해당 그리드 좌표에 배치된 건물 ID를 반환합니다. 없으면 null.
    /// </summary>
    private string GetBuildingIdAt(Vector2Int gridPos, List<BuildingState> buildingStates, GameDataManager dataManager)
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
                return data.id;
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
}