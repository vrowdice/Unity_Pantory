using System.Collections.Generic;
using UnityEngine;

public class DualLaneRoadObject : MonoBehaviour, IResourceNode
{
    [SerializeField] private Vector2Int _gridPosition;
    [SerializeField] private int _rotation;
    [SerializeField] private int _maxCapacityPerLane = 8;
    [SerializeField] private float _heldIconScale = 0.14f;
    [SerializeField] private BuildingData _sourceBuildingData;

    private readonly Queue<ResourcePacket> _laneA = new Queue<ResourcePacket>();
    private readonly Queue<ResourcePacket> _laneB = new Queue<ResourcePacket>();
    private GameObject _heldIconContainer;
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
        RefreshHeldResourceIcons();
    }

    public bool TryPush(ResourcePacket packet)
    {
        if (packet == null) return false;

        if (_isSplitter)
        {
            Queue<ResourcePacket> targetLane = _splitterToggle ? _laneB : _laneA;
            FlowDirection splitterDirection = _splitterToggle ? GetOutputDirection(1) : GetOutputDirection(0);
            _splitterToggle = !_splitterToggle;
            if (targetLane.Count >= _maxCapacityPerLane) return false;
            packet.TravelDirection = splitterDirection;
            targetLane.Enqueue(packet);
            packet.BlockRoadForwardThisTick = true;
            RefreshHeldResourceIcons();
            return true;
        }

        if (IsVertical(packet.TravelDirection))
        {
            if (_laneA.Count >= _maxCapacityPerLane) return false;
            _laneA.Enqueue(packet);
        }
        else if (IsHorizontal(packet.TravelDirection))
        {
            if (_laneB.Count >= _maxCapacityPerLane) return false;
            _laneB.Enqueue(packet);
        }
        else if (_laneA.Count <= _laneB.Count)
        {
            if (_laneA.Count >= _maxCapacityPerLane) return false;
            _laneA.Enqueue(packet);
        }
        else
        {
            if (_laneB.Count >= _maxCapacityPerLane) return false;
            _laneB.Enqueue(packet);
        }

        packet.BlockRoadForwardThisTick = true;
        RefreshHeldResourceIcons();
        return true;
    }

    public void ResetRoadForwardGatesForQueuedPackets()
    {
        foreach (ResourcePacket packet in _laneA)
        {
            if (packet != null) packet.BlockRoadForwardThisTick = false;
        }

        foreach (ResourcePacket packet in _laneB)
        {
            if (packet != null) packet.BlockRoadForwardThisTick = false;
        }
    }

    public bool TryForwardToCell(Vector2Int outCell, IResourceNode destination)
    {
        if (destination == null) return false;

        if (_isSplitter)
        {
            if (OutputGridPositions.Count < 2) return false;
            if (outCell == OutputGridPositions[0]) return TryForwardLaneAndRefresh(_laneA, destination, GetOutputDirection(0));
            if (outCell == OutputGridPositions[1]) return TryForwardLaneAndRefresh(_laneB, destination, GetOutputDirection(1));
            return false;
        }

        FlowDirection outDirection = DirectionFromDelta(outCell - _gridPosition);
        if (outDirection == FlowDirection.None) return false;
        if (IsVertical(outDirection)) return TryForwardLaneAndRefresh(_laneA, destination, outDirection);
        if (IsHorizontal(outDirection)) return TryForwardLaneAndRefresh(_laneB, destination, outDirection);
        return true;
    }

    public PlacedRoadSaveData ExportSaveData()
    {
        PlacedRoadSaveData data = new PlacedRoadSaveData();
        data.x = _gridPosition.x;
        data.y = _gridPosition.y;
        data.rotation = _rotation;
        data.roadDataId = _sourceBuildingData != null ? _sourceBuildingData.id : null;
        data.sourceBuildingDataId = _sourceBuildingData != null ? _sourceBuildingData.id : null;

        foreach (ResourcePacket packet in _laneA)
        {
            if (packet == null || string.IsNullOrEmpty(packet.Id)) continue;
            data.buffer.Add(new ResourcePacketSaveData(packet.Id, Mathf.Max(1, packet.Amount), packet.TravelDirection));
        }

        foreach (ResourcePacket packet in _laneB)
        {
            if (packet == null || string.IsNullOrEmpty(packet.Id)) continue;
            data.buffer.Add(new ResourcePacketSaveData(packet.Id, Mathf.Max(1, packet.Amount), packet.TravelDirection));
        }

        return data;
    }

    public void ImportSaveData(PlacedRoadSaveData saveData)
    {
        _laneA.Clear();
        _laneB.Clear();
        if (saveData == null || saveData.buffer == null) return;

        for (int i = 0; i < saveData.buffer.Count; i++)
        {
            ResourcePacketSaveData saved = saveData.buffer[i];
            if (saved == null || string.IsNullOrEmpty(saved.id)) continue;
            if (_laneA.Count <= _laneB.Count) _laneA.Enqueue(new ResourcePacket(saved.id, Mathf.Max(1, saved.amount), saved.direction));
            else _laneB.Enqueue(new ResourcePacket(saved.id, Mathf.Max(1, saved.amount), saved.direction));
        }

        RefreshHeldResourceIcons();
    }

    private void RebuildOutputGridPositions()
    {
        OutputGridPositions.Clear();
        if (_isSplitter)
        {
            Vector2Int right = RotateDirectionClockwise(new Vector2Int(1, 0), _rotation);
            Vector2Int down = RotateDirectionClockwise(new Vector2Int(0, 1), _rotation);
            OutputGridPositions.Add(_gridPosition + right);
            OutputGridPositions.Add(_gridPosition + down);
            return;
        }

        OutputGridPositions.Add(_gridPosition + new Vector2Int(0, -1));
        OutputGridPositions.Add(_gridPosition + new Vector2Int(1, 0));
        OutputGridPositions.Add(_gridPosition + new Vector2Int(0, 1));
        OutputGridPositions.Add(_gridPosition + new Vector2Int(-1, 0));
    }

    private static bool TryForwardLane(Queue<ResourcePacket> lane, IResourceNode destination, FlowDirection outputDirection)
    {
        if (lane.Count == 0) return false;
        ResourcePacket packet = lane.Peek();
        if (packet == null || packet.BlockRoadForwardThisTick) return false;
        if (packet.TravelDirection != FlowDirection.None && packet.TravelDirection != outputDirection) return false;
        packet.TravelDirection = outputDirection;
        if (!destination.TryPush(packet)) return false;
        lane.Dequeue();
        return true;
    }

    private bool TryForwardLaneAndRefresh(Queue<ResourcePacket> lane, IResourceNode destination, FlowDirection outputDirection)
    {
        bool moved = TryForwardLane(lane, destination, outputDirection);
        if (moved) RefreshHeldResourceIcons();
        return moved;
    }

    private Dictionary<string, int> BuildHeldResourceCounts()
    {
        Dictionary<string, int> counts = new Dictionary<string, int>();
        AggregateLaneCounts(_laneA, counts);
        AggregateLaneCounts(_laneB, counts);
        return counts;
    }

    private static void AggregateLaneCounts(Queue<ResourcePacket> lane, Dictionary<string, int> counts)
    {
        foreach (ResourcePacket packet in lane)
        {
            if (packet == null || string.IsNullOrEmpty(packet.Id)) continue;
            if (counts.TryGetValue(packet.Id, out int current)) counts[packet.Id] = current + packet.Amount;
            else counts[packet.Id] = packet.Amount;
        }
    }

    private void RefreshHeldResourceIcons()
    {
        ClearHeldIconContainer();

        GameManager gameManager = GameManager.Instance;
        Transform worldCanvas = gameManager.GetWorldCanvas();
        if (worldCanvas == null) return;
        Dictionary<string, int> counts = BuildHeldResourceCounts();
        if (counts == null || counts.Count == 0) return;

        Vector3 worldPosition = transform.position + new Vector3(0f, 0f, -1f);
        _heldIconContainer = UIManager.Instance.CreateProductionIconContainer(
            worldCanvas,
            $"DualRoadHeldIcons_{gameObject.GetInstanceID()}",
            worldPosition,
            _heldIconScale,
            counts);
    }

    private void ClearHeldIconContainer()
    {
        if (_heldIconContainer == null) return;
        PoolingManager.Instance?.ClearChildrenToPool(_heldIconContainer.transform);
        Destroy(_heldIconContainer);
        _heldIconContainer = null;
    }

    private void OnDestroy()
    {
        ClearHeldIconContainer();
    }

    private static Vector2Int RotateDirectionClockwise(Vector2Int direction, int rotation)
    {
        int normalized = ((rotation % 4) + 4) % 4;
        Vector2Int current = direction;
        for (int i = 0; i < normalized; i++)
            current = new Vector2Int(-current.y, current.x);
        return current;
    }

    private static bool IsVertical(FlowDirection direction)
    {
        return direction == FlowDirection.Up || direction == FlowDirection.Down;
    }

    private static bool IsHorizontal(FlowDirection direction)
    {
        return direction == FlowDirection.Left || direction == FlowDirection.Right;
    }

    private static FlowDirection DirectionFromDelta(Vector2Int delta)
    {
        if (delta == new Vector2Int(0, -1)) return FlowDirection.Up;
        if (delta == new Vector2Int(1, 0)) return FlowDirection.Right;
        if (delta == new Vector2Int(0, 1)) return FlowDirection.Down;
        if (delta == new Vector2Int(-1, 0)) return FlowDirection.Left;
        return FlowDirection.None;
    }

    private FlowDirection GetOutputDirection(int outputIndex)
    {
        if (outputIndex < 0 || outputIndex >= OutputGridPositions.Count) return FlowDirection.None;
        return DirectionFromDelta(OutputGridPositions[outputIndex] - _gridPosition);
    }
}
