using UnityEngine;

/// <summary>
/// 배치된 로드(운반물)의 상태/데이터를 보관하는 컴포넌트.
/// 러너/핸들러는 이 컴포넌트만 참조해서 위치/소유 건물 등을 읽습니다.
/// </summary>
public class PlacedLoadState : MonoBehaviour
{
    [SerializeField] private Vector2Int _gridPosition;
    [SerializeField] private BuildingObject _ownerBuilding;

    public Vector2Int GridPosition => _gridPosition;
    public BuildingObject OwnerBuilding => _ownerBuilding;

    public void Init(Vector2Int gridPosition, BuildingObject ownerBuilding)
    {
        _gridPosition = gridPosition;
        _ownerBuilding = ownerBuilding;
    }
}
