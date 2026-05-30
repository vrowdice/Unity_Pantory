using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Main / Tutorial 씬 공통 그리드·배치 러너 베이스.
/// </summary>
public abstract class BuildingSceneRunnerBase : RunnerBase
{
    [SerializeField] private AudioClip _buildSound;
    [SerializeField] private AudioClip _removalSound;
    [SerializeField] private GameObject _buildParticlePrefab;
    [SerializeField] private GameObject _removalParticlePrefab;
    [SerializeField] private float _buildEffectZ = 8.5f;
    [SerializeField] private float _removalEffectZ = 8.5f;

    [Header("Removal feedback (DOTween)")]
    [SerializeField] private float _removalScaleDuration = 0.2f;
    [SerializeField] private float _removalPunchDuration = 0.08f;
    [SerializeField] private float _removalPunchStrength = 0.05f;
    [SerializeField] private bool _removalShakeCamera = true;

    [Header("Prefabs")]
    [SerializeField] private GameObject _previewPrefab;
    [SerializeField] private GameObject _blueprintPreviewPrefab;
    [SerializeField] private GameObject _tilePrefab;
    [SerializeField] private GameObject _buildingObjectPrefab;
    [SerializeField] private GameObject _rawBuildingObjectPrefab;
    [SerializeField] private GameObject _roadObjectPrefab;
    [SerializeField] private GameObject _dualLaneRoadObjectPrefab;

    [Header("Raw Buildings Spawning Offset")]
    [SerializeField] private float _rawBuildingsSpawnY = 2.0f;
    [SerializeField] private float _rawBuildingsStartX = 1.5f;
    [SerializeField] private float _rawBuildingsSpacingX = 3.0f;

    [Header("Grid Settings")]
    [SerializeField] private int _gridWidth = 10;
    [SerializeField] private int _gridHeight = 10;
    [SerializeField] private float _cameraZOffset = 11f;

    [Header("Tile Zoom LOD")]
    [Tooltip("카메라 orthographicSize가 이 값 이상이면 단일 그리드 스프라이트만 표시합니다 (줌 아웃).")]
    [SerializeField] private float _tileOverviewOrthographicSizeThreshold = 12f;
    [Tooltip("줌 아웃 오버뷰 타일 무늬 밀도. 1=그리드 칸당 약 1칸, 값을 올리면 더 촘촘하게 반복됩니다.")]
    [SerializeField] private float _tileOverviewPatternDensity = 1f;

    private Camera _mainCamera;
    private MainCameraController _mainCameraController;

    private MainBuildingGridHandler _gridHandler;
    private MainBuildingPlacementHandler _placementHandler;
    private MainBlueprintHandler _blueprintHandler;

    public MainBuildingGridHandler GridHandler => _gridHandler;
    public MainBuildingPlacementHandler PlacementHandler => _placementHandler;
    public MainBlueprintHandler BlueprintHandler => _blueprintHandler;

    public AudioClip BuildSound => _buildSound;
    public AudioClip RemovalSound => _removalSound;
    public GameObject BuildParticlePrefab => _buildParticlePrefab;
    public GameObject RemovalParticlePrefab => _removalParticlePrefab;
    public float BuildEffectZ => _buildEffectZ;
    public float RemovalEffectZ => _removalEffectZ;
    public float RemovalScaleDuration => Mathf.Max(0.01f, _removalScaleDuration);
    public float RemovalPunchDuration => Mathf.Max(0f, _removalPunchDuration);
    public float RemovalPunchStrength => Mathf.Max(0f, _removalPunchStrength);
    public int GridWidth => _gridWidth;
    public int GridHeight => _gridHeight;
    public float TileOverviewOrthographicSizeThreshold => _tileOverviewOrthographicSizeThreshold;
    public float TileOverviewPatternDensity => Mathf.Max(0.01f, _tileOverviewPatternDensity);
    public GameObject PreviewPrefab => _previewPrefab;
    public GameObject BlueprintPreviewPrefab => _blueprintPreviewPrefab;
    public GameObject TilePrefab => _tilePrefab;
    public GameObject BuildingObjectPrefab => _buildingObjectPrefab;
    public GameObject RawBuildingObjectPrefab => _rawBuildingObjectPrefab;
    public GameObject RoadObjectPrefab => _roadObjectPrefab;
    public GameObject DualLaneRoadObjectPrefab => _dualLaneRoadObjectPrefab;

    public float RawBuildingsSpawnY => _rawBuildingsSpawnY;
    public float RawBuildingsStartX => _rawBuildingsStartX;
    public float RawBuildingsSpacingX => _rawBuildingsSpacingX;

    public abstract IBuildSceneCanvas BuildSceneCanvas { get; }

