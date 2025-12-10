using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Pool;

/// <summary>
/// 도로 위 자원 애니메이션을 관리하는 핸들러 (Non-MonoBehaviour)
/// </summary>
public class ResourceAnimHandler
{
    // ==================== References ====================
    private readonly BuildingTileManager _tileManager;
    private readonly GameObject _resourceItemPrefab;
    private IObjectPool<ResourceItem> _itemPool;
    
    // ==================== Settings ====================
    private float _spawnInterval;
    private float _timer;

    // ==================== State ====================
    // 경로 충돌 감지를 위한 맵 (Key: 그리드좌표, Value: 자원ID)
    private Dictionary<Vector2Int, string> _roadOccupancyMap = new Dictionary<Vector2Int, string>();
    private List<ActiveRoute> _validRoutes = new List<ActiveRoute>();
    private bool _isRoutesDirty = true;
    private bool _hasConflicts = false;


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
        _itemPool = new ObjectPool<ResourceItem>(
            createFunc: CreateResourceItem,
            actionOnGet: OnGetItem,
            actionOnRelease: OnReleaseItem,
            actionOnDestroy: OnDestroyItem,
            defaultCapacity: 50,
            maxSize: 500
        );
    }
    // ==================== Pool Callback Methods (추가됨) ====================
    
    // 1. 생성
    private ResourceItem CreateResourceItem()
    {
        GameObject go = Object.Instantiate(_resourceItemPrefab);
        ResourceItem item = go.GetComponent<ResourceItem>();
        go.transform.SetParent(_tileManager.transform); 
        return item;
    }

    private void OnGetItem(ResourceItem item)
    {
        item.gameObject.SetActive(true);
    }

    private void OnReleaseItem(ResourceItem item)
    {
        item.gameObject.SetActive(false);
    }

    private void OnDestroyItem(ResourceItem item)
    {
        Object.Destroy(item.gameObject);
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
            // 충돌이 있으면 애니메이션 스폰을 멈춘다
            if (!_hasConflicts)
            {
                SpawnFromValidRoutes();
            }
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
        _hasConflicts = false;
        
        var states = _tileManager.GetCurrentBuildingStates();
        if (states == null) return;

        Dictionary<Vector2Int, BuildingState> stateMap = new Dictionary<Vector2Int, BuildingState>();
        foreach (var state in states)
        {
            state.ResetRuntimeStatus(); // 기존 상태 리셋
            stateMap[new Vector2Int(state.positionX, state.positionY)] = state;
            
            // 정거장(Station)은 자신이 취급하는 자원을 미리 설정
            BuildingData bData = _tileManager.DataManager.Building.GetBuildingData(state.buildingId);
            if (bData != null && bData.handlingResource != null)
            {
                state.currentResourceId = bData.handlingResource.id;
            }
        }
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
                    if (ValidateAndRegisterRoute(path, inputId, stateMap)) 
                    {
                        AddRoute(path, inputId);
                        UpdateRoadStates(path, inputId, stateMap); // [추가] 성공 시 도로에 자원 기록
                    }
                    else 
                    {
                        conflictDetected = true;
                        MarkConflicts(path, stateMap); // [추가] 실패 시 충돌 기록
                    }
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
                    if (ValidateAndRegisterRoute(path, outputId, stateMap)) 
                    {
                        AddRoute(path, outputId);
                        UpdateRoadStates(path, outputId, stateMap);
                    }
                    else 
                    {
                        conflictDetected = true;
                        MarkConflicts(path, stateMap);
                    }
                }
            }
        }

        if (conflictDetected)
        {
            _hasConflicts = true;
            Debug.LogWarning("⚠️ [ResourceAnimHandler] 자원 경로 충돌 감지됨.");
        }
    }

    private bool ValidateAndRegisterRoute(List<Vector2Int> path, string resourceId, Dictionary<Vector2Int, BuildingState> stateMap)
    {
        foreach (var pos in path)
        {
            if (stateMap.TryGetValue(pos, out BuildingState state))
            {
                // 이미 자원이 있는데, 내가 보내려는거랑 다르면 충돌
                if (!string.IsNullOrEmpty(state.currentResourceId) && state.currentResourceId != resourceId)
                    return false; 
                
                // 이미 충돌난 곳이면 통과 불가
                if (state.hasResourceConflict)
                    return false;
            }
        }
        return true;
    }
    //도로 State에 자원 ID 기록 (UI 표시용)
    private void UpdateRoadStates(List<Vector2Int> path, string resourceId, Dictionary<Vector2Int, BuildingState> stateMap)
    {
        foreach (var pos in path)
        {
            if (stateMap.TryGetValue(pos, out BuildingState state))
            {
                state.currentResourceId = resourceId;
            }
        }
    }

    //도로 State에 충돌 마킹 (UI 표시용)
    private void MarkConflicts(List<Vector2Int> path, Dictionary<Vector2Int, BuildingState> stateMap)
    {
        foreach (var pos in path)
        {
            if (stateMap.TryGetValue(pos, out BuildingState state))
            {
                BuildingData bd = _tileManager.DataManager.Building.GetBuildingData(state.buildingId);
                // 도로인 경우에만 빨간불 켜기
                if (bd != null && bd.IsRoad)
                {
                     state.hasResourceConflict = true;
                     state.currentResourceId = null; // 충돌 시 자원 안보이게
                }
            }
        }
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
            ResourceItem item = _itemPool.Get();
            if (item != null)
            {
                item.Initialize(worldPath, sprite, 2.0f, _itemPool);
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