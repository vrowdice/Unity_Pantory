using UnityEngine;

/// <summary>
/// Safe Area inset을 한쪽 가장자리에만 적용합니다.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class SafeAreaEdgeInset : MonoBehaviour
{
    public enum Edge
    {
        Top,
        Bottom,
        Left,
        Right
    }

    [SerializeField] private Edge _edge = Edge.Bottom;
    [SerializeField] private float _extraPadding;

    private RectTransform _rectTransform;
    private Vector2 _baseAnchoredPosition;
    private bool _hasBasePosition;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        CacheBasePosition();
        ApplyInset();
    }

    private void OnEnable()
    {
        ApplyInset();
    }

    private void Update()
    {
        ApplyInset();
    }

    public void Configure(Edge edge, float extraPadding = 0f)
    {
        _edge = edge;
        _extraPadding = extraPadding;
        CacheBasePosition();
        ApplyInset();
    }

    private void CacheBasePosition()
    {
        if (_rectTransform == null)
            _rectTransform = GetComponent<RectTransform>();

        _baseAnchoredPosition = _rectTransform.anchoredPosition;
        _hasBasePosition = true;
    }

    private void ApplyInset()
    {
        if (!_hasBasePosition || _rectTransform == null)
            return;

        Rect safeArea = Screen.safeArea;
        float inset = _extraPadding;

        switch (_edge)
        {
            case Edge.Bottom:
                inset += safeArea.y;
                _rectTransform.anchoredPosition = _baseAnchoredPosition + new Vector2(0f, inset);
                break;
            case Edge.Top:
                inset += Screen.height - safeArea.yMax;
                _rectTransform.anchoredPosition = _baseAnchoredPosition + new Vector2(0f, -inset);
                break;
            case Edge.Left:
                inset += safeArea.x;
                _rectTransform.anchoredPosition = _baseAnchoredPosition + new Vector2(inset, 0f);
                break;
            case Edge.Right:
                inset += Screen.width - safeArea.xMax;
                _rectTransform.anchoredPosition = _baseAnchoredPosition + new Vector2(-inset, 0f);
                break;
        }
    }
}
