using UnityEngine;

/// <summary>
/// 메인 씬에서 그리드에 설치된 건물 오브젝트의 뷰/콜라이더 관리.
/// 실제 그리드/데이터 상태는 PlacedBuildingState 에서 관리합니다.
/// </summary>
public class BuildingObject : MonoBehaviour
{
    [SerializeField] private BuildingData _buildingData;
    [SerializeField] private SpriteRenderer _viewObjRenderer;
    [SerializeField] private BoxCollider2D _collider;
    [SerializeField] private PlacedBuildingState _state;

    public BuildingData BuildingData => _buildingData;
    public PlacedBuildingState State => _state;

    private void Awake()
    {
        EnsureStateComponent();
    }

    public void Init(BuildingData buildingData, Vector2Int origin, Vector2Int rotatedSize, int rotation)
    {
        _buildingData = buildingData;

        EnsureStateComponent();
        _state.Init(buildingData, origin, rotatedSize, rotation);

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

    private void EnsureStateComponent()
    {
        if (_state == null)
        {
            _state = GetComponent<PlacedBuildingState>();
            if (_state == null)
            {
                _state = gameObject.AddComponent<PlacedBuildingState>();
            }
        }
    }
}
