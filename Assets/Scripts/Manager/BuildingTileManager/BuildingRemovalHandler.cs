using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 건물 제거 모드를 처리하는 핸들러
/// </summary>
public class BuildingRemovalHandler
{
    // ==================== References ====================
    private readonly BuildingTileManager _buildingTileManager;
    private readonly BuildingGridHandler _gridManager;
    private readonly GameDataManager _dataManager;
    private readonly MainCameraController _mainCameraController;
    private readonly Camera _camera;
    private readonly DesignUiManager _designUiManager;

    // ==================== State ====================
    private bool _isActive = false;
    private GameObject _hoveredBuilding = null;
    
    // ==================== Colors ====================
    private Color _validColor = new Color(0, 1, 0, 0.5f); // 사용되지 않지만 통일성을 위해 유지
    private Color _invalidColor = new Color(1, 0, 0, 0.5f); // 제거 모드 하이라이트 색상 (빨간색)

    // ==================== Properties ====================
    public bool IsActive => _isActive;

    // ==================== Constructor ====================
    public BuildingRemovalHandler(BuildingTileManager buildingTileManager)
    {
        _buildingTileManager = buildingTileManager;
        _gridManager = buildingTileManager.GridGenHandler;
        _dataManager = buildingTileManager.DataManager;
        _mainCameraController = buildingTileManager.MainCameraController;
        _camera = _mainCameraController != null ? _mainCameraController.Camera : buildingTileManager.MainCamera;
        _designUiManager = buildingTileManager.DesignUiManager;
    }

    #region Public Methods

    /// <summary>
    /// 하이라이트 색상을 설정합니다. (제거 모드에서는 보통 _invalidColor만 사용됩니다.)
    /// </summary>
    public void SetColor(Color validColor, Color invalidColor)
    {
        _validColor = validColor;
        _invalidColor = invalidColor;
    }

    /// <summary>
    /// 제거 모드를 시작합니다.
    /// </summary>
    public void StartRemoval()
    {
        _isActive = true;
        // 제거 모드에서는 모든 타일에 빨간색 윤곽선을 표시하는 것은 비직관적일 수 있으나, 기존 로직 유지
        _gridManager.SetAllTilesOutline(false); // 타일 윤곽선은 제거 모드 시 건물에만 적용하도록 변경 (옵션)
        
        _designUiManager?.UpdateModeBtnImages(false, true);
        Debug.Log("[BuildingRemovalHandler] Removal mode started");
    }

    /// <summary>
    /// 제거 모드를 취소합니다.
    /// </summary>
    public void CancelRemoval()
    {
        _isActive = false;
        ResetBuildingHighlight();
        _gridManager.SetAllTilesOutline(false); // 타일 윤곽선 숨김
        
        _designUiManager?.UpdateModeBtnImages(false, false);
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

    #endregion

    #region Private Logic: Preview and Input

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
        // 왼쪽 클릭 - 건물 제거
        Camera activeCamera = GetActiveCamera();
        if (activeCamera == null)
            return;

        if (Input.GetMouseButtonDown(0) && _hoveredBuilding != null)
        {
            // 클릭된 위치를 다시 계산하여 Grid Pos를 확보
            Vector3 mouseWorldPos = activeCamera.ScreenToWorldPoint(Input.mousePosition);
            Vector2Int gridPos = _gridManager.WorldToGridPosition(mouseWorldPos);

            // 중요: GetBuildingAtPosition은 해당 gridPos를 포함하는 건물의 '원점'을 찾아야 하므로, 
            // 여기서는 BuildingObject 컴포넌트에서 원점 좌표를 가져오는 것이 더 확실합니다.
            BuildingObject buildingComp = _hoveredBuilding.GetComponent<BuildingObject>();
            if (buildingComp != null)
            {
                 // BuildingObject는 반드시 BuildingState를 가지고 있으며, 그 안에 원점(position)이 있습니다.
                RemoveBuilding(new Vector2Int(buildingComp.BuildingState.positionX, buildingComp.BuildingState.positionY)); 
            }
            else
            {
                // 최후의 수단: BuildingObject가 없더라도 GridManager에서 원점을 역추적해야 함.
                // 그러나 GetBuildingAtPosition이 BuildingObject를 반환하는 한, 이 컴포넌트가 있어야 함.
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
        // 임시 저장소에서 건물 제거
        if (_buildingTileManager.RemoveBuildingFromTemp(originGridPos))
        {
            // 하이라이트 먼저 리셋 (RefreshBuildings가 오브젝트를 재생성하므로)
            ResetBuildingHighlight();

            // 데이터만 제거하고, 실제 오브젝트는 BuildingTileManager를 통해 갱신
            _buildingTileManager.RefreshBuildings();

            // 제거된 건물 데이터는 Refresh 전에 이미 접근할 수 없음. BuildingState를 미리 캐시해야 함.
            // 여기서는 간단히 로그만 남깁니다.
            Debug.Log($"[BuildingRemovalHandler] Building removed (temp) at {originGridPos}. Refreshing layout.");
        }
        else
        {
            Debug.LogWarning($"[BuildingRemovalHandler] Failed to remove building at origin: {originGridPos}. Building not found in temp data.");
        }
    }

    #endregion

    #region Visuals

    /// <summary>
    /// 건물을 하이라이트 표시합니다.
    /// </summary>
    private void HighlightBuilding(GameObject building)
    {
        if (building == null) return;

        SpriteRenderer renderer = building.GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            // 제거 모드이므로 빨간색(_invalidColor)을 사용하여 제거 가능함을 시각적으로 강조
            renderer.color = _invalidColor;
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
                // 원래 색상(흰색)으로 복원
                renderer.color = Color.white; 
            }
            _hoveredBuilding = null;
        }
    }

    #endregion
}