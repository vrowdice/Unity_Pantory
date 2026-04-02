using UnityEngine;
using UnityEngine.EventSystems;

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

    private DataManager _dataManager;
    private Camera _mainCamera;
    private MainCameraController _mainCameraController;
    private BoxCollider2D _cameraCollider;

    private MainBuildingGridHandler _gridHandler;
    private MainBuildingPlacementHandler _placementHandler;
    private MainResourceHandler _resourceHandler;

    public bool IsPlacementMode => _placementHandler.IsPlacementMode;
    public bool IsRemovalMode => _placementHandler.IsRemovalMode;

    public MainBuildingGridHandler GridHandler => _gridHandler;
    public MainBuildingPlacementHandler PlacementHandler => _placementHandler;
    public MainResourceHandler ResourceHandler => _resourceHandler;

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

        DataManager.Time.OnHourChanged -= _gridHandler.TickResourceFlow;
        DataManager.Time.OnHourChanged += _gridHandler.TickResourceFlow;

        _mainCanvas.Init(this);
    }

    /// <summary>
    /// 카메라 이동 제한을 위한 그리드 범위 콜라이더를 설정합니다.
    /// </summary>
    public void SetCameraCollider()
    {
        _cameraCollider = GetComponent<BoxCollider2D>();
        if (_cameraCollider == null)
        {
            _cameraCollider = gameObject.AddComponent<BoxCollider2D>();
        }

        _cameraCollider.offset = new Vector2(_gridWidth / 2f, -_gridHeight / 2f);
        _cameraCollider.size = new Vector2(_gridWidth, _gridHeight);

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
        _dataManager.Time.OnHourChanged -= _gridHandler.TickResourceFlow;
    }
}