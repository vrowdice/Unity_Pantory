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

    public List<Vector2Int> OutputGridPositions { get; private set; }

    [Header("Resource Buffer")]
    [SerializeField] private int _maxCapacity = 1;
    private readonly Queue<ResourcePacket> _buffer = new Queue<ResourcePacket>();

    public Vector2Int GridPosition => _gridPosition;
    public int Rotation => _rotation;

    public bool IsEmpty => _buffer.Count == 0;
    public bool IsFull => _buffer.Count >= _maxCapacity;

    public void Init(Vector2Int gridPosition, int rotation)
    {
        _gridPosition = gridPosition;
        _rotation = rotation % 4;

        RebuildOutputGridPositions();
        UpdateOutputIndicators();
    }

    public bool TryPush(ResourcePacket packet)
    {
        if (packet == null)
        {
            return false;
        }

        if (_buffer.Count >= _maxCapacity)
        {
            return false;
        }

        _buffer.Enqueue(packet);
        return true;
    }

    public bool TryPeek(out ResourcePacket packet)
    {
        if (_buffer.Count == 0)
        {
            packet = null;
            return false;
        }

        packet = _buffer.Peek();
        return true;
    }

    public bool TryPop(out ResourcePacket packet)
    {
        if (_buffer.Count == 0)
        {
            packet = null;
            return false;
        }

        packet = _buffer.Dequeue();
        return true;
    }

    private void RebuildOutputGridPositions()
    {
        Vector2Int size = Vector2Int.one;
        OutputGridPositions = BuildingCalculationUtils.GetOutputGridPositions(_gridPosition, size, _rotation);
    }

    private void UpdateOutputIndicators()
    {
        if (_outputIndicatorPrefab == null)
        {
            return;
        }

        Vector2Int size = Vector2Int.one;
        foreach (Vector3 localPos in BuildingCalculationUtils.GetOutputLocalPositions(size, _rotation))
        {
            GameObject indicator = Instantiate(_outputIndicatorPrefab, transform);
            indicator.transform.localPosition = localPos;
            indicator.transform.localRotation = Quaternion.Euler(0f, 0f, -_rotation * 90f);
        }
    }
}
