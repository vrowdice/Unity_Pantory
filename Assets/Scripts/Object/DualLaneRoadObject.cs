using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class DualLaneRoadObject : MonoBehaviour, IResourceNode
{
    private static readonly Vector2Int[] NeighborDeltas =
    {
        new Vector2Int(0, -1),
        new Vector2Int(1, 0),
        new Vector2Int(0, 1),
        new Vector2Int(-1, 0)
    };

    [SerializeField] private SpriteRenderer _viewObjRenderer;
    [SerializeField] private Vector2Int _gridPosition;
    [SerializeField] private int _rotation;
    [SerializeField] private int _maxCapacityPerLane = 8;
    [SerializeField] private float _heldIconScale = 0.14f;
    [SerializeField] private BuildingData _sourceBuildingData;
    [SerializeField] private GameObject _outputIndicatorPrefab;

    [Header("Splitter")]
    [SerializeField] private Transform _splitterArrowRoot;
    [SerializeField] private Sprite _splitterArrow;
    [SerializeField] private float _splitterVisualTweenDuration = 0.2f;

    private readonly Queue<ResourcePacket> _laneA = new Queue<ResourcePacket>();
    private readonly Queue<ResourcePacket> _laneB = new Queue<ResourcePacket>();
    private GameObject _heldIconContainer;
    private Transform _outputIndicatorVisual;
    private Tween _splitterArrowTween;
    private Tween _splitterIndicatorTween;
    private bool _splitterToggle;
    private bool _isSplitter;

    public Vector2Int GridPosition => _gridPosition;
    public BuildingData SourceBuildingData => _sourceBuildingData;
    public bool IsEmpty => _laneA.Count == 0 && _laneB.Count == 0;
    public List<Vector2Int> OutputGridPositions { get; private set; } = new List<Vector2Int>();

    public void Init(Vector2Int gridPosition, int rotation, BuildingData sourceBuildingData = null)
    {
        _gridPosition = gridPosition;
        _rotation = rotation % 4;
        _sourceBuildingData = sourceBuildingData;
        _isSplitter = sourceBuildingData != null && sourceBuildingData.id == "splitter";
        transform.localRotation = Quaternion.Euler(0f, 0f, -_rotation * 90f);
        RebuildOutputGridPositions();

        if (_splitterArrowRoot != null)
            _splitterArrowRoot.gameObject.SetActive(_isSplitter);

        if (_isSplitter)
        {
            EnsureSplitterVisuals();
            SetSplitterOutputVisual(GetOutputDirection(0), true);
        }

        RefreshHeldResourceIcons();

        if (_sourceBuildingData != null)
            _viewObjRenderer.sprite = _sourceBuildingData.buildingSprite;
    }

    public bool CanAcceptIncoming(ResourcePacket packet)
    {
        if (packet == null) return false;

        if (_isSplitter)
        {
            Queue<ResourcePacket> targetLane = _splitterToggle ? _laneB : _laneA;
            return targetLane.Count < _maxCapacityPerLane;
        }

        Queue<ResourcePacket> lane = SelectLaneForPacket(packet);
        return lane.Count < _maxCapacityPerLane;
    }

    public bool TryPush(ResourcePacket packet)
    {
        if (packet == null) return false;

        if (_isSplitter)
            return TryPushSplitter(packet);

        Queue<ResourcePacket> lane = SelectLaneForPacket(packet);
        if (lane.Count >= _maxCapacityPerLane) return false;

        lane.Enqueue(packet);
        packet.BlockRoadForwardThisTick = true;
        RefreshHeldResourceIcons();
        ResourceFlowFx.TryPulseHeldIconContainer(_heldIconContainer, transform.position);
        return true;
    }

    public void ResetRoadForwardGatesForQueuedPackets() =>
        ResourcePacketQueueUtils.ResetRoadForwardGates(_laneA, _laneB);

    public bool TryForwardToCell(Vector2Int outCell, IResourceNode destination)
    {
        if (destination == null) return false;

        if (_isSplitter)
        {
            if (OutputGridPositions.Count < 2) return false;
            if (outCell == OutputGridPositions[0])
                return TryForwardLaneAndRefresh(_laneA, destination, GetOutputDirection(0));
            if (outCell == OutputGridPositions[1])
                return TryForwardLaneAndRefresh(_laneB, destination, GetOutputDirection(1));
            return false;
        }

        FlowDirection outDirection = GridFlowUtils.DirectionFromDelta(outCell - _gridPosition);
        if (outDirection == FlowDirection.None) return false;
        if (GridFlowUtils.IsVertical(outDirection))
            return TryForwardLaneAndRefresh(_laneA, destination, outDirection);
        if (GridFlowUtils.IsHorizontal(outDirection))
            return TryForwardLaneAndRefresh(_laneB, destination, outDirection);
        return true;
    }

    public PlacedRoadSaveData ExportSaveData()
    {
        PlacedRoadSaveData data = new PlacedRoadSaveData();
        data.x = _gridPosition.x;
        data.y = _gridPosition.y;
        data.rotation = _rotation;
        data.roadDataId = _sourceBuildingData != null ? _sourceBuildingData.id : null;
        data.sourceBuildingDataId = data.roadDataId;
        ResourcePacketQueueUtils.ExportToSaveBuffer(_laneA, data.buffer);
        ResourcePacketQueueUtils.ExportToSaveBuffer(_laneB, data.buffer);
        return data;
    }

    public void ImportSaveData(PlacedRoadSaveData saveData)
    {
        _laneA.Clear();
        _laneB.Clear();
        if (saveData?.buffer == null) return;

        for (int i = 0; i < saveData.buffer.Count; i++)
        {
            ResourcePacketSaveData saved = saveData.buffer[i];
            if (saved == null || string.IsNullOrEmpty(saved.id)) continue;

            Queue<ResourcePacket> lane = _laneA.Count <= _laneB.Count ? _laneA : _laneB;
            lane.Enqueue(new ResourcePacket(saved.id, Mathf.Max(1, saved.amount), saved.direction));
        }

        RefreshHeldResourceIcons();
    }

    private bool TryPushSplitter(ResourcePacket packet)
    {
        Queue<ResourcePacket> targetLane = _splitterToggle ? _laneB : _laneA;
        FlowDirection splitterDirection = _splitterToggle ? GetOutputDirection(1) : GetOutputDirection(0);
        _splitterToggle = !_splitterToggle;
        if (targetLane.Count >= _maxCapacityPerLane) return false;

        packet.TravelDirection = splitterDirection;
        targetLane.Enqueue(packet);
        packet.BlockRoadForwardThisTick = true;
        SetSplitterOutputVisual(splitterDirection, false);
        RefreshHeldResourceIcons();
        ResourceFlowFx.TryPulseHeldIconContainer(_heldIconContainer, transform.position);
        return true;
    }

    private Queue<ResourcePacket> SelectLaneForPacket(ResourcePacket packet)
    {
        if (GridFlowUtils.IsVertical(packet.TravelDirection)) return _laneA;
        if (GridFlowUtils.IsHorizontal(packet.TravelDirection)) return _laneB;
        return _laneA.Count <= _laneB.Count ? _laneA : _laneB;
    }

    private void RebuildOutputGridPositions()
    {
        OutputGridPositions.Clear();
        if (_isSplitter)
        {
            Vector2Int right = GridFlowUtils.RotateCellClockwise(new Vector2Int(1, 0), _rotation);
            Vector2Int down = GridFlowUtils.RotateCellClockwise(new Vector2Int(0, 1), _rotation);
            OutputGridPositions.Add(_gridPosition + right);
            OutputGridPositions.Add(_gridPosition + down);
            return;
        }

        for (int i = 0; i < NeighborDeltas.Length; i++)
            OutputGridPositions.Add(_gridPosition + NeighborDeltas[i]);
    }

    private void EnsureSplitterVisuals()
    {
        if (_splitterArrowRoot != null && _splitterArrow != null)
        {
            SpriteRenderer renderer = _splitterArrowRoot.GetComponent<SpriteRenderer>();
            if (renderer == null)
                renderer = _splitterArrowRoot.gameObject.AddComponent<SpriteRenderer>();
            renderer.sprite = _splitterArrow;
            renderer.sortingOrder = 1;
            _splitterArrowRoot.localPosition = Vector3.zero;
        }

        if (_outputIndicatorVisual == null && _outputIndicatorPrefab != null)
        {
            GameObject indicator = Instantiate(_outputIndicatorPrefab, transform);
            _outputIndicatorVisual = indicator.transform;
            OutputIndicatorHelper.ApplyLocalPlacement(_outputIndicatorVisual, Vector2Int.one, OutputIndicatorEdge.Right);
        }
    }

    private void SetSplitterOutputVisual(FlowDirection direction, bool immediate)
    {
        if (!_isSplitter) return;

        OutputIndicatorEdge edge = OutputIndicatorHelper.EdgeFromSplitterFlow(direction, GetOutputDirection(1));
        float arrowRotationZ = OutputIndicatorHelper.GetSplitterArrowRotationZ(edge);
        float indicatorRotationZ = BuildingCalculationUtils.GetOutputIndicatorLocalRotationZ(edge);

        OutputIndicatorHelper.SetLocalRotationZ(_splitterArrowRoot, arrowRotationZ, immediate, _splitterVisualTweenDuration, ref _splitterArrowTween);
        OutputIndicatorHelper.SetLocalRotationZ(_outputIndicatorVisual, indicatorRotationZ, immediate, _splitterVisualTweenDuration, ref _splitterIndicatorTween);
    }

    private void DestroySplitterVisuals()
    {
        _splitterArrowTween?.Kill();
        _splitterIndicatorTween?.Kill();
        _splitterArrowTween = null;
        _splitterIndicatorTween = null;
        _splitterArrowRoot?.DOKill();
        _outputIndicatorVisual?.DOKill();

        if (_outputIndicatorVisual != null)
        {
            Destroy(_outputIndicatorVisual.gameObject);
            _outputIndicatorVisual = null;
        }
    }

    private bool TryForwardLaneAndRefresh(Queue<ResourcePacket> lane, IResourceNode destination, FlowDirection outputDirection)
    {
        if (lane.Count == 0) return false;
        ResourcePacket packet = lane.Peek();
        if (packet == null || packet.BlockRoadForwardThisTick) return false;
        if (packet.TravelDirection != FlowDirection.None && packet.TravelDirection != outputDirection) return false;

        packet.TravelDirection = outputDirection;
        if (!destination.TryPush(packet)) return false;

        lane.Dequeue();

        if (!(destination is RoadObject) && !(destination is DualLaneRoadObject))
            ResourceFlowFx.TryPlayNodeTransit(packet.Id, this, destination);

        RefreshHeldResourceIcons();
        return true;
    }

    private void RefreshHeldResourceIcons() =>
        HeldResourceIconHelper.Refresh(
            ref _heldIconContainer,
            transform,
            _heldIconScale,
            "DualRoadHeldIcons",
            ResourcePacketQueueUtils.AggregateCounts(_laneA, _laneB));

    private void OnDestroy()
    {
        HeldResourceIconHelper.Clear(ref _heldIconContainer);
        DestroySplitterVisuals();
    }

    private FlowDirection GetOutputDirection(int outputIndex)
    {
        if (outputIndex < 0 || outputIndex >= OutputGridPositions.Count) return FlowDirection.None;
        return GridFlowUtils.DirectionFromDelta(OutputGridPositions[outputIndex] - _gridPosition);
    }
}
