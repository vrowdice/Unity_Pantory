using System;
using DG.Tweening;
using UnityEngine;

public class PanelDoAni : MonoBehaviour
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

    /// <summary>
    /// 현재 패널이 열려있는지 반환합니다 (목표 상태 기준).
    /// </summary>
    public bool IsOpen => _targetIsOpen;

    private RectTransform _target;
    private Vector2 _openedAnchoredPos;
    private bool _hasOpenedAnchoredPos;
    private Tweener _activeTween;
    private bool _targetIsOpen; // 목표 상태 (애니메이션 완료 후 상태)

    private void Awake()
    {
        _target = GetComponent<RectTransform>();
        CacheOpenedPosition(force: true);
        _targetIsOpen = isOpen;
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
        // 목표 상태를 반전시켜서 애니메이션 시작
        _targetIsOpen = !_targetIsOpen;
        
        // 진행 중인 애니메이션 즉시 취소
        if (_activeTween != null && _activeTween.IsActive())
        {
            _activeTween.Kill();
        }
        _activeTween = null;
        
        if (_targetIsOpen)
        {
            OpenPanelInternal();
        }
        else
        {
            ClosePanelInternal();
        }
    }

    [ContextMenu("Play Close Animation")]
    public void ClosePanel(Action onComplete = null)
    {
        if (_target == null)
        {
            onComplete?.Invoke();
            return;
        }

        // 목표 상태 설정
        _targetIsOpen = false;
        
        // 진행 중인 애니메이션 즉시 취소
        if (_activeTween != null && _activeTween.IsActive())
        {
            _activeTween.Kill();
        }
        _activeTween = null;
        
        ClosePanelInternal(onComplete);
    }

    [ContextMenu("Play Open Animation")]
    public void OpenPanel(Action onComplete = null)
    {
        if (_target == null)
        {
            onComplete?.Invoke();
            return;
        }

        EnsureOpenedPositionCached();
        
        // 목표 상태 설정
        _targetIsOpen = true;
        
        // 진행 중인 애니메이션 즉시 취소
        if (_activeTween != null && _activeTween.IsActive())
        {
            _activeTween.Kill();
        }
        _activeTween = null;
        
        OpenPanelInternal(onComplete);
    }

    private void ClosePanelInternal(Action onComplete = null)
    {
        AnimateTo(GetOffScreenPosition(), () =>
        {
            isOpen = false;
            onComplete?.Invoke();
        });
    }

    private void OpenPanelInternal(Action onComplete = null)
    {
        EnsureOpenedPositionCached();
        AnimateTo(_openedAnchoredPos, () =>
        {
            isOpen = true;
            onComplete?.Invoke();
        });
    }

    public void SnapToClosedPosition()
    {
        if (_target == null)
        {
            return;
        }

        if (_activeTween != null && _activeTween.IsActive())
        {
            _activeTween.Kill();
        }
        _activeTween = null;
        
        _target.anchoredPosition = GetOffScreenPosition();
        isOpen = false;
        _targetIsOpen = false;
    }

    private void AnimateTo(Vector2 destination, Action onComplete = null)
    {
        if (_target == null)
        {
            onComplete?.Invoke();
            return;
        }

        // 현재 위치에서 목표 위치로 애니메이션 시작 (호출부에서 이미 Kill 처리됨)
        _activeTween = _target.DOAnchorPos(destination, duration)
            .SetEase(ease)
            .SetUpdate(UpdateType.Normal) // DOTween 설정과 일치
            .OnComplete(() =>
            {
                _activeTween = null;
                onComplete?.Invoke();
            })
            .OnKill(() =>
            {
                _activeTween = null;
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
        if (_activeTween != null && _activeTween.IsActive())
        {
            _activeTween.Kill();
        }
        _activeTween = null;
    }
}