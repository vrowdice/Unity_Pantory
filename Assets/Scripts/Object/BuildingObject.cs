using UnityEngine;

/// <summary>
/// 메인 씬에서 그리드에 설치된 건물 오브젝트의 최소 메타데이터.
/// 멀티 타일(1x1 이상) 건물 제거/조회 시 신뢰 가능한 원점/사이즈/회전을 제공하기 위함.
/// </summary>
public class BuildingObject : MonoBehaviour
{
    [SerializeField] private BuildingData _buildingData;
    [SerializeField] private Vector2Int _origin;
    [SerializeField] private Vector2Int _size;
    [SerializeField] private int _rotation;
    [SerializeField] private SpriteRenderer _viewObjRenderer;
    [SerializeField] private BoxCollider2D _collider;

    public BuildingData BuildingData => _buildingData;
    public Vector2Int Origin => _origin;
    public Vector2Int Size => _size;
    public int Rotation => _rotation;

    public void Init(BuildingData buildingData, Vector2Int origin, Vector2Int rotatedSize, int rotation)
    {
        _buildingData = buildingData;
        _origin = origin;
        _size = rotatedSize;
        _rotation = rotation;

        if (_viewObjRenderer != null)
        {
            _viewObjRenderer.sprite = buildingData != null ? buildingData.buildingSprite : null;
            _viewObjRenderer.transform.localRotation = Quaternion.Euler(0, 0, -rotation * 90f);
            _viewObjRenderer.transform.localScale = new Vector3(rotatedSize.x, rotatedSize.y, 1);
        }
        if (_collider != null)
        {
            _collider.size = new Vector2(rotatedSize.x, rotatedSize.y);
            _collider.offset = Vector2.zero;
        }
    }
}

