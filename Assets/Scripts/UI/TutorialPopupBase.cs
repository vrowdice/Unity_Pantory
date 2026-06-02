using DG.Tweening;
using UnityEngine;

/// <summary>
/// 튜토리얼 팝업 공통 베이스 (패널 위치 이동 + 포커스 패널/화살표 표시).
/// </summary>
public abstract class TutorialPopupBase : PopupBase
{
    [SerializeField] protected GameObject _panel;
    [SerializeField] protected GameObject _focusArrow;
    [SerializeField] protected GameObject _focusPanel;

    [SerializeField] protected float _focusPulseScale = 1.1f;
    [SerializeField] protected float _focusPulseDuration = 0.6f;

    [SerializeField] protected float _focusArrowDistance = 80f;
    [SerializeField] protected float _focusArrowMoveAmount = 10f;
    [SerializeField] protected float _focusArrowMoveDuration = 0.5f;
    [SerializeField] protected float _focusArrowRotationOffset = 0f;

    [SerializeField] protected float _panelMoveDuration = 0.35f;
    [SerializeField] protected Ease _panelMoveEase = Ease.OutCubic;
    [SerializeField] protected float _screenEdgePadding = 16f;
    [SerializeField] protected float _worldFocusDefaultScreenSize = 72f;
    [SerializeField] protected float _worldFocusDefaultWorldSize = 1.5f;

    private RectTransform _currentUiFocusTarget;
    private Transform _currentWorldFocusTarget;
    private Vector3 _focusAnchorPosition;
    private Tweener _arrowBobTween;
    private float _arrowBob01;

    protected void ApplyPanelPosition(Vector2 desiredPosition, GameObject focusTarget, bool animatePanel)
    {
        if (_panel == null)
        {
            return;
        }

        RectTransform panelRect = _panel.GetComponent<RectTransform>();
        TutorialPanelLayout.MovePanelToPosition(
            panelRect,
            desiredPosition,
            focusTarget,
            _screenEdgePadding,
            animatePanel,
            _panelMoveDuration,
            _panelMoveEase);
    }

    protected void ApplyFocusTarget(GameObject focusGameObject)
    {
        if (_focusPanel == null)
        {
            ClearFocusHighlight();
            return;
        }

        if (focusGameObject == null)
        {
            ClearFocusHighlight();
            return;
        }

        RectTransform focusTransform = _focusPanel.GetComponent<RectTransform>();
        if (focusTransform == null)
        {
            ClearFocusHighlight();
            return;
        }

        _focusPanel.SetActive(true);
        focusTransform.DOKill();
        Canvas.ForceUpdateCanvases();

        if (TutorialFocusResolver.TryGetFocusTargets(focusGameObject, out RectTransform uiTarget, out Transform worldTarget))
        {
            if (uiTarget != null)
                ApplyUiFocusTarget(focusTransform, uiTarget);
            else
                ApplyWorldFocusTarget(focusTransform, worldTarget);
        }
        else
        {
            ClearFocusHighlight();
            return;
        }

        EnsureArrowBobTween();
        UpdateFocusArrowImmediate();
    }

    private void ApplyUiFocusTarget(RectTransform focusTransform, RectTransform targetTransform)
    {
        _currentWorldFocusTarget = null;
        _currentUiFocusTarget = targetTransform;

        RectTransformUtils.SyncUiFocusToTarget(focusTransform, targetTransform);

        focusTransform.localScale = Vector3.one;
        focusTransform
            .DOScale(Vector3.one * _focusPulseScale, _focusPulseDuration)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine);

