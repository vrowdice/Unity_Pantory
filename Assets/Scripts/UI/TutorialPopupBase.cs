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

    // Arrow: distance from focus target towards panel.
    [SerializeField] protected float _focusArrowDistance = 80f;
    [SerializeField] protected float _focusArrowMoveAmount = 10f;
    [SerializeField] protected float _focusArrowMoveDuration = 0.5f;
    [SerializeField] protected float _focusArrowRotationOffset = 0f;

    [SerializeField] protected float _panelMoveDuration = 0.35f;
    [SerializeField] protected Ease _panelMoveEase = Ease.OutCubic;
    [SerializeField] protected float _screenEdgePadding = 16f;

    private RectTransform _currentFocusTarget;
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
            _currentFocusTarget = null;
            HideFocusArrow();
            return;
        }

        if (focusGameObject != null)
        {
            _focusPanel.SetActive(true);

            RectTransform focusTransform = _focusPanel.GetComponent<RectTransform>();
            RectTransform targetTransform = focusGameObject.GetComponent<RectTransform>();

            if (focusTransform == null || targetTransform == null)
            {
                _currentFocusTarget = null;
                HideFocusArrow();
                return;
            }

            focusTransform.position = targetTransform.position;
            Vector3 baseScale = targetTransform.localScale;
            focusTransform.localScale = baseScale;
            RectTransformUtils.SyncSizeToTarget(focusTransform, targetTransform);
            focusTransform.DOKill();

            focusTransform
                .DOScale(baseScale * _focusPulseScale, _focusPulseDuration)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine);

            _currentFocusTarget = targetTransform;
            EnsureArrowBobTween();
            UpdateFocusArrowImmediate();
        }
        else
        {
            _currentFocusTarget = null;
            _focusPanel.transform.DOKill();
            _focusPanel.SetActive(false);
            HideFocusArrow();
        }
    }

    private void LateUpdate()
    {
        if (_currentFocusTarget == null)
        {
            return;
        }

        // Keep arrow attached while panel is tweening.
        UpdateFocusArrowImmediate();
    }

    private void EnsureArrowBobTween()
    {
        if (_arrowBobTween != null && _arrowBobTween.IsActive())
        {
            return;
        }

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
        if (_focusArrow == null || _currentFocusTarget == null || _panel == null)
        {
            return;
        }

        RectTransform panelRect = _panel.GetComponent<RectTransform>();
        RectTransform arrowRect = _focusArrow.GetComponent<RectTransform>();
        if (panelRect == null || arrowRect == null)
        {
            return;
        }

        _focusArrow.SetActive(true);

        Vector3 panelWorld = panelRect.position;
        Vector3 targetWorld = _currentFocusTarget.position;
        Vector3 dir = (targetWorld - panelWorld);
        if (dir.sqrMagnitude < 0.001f)
        {
            dir = Vector3.up;
        }
        dir.Normalize();

        // Arrow asset is assumed to point up at 0 degrees.
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f + _focusArrowRotationOffset;
        arrowRect.rotation = Quaternion.Euler(0f, 0f, angle);

        // Near target: offset towards panel.
        Vector3 basePos = targetWorld - dir * _focusArrowDistance;
        float bob = (_focusArrowMoveAmount > 0f) ? (_arrowBob01 * _focusArrowMoveAmount) : 0f;
        arrowRect.position = basePos + dir * bob;
    }

    protected void HideFocusArrow()
    {
        if (_focusArrow == null)
        {
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

        _currentFocusTarget = null;
        HideFocusArrow();
    }
}

