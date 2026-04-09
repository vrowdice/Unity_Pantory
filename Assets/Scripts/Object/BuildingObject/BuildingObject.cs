using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

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
    private bool _removalAnimating;

    private const float PlaceScaleDuration = 0.22f;
    private const float PlacePunchDuration = 0.12f;
    private const float PlacePunchStrength = 0.06f;
    private const float RemoveScaleDuration = 0.2f;

    private bool _clickArmed;
    private Vector3 _mouseDownScreenPos;
    private const float ClickDragThresholdPixels = 8f;

    public BuildingData BuildingData => _buildingData;
    public bool IsRemovalAnimating => _removalAnimating;
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

    /// <summary>
    /// 배치 직후 호출: 로컬 스케일 0에서 target까지 스케일 업 후 살짝 펀치.
    /// </summary>
    public void PlayPlaceEntranceAnimation(Vector3 targetLocalScale)
    {
        transform.DOKill(false);

        Vector3 t = targetLocalScale;
        if (t.x == 0f && t.y == 0f && t.z == 0f) t = Vector3.one;

        Sequence seq = DOTween.Sequence();
        seq.SetLink(gameObject);
        seq.SetUpdate(true);
        seq.Append(transform.DOScale(t, PlaceScaleDuration).SetEase(Ease.OutBack));
        seq.Append(transform.DOPunchScale(Vector3.one * PlacePunchStrength, PlacePunchDuration, 10, 0.35f));
    }

    /// <summary>
    /// 제거 시 스케일 다운 후 콜백. 점유 해제·파괴는 콜백에서 처리.
    /// </summary>
    public void PlayRemovalAnimation(Action onComplete)
    {
        if (_removalAnimating) return;

        _removalAnimating = true;
        if (_collider != null) _collider.enabled = false;

        transform.DOKill(false);

        transform.DOScale(Vector3.zero, RemoveScaleDuration)
            .SetEase(Ease.InBack)
            .SetUpdate(true)
            .SetLink(gameObject)
            .OnComplete(() =>
            {
                _removalAnimating = false;
                onComplete?.Invoke();
            });
    }

    private void Update()
    {
        if (_mainRunner != null && (_mainRunner.IsPlacementMode || _mainRunner.IsRemovalMode)) return;
        if (_collider == null) return;

        if (Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

            Camera cam = Camera.main;
            if (cam == null) return;

            Vector3 mouseWorld = cam.ScreenToWorldPoint(Input.mousePosition);
            mouseWorld.z = 0f;
            if (!_collider.OverlapPoint(mouseWorld)) return;

            _clickArmed = true;
            _mouseDownScreenPos = Input.mousePosition;
            return;
        }

        if (Input.GetMouseButtonUp(0) && _clickArmed)
        {
            _clickArmed = false;

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

            Vector3 delta = Input.mousePosition - _mouseDownScreenPos;
            if (delta.sqrMagnitude > ClickDragThresholdPixels * ClickDragThresholdPixels) return;

            Camera cam = Camera.main;
            if (cam == null) return;

            Vector3 mouseWorld = cam.ScreenToWorldPoint(Input.mousePosition);
            mouseWorld.z = 0f;
            if (!_collider.OverlapPoint(mouseWorld)) return;

            UIManager.Instance?.ShowBuildingInfoPopup(this);
        }
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
        transform.DOKill(false);
        ClearOutgoingIconContainer();
    }

    // --- IResourceNode / 버퍼: 역할별 분기 ---

    public bool TryPush(ResourcePacket packet)
    {
        if (packet == null) return false;

        if (_buildingData is RawMaterialFactoryData || _buildingData is UnloadStationData)
            return false;

        if (_buildingData is LoadStationData || _buildingData is ProductionBuildingData)
            return TryEnqueueInputPacket(packet);

        return false;
    }

    public bool TryForwardTo(IResourceNode destination)
    {
        if (destination == null) return false;

        if (_buildingData is ProductionBuildingData || _buildingData is UnloadStationData)
            return TryForwardOutputBufferTo(destination);

        if (_buildingData is LoadStationData)
            return TryForwardInputBufferTo(destination);

        return false;
    }

    private bool TryEnqueueInputPacket(ResourcePacket packet)
    {
        if (_maxInputCapacity <= 0 || _inputBuffer.Count >= _maxInputCapacity) return false;
        _inputBuffer.Enqueue(packet);
        return true;
    }

    private bool TryForwardOutputBufferTo(IResourceNode destination)
    {
        if (_outputBuffer.Count == 0) return false;
        ResourcePacket packet = _outputBuffer.Peek();
        if (!destination.TryPush(packet)) return false;
        _outputBuffer.Dequeue();
        return true;
    }

    private bool TryForwardInputBufferTo(IResourceNode destination)
    {
        if (_inputBuffer.Count == 0) return false;
        ResourcePacket packet = _inputBuffer.Peek();
        if (!destination.TryPush(packet)) return false;
        _inputBuffer.Dequeue();
        return true;
    }

    public Dictionary<string, int> GetRuntimeInputResourceCounts() => AggregateQueueCounts(_inputBuffer);
    public Dictionary<string, int> GetRuntimeOutputResourceCounts() => AggregateQueueCounts(_outputBuffer);

    public void TickSimulation(DataManager dataManager)
    {
        switch (_buildingData)
        {
            case RawMaterialFactoryData rawFactory:
                TickSimulationRawMaterialFactory(dataManager, rawFactory);
                break;
            case LoadStationData:
                TickSimulationLoadStation(dataManager);
                break;
            case UnloadStationData:
                TickSimulationUnloadStation(dataManager);
                break;
            case ProductionBuildingData:
                TickSimulationProduction(dataManager);
                break;
        }
    }

    private void TickStaffedBatchWork(DataManager dataManager, Func<bool> canCompleteBatch, Func<bool> tryCompleteBatch)
    {
        float delta = GetWorkProgressDeltaPerTick(dataManager);
        if (delta <= 0f) return;

        _workProgress += delta;
        if (_workProgress > 1f && !canCompleteBatch()) _workProgress = 1f;

        while (_workProgress >= 1f)
        {
            if (!tryCompleteBatch()) break;
            _workProgress -= 1f;
        }

        if (_workProgress >= 1f && !canCompleteBatch())
            _workProgress = 0.999f;
    }

    public bool TrySetSelectedResource(ResourceData data)
    {
        if (data == null) return false;

        switch (_buildingData)
        {
            case RawMaterialFactoryData raw:
                if (!IsResourceAllowedForRawFactory(raw, data)) return false;
                break;
            case ProductionBuildingData prod:
                if (!IsResourceAllowedForProduction(prod, data)) return false;
                break;
        }

        _selectedResource = data;
        RefreshOutgoingResourceIcons();
        return true;
    }

    public float GetProductionProgressNormalized()
    {
        if (_buildingData is ProductionBuildingData || _buildingData is UnloadStationData || _buildingData is LoadStationData)
            return Mathf.Clamp01(_workProgress);
        return 0f;
    }

    public PlacedBuildingSaveData ExportSaveData()
    {
        PlacedBuildingSaveData data = new PlacedBuildingSaveData();
        data.buildingDataId = _buildingData != null ? _buildingData.id : null;
        data.originX = _origin.x;
        data.originY = _origin.y;
        data.rotation = _rotation;

        data.selectedResourceId = _selectedResource != null ? _selectedResource.id : null;
        data.workProgress = _workProgress;
        data.assignedWorkers = _assignedWorkers;
        data.assignedTechnicians = _assignedTechnicians;

        data.inputBuffer = ExportBuffer(_inputBuffer);
        data.outputBuffer = ExportBuffer(_outputBuffer);

        return data;
    }

    public void ImportSaveData(PlacedBuildingSaveData saveData, DataManager dataManager)
    {
        if (saveData == null) return;

        _workProgress = Mathf.Clamp01(saveData.workProgress);
        _assignedWorkers = Mathf.Max(0, saveData.assignedWorkers);
        _assignedTechnicians = Mathf.Max(0, saveData.assignedTechnicians);

        if (dataManager != null && dataManager.Resource != null && !string.IsNullOrEmpty(saveData.selectedResourceId))
        {
            ResourceEntry entry = dataManager.Resource.GetResourceEntry(saveData.selectedResourceId);
            _selectedResource = entry != null ? entry.data : null;
        }

        ImportBuffer(_inputBuffer, saveData.inputBuffer, _maxInputCapacity);
        ImportBuffer(_outputBuffer, saveData.outputBuffer, _maxOutputCapacity);

        RefreshOutgoingResourceIcons();
    }

    private static List<ResourcePacketSaveData> ExportBuffer(Queue<ResourcePacket> buffer)
    {
        List<ResourcePacketSaveData> list = new List<ResourcePacketSaveData>();
        if (buffer == null) return list;

        foreach (ResourcePacket p in buffer)
        {
            if (p == null || string.IsNullOrEmpty(p.Id)) continue;
            list.Add(new ResourcePacketSaveData(p.Id, Mathf.Max(1, p.Amount)));
        }

        return list;
    }

    private static void ImportBuffer(Queue<ResourcePacket> target, List<ResourcePacketSaveData> list, int maxCapacity)
    {
        if (target == null) return;
        target.Clear();

        if (list == null) return;

        for (int i = 0; i < list.Count; i++)
        {
            if (maxCapacity > 0 && target.Count >= maxCapacity) break;

            ResourcePacketSaveData s = list[i];
            if (s == null || string.IsNullOrEmpty(s.id)) continue;

            int amount = Mathf.Max(1, s.amount);
            target.Enqueue(new ResourcePacket(s.id, amount));
        }
    }
}
