using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 건물 타일 시스템의 메인 조율자입니다.
/// 그리드 생성, 배치/삭제 모드 전환, 임시 데이터 관리 및 최종 저장 로직을 담당합니다.
/// </summary>
public class DesignRunner : RunnerBase
{
    [Header("Managers & UI")]
    [SerializeField] private DesignCanvas _designCanvas;

    [Header("Prefabs")]
    [SerializeField] private GameObject _buildingTilePrefab;
    [SerializeField] private GameObject _buildingObjectPrefab;
    [SerializeField] private GameObject _inputMarkerPrefab;
    [SerializeField] private GameObject _outputMarkerPrefab;
    [SerializeField] private GameObject _resourceItemPrefab;

    [Header("Grid Settings")]
    [SerializeField] private int _gridWidth = 10;
    [SerializeField] private int _gridHeight = 10;
    [SerializeField] private float _cameraZOffset = 11f;

    public DesignRunnerGridHandler GridGenHandler { get; private set; }
    public DesignRunnerPlacementHandler PlacementHandler { get; private set; }
    public DesignRunnerRemovalHandler RemovalHandler { get; private set; }
    public DesignRunnerCalculateHandler CalculateHandler { get; private set; }
    public DesignRunnerCaptureHandler CaptureHandler { get; private set; }

    public MainCameraController MainCameraController => _mainCameraController;
    public DesignCanvas DesignUiManager => _designCanvas;

    public GameObject InputMarkerPrefab => _inputMarkerPrefab;
    public GameObject OutputMarkerPrefab => _outputMarkerPrefab;

    private MainCameraController _mainCameraController;
    private string _currentThreadId = string.Empty;
    private List<BuildingState> _temporaryBuildingStates = new List<BuildingState>();
    private bool _isTemporaryDataDirty = false;

    public string CurrentThreadId => _currentThreadId;
    public bool IsPlacementMode => PlacementHandler?.IsActive ?? false;
    public bool IsRemovalMode => RemovalHandler?.IsActive ?? false;
    private bool IsThreadActive => !string.IsNullOrEmpty(_currentThreadId);

    private void Update()
    {
        if (!IsThreadActive && !IsPlacementMode && !IsRemovalMode)
        {
            return;
        }

        HandleInput();
    }

    private void HandleInput()
    {
        // 1. 배치 모드 업데이트
        if (PlacementHandler != null)
        {
            PlacementHandler.Update();
        }

        // 2. 삭제 모드 업데이트 (스레드가 활성화된 경우만)
        if (IsThreadActive && RemovalHandler != null)
        {
            RemovalHandler.Update(_currentThreadId);
        }

        // 3. 일반 상태에서의 클릭 처리 (모드 중이 아닐 때)
        if (!IsPlacementMode && !IsRemovalMode && IsThreadActive)
        {
            HandleBuildingClick();
        }
    }

    override public void Init()
    {
        base.Init();

        InitializeHandlers();
        SetupGridSystem();
        LoadInitialThreadState();

        _designCanvas.Init(this);
    }

    private void InitializeHandlers()
    {
        // 핸들러 생성 시 현재 상태 리스트를 참조할 수 있도록 전달
        List<BuildingState> currentStates = GetCurrentBuildingStates();

        GridGenHandler = new DesignRunnerGridHandler(this, _buildingTilePrefab, _buildingObjectPrefab, _inputMarkerPrefab, _outputMarkerPrefab, _gridWidth, _gridHeight);
        PlacementHandler = new DesignRunnerPlacementHandler(this, _buildingObjectPrefab);
        RemovalHandler = new DesignRunnerRemovalHandler(this);
        CalculateHandler = new DesignRunnerCalculateHandler(this, currentStates);
        CaptureHandler = new DesignRunnerCaptureHandler(this, currentStates);
    }

    private void SetupGridSystem()
    {
        if (GridGenHandler != null)
        {
            GridGenHandler.CreateGrid(_gridWidth, _gridHeight);
            SetPositionCenter();
            UpdateCameraCollider();
        }
    }

    private void LoadInitialThreadState()
    {
        _currentThreadId = GameManager.CurrentThreadId;

        if (string.IsNullOrEmpty(_currentThreadId))
        {
            _temporaryBuildingStates = new List<BuildingState>();
            return;
        }

        List<BuildingState> buildingStates = DataManager.Thread.GetBuildingStates(_currentThreadId);
        if (buildingStates != null)
        {
            _temporaryBuildingStates = new List<BuildingState>(buildingStates);
            RefreshBuildings();
        }
        else
        {
            _temporaryBuildingStates = new List<BuildingState>();
        }
    }

