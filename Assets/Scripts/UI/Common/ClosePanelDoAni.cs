using DG.Tweening;
using UnityEngine;

public class ClosePanelDoAni : MonoBehaviour
{
    public enum SlideDirection
    {
        Left,
        Right,
        Up,
        Down
    }

    [Header("Animation")]
    [SerializeField] private SlideDirection direction = SlideDirection.Down;
    [SerializeField, Min(0f)] private float duration = 0.5f;
    [SerializeField] private Ease ease = Ease.OutCubic;
    [SerializeField, Min(0f)] private float extraPadding = 0f;

    [Header("Options")]
    [SerializeField] private bool playOnEnable;
    [SerializeField] private bool isOpen = true;

    private RectTransform _target;
    private Vector2 _openedAnchoredPos;
    private Tweener _activeTween;

    private void Awake()
    {
        _target = GetComponent<RectTransform>();
        CacheOpenedPosition();
    }

    private void OnEnable()
    {
        if (playOnEnable)
        {
            ClosePanel();
        }
    }

    [ContextMenu("Toggle Panel")]
    public void TogglePanel()
    {
        if (isOpen)
        {
            ClosePanel();
        }
        else
        {
            OpenPanel();
        }
    }

    [ContextMenu("Play Close Animation")]
    public void ClosePanel()
    {
        AnimateTo(GetOffScreenPosition());
        isOpen = false;
    }

    [ContextMenu("Play Open Animation")]
    public void OpenPanel()
    {
        AnimateTo(_openedAnchoredPos);
        isOpen = true;
    }

    private void AnimateTo(Vector2 destination)
    {
        if (_target == null) return;

        _activeTween?.Kill();
        _activeTween = _target.DOAnchorPos(destination, duration)
            .SetEase(ease)
            .SetUpdate(true);
    }

    private Vector2 GetOffScreenPosition()
    {
        if (_target == null) return Vector2.zero;

        RectTransform canvasRect = _target.GetComponentInParent<Canvas>()?.GetComponent<RectTransform>();
        if (canvasRect == null)
        {
            return _openedAnchoredPos;
        }

        float canvasWidth = canvasRect.rect.width;
        float canvasHeight = canvasRect.rect.height;
        float panelWidth = _target.rect.width;
        float panelHeight = _target.rect.height;

        Vector2 offscreenPos = _openedAnchoredPos;
        float horizontalDistance = panelWidth + extraPadding;
        float verticalDistance = panelHeight + extraPadding;

        switch (direction)
        {
            case SlideDirection.Left:
                offscreenPos.x = _openedAnchoredPos.x - horizontalDistance;
                break;
            case SlideDirection.Right:
                offscreenPos.x = _openedAnchoredPos.x + horizontalDistance;
                break;
            case SlideDirection.Up:
                offscreenPos.y = _openedAnchoredPos.y + verticalDistance;
                break;
            case SlideDirection.Down:
                offscreenPos.y = _openedAnchoredPos.y - verticalDistance;
                break;
        }

        return offscreenPos;
    }

    public void RefreshOpenedPosition()
    {
        CacheOpenedPosition();
    }

    private void CacheOpenedPosition()
    {
        if (_target == null) return;
        _openedAnchoredPos = _target.anchoredPosition;
    }

    private void OnDisable()
    {
        _activeTween?.Kill();
        _activeTween = null;
    }
}