using UnityEngine;

/// <summary>
/// 그리드에 설치된 건물의 상태/데이터를 보관하는 컴포넌트.
/// 러너/핸들러는 이 컴포넌트만 참조해서 위치/사이즈/회전을 읽습니다.
/// </summary>
public class PlacedBuildingState : MonoBehaviour
{
    [SerializeField] private BuildingData _buildingData;
    [SerializeField] private Vector2Int _origin;
    [SerializeField] private Vector2Int _size;
    [SerializeField] private int _rotation;

    public BuildingData BuildingData => _buildingData;
    public Vector2Int Origin => _origin;
    public Vector2Int Size => _size;
    public int Rotation => _rotation;

    public void Init(BuildingData buildingData, Vector2Int origin, Vector2Int size, int rotation)
    {
        _buildingData = buildingData;
        _origin = origin;
        _size = size;
        _rotation = rotation;
    }
}
