using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 로드(도로) 오브젝트의 데이터 + 뷰/콜라이더를 관리합니다.
/// BuildingObject 와 동일한 방식으로 출력 인디케이터/출력 그리드를 계산합니다.
/// </summary>
public class RoadObject : MonoBehaviour, IResourceNode
{
    [SerializeField] private Vector2Int _gridPosition;
    [SerializeField] private int _rotation;
    [SerializeField] private GameObject _outputIndicatorPrefab;

    [Header("World resource icons (held)")]
    [SerializeField] private float _heldIconScale = 0.14f;

    private GameObject _heldIconContainer;

    private BuildingData _sourceBuildingData;

    public List<Vector2Int> OutputGridPositions { get; private set; }

    public BuildingData SourceBuildingData => _sourceBuildingData;

    [Header("Resource Buffer")]
    [SerializeField] private int _maxCapacity = 1;
    private readonly Queue<ResourcePacket> _buffer = new Queue<ResourcePacket>();

    public Vector2Int GridPosition => _gridPosition;
    public int Rotation => _rotation;

    public bool IsEmpty => _buffer.Count == 0;
    public bool IsFull => _buffer.Count >= _maxCapacity;

    public void Init(Vector2Int gridPosition, int rotation, BuildingData sourceBuildingData = null)
    {
        _gridPosition = gridPosition;
        _rotation = rotation % 4;
        _sourceBuildingData = sourceBuildingData;

        transform.localRotation = Quaternion.Euler(0f, 0f, -_rotation * 90f);

        RebuildOutputGridPositions();
        UpdateOutputIndicators();
        RefreshHeldResourceIcons();
    }

    public bool TryPush(ResourcePacket packet)
    {
        if (packet == null || _buffer.Count >= _maxCapacity) return false;
        _buffer.Enqueue(packet);
        packet.BlockRoadForwardThisTick = true;
        RefreshHeldResourceIcons();
        return true;
    }

    /// <summary>
    /// 틱 시작 시 호출: 이미 큐에 있던 패킷만 이번 틱에 도로에서 나갈 수 있게 합니다.
    /// </summary>
    public void ResetRoadForwardGatesForQueuedPackets()
    {
        foreach (ResourcePacket p in _buffer)
        {
            if (p != null) p.BlockRoadForwardThisTick = false;
        }
    }

    /// <summary>
    /// 맨 앞 패킷을 destination에 넣을 수 있을 때만 전달합니다.
    /// </summary>
    public bool TryForwardTo(IResourceNode destination)
    {
        if (destination == null || _buffer.Count == 0) return false;
        ResourcePacket packet = _buffer.Peek();
        if (packet.BlockRoadForwardThisTick) return false;
        if (!destination.TryPush(packet)) return false;
        _buffer.Dequeue();
        RefreshHeldResourceIcons();
        return true;
    }

    private void RebuildOutputGridPositions()
    {
        Vector2Int size = Vector2Int.one;
        OutputGridPositions = BuildingCalculationUtils.GetOutputGridPositions(_gridPosition, size, _rotation);
    }

    private void UpdateOutputIndicators()
    {
        if (_outputIndicatorPrefab == null) return;

        Vector2Int size = Vector2Int.one;
        foreach (Vector3 localPos in BuildingCalculationUtils.GetOutputLocalPositions(size))
        {
            GameObject indicator = Instantiate(_outputIndicatorPrefab, transform);
            indicator.transform.localPosition = localPos;
            indicator.transform.localRotation = Quaternion.identity;
        }
    }

    private Dictionary<string, int> BuildHeldResourceCounts()
    {
        Dictionary<string, int> counts = new Dictionary<string, int>();
        foreach (ResourcePacket p in _buffer)
        {
            if (p == null || string.IsNullOrEmpty(p.Id)) continue;
            if (counts.TryGetValue(p.Id, out int v)) counts[p.Id] = v + p.Amount;
            else counts[p.Id] = p.Amount;
        }

        return counts;
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
            $"RoadHeldIcons_{gameObject.GetInstanceID()}",
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

    public PlacedRoadSaveData ExportSaveData()
    {
        PlacedRoadSaveData data = new PlacedRoadSaveData();
        data.x = _gridPosition.x;
        data.y = _gridPosition.y;
        data.rotation = _rotation;
        data.roadDataId = _sourceBuildingData != null ? _sourceBuildingData.id : null;
        data.sourceBuildingDataId = _sourceBuildingData != null ? _sourceBuildingData.id : null;

        foreach (ResourcePacket p in _buffer)
        {
            if (p == null || string.IsNullOrEmpty(p.Id)) continue;
            data.buffer.Add(new ResourcePacketSaveData(p.Id, Mathf.Max(1, p.Amount)));
        }

        return data;
    }

    public void ImportSaveData(PlacedRoadSaveData saveData)
    {
        if (saveData == null) return;

        _buffer.Clear();
        if (saveData.buffer != null)
        {
            for (int i = 0; i < saveData.buffer.Count; i++)
            {
                ResourcePacketSaveData s = saveData.buffer[i];
                if (s == null || string.IsNullOrEmpty(s.id)) continue;

                if (_buffer.Count >= _maxCapacity) break;
                _buffer.Enqueue(new ResourcePacket(s.id, Mathf.Max(1, s.amount)));
            }
        }

        RefreshHeldResourceIcons();
    }
}