    public bool StartPlacementMode(BuildingData buildingData)
    {
        _placementHandler.StartPlacement(buildingData);
        return true;
    }

    public bool StartBlueprintPlacementMode(string blueprintName, List<PlacedBuildingSaveData> blueprintBuildings, List<PlacedRoadSaveData> blueprintRoads)
    {
        _placementHandler.StartBlueprintPlacement(blueprintName, blueprintBuildings, blueprintRoads);
        return true;
    }

    public virtual void SetBlueprintMode(bool active)
    {
        _blueprintHandler.SetBlueprintMode(active);
        BuildSceneCanvas?.SyncBlueprintAddButtonSelected(active);
    }

    private void Update()
    {
        if (_placementHandler == null || _blueprintHandler == null)
        {
            return;
        }

        _placementHandler.Update(_mainCamera);
        _blueprintHandler.Update(_mainCamera);
    }

    private void LateUpdate()
    {
        if (_gridHandler == null || _mainCamera == null)
            return;

        _gridHandler.RefreshTileZoomVisuals(_mainCamera);
    }

    public override void Init()
    {
        base.Init();

        _mainCamera = MainCamera;
        _mainCameraController = GameManager.MainCameraController;

        _gridHandler = CreateGridHandler();
        _placementHandler = CreatePlacementHandler();
        _blueprintHandler = CreateBlueprintHandler();

        transform.position = new Vector3(0f, 0f, _cameraZOffset);

        _gridHandler.CreateGrid(_gridWidth, _gridHeight);
        SetCameraCollider();

        OnGridReady();

        DataManager.Time.OnHourChanged -= _gridHandler.OnMainHourChanged;
        DataManager.Time.OnHourChanged += _gridHandler.OnMainHourChanged;

        DataManager.OnResearchCompleted -= _gridHandler.OnResearchCompleted;
        DataManager.OnResearchCompleted += _gridHandler.OnResearchCompleted;

        InitBuildSceneCanvas();
    }

    protected virtual MainBuildingGridHandler CreateGridHandler()
    {
        return new MainBuildingGridHandler(this);
    }

    protected virtual MainBuildingPlacementHandler CreatePlacementHandler()
    {
        return new MainBuildingPlacementHandler(this);
    }

    protected virtual MainBlueprintHandler CreateBlueprintHandler()
    {
        return new MainBlueprintHandler(this);
    }

    protected virtual void OnGridReady()
    {
        RestorePlacedLayoutIfAny();
    }

    protected void RestorePlacedLayoutIfAny()
    {
        DataManager.PlacedLayout.Consume(out List<PlacedBuildingSaveData> buildings,
            out List<PlacedRoadSaveData> roads);

        if (buildings.Count == 0 && roads.Count == 0)
        {
            return;
        }

        _gridHandler.RestoreFromSave(buildings, roads);
    }

    protected abstract void InitBuildSceneCanvas();

    public void FlushPlacedLayoutToDataManager()
    {
        DataManager.PlacedLayout.SetFromSave(
            _gridHandler.ExportPlacedBuildings(),
            _gridHandler.ExportPlacedRoads());
    }

    /// <summary>
    /// 그리드 건물 제거 시 카메라 등 씬 피드백 (건설 ShakeForConstruction과 대칭).
    /// </summary>
    public void PlayBuildingRemovalFeedback()
    {
        if (!_removalShakeCamera || _mainCameraController == null)
            return;

        _mainCameraController.ShakeForRemoval();
    }

    public void SetCameraCollider()
    {
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        if (col == null)
        {
            col = gameObject.AddComponent<BoxCollider2D>();
        }

        float rawSpawnY = Mathf.Max(0f, _rawBuildingsSpawnY);
        float boundaryHeight = _gridHeight + rawSpawnY;
        float boundaryCenterY = -_gridHeight / 2f + rawSpawnY / 2f;

        col.offset = new Vector2(_gridWidth / 2f, boundaryCenterY);
        col.size = new Vector2(_gridWidth, boundaryHeight);

        Vector3 gridWorldCenter = transform.position + new Vector3(_gridWidth / 2f, boundaryCenterY, 0);
        Vector2 center = new Vector2(gridWorldCenter.x, gridWorldCenter.y);
        Vector2 size = new Vector2(_gridWidth, boundaryHeight);
        _mainCameraController.SetBoundary(center, size);
    }

    private void OnDisable()
    {
        if (_gridHandler != null)
        {
            DataManager.Time.OnHourChanged -= _gridHandler.OnMainHourChanged;
            DataManager.OnResearchCompleted -= _gridHandler.OnResearchCompleted;
        }
    }
}
