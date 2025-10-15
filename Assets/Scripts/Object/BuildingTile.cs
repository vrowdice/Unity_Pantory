using UnityEngine;

/// <summary>
/// 개별 건물 타일을 나타내는 클래스
/// </summary>
public class BuildingTile : MonoBehaviour
{
    private Collider2D _collider;
    private SpriteRenderer _spriteRenderer;
    private SpriteRenderer _outlineRenderer;  // 윤곽선용 렌더러
    private Vector2Int _gridPosition;
    private BuildingTileManager _manager;
    private bool _isOccupied = false;  // 타일이 건물에 의해 차지되었는지
    private bool _showOutline = false;  // 윤곽선 표시 여부

    [Header("Tile Colors")]
    [SerializeField] private Color _normalColor = Color.white;
    [SerializeField] private Color _occupiedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
    
    [Header("Outline Settings")]
    [SerializeField] private Color _outlineColor = new Color(0.2f, 0.8f, 1f, 0.8f);  // 밝은 파란색 윤곽선
    [SerializeField] private float _outlineThickness = 0.05f;

    public Vector2Int GridPosition => _gridPosition;
    public bool IsOccupied => _isOccupied;

    void Awake()
    {
        _collider = GetComponent<Collider2D>();
        _spriteRenderer = GetComponent<SpriteRenderer>();
        CreateOutline();
    }

    /// <summary>
    /// 타일을 초기화합니다.
    /// </summary>
    public void Initialize(Vector2Int position, BuildingTileManager manager)
    {
        _gridPosition = position;
        _manager = manager;
        UpdateVisual();
    }

    /// <summary>
    /// 윤곽선 오브젝트를 생성합니다.
    /// </summary>
    private void CreateOutline()
    {
        if (_spriteRenderer == null) return;

        // 윤곽선용 자식 오브젝트 생성
        GameObject outlineObj = new GameObject("Outline");
        outlineObj.transform.SetParent(transform);
        outlineObj.transform.localPosition = Vector3.zero;
        outlineObj.transform.localRotation = Quaternion.identity;
        outlineObj.transform.localScale = Vector3.one * (1f + _outlineThickness);

        // 윤곽선 렌더러 설정
        _outlineRenderer = outlineObj.AddComponent<SpriteRenderer>();
        _outlineRenderer.sprite = _spriteRenderer.sprite;
        _outlineRenderer.color = _outlineColor;
        _outlineRenderer.sortingLayerName = _spriteRenderer.sortingLayerName;
        _outlineRenderer.sortingOrder = _spriteRenderer.sortingOrder + 1;
        _outlineRenderer.enabled = false;  // 기본적으로 비활성화
    }

    /// <summary>
    /// 윤곽선 표시/숨김을 설정합니다.
    /// </summary>
    public void SetOutlineVisible(bool visible)
    {
        _showOutline = visible;
        if (_outlineRenderer != null)
        {
            _outlineRenderer.enabled = visible;
        }
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
        if (_spriteRenderer != null)
        {
            _spriteRenderer.color = _isOccupied ? _occupiedColor : _normalColor;
        }
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
