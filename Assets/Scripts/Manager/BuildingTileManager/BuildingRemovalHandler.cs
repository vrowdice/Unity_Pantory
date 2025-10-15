using UnityEngine;

/// <summary>
/// 건물 제거 모드를 처리하는 핸들러
/// </summary>
public class BuildingRemovalHandler
{
    private readonly BuildingGridHandler _gridManager;
    private readonly GameDataManager _dataManager;
    private readonly Camera _mainCamera;

    private bool _isActive = false;
    private GameObject _hoveredBuilding = null;

    // Highlight Settings
    private Color _removalHighlightColor = new Color(1, 0, 0, 0.7f);

    public bool IsActive => _isActive;

    public BuildingRemovalHandler(BuildingGridHandler gridManager, GameDataManager dataManager, Camera mainCamera)
    {
        _gridManager = gridManager;
        _dataManager = dataManager;
        _mainCamera = mainCamera;
    }

    /// <summary>
    /// 하이라이트 색상을 설정합니다.
    /// </summary>
    public void SetHighlightColor(Color color)
    {
        _removalHighlightColor = color;
    }

    /// <summary>
    /// 제거 모드를 시작합니다.
    /// </summary>
    public void StartRemoval()
    {
        _isActive = true;
        _gridManager.SetAllTilesOutline(true);  // 타일 윤곽선 표시
        Debug.Log("[BuildingRemovalHandler] Removal mode started");
    }

    /// <summary>
    /// 제거 모드를 취소합니다.
    /// </summary>
    public void CancelRemoval()
    {
        _isActive = false;
        ResetBuildingHighlight();
        _gridManager.SetAllTilesOutline(false);  // 타일 윤곽선 숨김
        Debug.Log("[BuildingRemovalHandler] Removal mode cancelled");
    }

    /// <summary>
    /// 제거 모드를 업데이트합니다 (매 프레임 호출).
    /// </summary>
    public void Update(string currentThreadId)
    {
        if (!_isActive)
            return;

        UpdatePreview(currentThreadId);
        HandleInput(currentThreadId);
    }

    /// <summary>
    /// 제거 프리뷰를 업데이트합니다.
    /// </summary>
    private void UpdatePreview(string currentThreadId)
    {
        // 마우스 위치를 월드 좌표로 변환
        Vector3 mouseWorldPos = _mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0;

        // 그리드 좌표로 변환
        Vector2Int gridPos = _gridManager.WorldToGridPosition(mouseWorldPos);

        // 해당 위치에 건물이 있는지 확인
        GameObject buildingAtPos = _gridManager.GetBuildingAtPosition(gridPos, currentThreadId);

        // 이전에 하이라이트된 건물과 다른 경우
        if (buildingAtPos != _hoveredBuilding)
        {
            ResetBuildingHighlight();
            _hoveredBuilding = buildingAtPos;
            HighlightBuilding(_hoveredBuilding);
        }
    }

    /// <summary>
    /// 제거 입력을 처리합니다.
    /// </summary>
    private void HandleInput(string currentThreadId)
    {
        // 왼쪽 클릭 - 건물 제거
        if (Input.GetMouseButtonDown(0) && _hoveredBuilding != null)
        {
            // 마우스 위치를 월드 좌표로 변환
            Vector3 mouseWorldPos = _mainCamera.ScreenToWorldPoint(Input.mousePosition);
            mouseWorldPos.z = 0;
            Vector2Int gridPos = _gridManager.WorldToGridPosition(mouseWorldPos);

            RemoveBuilding(gridPos, currentThreadId);
        }

        // 오른쪽 클릭 또는 ESC - 취소
        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
        {
            CancelRemoval();
        }
    }

    /// <summary>
    /// 건물을 제거합니다.
    /// </summary>
    private void RemoveBuilding(Vector2Int gridPos, string currentThreadId)
    {
        // 건물 찾기
        Vector2Int buildingOriginPos = Vector2Int.zero;
        BuildingState targetBuilding = null;
        BuildingData targetBuildingData = null;

        var buildingStates = _dataManager.GetBuildingStates(currentThreadId);
        if (buildingStates != null)
        {
            foreach (var buildingState in buildingStates)
            {
                BuildingData buildingData = _dataManager.GetBuildingData(buildingState.buildingId);
                if (buildingData != null)
                {
                    // 건물이 차지하는 영역 확인
                    if (gridPos.x >= buildingState.position.x && gridPos.x < buildingState.position.x + buildingData.size.x &&
                        gridPos.y >= buildingState.position.y && gridPos.y < buildingState.position.y + buildingData.size.y)
                    {
                        buildingOriginPos = buildingState.position;
                        targetBuilding = buildingState;
                        targetBuildingData = buildingData;
                        break;
                    }
                }
            }
        }

        if (targetBuilding == null)
        {
            Debug.LogWarning("[BuildingRemovalHandler] No building found at this position");
            return;
        }

        // ThreadService에서 건물 제거
        if (_dataManager.RemoveBuildingFromThread(currentThreadId, buildingOriginPos))
        {
            // 건물 오브젝트 제거
            _gridManager.RemoveBuildingObject(buildingOriginPos);

            // 타일 점유 해제
            _gridManager.MarkTilesAsOccupied(buildingOriginPos, targetBuildingData.size, false);

            Debug.Log($"[BuildingRemovalHandler] Building removed: {targetBuildingData.displayName} at {buildingOriginPos}");
        }

        ResetBuildingHighlight();
    }

    /// <summary>
    /// 건물을 하이라이트 표시합니다.
    /// </summary>
    private void HighlightBuilding(GameObject building)
    {
        if (building == null) return;

        SpriteRenderer renderer = building.GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            renderer.color = _removalHighlightColor;
        }
    }

    /// <summary>
    /// 건물 하이라이트를 초기화합니다.
    /// </summary>
    private void ResetBuildingHighlight()
    {
        if (_hoveredBuilding != null)
        {
            SpriteRenderer renderer = _hoveredBuilding.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                renderer.color = Color.white;
            }
            _hoveredBuilding = null;
        }
    }
}

