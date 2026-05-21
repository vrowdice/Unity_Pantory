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
    [SerializeField] private bool _onlyOnMobilePlatform;

    private RectTransform _rectTransform;
    private Vector2 _baseAnchoredPosition;
    private bool _hasBasePosition;
    private Rect _lastSafeArea;
    private Vector2Int _lastScreenSize;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        CacheBasePosition();
        ApplyInsetIfNeeded(force: true);
    }

    private void OnEnable()
    {
        ApplyInsetIfNeeded(force: true);
    }

    private void Update()
    {
        ApplyInsetIfNeeded(force: false);
    }

    public void Configure(Edge edge, float extraPadding = 0f)
    {
        _edge = edge;
        _extraPadding = extraPadding;
        CacheBasePosition();
        ApplyInsetIfNeeded(force: true);
    }

    private void CacheBasePosition()
    {
        if (_rectTransform == null)
            _rectTransform = GetComponent<RectTransform>();

        _baseAnchoredPosition = _rectTransform.anchoredPosition;
        _hasBasePosition = true;
    }

    private void ApplyInsetIfNeeded(bool force)
    {
        if (!_hasBasePosition || _rectTransform == null)
            return;

        if (_onlyOnMobilePlatform && !Application.isMobilePlatform)
        {
            _rectTransform.anchoredPosition = _baseAnchoredPosition;
            return;
        }

        Vector2Int screenSize = new Vector2Int(Screen.width, Screen.height);
        if (!force && _lastSafeArea == Screen.safeArea && _lastScreenSize == screenSize)
            return;

        _lastSafeArea = Screen.safeArea;
        _lastScreenSize = screenSize;

        float safeInset = GetSafeAreaInset();
        if (safeInset <= 0f && _extraPadding <= 0f)
        {
            _rectTransform.anchoredPosition = _baseAnchoredPosition;
            return;
        }

        float inset = safeInset + _extraPadding;
        switch (_edge)
        {
            case Edge.Bottom:
                _rectTransform.anchoredPosition = _baseAnchoredPosition + new Vector2(0f, inset);
                break;
            case Edge.Top:
                _rectTransform.anchoredPosition = _baseAnchoredPosition + new Vector2(0f, -inset);
                break;
            case Edge.Left:
                _rectTransform.anchoredPosition = _baseAnchoredPosition + new Vector2(inset, 0f);
                break;
            case Edge.Right:
                _rectTransform.anchoredPosition = _baseAnchoredPosition + new Vector2(-inset, 0f);
                break;
        }
    }

    private float GetSafeAreaInset()
    {
        Rect safeArea = Screen.safeArea;
        switch (_edge)
        {
            case Edge.Bottom:
                return safeArea.y;
            case Edge.Top:
                return Screen.height - safeArea.yMax;
            case Edge.Left:
                return safeArea.x;
            case Edge.Right:
                return Screen.width - safeArea.xMax;
            default:
                return 0f;
        }
    }
}
