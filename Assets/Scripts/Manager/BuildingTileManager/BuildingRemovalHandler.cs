using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 건물 제거 모드를 처리하는 핸들러
/// </summary>
public class BuildingRemovalHandler
{
    private readonly BuildingTileManager _buildingTileManager;
    private readonly BuildingGridHandler _gridManager;
    private readonly dataManager _dataManager;
    private readonly MainCameraController _mainCameraController;
    private readonly Camera _camera;
    private readonly DesignUiManager _designUiManager;

    private bool _isActive = false;
    private GameObject _hoveredBuilding = null;
    public bool IsActive => _isActive;
    public BuildingRemovalHandler(BuildingTileManager buildingTileManager)
    {
        _buildingTileManager = buildingTileManager;
        _gridManager = buildingTileManager.GridGenHandler;
        _dataManager = buildingTileManager.DataManager;
        _mainCameraController = buildingTileManager.MainCameraController;
        _camera = _mainCameraController != null ? _mainCameraController.Camera : buildingTileManager.MainCamera;
        _designUiManager = buildingTileManager.DesignUiManager;
    }

    /// <summary>
    /// 제거 모드를 시작합니다.
    /// </summary>
    public void StartRemoval()
    {
        _isActive = true;
        _gridManager.SetAllTilesOutline(false);
        _designUiManager?.UpdateModeBtnImages(false, true);
    }

    /// <summary>
    /// 제거 모드를 취소합니다.
    /// </summary>
    public void CancelRemoval()
    {
        _isActive = false;
        ResetBuildingHighlight();
        _gridManager.SetAllTilesOutline(false);
        _designUiManager?.UpdateModeBtnImages(false, false);
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
    /// 제거 프리뷰를 업데이트합니다. 마우스 아래 건물을 감지하고 하이라이트합니다.
    /// </summary>
    private void UpdatePreview(string currentThreadId)
    {
        // 마우스 위치를 그리드 좌표로 변환
        Camera activeCamera = GetActiveCamera();
        if (activeCamera == null)
            return;

        Vector3 mouseWorldPos = activeCamera.ScreenToWorldPoint(Input.mousePosition);
        Vector2Int gridPos = _gridManager.WorldToGridPosition(mouseWorldPos);

        // 해당 위치에 건물이 있는지 확인 (GridManager가 크기/회전 고려)
        GameObject buildingAtPos = _gridManager.GetBuildingAtPosition(gridPos, currentThreadId);

        // 이전에 하이라이트된 건물과 다르면 갱신
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
        Camera activeCamera = GetActiveCamera();
        if (activeCamera == null)
            return;

        if (Input.GetMouseButtonDown(0) && _hoveredBuilding != null)
        {
            Vector3 mouseWorldPos = activeCamera.ScreenToWorldPoint(Input.mousePosition);
            Vector2Int gridPos = _gridManager.WorldToGridPosition(mouseWorldPos);
            BuildingObject buildingComp = _hoveredBuilding.GetComponent<BuildingObject>();
            if (buildingComp != null)
            {
                RemoveBuilding(new Vector2Int(buildingComp.BuildingState.positionX, buildingComp.BuildingState.positionY)); 
            }
            else
            {
                Debug.LogError("[BuildingRemovalHandler] Clicked GameObject is missing BuildingObject component.");
            }
        }

        // 오른쪽 클릭 또는 ESC - 취소
        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
        {
            CancelRemoval();
        }
    }

    private Camera GetActiveCamera()
    {
        if (_mainCameraController != null && _mainCameraController.Camera != null)
        {
            return _mainCameraController.Camera;
        }

        return _camera;
    }

    /// <summary>
    /// 건물을 제거합니다. (원점 좌표 사용)
    /// </summary>
    private void RemoveBuilding(Vector2Int originGridPos)
    {
        if (_buildingTileManager.RemoveBuildingFromTemp(originGridPos))
        {
            ResetBuildingHighlight();
            _buildingTileManager.RefreshBuildings();
        }
        else
        {
            Debug.LogWarning($"[BuildingRemovalHandler] Failed to remove building at origin: {originGridPos}. Building not found in temp data.");
        }
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
            Color invalidColor = VisualManager.Instance?.InvalidColor ?? new Color(1, 0, 0, 0.5f);
            renderer.color = invalidColor;
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