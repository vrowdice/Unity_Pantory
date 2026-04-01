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

    public Vector2Int GridPosition => _gridPosition;
    public bool IsOccupied => _isOccupied;

    /// <summary>
    /// 타일을 초기화합니다.
    /// </summary>
    public void Initialize(Vector2Int position)
    {
        _gridPosition = position;
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
}