    /// <summary>
    /// 현재 스레드의 생산 체인을 계산합니다. (입력/출력 자원 및 수량)
    /// </summary>
    public void CalculateProductionChain(
        string threadIdentifier,
        out List<string> inputResourceIdentifiers,
        out Dictionary<string, int> inputResourceCounts,
        out List<string> outputResourceIdentifiers,
        out Dictionary<string, int> outputResourceCounts)
    {
        inputResourceIdentifiers = new List<string>();
        inputResourceCounts = new Dictionary<string, int>();
        outputResourceIdentifiers = new List<string>();
        outputResourceCounts = new Dictionary<string, int>();

        CalculateHandler = new DesignRunnerCalculateHandler(this, GetCurrentBuildingStates());

        // 실제 계산은 핸들러에게 위임
        CalculateHandler.CalculateProductionChain(
            threadIdentifier,
            GetCurrentBuildingStates(),
            out inputResourceIdentifiers,
            out inputResourceCounts,
            out outputResourceIdentifiers,
            out outputResourceCounts
        );
    }

    /// <summary>
    /// 새로운 건물을 임시 데이터 리스트에 추가합니다. 
    /// 동일한 위치에 건물이 이미 있다면 기존 데이터를 제거하고 새로 추가합니다.
    /// </summary>
    internal void AddBuildingToTemp(BuildingState newBuildingState)
    {
        if (newBuildingState == null)
        {
            return;
        }

        // 1. 중복 위치 확인: 새 건물이 들어올 자리에 이미 데이터가 있다면 삭제 (덮어쓰기 로직)
        // 멀티 타일 건물일 경우 원점(PositionX, PositionY)을 기준으로 관리합니다.
        _temporaryBuildingStates.RemoveAll((BuildingState existingState) =>
            existingState.positionX == newBuildingState.positionX &&
            existingState.positionY == newBuildingState.positionY);

        // 2. 리스트에 추가
        _temporaryBuildingStates.Add(newBuildingState);

        // 3. 데이터가 변경되었음을 표시
        _isTemporaryDataDirty = true;

        Debug.Log("[BuildingTileManager] New building added to temporary state: " + newBuildingState.buildingId + " at " + newBuildingState.positionX + ", " + newBuildingState.positionY);
    }

    /// <summary>
    /// 임시 데이터 리스트에서 특정 좌표에 위치한 건물을 제거합니다.
    /// </summary>
    /// <param name="gridPosition">제거할 건물의 원점(Origin) 좌표</param>
    /// <returns>삭제 성공 여부</returns>
    internal bool RemoveBuildingFromTemp(Vector2Int gridPosition)
    {
        // 리스트에서 해당 좌표와 일치하는 BuildingState를 모두 삭제
        int removedCount = _temporaryBuildingStates.RemoveAll((BuildingState buildingState) =>
            buildingState.positionX == gridPosition.x && buildingState.positionY == gridPosition.y);

        if (removedCount > 0)
        {
            // 데이터가 변경되었음을 표시 (저장 시 확인용)
            _isTemporaryDataDirty = true;

            Debug.Log("[BuildingTileManager] Building removed from temporary states at: " + gridPosition);
            return true;
        }

        return false;
    }

    /// <summary>
    /// 현재 편집 중인 세션의 모든 건물 상태 리스트를 반환합니다.
    /// </summary>
    public List<BuildingState> GetCurrentBuildingStates()
    {
        return _temporaryBuildingStates;
    }

    public void SetCurrentThread(string threadIdentifier)
    {
        if (string.IsNullOrEmpty(threadIdentifier) || _currentThreadId == threadIdentifier)
        {
            return;
        }

        if (_isTemporaryDataDirty)
        {
            Debug.Log("[BuildingTileManager] Discarding dirty data for: " + _currentThreadId);
        }

        _currentThreadId = threadIdentifier;
        _isTemporaryDataDirty = false;

        List<BuildingState> buildingStatesFromData = DataManager.Thread.GetBuildingStates(threadIdentifier);
        _temporaryBuildingStates = (buildingStatesFromData != null) ? new List<BuildingState>(buildingStatesFromData) : new List<BuildingState>();

        // 핸들러들에 갱신된 상태 리스트 반영 (필요 시 핸들러 내부 데이터 업데이트 메서드 호출)
        RefreshBuildings();
    }

