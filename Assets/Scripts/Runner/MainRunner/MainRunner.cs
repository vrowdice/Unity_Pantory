using UnityEngine;

/// <summary>
/// 메인 씬의 건설(건물 배치) 러너.
/// 기존 Thread(타일) 배치 시스템을 제거하고 BuildingData 기반 설치/회전/삭제만 제공합니다.
/// </summary>
public class MainRunner : RunnerBase
{
    [Header("UI Manager")]
    [SerializeField] private MainCanvas _mainCanvas;

    [Header("Prefabs")]
    [SerializeField] private GameObject _tilePrefab;
    [SerializeField] private GameObject _buildingObjectPrefab;
    [SerializeField] private GameObject _roadObjectPrefab;

    [Header("Grid Settings")]
    [SerializeField] private int _gridWidth = 10;
    [SerializeField] private int _gridHeight = 10;
    [SerializeField] private float _cameraZOffset = 11f;

    private Camera _mainCamera;
    private MainCameraController _mainCameraController;

    private MainBuildingGridHandler _gridHandler;
    private MainBuildingPlacementHandler _placementHandler;

    public bool IsPlacementMode => _placementHandler.IsPlacementMode;
    public bool IsRemovalMode => _placementHandler.IsRemovalMode;

    public MainBuildingGridHandler GridHandler => _gridHandler;
    public MainBuildingPlacementHandler PlacementHandler => _placementHandler;

    public int GridWidth => _gridWidth;
    public int GridHeight => _gridHeight;
    public GameObject TilePrefab => _tilePrefab;
    public GameObject BuildingObjectPrefab => _buildingObjectPrefab;
    public GameObject RoadObjectPrefab => _roadObjectPrefab;

    public bool StartPlacementMode(BuildingData buildingData)
    {
        if (_placementHandler == null || buildingData == null) return false;
        _placementHandler.StartPlacement(buildingData);
        return true;
    }

    private void Update()
    {
        _placementHandler.Update(_mainCamera);
    }

    /// <summary>
    /// 매니저를 초기화하고 그리드 및 카메라 콜라이더를 설정합니다.
    /// </summary>
    override public void Init()
    {
        base.Init();

        _mainCamera = Camera.main;
        _mainCameraController = GameManager.MainCameraController;

        _gridHandler = new MainBuildingGridHandler(this);
        _placementHandler = new MainBuildingPlacementHandler(this);

        transform.position = new Vector3(-_gridWidth / 2f, _gridHeight / 2f, _cameraZOffset);

        _gridHandler.CreateGrid(_gridWidth, _gridHeight);
        SetCameraCollider();

        RestorePlacedLayoutIfAny();

        DataManager.Time.OnHourChanged -= _gridHandler.OnMainHourChanged;
        DataManager.Time.OnHourChanged += _gridHandler.OnMainHourChanged;

        _mainCanvas.Init(this);
    }

    private void RestorePlacedLayoutIfAny()
    {
        DataManager dataManager = DataManager.Instance;
        if (dataManager == null || dataManager.PlacedLayout == null || _gridHandler == null) return;

        dataManager.PlacedLayout.Consume(out System.Collections.Generic.List<PlacedBuildingSaveData> buildings,
            out System.Collections.Generic.List<PlacedRoadSaveData> roads);

        bool hasBuildings = buildings != null && buildings.Count > 0;
        bool hasRoads = roads != null && roads.Count > 0;
        if (!hasBuildings && !hasRoads) return;

        _gridHandler.RestoreFromSave(buildings, roads);
    }

    /// <summary>
    /// 카메라 이동 제한을 위한 그리드 범위 콜라이더를 설정합니다.
    /// </summary>
    public void SetCameraCollider()
    {
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        if (col == null)
            col = gameObject.AddComponent<BoxCollider2D>();

        col.offset = new Vector2(_gridWidth / 2f, -_gridHeight / 2f);
        col.size = new Vector2(_gridWidth, _gridHeight);

        if (_mainCameraController != null)
        {
            Vector3 gridWorldCenter = transform.position + new Vector3(_gridWidth / 2f, -_gridHeight / 2f, 0);
            Vector2 center = new Vector2(gridWorldCenter.x, gridWorldCenter.y);
            Vector2 size = new Vector2(_gridWidth, _gridHeight);
            _mainCameraController.SetBoundary(center, size);
        }
    }

    private void OnDisable()
    {
        if (_gridHandler != null && DataManager.Instance != null)
            DataManager.Instance.Time.OnHourChanged -= _gridHandler.OnMainHourChanged;
    }
}