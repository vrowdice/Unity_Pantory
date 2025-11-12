using UnityEngine;

/// <summary>
/// 스레드 배치용 그리드 타일
/// </summary>
public class ThreadTile : MonoBehaviour
{
    [Header("Tile Colors")]
    [SerializeField] private Color _normalColor = Color.white;
    [SerializeField] private Color _occupiedColor = new Color(0.75f, 0.75f, 0.75f, 1f);

    [Header("Outline Settings")]
    [SerializeField] private Color _outlineColor = new Color(0.2f, 0.8f, 1f, 0.8f);
    [SerializeField] private float _outlineThickness = 0.05f;

    private SpriteRenderer _spriteRenderer;
    private SpriteRenderer _outlineRenderer;
    private bool _isOccupied;
    private bool _showOutline;
    private Vector2Int _gridPosition;

    public Vector2Int GridPosition => _gridPosition;
    public bool IsOccupied => _isOccupied;

    void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        CreateOutline();
    }

    public void Initialize(Vector2Int gridPosition)
    {
        _gridPosition = gridPosition;
        UpdateVisual();
    }

    public void SetOccupied(bool occupied)
    {
        _isOccupied = occupied;
        UpdateVisual();
    }

    public void SetOutlineVisible(bool visible, Color color = default)
    {
        _showOutline = visible;

        if (_outlineRenderer == null)
            return;

        if (color != default(Color))
        {
            _outlineColor = color;
            _outlineRenderer.color = _outlineColor;
        }

        _outlineRenderer.enabled = _showOutline;
    }

    private void UpdateVisual()
    {
        if (_spriteRenderer == null)
            return;

        _spriteRenderer.color = _isOccupied ? _occupiedColor : _normalColor;
    }

    private void CreateOutline()
    {
        if (_spriteRenderer == null || _outlineRenderer != null)
            return;

        GameObject outlineObj = new GameObject("Outline");
        outlineObj.transform.SetParent(transform);
        outlineObj.transform.localPosition = Vector3.zero;
        outlineObj.transform.localRotation = Quaternion.identity;
        outlineObj.transform.localScale = Vector3.one * (1f + _outlineThickness);

        _outlineRenderer = outlineObj.AddComponent<SpriteRenderer>();
        _outlineRenderer.sprite = _spriteRenderer.sprite;
        _outlineRenderer.color = _outlineColor;
        _outlineRenderer.sortingLayerID = _spriteRenderer.sortingLayerID;
        _outlineRenderer.sortingOrder = _spriteRenderer.sortingOrder;
        _outlineRenderer.enabled = false;
    }
}