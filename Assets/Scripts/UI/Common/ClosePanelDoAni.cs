using System;
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
    private bool _hasOpenedAnchoredPos;
    private Tweener _activeTween;

    private void Awake()
    {
        _target = GetComponent<RectTransform>();
        CacheOpenedPosition(force: true);
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
    public void ClosePanel(Action onComplete = null)
    {
        AnimateTo(GetOffScreenPosition(), onComplete);
        isOpen = false;
    }

    [ContextMenu("Play Open Animation")]
    public void OpenPanel(Action onComplete = null)
    {
        EnsureOpenedPositionCached();
        AnimateTo(_openedAnchoredPos, onComplete);
        isOpen = true;
    }

    public void SnapToClosedPosition()
    {
        if (_target == null)
        {
            return;
        }

        _target.anchoredPosition = GetOffScreenPosition();
        isOpen = false;
    }

    private void AnimateTo(Vector2 destination, Action onComplete = null)
    {
        if (_target == null)
        {
            onComplete?.Invoke();
            return;
        }

        _activeTween?.Kill();
        _activeTween = _target.DOAnchorPos(destination, duration)
            .SetEase(ease)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                onComplete?.Invoke();
            });
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
        CacheOpenedPosition(force: true);
    }

    public void EnsureOpenedPositionCached()
    {
        CacheOpenedPosition(force: false);
    }

    private void CacheOpenedPosition(bool force)
    {
        if (_target == null)
        {
            return;
        }

        if (_hasOpenedAnchoredPos && !force)
        {
            return;
        }

        _openedAnchoredPos = _target.anchoredPosition;
        _hasOpenedAnchoredPos = true;
    }

    private void OnDisable()
    {
        _activeTween?.Kill();
        _activeTween = null;
    }
}