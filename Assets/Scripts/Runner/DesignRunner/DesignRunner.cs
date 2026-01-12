using System.Collections.Generic;
using System.IO;
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
    public DesignRunnerCaptureHandler CaptureHandler { get; private set; }

    public MainCameraController MainCameraController => _mainCameraController;
    public DesignCanvas DesignUiManager => _designCanvas;

    public GameObject BuildingTilePrefab => _buildingTilePrefab;
    public GameObject BuildingObjectPrefab => _buildingObjectPrefab;
    public GameObject InputMarkerPrefab => _inputMarkerPrefab;
    public GameObject OutputMarkerPrefab => _outputMarkerPrefab;
    public int GridWidth => _gridWidth;
    public int GridHeight => _gridHeight;

    private MainCameraController _mainCameraController;
    private SaveLoadManager _saveLoadManager;
    private string _currentThreadId = string.Empty;
    private List<BuildingState> _temporaryBuildingStates = new List<BuildingState>();
    private bool _isTemporaryDataDirty = false;

    public string CurrentThreadId => _currentThreadId;
    public bool IsPlacementMode => GridGenHandler?.IsPlacementActive ?? false;
    public bool IsRemovalMode => GridGenHandler?.IsRemovalActive ?? false;
    private bool IsThreadActive => !string.IsNullOrEmpty(_currentThreadId);

    private void Update()
    {
        if (IsPlacementMode)
        {
            UpdatePlacementMode();
        }
        else if (IsRemovalMode)
        {
            UpdateRemovalMode();
        }
        else if (IsThreadActive)
        {
            HandleBuildingClick();
        }
    }

    /// <summary>
    /// 배치 모드 상태에서 마우스 입력 및 미리보기를 처리합니다.
    /// </summary>
    private void UpdatePlacementMode()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        Vector3 mouseWorldPos = GetMouseWorldPosition();
        (Vector2Int gridPos, bool canPlace) = GridGenHandler.UpdatePlacement(mouseWorldPos);

        if (Input.GetMouseButtonDown(0) && canPlace)
        {
            PlaceBuilding(gridPos);
        }
        else if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
        {
            CancelPlacementMode();
        }

        if (Input.GetKeyDown(KeyCode.Q)) GridGenHandler.Rotate(false);
        if (Input.GetKeyDown(KeyCode.E)) GridGenHandler.Rotate(true);
    }

    /// <summary>
    /// 삭제 모드 상태에서 마우스 입력을 처리합니다.
    /// </summary>
    private void UpdateRemovalMode()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        Vector3 mouseWorldPos = GetMouseWorldPosition();
        GameObject hoveredBuilding = GridGenHandler.UpdateRemoval(mouseWorldPos);

        if (Input.GetMouseButtonDown(0) && hoveredBuilding != null)
        {
            if (hoveredBuilding.TryGetComponent(out BuildingObject comp))
            {
                if (RemoveBuilding(new Vector2Int(comp.BuildingState.positionX, comp.BuildingState.positionY)))
                {
                    GridGenHandler.ResetBuildingHighlight();
                }
            }
        }
        else if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
        {
            CancelRemovalMode();
        }
    }

    private Vector3 GetMouseWorldPosition()
    {
        Camera camera = MainCameraController?.Camera ?? MainCamera;
        Vector3 mouseWorldPos = camera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0f;
        return mouseWorldPos;
    }

    private void PlaceBuilding(Vector2Int gridPos)
    {
        BuildingData selectedBuilding = GridGenHandler.SelectedBuilding;
        if (selectedBuilding == null) return;

        int rotation = GridGenHandler.RotationIndex;
        BuildingState state = new BuildingState(selectedBuilding.id, gridPos, selectedBuilding, rotation);
        AddBuildingToTemp(state);
        RefreshBuildings();
    }

    private bool RemoveBuilding(Vector2Int origin)
    {
        if (RemoveBuildingFromTemp(origin))
        {
            RefreshBuildings();
            return true;
        }
        return false;
    }

    public bool StartPlacementMode(BuildingData buildingData)
    {
        if (buildingData == null) return false;

        if (IsRemovalMode) GridGenHandler.CancelRemoval();

        GridGenHandler.StartPlacement(buildingData);
        return true;
    }

    public void CancelPlacementMode() => GridGenHandler?.CancelPlacement();

    public void StartRemovalMode()
    {
        if (IsPlacementMode) GridGenHandler.CancelPlacement();
        GridGenHandler?.StartRemoval();
    }

    public void CancelRemovalMode() => GridGenHandler?.CancelRemoval();

    /// <summary>
    /// DesignRunner를 초기화합니다.
    /// </summary>
    override public void Init()
    {
        base.Init();

        _mainCameraController = MainCamera?.GetComponent<MainCameraController>();
        _saveLoadManager = SaveLoadManager.Instance;
        
        InitializeHandlers();
        SetupGridSystem();
        LoadInitialThreadState();

        _designCanvas.Init(this);
    }

    /// <summary>
    /// 모든 핸들러를 초기화합니다.
    /// </summary>
    private void InitializeHandlers()
    {
        List<BuildingState> currentStates = GetCurrentBuildingStates();

        GridGenHandler = new DesignRunnerGridHandler(this);
        GridGenHandler.RefreshCalculationData(currentStates);
        CaptureHandler = new DesignRunnerCaptureHandler(this);
    }

    /// <summary>
    /// 그리드 시스템을 설정합니다.
    /// </summary>
    private void SetupGridSystem()
    {
        if (GridGenHandler != null)
        {
            GridGenHandler.CreateGrid(_gridWidth, _gridHeight);
            SetPositionCenter();
            UpdateCameraCollider();
        }
    }

    /// <summary>
    /// 초기 스레드 상태를 로드합니다.
    /// </summary>
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
        }
        else
        {
            _temporaryBuildingStates = new List<BuildingState>();
        }

        RefreshBuildings();
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
        GridGenHandler.RefreshCalculationData(GetCurrentBuildingStates());

        GridGenHandler.CalculateProductionChain(
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
    /// <param name="newBuildingState">추가할 건물 상태</param>
    internal void AddBuildingToTemp(BuildingState newBuildingState)
    {
        if (newBuildingState == null)
        {
            return;
        }

        _temporaryBuildingStates.RemoveAll((BuildingState existingState) =>
            existingState.positionX == newBuildingState.positionX &&
            existingState.positionY == newBuildingState.positionY);

        _temporaryBuildingStates.Add(newBuildingState);
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
        int removedCount = _temporaryBuildingStates.RemoveAll((BuildingState buildingState) =>
            buildingState.positionX == gridPosition.x && buildingState.positionY == gridPosition.y);

        if (removedCount > 0)
        {
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

    /// <summary>
    /// 현재 편집 중인 스레드를 변경합니다.
    /// </summary>
    /// <param name="threadIdentifier">변경할 스레드 식별자</param>
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

        RefreshBuildings();
    }

    /// <summary>
    /// 스레드 변경사항을 저장합니다.
    /// </summary>
    /// <param name="threadName">스레드 이름</param>
    /// <param name="categoryIdentifier">카테고리 식별자</param>
    public void SaveThreadChanges(string threadName, string categoryIdentifier)
    {
        string newThreadIdentifier = _designCanvas.GetThreadIdFromTitle(threadName);
        DataManager.Thread.CreateThread(newThreadIdentifier, threadName);

        DataManager.Thread.OverwriteBuildings(newThreadIdentifier, _temporaryBuildingStates);
        _currentThreadId = newThreadIdentifier;
        _isTemporaryDataDirty = false;

        ProcessPostSaveLogic(newThreadIdentifier, categoryIdentifier);

        _saveLoadManager.Thread.SaveThreadData(DataManager.Thread);
        SetCurrentThread(newThreadIdentifier);
    }

    /// <summary>
    /// 저장 후 처리 로직을 수행합니다.
    /// </summary>
    /// <param name="threadIdentifier">스레드 식별자</param>
    /// <param name="categoryIdentifier">카테고리 식별자</param>
    private void ProcessPostSaveLogic(string threadIdentifier, string categoryIdentifier)
    {
        if (!string.IsNullOrEmpty(categoryIdentifier))
        {
            DataManager.Thread.AddThreadToCategory(categoryIdentifier, threadIdentifier);
        }

        ThreadState threadState = DataManager.Thread.GetThread(threadIdentifier);
        if (threadState != null)
        {
            if (!string.IsNullOrEmpty(threadState.previewImagePath) && File.Exists(threadState.previewImagePath))
            {
                File.Delete(threadState.previewImagePath);
            }
            threadState.previewImagePath = CaptureHandler.CaptureThreadLayout(threadIdentifier, _temporaryBuildingStates);
            threadState.totalMaintenanceCost = CalculateTotalMaintenanceCost(threadIdentifier);
            threadState.requiredEmployees = DataManager.ThreadPlacement.CalculateRequiredEmployees(threadIdentifier, _temporaryBuildingStates);
        }
    }

    /// <summary>
    /// 배치된 모든 건물을 새로고침합니다.
    /// </summary>
    public void RefreshBuildings()
    {
        GridGenHandler.ClearAllPlacedBuildings();
        GridGenHandler.RefreshCalculationData(_temporaryBuildingStates);

        foreach (BuildingState buildingState in _temporaryBuildingStates)
        {
            BuildingData buildingData = DataManager.Building.GetBuildingData(buildingState.buildingId);
            if (buildingData != null)
            {
                GridGenHandler.CreateBuildingObject(new Vector2Int(buildingState.positionX, buildingState.positionY), buildingData, buildingState);
                GridGenHandler.MarkTilesAsOccupied(new Vector2Int(buildingState.positionX, buildingState.positionY), GridMathUtility.GetRotatedSize(buildingData.size, buildingState.rotation));
            }
        }
    }

    /// <summary>
    /// 건물 클릭을 처리합니다.
    /// </summary>
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
                Vector2Int rotatedSize = GridMathUtility.GetRotatedSize(buildingData.size, state.rotation);
                return gridPosition.x >= state.positionX && gridPosition.x < state.positionX + rotatedSize.x &&
                       gridPosition.y >= state.positionY && gridPosition.y < state.positionY + rotatedSize.y;
            });

            if (clickedBuildingState != null)
            {
                ShowBuildingInfo(clickedBuildingState);
            }
        }
    }

    /// <summary>
    /// 건물 정보를 표시합니다.
    /// </summary>
    /// <param name="buildingState">건물 상태</param>
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

    /// <summary>
    /// 스프라이트 스케일을 계산합니다.
    /// </summary>
    /// <param name="buildingSprite">건물 스프라이트</param>
    /// <param name="targetSize">목표 크기</param>
    /// <returns>계산된 스케일</returns>
    public Vector3 CalculateSpriteScale(Sprite buildingSprite, Vector2Int targetSize)
    {
        if (buildingSprite == null) return Vector3.one;
        float scaleX = (float)targetSize.x / buildingSprite.bounds.size.x;
        float scaleY = (float)targetSize.y / buildingSprite.bounds.size.y;
        return new Vector3(scaleX, scaleY, 1f);
    }


    /// <summary>
    /// 스레드의 총 유지비를 계산합니다.
    /// </summary>
    /// <param name="threadIdentifier">스레드 식별자</param>
    /// <returns>총 유지비</returns>
    public int CalculateTotalMaintenanceCost(string threadIdentifier)
    {
        return DataManager.ThreadPlacement?.CalculateTotalMaintenanceCost(threadIdentifier, GetCurrentBuildingStates()) ?? 0;
    }

    /// <summary>
    /// 그리드 중심 위치로 설정합니다.
    /// </summary>
    public void SetPositionCenter()
    {
        transform.position = new Vector3(-_gridWidth / 2f, _gridHeight / 2f, _cameraZOffset);
    }

    /// <summary>
    /// 카메라 콜라이더를 업데이트합니다.
    /// </summary>
    public void UpdateCameraCollider()
    {
        BoxCollider2D boxCollider;
        if (TryGetComponent<BoxCollider2D>(out boxCollider))
        {
            boxCollider.offset = new Vector2(_gridWidth / 2f, -_gridHeight / 2f);
            boxCollider.size = new Vector2(_gridWidth, _gridHeight);
        }

        if (_mainCameraController != null)
        {
            Vector3 gridWorldCenter = transform.position + new Vector3(_gridWidth / 2f, -_gridHeight / 2f, 0);
            Vector2 center = new Vector2(gridWorldCenter.x, gridWorldCenter.y);
            Vector2 size = new Vector2(_gridWidth, _gridHeight);
            _mainCameraController.SetBoundary(center, size);
        }
    }
}