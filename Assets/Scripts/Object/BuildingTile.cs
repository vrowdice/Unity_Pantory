using UnityEngine;

/// <summary>
/// 개별 건물 타일을 나타내는 클래스
/// </summary>
public class BuildingTile : MonoBehaviour
{
    private Collider2D _collider;
    private SpriteRenderer _spriteRenderer;
    private Vector2Int _gridPosition;
    private DesignRunner _manager;
    private bool _isOccupied = false;  // 타일이 건물에 의해 차지되었는지

    [Header("Tile Colors")]
    [SerializeField] private Color _normalColor = Color.white;
    [SerializeField] private Color _occupiedColor = new Color(0.7f, 0.7f, 0.7f, 1f);

    public Vector2Int GridPosition => _gridPosition;
    public bool IsOccupied => _isOccupied;

    void Awake()
    {
        _collider = GetComponent<Collider2D>();
        _spriteRenderer = GetComponent<SpriteRenderer>();
    }

    /// <summary>
    /// 타일을 초기화합니다.
    /// </summary>
    public void Initialize(Vector2Int position, DesignRunner manager)
    {
        _gridPosition = position;
        _manager = manager;
        UpdateVisual();
    }

    /// <summary>
    /// 타일의 점유 상태를 설정합니다.
    /// </summary>
    public void SetOccupied(bool occupied)
    {
        _isOccupied = occupied;
        UpdateVisual();
    }

    /// <summary>
    /// 타일의 시각적 표현을 업데이트합니다.
    /// </summary>
    private void UpdateVisual()
    {
        _spriteRenderer.color = _isOccupied ? _occupiedColor : _normalColor;
    }

    /// <summary>
    /// 마우스가 타일 위에 올라갔을 때
    /// </summary>
    void OnMouseEnter()
    {
        // 추후 하이라이트 효과 등 추가 가능
    }

    /// <summary>
    /// 마우스가 타일에서 벗어났을 때
    /// </summary>
    void OnMouseExit()
    {
        // 추후 하이라이트 해제 등 추가 가능
    }
}
