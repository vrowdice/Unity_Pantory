using DG.Tweening;
using System;
using System.Collections;
using UnityEngine;

public class PopupBase : TutorialBase
{
    [SerializeField] private RectTransform _animationTarget;
    [SerializeField] private BasePanelEffectType.TYPE _effectType = BasePanelEffectType.TYPE.FadeInFadeOut;

    public const float DISPLAY_TIME = 0.6f;
    public const float MOVE_DURATION = 0.5f;
    public const float PUNCH_SCALE_DURATION = 1f;
    public const float INOUT_DURATION = 0.1f;

    private CanvasGroup _canvasGroup = null;
    protected Coroutine _showCoroutine = null;
    protected Coroutine _closeCoroutine = null;

    private Vector3? _originalScale = null;

    private Action _onShowCompleteCallback = null;
    private Action _cachedClose;

    private void OnDisable()
    {
        if (_cachedClose != null && UIManager.Instance != null)
        {
            UIManager.Instance.RemoveCloseable(_cachedClose);
        }
    }

    private void OnDestroy()
    {
        if (_canvasGroup != null)
        {
            _canvasGroup.DOKill();
        }
    }

    private Transform GetAnimationTarget()
    {
        return _animationTarget != null ? _animationTarget : transform;
    }

    public virtual void Init()
    {
        Transform target = GetAnimationTarget();
        _originalScale ??= target.localScale;
        _canvasGroup = target.GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
        {
            _canvasGroup = target.gameObject.AddComponent<CanvasGroup>();
        }

        gameObject.SetActive(true);
    }

    public virtual void Show()
    {
        if (_cachedClose == null) _cachedClose = Close;
        if (UIManager.Instance != null)
        {
            UIManager.Instance.RemoveCloseable(_cachedClose);
            UIManager.Instance.PushCloseable(_cachedClose);
        }
        gameObject.SetActive(true);

        if (_showCoroutine != null)
        {
            StopCoroutine(_showCoroutine);
        }
        _showCoroutine = StartCoroutine(ShowEffectCoroutine());
    }
    public virtual Coroutine ShowCoroutine()
    {
        gameObject.SetActive(true);
        if (_showCoroutine != null)
        {
            StopCoroutine(_showCoroutine);
        }
        _showCoroutine = StartCoroutine(ShowEffectCoroutine());
        return _showCoroutine;
    }

    public virtual IEnumerator ShowEffectCoroutine()
    {
        switch (_effectType)
        {
            case BasePanelEffectType.TYPE.FadeInFadeOut:
                if (_canvasGroup != null)
                {
                    _canvasGroup.alpha = 0f;
                    Tween fadeTween = _canvasGroup.DOFade(1f, INOUT_DURATION).SetUpdate(true).SetLink(GetAnimationTarget().gameObject);
                    yield return fadeTween.WaitForCompletion();
                }
                break;
            default:
                break;
        }
        _onShowCompleteCallback?.Invoke();
    }

    public virtual void Close()
    {
        if (this == null) return;

        if (_cachedClose != null && UIManager.Instance != null)
            UIManager.Instance.RemoveCloseable(_cachedClose);

        if (_showCoroutine != null)
        {
            if (gameObject != null && gameObject.activeInHierarchy)
            {
                StopCoroutine(_showCoroutine);
            }
            _showCoroutine = null;
        }

        if (_effectType == BasePanelEffectType.TYPE.FadeInFadeOut && gameObject.activeInHierarchy)
        {
            if (_closeCoroutine != null)
                StopCoroutine(_closeCoroutine);
            _closeCoroutine = StartCoroutine(CloseEffectCoroutine());
            return;
        }

        gameObject.SetActive(false);
    }

    protected void CloseAndDestroy()
    {
        Close();
        Destroy(gameObject);
    }

    protected virtual IEnumerator CloseEffectCoroutine()
    {
        if (_canvasGroup != null)
        {
            Tween fadeTween = _canvasGroup.DOFade(0f, INOUT_DURATION).SetUpdate(true).SetLink(GetAnimationTarget().gameObject);
            yield return fadeTween.WaitForCompletion();
        }

        _closeCoroutine = null;
        gameObject.SetActive(false);
    }
}
