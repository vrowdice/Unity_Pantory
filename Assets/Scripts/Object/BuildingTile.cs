using UnityEngine;

/// <summary>
/// 개별 건물 타일을 나타내는 클래스
/// </summary>
public class BuildingTile : MonoBehaviour
{
    [SerializeField] SpriteRenderer _spriteRenderer;

    [Header("Tile Colors")]
    [SerializeField] private Color _normalColor = Color.white;
    [SerializeField] private Color _occupiedColor = new Color(0.7f, 0.7f, 0.7f, 1f);

    private Vector2Int _gridPosition;
    private bool _isOccupied = false;
    private bool _viewportCulled;
    private bool _detailRenderingEnabled = true;
    private SpriteRenderer[] _spriteRenderers;
    private Collider2D[] _colliders;

    public Vector2Int GridPosition => _gridPosition;
    public bool IsOccupied => _isOccupied;

    /// <summary>
    /// 타일을 초기화합니다.
    /// </summary>
    public void Initialize(Vector2Int position)
    {
        _gridPosition = position;
        CacheRenderersAndColliders();
        UpdateVisual();
    }

    /// <summary>
    /// 줌 아웃(오버뷰) 모드일 때 false — 스프라이트·콜라이더를 모두 끕니다.
    /// </summary>
    public void SetDetailRenderingEnabled(bool enabled)
    {
        if (_detailRenderingEnabled == enabled)
            return;

        _detailRenderingEnabled = enabled;
        ApplySpriteVisibility();
        ApplyCollidersEnabled();
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
    /// 카메라 뷰포트 밖 타일은 스프라이트만 끕니다. 점유·그리드 데이터는 그대로입니다.
    /// </summary>
    public void SetViewportCulled(bool culled)
    {
        if (_viewportCulled == culled)
            return;

        _viewportCulled = culled;
        ApplySpriteVisibility();
    }

    /// <summary>
    /// 타일의 시각적 표현을 업데이트합니다.
    /// </summary>
    private void UpdateVisual()
    {
        _spriteRenderer.color = _isOccupied ? _occupiedColor : _normalColor;
        ApplySpriteVisibility();
    }

    private void CacheRenderersAndColliders()
    {
        _spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        _colliders = GetComponentsInChildren<Collider2D>(true);
    }

    private void ApplySpriteVisibility()
    {
        bool visible = _detailRenderingEnabled && !_viewportCulled;

        if (_spriteRenderers == null || _spriteRenderers.Length == 0)
        {
            if (_spriteRenderer != null)
                _spriteRenderer.enabled = visible;
            return;
        }

        for (int i = 0; i < _spriteRenderers.Length; i++)
        {
            if (_spriteRenderers[i] != null)
                _spriteRenderers[i].enabled = visible;
        }
    }

    private void ApplyCollidersEnabled()
    {
        if (_colliders == null || _colliders.Length == 0)
            return;

        for (int i = 0; i < _colliders.Length; i++)
        {
            if (_colliders[i] != null)
                _colliders[i].enabled = _detailRenderingEnabled;
        }
    }
}