        _focusAnchorPosition = focusTransform.position;
    }

    private void ApplyWorldFocusTarget(RectTransform focusTransform, Transform worldTarget)
    {
        _currentUiFocusTarget = null;
        _currentWorldFocusTarget = worldTarget;

        focusTransform.localScale = Vector3.one;
        SyncWorldFocusVisual(focusTransform);

        focusTransform
            .DOScale(Vector3.one * _focusPulseScale, _focusPulseDuration)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine);
    }

    private void SyncWorldFocusVisual(RectTransform focusTransform)
    {
        if (_currentWorldFocusTarget == null || focusTransform == null)
            return;

        if (!TutorialFocusProjection.TryProjectToScreen(
                _currentWorldFocusTarget,
                ResolveWorldCamera(),
                focusTransform,
                _worldFocusDefaultWorldSize,
                _worldFocusDefaultScreenSize,
                out Vector3 screenCenter,
                out Vector2 screenSize))
        {
            return;
        }

        TutorialFocusProjection.ApplyScreenRectToFocusPanel(focusTransform, screenCenter, screenSize);
        _focusAnchorPosition = screenCenter;
    }

    private static Camera ResolveWorldCamera()
    {
        RunnerBase runner = GameManager.Instance?.CurrentRunner;
        if (runner != null && runner.MainCamera != null)
            return runner.MainCamera;

        return Camera.main;
    }

    private void LateUpdate()
    {
        if (_currentWorldFocusTarget != null)
        {
            RectTransform focusTransform = _focusPanel != null
                ? _focusPanel.GetComponent<RectTransform>()
                : null;
            SyncWorldFocusVisual(focusTransform);
        }
        else if (_currentUiFocusTarget == null)
        {
            return;
        }
        else
        {
            RectTransform focusTransform = _focusPanel != null
                ? _focusPanel.GetComponent<RectTransform>()
                : null;
            if (focusTransform != null)
            {
                RectTransformUtils.SyncUiFocusToTarget(focusTransform, _currentUiFocusTarget);
                _focusAnchorPosition = focusTransform.position;
            }
            else
            {
                _focusAnchorPosition = _currentUiFocusTarget.position;
            }
        }

        UpdateFocusArrowImmediate();
    }

    private void EnsureArrowBobTween()
    {
        KillArrowBobTween();

        _arrowBob01 = 0f;
        _arrowBobTween = DOTween
            .To(() => _arrowBob01, v => _arrowBob01 = v, 1f, Mathf.Max(0.01f, _focusArrowMoveDuration))
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
    }

    private void KillArrowBobTween()
    {
        if (_arrowBobTween != null && _arrowBobTween.IsActive())
        {
            _arrowBobTween.Kill();
        }
        _arrowBobTween = null;
        _arrowBob01 = 0f;
    }

    private void UpdateFocusArrowImmediate()
    {
        if (_focusArrow == null || _panel == null)
            return;

        if (_currentUiFocusTarget == null && _currentWorldFocusTarget == null)
            return;

        RectTransform panelRect = _panel.GetComponent<RectTransform>();
        RectTransform arrowRect = _focusArrow.GetComponent<RectTransform>();
        if (panelRect == null || arrowRect == null)
            return;

        _focusArrow.SetActive(true);

        Vector3 panelWorld = panelRect.position;
        Vector3 targetWorld = _focusAnchorPosition;
        Vector3 dir = (targetWorld - panelWorld);
        if (dir.sqrMagnitude < 0.001f)
        {
            dir = Vector3.up;
        }
        dir.Normalize();

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f + _focusArrowRotationOffset;
        arrowRect.rotation = Quaternion.Euler(0f, 0f, angle);

        Vector3 basePos = targetWorld - dir * _focusArrowDistance;
        float bob = (_focusArrowMoveAmount > 0f) ? (_arrowBob01 * _focusArrowMoveAmount) : 0f;
        Vector3 arrowPos = ClampArrowPositionToScreen(arrowRect, basePos + dir * bob);
        arrowRect.position = arrowPos;
    }

    private Vector3 ClampArrowPositionToScreen(RectTransform arrowRect, Vector3 desiredPosition)
    {
        float width = arrowRect.rect.width * Mathf.Abs(arrowRect.lossyScale.x);
        float height = arrowRect.rect.height * Mathf.Abs(arrowRect.lossyScale.y);
        Vector2 pivot = arrowRect.pivot;

        float left = desiredPosition.x - width * pivot.x;
        float right = desiredPosition.x + width * (1f - pivot.x);
        float bottom = desiredPosition.y - height * pivot.y;
        float top = desiredPosition.y + height * (1f - pivot.y);

        float pad = _screenEdgePadding;
        float dx = 0f;
        float dy = 0f;

        if (left < pad)
            dx = pad - left;
        else if (right > Screen.width - pad)
            dx = (Screen.width - pad) - right;

        if (bottom < pad)
            dy = pad - bottom;
        else if (top > Screen.height - pad)
            dy = (Screen.height - pad) - top;

        desiredPosition.x += dx;
        desiredPosition.y += dy;
        return desiredPosition;
    }

    protected void ClearFocusHighlight()
    {
        _currentUiFocusTarget = null;
        _currentWorldFocusTarget = null;

        if (_focusPanel != null)
        {
            _focusPanel.transform.DOKill();
            _focusPanel.SetActive(false);
        }

        HideFocusArrow();
    }

    protected void HideFocusArrow()
    {
        if (_focusArrow == null)
        {
            KillArrowBobTween();
            return;
        }

        _focusArrow.GetComponent<RectTransform>()?.DOKill();
        _focusArrow.SetActive(false);
        KillArrowBobTween();
    }

    protected void KillCommonTweens()
    {
        if (_focusPanel != null)
        {
            _focusPanel.transform.DOKill();
        }

        if (_panel != null)
        {
            _panel.GetComponent<RectTransform>()?.DOKill();
        }

        ClearFocusHighlight();
    }
}