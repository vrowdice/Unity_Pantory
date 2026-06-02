using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 로드(도로) 오브젝트의 데이터 + 뷰/콜라이더를 관리합니다.
/// </summary>
public class RoadObject : MonoBehaviour, IResourceNode
{
    [SerializeField] private Vector2Int _gridPosition;
    [SerializeField] private int _rotation;
    [SerializeField] private GameObject _outputIndicatorPrefab;

    [Header("World resource icons (held)")]
    [SerializeField] private float _heldIconScale = 0.14f;

    [Header("Resource Buffer")]
    [SerializeField] private int _maxCapacity = 1;

    private readonly Queue<ResourcePacket> _buffer = new Queue<ResourcePacket>();
    private GameObject _heldIconContainer;
    private RoadBuildingData _roadData;

    public Vector2Int OutputGridPosition { get; private set; }
    public RoadBuildingData RoadData => _roadData;
    public Vector2Int GridPosition => _gridPosition;
    public int Rotation => _rotation;
    public bool IsEmpty => _buffer.Count == 0;
    public bool IsFull => _buffer.Count >= _maxCapacity;

    public void Init(Vector2Int gridPosition, int rotation, RoadBuildingData roadData)
    {
        if (roadData == null)
        {
            Debug.LogError("[RoadObject] RoadBuildingData is required.");
            return;
        }

        _gridPosition = gridPosition;
        _rotation = rotation % 4;
        _roadData = roadData;
        transform.localRotation = Quaternion.Euler(0f, 0f, -_rotation * 90f);

        OutputGridPosition = BuildingOutputUtils.GetOutputGridPosition(
            _gridPosition,
            Vector2Int.one,
            _rotation,
            _roadData);

        BuildingOutputUtils.SpawnIndicator(transform, _outputIndicatorPrefab, Vector2Int.one, _roadData);
        RefreshHeldResourceIcons();
    }

    public bool TryPush(ResourcePacket packet)
    {
        if (packet == null || _buffer.Count >= _maxCapacity) return false;
        _buffer.Enqueue(packet);
        packet.BlockRoadForwardThisTick = true;
        RefreshHeldResourceIcons();
        ResourceFlowFx.TryPulseHeldIconContainer(_heldIconContainer, transform.position);
        return true;
    }

    public void ResetRoadForwardGatesForQueuedPackets() =>
        ResourcePacketQueueUtils.ResetRoadForwardGates(_buffer);

    public bool TryForwardTo(IResourceNode destination, FlowDirection outputDirection)
    {
        if (destination == null || _buffer.Count == 0) return false;
        ResourcePacket packet = _buffer.Peek();
        if (packet.BlockRoadForwardThisTick) return false;
        packet.TravelDirection = outputDirection;
        if (!destination.TryPush(packet)) return false;
        _buffer.Dequeue();
        RefreshHeldResourceIcons();
        return true;
    }

    public PlacedRoadSaveData ExportSaveData()
    {
        PlacedRoadSaveData data = new PlacedRoadSaveData();
        data.x = _gridPosition.x;
        data.y = _gridPosition.y;
        data.rotation = _rotation;
        data.roadDataId = _roadData.id;
        ResourcePacketQueueUtils.ExportToSaveBuffer(_buffer, data.buffer);
        return data;
    }

    public void ImportSaveData(PlacedRoadSaveData saveData)
    {
        _buffer.Clear();
        if (saveData?.buffer == null) return;

        for (int i = 0; i < saveData.buffer.Count; i++)
        {
            if (_buffer.Count >= _maxCapacity) break;
            ResourcePacketSaveData saved = saveData.buffer[i];
            if (saved == null || string.IsNullOrEmpty(saved.id)) continue;
            _buffer.Enqueue(new ResourcePacket(saved.id, Mathf.Max(1, saved.amount), saved.direction));
        }

        RefreshHeldResourceIcons();
    }

    private void RefreshHeldResourceIcons() =>
        HeldResourceIconHelper.Refresh(
            ref _heldIconContainer,
            transform,
            _heldIconScale,
            "RoadHeldIcons",
            ResourcePacketQueueUtils.AggregateCounts(_buffer));

    private void OnDestroy() => HeldResourceIconHelper.Clear(ref _heldIconContainer);
}
