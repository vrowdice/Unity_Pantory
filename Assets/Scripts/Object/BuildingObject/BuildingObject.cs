using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 그리드에 설치된 건물: 뷰, 입출력 큐, 시간 틱 시뮬, 직원, 클릭 UI.
/// </summary>
public partial class BuildingObject : MonoBehaviour, IResourceNode
{
    [SerializeField] private SpriteRenderer _viewObjRenderer;
    [SerializeField] private BoxCollider2D _collider;

    [Header("Output Indicators")]
    [SerializeField] private GameObject _outputIndicatorPrefab;
    [SerializeField] private float _outgoingIconScale = 0.01f;

    [Header("Player Selection")]
    [SerializeField] private ResourceData _selectedResource;

    [Header("Employees")]
    [SerializeField] private int _assignedWorkers;
    [SerializeField] private int _assignedTechnicians;

    private GameObject _outgoingIconContainer;
    private readonly Queue<ResourcePacket> _inputBuffer = new Queue<ResourcePacket>();
    private readonly Queue<ResourcePacket> _outputBuffer = new Queue<ResourcePacket>();

    private MainRunner _mainRunner;
    private BuildingData _buildingData;
    private Vector2Int _origin;
    private Vector2Int _size;
    private int _rotation;
    private float _workProgress;
    private int _maxInputCapacity;
    private int _maxOutputCapacity;

    public BuildingData BuildingData => _buildingData;
    public Vector2Int Origin => _origin;
    public Vector2Int Size => _size;
    public List<Vector2Int> OutputGridPositions { get; private set; }

    public int AssignedWorkers => _assignedWorkers;
    public int AssignedTechnicians => _assignedTechnicians;
    public int RequiredEmployeeSlots => Mathf.Max(0, _buildingData.requiredEmployees);
    public int RequiredTechnicianMinimum => _buildingData.isProfessional && _buildingData.requiredEmployees > 0 ? 1 : 0;
    public int MaxWorkerSlots => Mathf.Max(0, RequiredEmployeeSlots - RequiredTechnicianMinimum);
    public int MaxTechnicianSlots => RequiredEmployeeSlots;

    public void Init(MainRunner runner, BuildingData buildingData, Vector2Int origin, Vector2Int rotatedSize, int rotation)
    {
        _mainRunner = runner;
        _buildingData = buildingData;
        _origin = origin;
        _size = rotatedSize;
        _rotation = rotation;
        _maxInputCapacity = Mathf.Max(0, buildingData.maxInputBufferCapacity);
        _maxOutputCapacity = Mathf.Max(0, buildingData.maxOutputBufferCapacity);

        transform.localRotation = Quaternion.Euler(0f, 0f, -rotation * 90f);

        _viewObjRenderer.sprite = buildingData.buildingSprite;
        _viewObjRenderer.transform.localRotation = Quaternion.identity;
        _viewObjRenderer.transform.localScale = new Vector3(rotatedSize.x, rotatedSize.y, 1);
        _collider.size = new Vector2(rotatedSize.x, rotatedSize.y);
        _collider.offset = Vector2.zero;

        RebuildOutputGridPositions();
        UpdateOutputIndicators();
        RefreshOutgoingResourceIcons();

        if (_selectedResource == null && buildingData is RawMaterialFactoryData raw && raw.DefaultRawResource != null)
            _selectedResource = raw.DefaultRawResource;
    }

    private void Update()
    {
        if (!Input.GetMouseButtonDown(0)) return;
        if (_mainRunner != null && (_mainRunner.IsPlacementMode || _mainRunner.IsRemovalMode)) return;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(-1)) return;
        if (_collider == null) return;

        Camera cam = Camera.main;
        if (cam == null) return;

        Vector3 mouseWorld = cam.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0f;
        if (!_collider.OverlapPoint(mouseWorld)) return;

        UIManager.Instance?.ShowBuildingInfoPopup(this);
    }

    private void RebuildOutputGridPositions()
    {
        OutputGridPositions = BuildingCalculationUtils.GetOutputGridPositions(_origin, _size, _rotation);
    }

    private void UpdateOutputIndicators()
    {
        if (_outputIndicatorPrefab == null || _buildingData is LoadStationData || _buildingData is RawMaterialFactoryData) return;
        foreach (Vector3 localPos in BuildingCalculationUtils.GetOutputLocalPositions(_size))
        {
            GameObject indicator = Instantiate(_outputIndicatorPrefab, transform);
            indicator.transform.localPosition = localPos;
            indicator.transform.localRotation = Quaternion.identity;
        }
    }

    private static Dictionary<string, int> AggregateQueueCounts(Queue<ResourcePacket> queue)
    {
        Dictionary<string, int> counts = new Dictionary<string, int>();
        foreach (ResourcePacket p in queue)
        {
            if (p == null || string.IsNullOrEmpty(p.Id)) continue;
            if (counts.TryGetValue(p.Id, out int v)) counts[p.Id] = v + p.Amount;
            else counts[p.Id] = p.Amount;
        }

        return counts;
    }

    private void OnDestroy()
    {
        ClearOutgoingIconContainer();
    }
}
