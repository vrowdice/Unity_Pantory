using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 메인·튜토리얼 건설 씬 러너. 그리드·배치를 담당합니다.
/// </summary>
public class MainRunner : RunnerBase
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

    [Header("UI")]
    [SerializeField] private MainCanvas _mainCanvas;

    private Camera _mainCamera;
    private MainCameraController _mainCameraController;

    private MainBuildingGridHandler _gridHandler;
    private MainTileGridHandler _tileGridHandler;
    private MainRawBuildingHandler _rawBuildingHandler;
    private MainResourceFlowHandler _resourceFlowHandler;
    private MainBuildingPlacementHandler _placementHandler;
    private MainBlueprintHandler _blueprintHandler;
    private Action<string> _syncRawBuildingsOnResearchCompleted;

    public MainBuildingGridHandler GridHandler => _gridHandler;
    public MainTileGridHandler TileGridHandler => _tileGridHandler;
    public MainRawBuildingHandler RawBuildingHandler => _rawBuildingHandler;
    public MainResourceFlowHandler ResourceFlowHandler => _resourceFlowHandler;
    public MainBuildingPlacementHandler PlacementHandler => _placementHandler;
    public MainBlueprintHandler BlueprintHandler => _blueprintHandler;

    public bool IsRestoringPlacedLayout { get; private set; }

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

    public virtual bool AllowRawBuildingPlacement => false;

    public bool IsGridCellOccupied(Vector2Int gridPosition)
    {
        return _gridHandler != null && _gridHandler.IsCellOccupied(gridPosition);
    }

    public bool TryGetPlacedObjectAt(Vector2Int gridPosition, out GameObject placedObject)
    {
        placedObject = null;
        return _gridHandler != null && _gridHandler.TryGetPlacedGameObjectAt(gridPosition, out placedObject);
    }

    public GameObject GetPlacedObjectAt(Vector2Int gridPosition)
    {
        TryGetPlacedObjectAt(gridPosition, out GameObject placedObject);
        return placedObject;
    }

    public bool TryGetRoadAt(Vector2Int gridPosition, out RoadObject road)
    {
        road = null;
        return _gridHandler != null && _gridHandler.TryGetRoadAt(gridPosition, out road);
    }

    public bool TryPlaceRoadAt(
        string buildingId,
        Vector2Int gridPosition,
        int rotation,
        out GameObject placed,
        out bool insufficientCredits)
    {
        placed = null;
        insufficientCredits = false;

        if (_gridHandler == null || DataManager == null)
            return false;

        BuildingData roadData = DataManager.Building.GetBuildingData(buildingId);
        if (roadData == null)
            return false;

        return _gridHandler.TryPlaceRoad(roadData, gridPosition, rotation, out placed, out insufficientCredits);
    }

    public bool TryPlaceBuildingAt(
        string buildingId,
        Vector2Int gridPosition,
        int rotation,
        out GameObject placed,
        out bool insufficientCredits)
    {
        placed = null;
        insufficientCredits = false;

        if (_gridHandler == null || DataManager == null)
            return false;

        BuildingData buildingData = DataManager.Building.GetBuildingData(buildingId);
        if (buildingData == null)
            return false;

        return _gridHandler.TryPlaceBuilding(buildingData, gridPosition, rotation, out placed, out insufficientCredits);
    }

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
    }

    public void CommitBlueprintSelection(List<PlacedBuildingSaveData> buildings, List<PlacedRoadSaveData> roads)
    {
        if (_mainCanvas == null)
            return;

        bool hasBuildings = buildings != null && buildings.Count > 0;
        bool hasRoads = roads != null && roads.Count > 0;
        if (!hasBuildings && !hasRoads)
            return;

        SetBlueprintMode(false);
        _mainCanvas.RequestSaveBlueprintEntry(buildings, roads);
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
        if (_tileGridHandler == null || _mainCamera == null)
            return;

        _tileGridHandler.RefreshTileZoomVisuals(_mainCamera);
    }

    public override void Init()
    {
        base.Init();
        InitBuildingScene();
    }

    protected virtual void InitBuildingScene()
    {
        _mainCamera = MainCamera;
        _mainCameraController = GameManager.MainCameraController;

        _gridHandler = CreateGridHandler();
        _tileGridHandler = new MainTileGridHandler(this, _gridHandler.TileParent);
        _rawBuildingHandler = new MainRawBuildingHandler(this, _gridHandler.BuildingParent);
        _resourceFlowHandler = new MainResourceFlowHandler(
            this,
            _gridHandler.RoadObjDict,
            _gridHandler.DualLaneRoadObjDict,
            _gridHandler.BuildingObjDict
        );
        _placementHandler = CreatePlacementHandler();
        _blueprintHandler = CreateBlueprintHandler();

        transform.position = new Vector3(0f, 0f, _cameraZOffset);

        _tileGridHandler.CreateGrid(_gridWidth, _gridHeight, null);
        SetCameraCollider();

        OnGridReady();

        DataManager.Time.OnHourChanged -= _resourceFlowHandler.OnMainHourChanged;
        DataManager.Time.OnHourChanged += _resourceFlowHandler.OnMainHourChanged;

        _syncRawBuildingsOnResearchCompleted = _ => _rawBuildingHandler.SyncRepresentativeRawBuildings();
        DataManager.OnResearchCompleted -= _syncRawBuildingsOnResearchCompleted;
        DataManager.OnResearchCompleted += _syncRawBuildingsOnResearchCompleted;

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
        IsRestoringPlacedLayout = true;

        try
        {
            DataManager.PlacedLayout.Consume(out List<PlacedBuildingSaveData> buildings,
                out List<PlacedRoadSaveData> roads);

            bool hasLayout = buildings.Count > 0 || roads.Count > 0;
            DataManager.Goal?.BeginLayoutRestore();

            if (hasLayout)
            {
                _gridHandler.RestoreFromSave(buildings, roads);
                _rawBuildingHandler.ApplyLayoutFromSave(buildings);
            }
            else
            {
                _rawBuildingHandler.SyncRepresentativeRawBuildings();
            }

            DataManager.Goal?.EndLayoutRestore();
        }
        finally
        {
            IsRestoringPlacedLayout = false;
        }
    }

    protected virtual void InitBuildSceneCanvas()
    {
        if (_mainCanvas != null)
        {
            _mainCanvas.Init(this);
        }
    }

    public void FlushPlacedLayoutToDataManager()
    {
        List<PlacedBuildingSaveData> allBuildings = new List<PlacedBuildingSaveData>();
        allBuildings.AddRange(_gridHandler.ExportPlacedBuildings());
        if (_rawBuildingHandler != null)
        {
            allBuildings.AddRange(_rawBuildingHandler.ExportPlacedBuildings());
        }

        DataManager.PlacedLayout.SetFromSave(
            allBuildings,
            _gridHandler.ExportPlacedRoads());
    }

    public void PlayBuildSound()
    {
        if (_buildSound != null && SoundManager != null)
            SoundManager.PlaySFX(_buildSound);
    }

    public void PlayRemovalSound()
    {
        if (_removalSound != null && SoundManager != null)
            SoundManager.PlaySFX(_removalSound);
    }

    public void PlayBuildFeedbackAt(Vector3 worldPosition, bool playSound)
    {
        if (playSound)
            PlayBuildSound();

        if (_buildParticlePrefab == null)
            return;

        Vector3 effectPosition = worldPosition;
        effectPosition.z = _buildEffectZ;
        UnityEngine.Object.Instantiate(_buildParticlePrefab, effectPosition, Quaternion.identity);
    }

    public void PlayRemovalFeedbackAt(Vector3 worldPosition, bool playSound)
    {
        if (playSound)
            PlayRemovalSound();

        if (_removalParticlePrefab == null)
            return;

        Vector3 effectPosition = worldPosition;
        effectPosition.z = _removalEffectZ;
        UnityEngine.Object.Instantiate(_removalParticlePrefab, effectPosition, Quaternion.identity);
    }

    public void PlayBuildingRemovalFeedback()
    {
        if (!_removalShakeCamera || _mainCameraController == null)
            return;

        _mainCameraController.ShakeForRemoval();
    }

    public void PlayGridBuildingBuildFeedback(Vector3 worldPosition, bool playSound, bool shakeCamera)
    {
        PlayBuildFeedbackAt(worldPosition, playSound);

        if (shakeCamera)
            GameManager.Instance?.MainCameraController?.ShakeForConstruction();
    }

    public void PlayGridBuildingRemovalFeedback(Vector3 worldPosition, bool playSound)
    {
        PlayRemovalFeedbackAt(worldPosition, playSound);
        PlayBuildingRemovalFeedback();
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
        if (_resourceFlowHandler != null)
        {
            DataManager.Time.OnHourChanged -= _resourceFlowHandler.OnMainHourChanged;
        }
        if (_syncRawBuildingsOnResearchCompleted != null)
            DataManager.OnResearchCompleted -= _syncRawBuildingsOnResearchCompleted;
    }
}
