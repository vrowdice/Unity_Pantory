using System;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// 그리드에 설치된 건물: 뷰, 입출력 큐, 시간 틱 시뮬, 직원, 클릭 UI.
/// </summary>
public partial class BuildingObject : MonoBehaviour, IResourceNode, IBuilding
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
    private readonly List<ResourcePacket> _failedReturnPackets = new List<ResourcePacket>();
    private readonly Queue<ResourcePacket> _inputBuffer = new Queue<ResourcePacket>();

    private MainRunner _mainRunner;
    private BuildingData _buildingData;
    private Vector2Int _origin;
    private Vector2Int _size;
    private int _rotation;
    private float _workProgress;
    private int _maxInputPerResource;
    private int _maxInputResourceKinds;
    private bool _removalAnimating;

    private const float PlaceScaleDuration = 0.22f;
    private const float PlacePunchDuration = 0.12f;
    private const float PlacePunchStrength = 0.06f;

    private bool _clickArmed;
    private Vector2 _pointerDownScreenPos;
    private const float ClickDragThresholdPixels = 8f;

    public BuildingData BuildingData => _buildingData;
    public string BuildingDataId => _buildingData != null ? _buildingData.id : null;
    public bool HasConfiguredOutputResource =>
        _selectedResource != null && !string.IsNullOrEmpty(_selectedResource.id);
    public bool IsRemovalAnimating => _removalAnimating;
    public Vector2Int Origin => _origin;
    public Vector2Int Size => _size;
    public List<Vector2Int> OutputGridPositions { get; private set; }

    public int AssignedWorkers => _assignedWorkers;
    public int AssignedTechnicians => _assignedTechnicians;
    public int RequiredEmployeeSlots => Mathf.Max(0, _buildingData.requiredEmployees);
    public int MaxWorkerSlots =>
        _buildingData.isProfessional ? 0 : RequiredEmployeeSlots;
    public int MaxTechnicianSlots => RequiredEmployeeSlots;

    public void Init(MainRunner runner, BuildingData buildingData, Vector2Int origin, Vector2Int rotatedSize, int rotation, bool isAutoEmployeeAssignment = false)
    {
        _mainRunner = runner;
        _buildingData = buildingData;
        _origin = origin;
        _size = rotatedSize;
        _rotation = rotation;
        _maxInputPerResource = Mathf.Max(0, buildingData.maxInputBufferCapacity);
        _maxInputResourceKinds = Mathf.Max(0, buildingData.maxInputResourceKinds);

        transform.localRotation = Quaternion.Euler(0f, 0f, -rotation * 90f);

        _viewObjRenderer.sprite = buildingData.buildingSprite;
        _viewObjRenderer.transform.localRotation = Quaternion.identity;
        _viewObjRenderer.transform.localScale = new Vector3(rotatedSize.x, rotatedSize.y, 1);
        _collider.size = new Vector2(rotatedSize.x, rotatedSize.y);
        _collider.offset = Vector2.zero;

        OutputGridPositions = BuildingCalculationUtils.GetOutputGridPositions(_origin, _size, _rotation);
        if (!(_buildingData is LoadStationData))
            OutputIndicatorHelper.SpawnOnRightEdgeForBuilding(transform, _outputIndicatorPrefab, _size);
        RefreshOutgoingResourceIcons();

        if (isAutoEmployeeAssignment)
            TryAutoAssignEmployeesToFill();
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
    /// 제거 시 펀치 후 스케일 다운. 점유 해제·파괴는 콜백에서 처리.
    /// </summary>
    public void PlayRemovalAnimation(Action onComplete)
    {
        if (_removalAnimating) return;

        _removalAnimating = true;
        _collider.enabled = false;

        transform.DOKill(false);

        float scaleDuration = _mainRunner != null ? _mainRunner.RemovalScaleDuration : 0.2f;
        float punchDuration = _mainRunner != null ? _mainRunner.RemovalPunchDuration : 0.08f;
        float punchStrength = _mainRunner != null ? _mainRunner.RemovalPunchStrength : 0.05f;

        Sequence seq = DOTween.Sequence();
        seq.SetLink(gameObject);
        seq.SetUpdate(true);

        if (punchStrength > 0f && punchDuration > 0f)
        {
            seq.Append(transform.DOPunchScale(Vector3.one * punchStrength, punchDuration, 8, 0.5f));
        }

        seq.Append(transform.DOScale(Vector3.zero, scaleDuration).SetEase(Ease.InBack));
        seq.OnComplete(() =>
        {
            _removalAnimating = false;
            onComplete?.Invoke();
        });
    }

    private void LateUpdate()
    {
        TryRefreshOutgoingIconsWhenVisible();
    }

    private void Update()
    {
        if (_mainRunner.PlacementHandler.IsPlacementMode || _mainRunner.PlacementHandler.IsRemovalMode ||
            _mainRunner.PlacementHandler.IsBlueprintPlacementMode ||
            _mainRunner.BlueprintHandler.IsBlueprintMode) return;

        if (PointerInput.IsMultiTouch)
            return;

        if (PointerInput.GetPrimaryPointerDown())
        {
            if (PointerInput.IsPointerOverUi()) return;

            Vector3 pointerWorld = PointerInput.ScreenToWorldOnPlane(Camera.main, PointerInput.PrimaryScreenPosition);
            if (!_collider.OverlapPoint(pointerWorld)) return;

            _clickArmed = true;
            _pointerDownScreenPos = PointerInput.PrimaryScreenPosition;
            return;
        }

        if (PointerInput.GetPrimaryPointerUp() && _clickArmed)
        {
            _clickArmed = false;

            if (PointerInput.IsPointerOverUi()) return;

            Vector2 delta = PointerInput.PrimaryScreenPosition - _pointerDownScreenPos;
            if (delta.sqrMagnitude > ClickDragThresholdPixels * ClickDragThresholdPixels) return;

            Vector3 pointerWorld = PointerInput.ScreenToWorldOnPlane(Camera.main, PointerInput.PrimaryScreenPosition);
            if (!_collider.OverlapPoint(pointerWorld)) return;

            UIManager.Instance.ShowBuildingInfoPopup(this);
        }
    }

    private void OnDestroy()
    {
        transform.DOKill(false);
        ClearOutgoingIconContainer();
    }

    public bool TryPush(ResourcePacket packet)
    {
        if (_buildingData is UnloadStationData)
            return false;

        if (_buildingData is LoadStationData || _buildingData is ProductionBuildingData)
            return TryEnqueueInputPacket(packet);

        return false;
    }

    private bool TryEnqueueInputPacket(ResourcePacket packet)
    {
        if (packet == null || string.IsNullOrEmpty(packet.Id)) return false;

        Dictionary<string, int> counts = ResourcePacketQueueUtils.AggregateCounts(_inputBuffer);
        if (!ResourcePacketQueueUtils.CanAcceptResourceAmount(
                counts, packet.Id, packet.Amount, _maxInputPerResource, _maxInputResourceKinds))
            return false;

        _inputBuffer.Enqueue(packet);
        return true;
    }

    public Dictionary<string, int> GetRuntimeInputResourceCounts() => ResourcePacketQueueUtils.AggregateCounts(_inputBuffer);

    /// <summary>
    /// 입력 큐의 자원을 모두 <see cref="ResourceDataHandler.ModifyResourceCount"/>로 플레이어 보유에 반영합니다. 실패한 패킷은 해당 큐에 다시 넣습니다.
    /// </summary>
    public void ReturnAllRuntimeBuffersToDataManager(DataManager dataManager) =>
        ReturnBufferToDataManager(_inputBuffer, dataManager);

    private void ReturnBufferToDataManager(Queue<ResourcePacket> buffer, DataManager dataManager)
    {
        _failedReturnPackets.Clear();
        while (buffer.Count > 0)
        {
            ResourcePacket packet = buffer.Dequeue();
            if (string.IsNullOrEmpty(packet.Id)) continue;
            if (!dataManager.Resource.ModifyResourceCount(packet.Id, packet.Amount))
                _failedReturnPackets.Add(packet);
        }

        for (int i = 0; i < _failedReturnPackets.Count; i++)
            buffer.Enqueue(_failedReturnPackets[i]);
    }

    /// <summary>
    /// 입력 큐에서 지정 id와 일치하는 패킷만 꺼내 <see cref="ResourceDataHandler.ModifyResourceCount"/>로 반영합니다. 순서는 유지합니다.
    /// </summary>
    public bool TryReturnInputBufferResourceToDataManager(DataManager dataManager, string resourceId)
    {
        if (string.IsNullOrEmpty(resourceId)) return false;

        Queue<ResourcePacket> queue = _inputBuffer;
        int n = queue.Count;
        if (n == 0) return false;

        List<ResourcePacket> remaining = new List<ResourcePacket>();
        bool anyReturned = false;

        for (int i = 0; i < n; i++)
        {
            ResourcePacket p = queue.Dequeue();
            if (string.IsNullOrEmpty(p.Id))
                continue;

            if (p.Id == resourceId)
            {
                if (dataManager.Resource.ModifyResourceCount(p.Id, p.Amount))
                    anyReturned = true;
                else
                    remaining.Add(p);
            }
            else
            {
                remaining.Add(p);
            }
        }

        foreach (ResourcePacket p in remaining)
            queue.Enqueue(p);

        return anyReturned;
    }

    public void TickSimulation(DataManager dataManager)
    {
        switch (_buildingData)
        {
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

    private void TickStaffedBatchWork(DataManager dataManager, Func<bool> canCompleteBatch, Func<bool> tryCompleteBatch, Func<bool> canAdvanceProgress = null)
    {
        float delta = GetWorkProgressDeltaPerTick(dataManager);
        if (delta <= 0f) return;

        if (canAdvanceProgress == null || canAdvanceProgress())
        {
            _workProgress += delta;
            if (_workProgress > 1f && !canCompleteBatch()) _workProgress = 1f;
        }

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
        switch (_buildingData)
        {
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
        data.buildingDataId = _buildingData.id;
        data.originX = _origin.x;
        data.originY = _origin.y;
        data.rotation = _rotation;

        data.selectedResourceId = _selectedResource != null ? _selectedResource.id : null;
        data.workProgress = _workProgress;
        data.assignedWorkers = _assignedWorkers;
        data.assignedTechnicians = _assignedTechnicians;

        data.inputBuffer = ExportBuffer(_inputBuffer);

        return data;
    }

    public void ImportSaveData(PlacedBuildingSaveData saveData, DataManager dataManager)
    {
        _workProgress = Mathf.Clamp01(saveData.workProgress);
        _assignedWorkers = Mathf.Max(0, saveData.assignedWorkers);
        _assignedTechnicians = Mathf.Max(0, saveData.assignedTechnicians);

        if (!string.IsNullOrEmpty(saveData.selectedResourceId))
        {
            ResourceData savedResource = dataManager.Resource.GetResourceEntry(saveData.selectedResourceId)?.data;
            if (savedResource != null)
                _selectedResource = savedResource;
        }

        ImportBuffer(_inputBuffer, saveData.inputBuffer, _maxInputPerResource, _maxInputResourceKinds);

        RefreshOutgoingResourceIcons();
    }

    private static List<ResourcePacketSaveData> ExportBuffer(Queue<ResourcePacket> buffer)
    {
        List<ResourcePacketSaveData> list = new List<ResourcePacketSaveData>();

        foreach (ResourcePacket p in buffer)
        {
            if (string.IsNullOrEmpty(p.Id)) continue;
            list.Add(new ResourcePacketSaveData(p.Id, Mathf.Max(1, p.Amount), p.TravelDirection));
        }

        return list;
    }

    private static void ImportBuffer(
        Queue<ResourcePacket> target,
        List<ResourcePacketSaveData> list,
        int maxPerResource,
        int maxKinds)
    {
        target.Clear();
        if (list == null) return;

        Dictionary<string, int> counts = new Dictionary<string, int>();
        for (int i = 0; i < list.Count; i++)
        {
            ResourcePacketSaveData saved = list[i];
            if (saved == null || string.IsNullOrEmpty(saved.id)) continue;

            int amount = Mathf.Max(1, saved.amount);
            if (!ResourcePacketQueueUtils.CanAcceptResourceAmount(counts, saved.id, amount, maxPerResource, maxKinds))
                continue;

            target.Enqueue(new ResourcePacket(saved.id, amount, saved.direction));
            if (counts.TryGetValue(saved.id, out int existing))
                counts[saved.id] = existing + amount;
            else
                counts[saved.id] = amount;
        }
    }
}
