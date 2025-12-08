using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 도로 위 자원 애니메이션을 관리하는 핸들러 (Non-MonoBehaviour)
/// </summary>
public class ResourceAnimHandler
{
    // ==================== References ====================
    private readonly BuildingTileManager _tileManager;
    private readonly GameObject _resourceItemPrefab;
    
    // ==================== Settings ====================
    private float _spawnInterval;
    private float _timer;

    // ==================== State ====================
    // 경로 충돌 감지를 위한 맵 (Key: 그리드좌표, Value: 자원ID)
    private Dictionary<Vector2Int, string> _roadOccupancyMap = new Dictionary<Vector2Int, string>();
    private List<ActiveRoute> _validRoutes = new List<ActiveRoute>();
    private bool _isRoutesDirty = true;

    // 내부 클래스
    private class ActiveRoute
    {
        public List<Vector2Int> path;
        public Sprite resourceSprite;
        public string resourceId;
    }

    // ==================== Constructor ====================
    public ResourceAnimHandler(BuildingTileManager tileManager, GameObject resourceItemPrefab, float spawnInterval)
    {
        _tileManager = tileManager;
        _resourceItemPrefab = resourceItemPrefab;
        _spawnInterval = spawnInterval;
        _timer = 0f;
    }

    // ==================== Public Methods ====================

    /// <summary>
    /// BuildingTileManager의 Update에서 호출됩니다.
    /// </summary>
    public void Update()
    {
        // 1. 경로 재계산 필요 시 수행 (건물 배치/제거 시 외부에서 Dirty 플래그 설정 가능)
        if (_isRoutesDirty)
        {
            CalculateAllRoutes();
            _isRoutesDirty = false;
        }

        // 2. 타이머 체크 및 스폰
        _timer += Time.deltaTime;
        if (_timer >= _spawnInterval)
        {
            _timer = 0f;
            SpawnFromValidRoutes();
        }
    }

    /// <summary>
    /// 건물 상태가 변경되었을 때 호출하여 경로를 갱신하도록 요청합니다.
    /// </summary>
    public void SetRoutesDirty()
    {
        _isRoutesDirty = true;
    }

    // ==================== Private Methods ====================

    private void CalculateAllRoutes()
    {
        _validRoutes.Clear();
        _roadOccupancyMap.Clear();
        
        var states = _tileManager.GetCurrentBuildingStates();
        if (states == null) return;

        bool conflictDetected = false;

        foreach (var state in states)
        {
            BuildingData data = _tileManager.DataManager.Building.GetBuildingData(state.buildingId);
            if (data == null || !data.IsProductionBuilding) continue;

            // 1. 입력 경로 (하역소 -> 공장)
            // CalculateHandler 접근 시 프로퍼티 사용
            List<string> inputIds = _tileManager.CalculateHandler.CollectInputProductionIds(_tileManager.CurrentThreadId, new List<BuildingState> { state });
            
            foreach(var inputId in inputIds)
            {
                Vector2Int inputGridPos = GetEndpointPos(state, data.InputPosition);
                var path = _tileManager.CalculateHandler.GetPathViaRoad(inputGridPos, true, false);
                
                if (path != null && path.Count > 1)
                {
                    path.Reverse(); 
                    if (ValidateAndRegisterRoute(path, inputId)) AddRoute(path, inputId);
                    else conflictDetected = true;
                }
            }

            // 2. 출력 경로 (공장 -> 상역소)
            List<string> outputIds = _tileManager.CalculateHandler.CollectOutputProductionIds(_tileManager.CurrentThreadId, new List<BuildingState> { state });
            
            foreach(var outputId in outputIds)
            {
                Vector2Int outputGridPos = GetEndpointPos(state, data.OutputPosition);
                var path = _tileManager.CalculateHandler.GetPathViaRoad(outputGridPos, false, true);

                if (path != null && path.Count > 1)
                {
                    if (ValidateAndRegisterRoute(path, outputId)) AddRoute(path, outputId);
                    else conflictDetected = true;
                }
            }
        }

        if (conflictDetected)
        {
            Debug.LogWarning("⚠️ [ResourceAnimHandler] 자원 경로 충돌 감지됨.");
        }
    }

    private bool ValidateAndRegisterRoute(List<Vector2Int> path, string resourceId)
    {
        // 검사
        foreach (var pos in path)
        {
            if (_roadOccupancyMap.TryGetValue(pos, out string existingId))
            {
                if (existingId != resourceId) return false; 
            }
        }
        // 등록
        foreach (var pos in path)
        {
            if (!_roadOccupancyMap.ContainsKey(pos)) _roadOccupancyMap[pos] = resourceId;
        }
        return true;
    }

    private void AddRoute(List<Vector2Int> path, string resourceId)
    {
        // ResourceData 접근 수정 (icon 경로 확인)
        var resourceEntry = _tileManager.DataManager.Resource.GetResourceEntry(resourceId);
        Sprite sprite = resourceEntry?.resourceData?.icon; 

        _validRoutes.Add(new ActiveRoute
        {
            path = path,
            resourceId = resourceId,
            resourceSprite = sprite
        });
    }

    private void SpawnFromValidRoutes()
    {
        foreach (var route in _validRoutes)
        {
            SpawnItem(route.path, route.resourceSprite);
        }
    }

    private void SpawnItem(List<Vector2Int> gridPath, Sprite sprite)
    {
        if (gridPath == null || gridPath.Count == 0) return;

        List<Vector3> worldPath = new List<Vector3>();
        foreach (var pos in gridPath)
        {
            // GridGenHandler를 통해 좌표 변환
            Vector3 worldPos = _tileManager.GridGenHandler.GridToWorldPosition(pos, new Vector2Int(1, 1));
            worldPath.Add(worldPos);
        }

        if (_resourceItemPrefab != null)
        {
            // MonoBehaviour가 아니어도 Object.Instantiate 사용 가능
            GameObject go = Object.Instantiate(_resourceItemPrefab);
            ResourceItem item = go.GetComponent<ResourceItem>();
            if (item != null)
            {
                item.Initialize(worldPath, sprite, 2.0f);
            }
        }
    }

    private Vector2Int GetEndpointPos(BuildingState state, Vector2Int offset)
    {
        int rotation = state.rotation % 4;
        Vector2Int rotatedOffset = offset;
        if (rotation == 1) rotatedOffset = new Vector2Int(offset.y, -offset.x);
        else if (rotation == 2) rotatedOffset = new Vector2Int(-offset.x, -offset.y);
        else if (rotation == 3) rotatedOffset = new Vector2Int(-offset.y, offset.x);

        return new Vector2Int(state.positionX, state.positionY) + rotatedOffset;
    }
}