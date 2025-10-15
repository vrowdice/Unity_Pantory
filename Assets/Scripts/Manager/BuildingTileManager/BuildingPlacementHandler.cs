using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 건물 배치 모드를 처리하는 핸들러
/// </summary>
public class BuildingPlacementHandler
{
    private readonly BuildingGridHandler _gridManager;
    private readonly GameDataManager _dataManager;
    private readonly Transform _parentTransform;
    private readonly Camera _mainCamera;

    private bool _isActive = false;
    private BuildingData _selectedBuilding = null;
    private GameObject _previewObject = null;
    private SpriteRenderer _previewRenderer = null;
    private Vector2Int _currentGridPos;
    private bool _canPlace = false;

    // Preview Settings
    private Color _validColor = new Color(0, 1, 0, 0.5f);
    private Color _invalidColor = new Color(1, 0, 0, 0.5f);

    public bool IsActive => _isActive;
    public BuildingData SelectedBuilding => _selectedBuilding;

    public BuildingPlacementHandler(BuildingGridHandler gridManager, GameDataManager dataManager, Transform parentTransform, Camera mainCamera)
    {
        _gridManager = gridManager;
        _dataManager = dataManager;
        _parentTransform = parentTransform;
        _mainCamera = mainCamera;
    }

    /// <summary>
    /// Preview 색상을 설정합니다.
    /// </summary>
    public void SetPreviewColors(Color validColor, Color invalidColor)
    {
        _validColor = validColor;
        _invalidColor = invalidColor;
    }

    /// <summary>
    /// 배치 모드를 시작합니다.
    /// </summary>
    public void StartPlacement(BuildingData buildingData)
    {
        _isActive = true;
        _selectedBuilding = buildingData;
        
        CreatePreviewObject();
        _gridManager.SetAllTilesOutline(true);  // 타일 윤곽선 표시
        Debug.Log($"[BuildingPlacementHandler] Placement mode started: {buildingData.displayName}");
    }

    /// <summary>
    /// 배치 모드를 취소합니다.
    /// </summary>
    public void CancelPlacement()
    {
        _isActive = false;
        _selectedBuilding = null;
        
        DestroyPreviewObject();
        _gridManager.SetAllTilesOutline(false);  // 타일 윤곽선 숨김
        Debug.Log("[BuildingPlacementHandler] Placement mode cancelled");
    }

    /// <summary>
    /// 배치 모드를 업데이트합니다 (매 프레임 호출).
    /// </summary>
    public void Update()
    {
        if (!_isActive)
            return;

        UpdatePreview();
        HandleInput();
    }

    /// <summary>
    /// 프리뷰를 업데이트합니다.
    /// </summary>
    private void UpdatePreview()
    {
        if (_previewObject == null || _selectedBuilding == null)
            return;

        // 마우스가 UI 위에 있는지 확인
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            // UI 위에 있으면 프리뷰 숨기기
            _previewRenderer.enabled = false;
            _canPlace = false;
            return;
        }

        // 프리뷰 다시 보이기
        _previewRenderer.enabled = true;

        // 마우스 위치를 월드 좌표로 변환
        Vector3 mouseWorldPos = _mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0;

        // 그리드 좌표로 변환
        Vector2Int gridPos = _gridManager.WorldToGridPosition(mouseWorldPos);
        _currentGridPos = gridPos;

        // 배치 가능 여부 체크
        _canPlace = _gridManager.CanPlaceBuilding(gridPos, _selectedBuilding.size);