    public void SaveThreadChanges(string threadName, string categoryIdentifier)
    {
        if (_temporaryBuildingStates.Count == 0)
        {
            Debug.LogWarning("[BuildingTileManager] No buildings to save.");
            return;
        }

        string newThreadIdentifier = _designCanvas.GetThreadIdFromTitle(threadName);
        DataManager.Thread.CreateThread(newThreadIdentifier, threadName);

        DataManager.Thread.OverwriteBuildings(newThreadIdentifier, _temporaryBuildingStates);
        _currentThreadId = newThreadIdentifier;
        _isTemporaryDataDirty = false;

        ProcessPostSaveLogic(newThreadIdentifier, categoryIdentifier);

        DataManager.Thread.Save();
        SetCurrentThread(newThreadIdentifier);
    }

    private void ProcessPostSaveLogic(string threadIdentifier, string categoryIdentifier)
    {
        if (!string.IsNullOrEmpty(categoryIdentifier))
        {
            DataManager.Thread.AddThreadToCategory(categoryIdentifier, threadIdentifier);
        }

        ThreadState threadState = DataManager.Thread.GetThread(threadIdentifier);
        if (threadState != null)
        {
            threadState.previewImagePath = CaptureHandler?.CaptureThreadLayout(threadIdentifier);
            threadState.totalMaintenanceCost = CalculateTotalMaintenanceCost(threadIdentifier);
            threadState.requiredEmployees = DataManager.ThreadCalculate.CalculateRequiredEmployees(threadIdentifier, _temporaryBuildingStates);
        }
    }

    public void RefreshBuildings()
    {
        GridGenHandler.ClearAllPlacedBuildings();

        foreach (BuildingState buildingState in _temporaryBuildingStates)
        {
            BuildingData buildingData = DataManager.Building.GetBuildingData(buildingState.buildingId);
            if (buildingData != null)
            {
                GridGenHandler.CreateBuildingObject(new Vector2Int(buildingState.positionX, buildingState.positionY), buildingData, buildingState);
                GridGenHandler.MarkTilesAsOccupied(new Vector2Int(buildingState.positionX, buildingState.positionY), GetRotatedSize(buildingData.size, buildingState.rotation));
            }
        }
    }

    private void HandleBuildingClick()
    {
        if (EventSystem.current?.IsPointerOverGameObject() == true)
        {
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mouseWorldPosition = MainCamera.ScreenToWorldPoint(Input.mousePosition);
            Vector2Int gridPosition = GridGenHandler.WorldToGridPosition(mouseWorldPosition);

            BuildingState clickedBuildingState = _temporaryBuildingStates.Find((BuildingState state) =>
            {
                BuildingData buildingData = DataManager.Building.GetBuildingData(state.buildingId);
                Vector2Int rotatedSize = GetRotatedSize(buildingData.size, state.rotation);
                return gridPosition.x >= state.positionX && gridPosition.x < state.positionX + rotatedSize.x &&
                       gridPosition.y >= state.positionY && gridPosition.y < state.positionY + rotatedSize.y;
            });

            if (clickedBuildingState != null)
            {
                ShowBuildingInfo(clickedBuildingState);
            }
        }
    }

    private void ShowBuildingInfo(BuildingState buildingState)
    {
        BuildingData buildingData = DataManager.Building.GetBuildingData(buildingState.buildingId);
        if (buildingState.IsUnlocked(DataManager))
        {
            _designCanvas.ShowBuildingInfo(buildingData, buildingState);
        }
        else
        {
            GameManager.ShowWarningPanel("Locked: " + buildingData.displayName);
        }
    }

    // 약어 없이 전체 타입 명시
    public Vector3 CalculateSpriteScale(Sprite buildingSprite, Vector2Int targetSize)
    {
        if (buildingSprite == null) return Vector3.one;
        float scaleX = (float)targetSize.x / buildingSprite.bounds.size.x;
        float scaleY = (float)targetSize.y / buildingSprite.bounds.size.y;
        return new Vector3(scaleX, scaleY, 1f);
    }

    private Vector2Int GetRotatedSize(Vector2Int size, int rotation)
    {
        return (rotation % 4 == 1 || rotation % 4 == 3) ? new Vector2Int(size.y, size.x) : size;
    }

    public int CalculateTotalMaintenanceCost(string threadIdentifier)
    {
        return DataManager.ThreadCalculate?.CalculateTotalMaintenanceCost(threadIdentifier, GetCurrentBuildingStates()) ?? 0;
    }

    public void SetPositionCenter()
    {
        transform.position = new Vector3(-_gridWidth / 2f, _gridHeight / 2f, _cameraZOffset);
    }

    public void UpdateCameraCollider()
    {
        BoxCollider2D boxCollider;
        if (TryGetComponent<BoxCollider2D>(out boxCollider))
        {
            boxCollider.offset = new Vector2(_gridWidth / 2f, -_gridHeight / 2f);
            boxCollider.size = new Vector2(_gridWidth, _gridHeight);
        }
    }
}