        // 프리뷰 위치 및 색상 업데이트
        Vector3 worldPos = _gridManager.GridToWorldPosition(gridPos, _selectedBuilding.size);
        _previewObject.transform.position = worldPos;
        _previewRenderer.color = _canPlace ? _validColor : _invalidColor;
    }

    /// <summary>
    /// 입력을 처리합니다.
    /// </summary>
    private void HandleInput()
    {
        // 왼쪽 클릭 - 건물 배치
        if (Input.GetMouseButtonDown(0) && _canPlace)
        {
            PlaceBuilding(_currentGridPos, _selectedBuilding);
        }

        // 오른쪽 클릭 또는 ESC - 취소
        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
        {
            CancelPlacement();
        }
    }

    /// <summary>
    /// 건물을 배치합니다.
    /// </summary>
    private void PlaceBuilding(Vector2Int gridPos, BuildingData buildingData)
    {
        if (!_gridManager.CanPlaceBuilding(gridPos, buildingData.size))
        {
            Debug.LogWarning("[BuildingPlacementHandler] Cannot place building at this position");
            return;
        }

        // BuildingState 생성 및 ThreadService에 추가
        BuildingState buildingState = new BuildingState(buildingData.id, gridPos);
        
        // 건물 오브젝트 생성
        _gridManager.CreateBuildingObject(gridPos, buildingData);
        
        // 배치된 타일 차지 표시
        _gridManager.MarkTilesAsOccupied(gridPos, buildingData.size);
        
        Debug.Log($"[BuildingPlacementHandler] Building placed: {buildingData.displayName} at {gridPos}");
    }

    /// <summary>
    /// 건물을 배치하고 데이터에 추가합니다.
    /// </summary>
    public void PlaceBuildingWithData(Vector2Int gridPos, BuildingData buildingData, string currentThreadId)
    {
        if (!_gridManager.CanPlaceBuilding(gridPos, buildingData.size))
        {
            Debug.LogWarning("[BuildingPlacementHandler] Cannot place building at this position");
            return;
        }

        // BuildingState 생성 및 ThreadService에 추가
        BuildingState buildingState = new BuildingState(buildingData.id, gridPos);
        if (_dataManager.AddBuildingToThread(currentThreadId, buildingState))
        {
            // 건물 오브젝트 생성
            _gridManager.CreateBuildingObject(gridPos, buildingData);
            
            // 배치된 타일 차지 표시
            _gridManager.MarkTilesAsOccupied(gridPos, buildingData.size);
            
            Debug.Log($"[BuildingPlacementHandler] Building placed: {buildingData.displayName} at {gridPos}");
        }
    }

    /// <summary>
    /// 프리뷰 오브젝트를 생성합니다.
    /// </summary>
    private void CreatePreviewObject()
    {
        if (_selectedBuilding == null || _selectedBuilding.buildingSprite == null)
        {
            Debug.LogWarning("[BuildingPlacementHandler] Cannot create preview object");
            return;
        }

        _previewObject = new GameObject("BuildingPreview");
        _previewObject.transform.SetParent(_parentTransform);
        _previewRenderer = _previewObject.AddComponent<SpriteRenderer>();
        _previewRenderer.sprite = _selectedBuilding.buildingSprite;
        _previewRenderer.sortingOrder = 100; // 가장 위에 표시
        
        // 프리뷰 크기를 타일 크기에 맞춤 (1타일 = 1유닛)
        Vector3 scale = CalculateSpriteScale(_selectedBuilding.buildingSprite, _selectedBuilding.size);
        _previewObject.transform.localScale = scale;
    }

    /// <summary>
    /// 프리뷰 오브젝트를 삭제합니다.
    /// </summary>
    private void DestroyPreviewObject()
    {
        if (_previewObject != null)
        {
            Object.Destroy(_previewObject);
            _previewObject = null;
            _previewRenderer = null;
        }
    }

    /// <summary>
    /// 스프라이트를 타일 크기에 맞게 스케일을 계산합니다.
    /// </summary>
    private Vector3 CalculateSpriteScale(Sprite sprite, Vector2Int targetSize)
    {
        if (sprite == null)
            return Vector3.one;

        float spriteWidth = sprite.bounds.size.x;
        float spriteHeight = sprite.bounds.size.y;

        float targetWidth = targetSize.x;
        float targetHeight = targetSize.y;

        float scaleX = targetWidth / spriteWidth;
        float scaleY = targetHeight / spriteHeight;

        return new Vector3(scaleX, scaleY, 1f);
    }
}

